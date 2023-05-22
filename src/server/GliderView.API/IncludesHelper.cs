using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GliderView.API
{
    /// <summary>
    /// Helper class which enables clients to specify a list of objects they want returned in the request in addition to the base meta-data.
    /// </summary>
    /// <typeparam name="TEntity">The base entity.</typeparam>
    public class IncludeHandler<TEntity> where TEntity : new()
    {
        private readonly ILogger _logger;

        /// <summary>
        /// A list of all handlers which have been registered. The dictionary is keyed off of the handler identifier.
        /// </summary>
        private readonly Dictionary<string, IHandler> _handlers;

        /// <summary>
        /// The maximum number of threads used to retrieve child objects.
        /// </summary>
        private const int MAX_PARALLELISM = 2;

        /// <summary>
        /// Function which retrieves and sets the value of a single property.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="entity">The parent entity which is being hydrated.</param>
        /// <returns></returns>
        public delegate Task SingleRetrievalFunction<TProperty>(TEntity entity);

        /// <summary>
        /// Method which retrieves and populates the value of a single property for multiple objects.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public delegate Task MultiplePopulationFunction<TProperty>(List<TEntity> entities);

        // Because the handlers are strongly typed, we can't included them in a polymorphic array. This interface hides the generic TProperty type.
        private interface IHandler
        {
            /// <summary>
            /// Hydrates a single entity.
            /// </summary>
            /// <param name="entityTask"></param>
            /// <returns></returns>
            Task Handle(TEntity entityTask);

            /// <summary>
            /// Hydrates a list of entities.
            /// </summary>
            /// <param name="entities"></param>
            /// <returns></returns>
            Task Handle(List<TEntity> entities);
        }

        /// <summary>
        /// Internal struct used to contain instructions to populate a single property.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        private class Handler<TProperty> : IHandler
        {
            public PropertyInfo Property { get; set; }
            public SingleRetrievalFunction<TProperty> SingleRetrievalFunction { get; set; }
            public MultiplePopulationFunction<TProperty> MultipleUpdateFunction { get; set; }

            public Task Handle(TEntity entity)
            {
                if (SingleRetrievalFunction == null)
                    throw new InvalidOperationException($"{nameof(SingleRetrievalFunction)} not provided for {Property} handler.");

                return SingleRetrievalFunction.Invoke(entity);
            }

            public Task Handle(List<TEntity> entities)
            {
                if (MultipleUpdateFunction == null)
                    throw new InvalidOperationException($"{nameof(MultipleUpdateFunction)} not provided for {Property} handler.");

                return MultipleUpdateFunction.Invoke(entities);
            }
        }

        public class HandlerConfig<TProperty>
        {
            /// <summary>
            /// Identifier used to include the <typeparamref name="TProperty"/> property in the model. By default this is a lowercase representation of the property name.
            /// </summary>
            public string Identifier { get; set; }

            /// <summary>
            /// Function which retrieves and populates the <typeparamref name="TProperty"/> property for the provided <typeparamref name="TEntity"/>.
            /// </summary>
            public SingleRetrievalFunction<TProperty> SingleUpdateFunction { get; set; }

            /// <summary>
            /// Method which retrieves and populates the <typeparamref name="TProperty"/> property for the provided list of <typeparamref name="TEntity"/> objects.
            /// </summary>
            public MultiplePopulationFunction<TProperty> MultipleUpdateFunction { get; set; }
        }

        public IncludeHandler(ILogger logger)
        {
            _logger = logger;

            _handlers = new Dictionary<string, IHandler>();
        }

        /// <summary>
        /// Registers the provided property to be optionally included in the response.
        /// </summary>
        /// <typeparam name="TProperty">The type of the child entity being added.</typeparam>
        /// <param name="property">Lambda selector for the property to be updated.</param>
        /// <param name="config"></param>
        /// <returns></returns>
        public IncludeHandler<TEntity> AddHandler<TProperty>(
            Expression<Func<TEntity, TProperty>> property,
            Action<HandlerConfig<TProperty>> config)
        {

            MemberExpression member = property.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{property}' refers to a method, not a property.");

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{property}' refers to a field, not a property.");

            // Use a config lambda to set up the handler like ASP.NET middleware
            var handlerConfig = new HandlerConfig<TProperty>()
            {
                Identifier = propInfo.Name.ToLower() //Set default identifier to be the property name
            };

            config.Invoke(handlerConfig);

            if (String.IsNullOrEmpty(handlerConfig.Identifier))
                throw new InvalidOperationException($"Invalid value for {nameof(handlerConfig.Identifier)}.");

            if (_handlers.ContainsKey(handlerConfig.Identifier))
                throw new InvalidOperationException($"Handler already exists for '{handlerConfig.Identifier}'.");

            if (handlerConfig.SingleUpdateFunction == null && handlerConfig.MultipleUpdateFunction == null)
                throw new InvalidOperationException("At least one retrieval function must be provided.");

            var handler = new Handler<TProperty>()
            {
                Property = propInfo,
                MultipleUpdateFunction = handlerConfig.MultipleUpdateFunction,
                SingleRetrievalFunction = handlerConfig.SingleUpdateFunction
            };

            _handlers.Add(handlerConfig.Identifier, handler);

            return this;
        }

        /// <summary>
        /// Populates optional child objects for the provided entity.
        /// </summary>
        /// <param name="entity">The base entity to which the specified child objects will be added.</param>
        /// <param name="includes">A comma-separated list of child items to include.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddIncludedProperties(TEntity entity, string includes, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrEmpty(includes))
                return;

            if (entity == null)
                return;

            List<string> includeList = ParseIncludeList(includes);

            // Use a cancellation source to stop executing handlers if any of them fails
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            //Execute handlers in parallel. Can't use Parallel.ForEach because the handlers are async.
            using (var threadLimiter = new SemaphoreSlim(MAX_PARALLELISM))
            {
                IEnumerable<Task> tasks = includeList.Select(async include =>
                {
                    try
                    {
                        await threadLimiter.WaitAsync();

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.LogDebug($"Task for '{include}' include was canceled");
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }

                        try
                        {
                            await _handlers[include].Handle(entity);
                        }
                        catch (Exception ex)
                        {
                            // Make sure no new tasks are started
                            cancellationTokenSource.Cancel();

                            _logger.LogError($"Error processing include for '{include}': {ex.Message}");

                            throw;
                        }
                    }
                    finally
                    {
                        threadLimiter.Release();
                    }
                });

                // Be aware WhenAll only throws the first exception which was encountered instead of an AggregateException
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Populates optional child objects for the provided entities.
        /// </summary>
        /// <param name="entities">A list of entities to which the optional child objects will be added.</param>
        /// <param name="includes">A comma-separated list of child items to include.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddIncludedProperties(List<TEntity> entities, string includes, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrEmpty(includes))
                return;

            if (entities.Count == 0)
                return;

            List<string> includeList = ParseIncludeList(includes);

            // Use a cancellation source to stop executing handlers if any of them fails
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            //Execute handlers in parallel. Can't use Parallel.ForEach because the handlers are async.
            using (var threadLimiter = new SemaphoreSlim(MAX_PARALLELISM))
            {
                IEnumerable<Task> tasks = includeList.Select(async include =>
                {
                    try
                    {
                        await threadLimiter.WaitAsync();

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.LogDebug($"Task for '{include}' include was canceled");
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }

                        try
                        {
                            await _handlers[include].Handle(entities);
                        }
                        catch (Exception ex)
                        {
                            // Make sure no new tasks are started
                            cancellationTokenSource.Cancel();

                            _logger.LogError($"Error processing include for '{include}': {ex.Message}");

                            throw;
                        }
                    }
                    finally
                    {
                        threadLimiter.Release();
                    }
                });

                // Be aware WhenAll only throws the first exception which was encountered instead of an AggregateException
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Parses and validates the comma-separated list of entities to be retrieved.
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        private List<string> ParseIncludeList(string includes)
        {
            List<string> includeList = includes.Split(",")
                .Select(x => x.ToLower())
                .Distinct()
                .ToList();

            if (includeList.Any(x => !_handlers.ContainsKey(x)))
                throw new ArgumentException($"Invalid item '{includeList.FirstOrDefault(x => !_handlers.ContainsKey(x))}' in includes list.");

            return includeList;
        }

        public bool ContainsProperty(string includes, string propertyName)
        {
            if (string.IsNullOrEmpty(includes))
                return false;

            return ParseIncludeList(includes)
                .Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }

    }
}
