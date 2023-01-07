using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Exeptions
{
    public class NotFoundException : InvalidOperationException
    {
        public NotFoundException(string message)
            : base(message)
        { }
    }

    public class NotFoundException<TEntity> : NotFoundException
    {
        public NotFoundException(Guid id)
            : base($"{typeof(TEntity).Name} was not found with ID {id}.")
        { }
    }
}
