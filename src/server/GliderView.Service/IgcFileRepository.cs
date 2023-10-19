using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class IgcFileRepository : IIgcFileRepository
    {
        private readonly string _directory;
        private readonly ILogger<IgcFileRepository> _logger;
        private readonly IFileSystem _fileSystem;

        public IgcFileRepository(string directory, ILogger<IgcFileRepository> logger, System.IO.Abstractions.IFileSystem fileSystem)
        {
            _directory = directory;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public Stream GetFile(string filename)
        {
            if (filename.StartsWith(_fileSystem.Path.DirectorySeparatorChar))
                filename = filename.Substring(1, filename.Length - 1);

            string fullPath = _fileSystem.Path.Combine(_directory, filename);

            _logger.LogDebug("Attemping to read IGC file {0}", fullPath);

            return _fileSystem.File.OpenRead(fullPath);
        }

        public IEnumerable<string> GetFiles(string airfield, string trackerId, DateTime date)
        {
            string path = _fileSystem.Path.Combine(
                _directory,
                GeneratePath(airfield, date)
            );

            if (!_fileSystem.Directory.Exists(path))
                return new string[] { };

            return _fileSystem.Directory.EnumerateFiles(path, $"*.{trackerId}*.igc")
                .Select(path => _fileSystem.Path.GetRelativePath(_directory, path));
        }

        public async Task<string> SaveFile(Stream igcFile, string airfield, string registration, string trackerId, DateTime eventDate)
        {
            string path = GeneratePath(airfield, eventDate);

            string absoluteDirectoryPath = _fileSystem.Path.Combine(
                _directory,
                path
            );

            _fileSystem.Directory.CreateDirectory(absoluteDirectoryPath);

            string relativePath = _fileSystem.Path.Combine(
               path,
               GenerateFileName(airfield, registration, trackerId, eventDate)
            );

            string fullPath = _fileSystem.Path.Combine(
                _directory,
                relativePath
            );

            _logger.LogDebug("Saving IGC file to {0}", fullPath);

            using (var fileStream = _fileSystem.File.OpenWrite(fullPath))
            {
                igcFile.Seek(0, SeekOrigin.Begin);
                await igcFile.CopyToAsync(fileStream);
            }

            return relativePath;
        }

        private string GeneratePath(string airfield, DateTime eventDate)
        {
            return _fileSystem.Path.Combine(
                eventDate.Year.ToString(),
                String.Format("{0:D2}", eventDate.Month),
                String.Format("{0:D2}", eventDate.Day),
                airfield
            );
        }

        private string GenerateFileName(string airfield, string registration, string trackerId, DateTime eventDate)
        {
            int numExistingFiles = GetFiles(airfield, trackerId, eventDate).Count();

            string fileName = $"{eventDate.ToString("yyyy-MM-dd")}_{registration}.{trackerId}";

            if (numExistingFiles > 0)
            {
                fileName = String.Format("{0}_{1:D2}", fileName, numExistingFiles);
            }

            fileName = fileName + ".igc";

            return fileName;
        }
    }
}
