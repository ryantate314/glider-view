using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class IgcFileRepository
    {
        private readonly string _directory;
        private readonly ILogger<IgcFileRepository> _logger;

        public IgcFileRepository(string directory, ILogger<IgcFileRepository> logger)
        {
            _directory = directory;
            _logger = logger;
        }

        public FileStream GetFile(string filename)
        {
            string fullPath = Path.Combine(_directory, filename);

            _logger.LogDebug("Attemping to read IGC file {0}", fullPath);

            return File.OpenRead(fullPath);
        }

        public IEnumerable<string> GetFiles(string airfield, string trackerId, DateTime date)
        {
            string path = Path.Combine(
                _directory,
                GeneratePath(airfield, date)
            );
            return Directory.EnumerateFiles(path, $"*.{trackerId}*.igc")
                .Select(path => Path.GetRelativePath(_directory, path));
        }

        public async Task<string> SaveFile(Stream igcFile, string airfield, string registration, string trackerId, DateTime eventDate)
        {
            string fileName = GenerateFileName(airfield, registration, trackerId, eventDate);
            
            _logger.LogDebug("Saving IGC file to {0}", fileName);

            using (var fileStream = File.OpenWrite(fileName))
            {
                igcFile.Seek(0, SeekOrigin.Begin);
                await igcFile.CopyToAsync(fileStream);
            }

            return fileName;
        }

        private string GeneratePath(string airfield, DateTime eventDate)
        {
            return Path.Combine(
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

            return Path.Combine(GeneratePath(airfield, eventDate), fileName);
        }
    }
}
