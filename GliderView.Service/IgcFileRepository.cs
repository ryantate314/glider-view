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

        public IgcFileRepository(string directory)
        {
            _directory = directory;
        }

        public FileStream GetFile(string filename)
        {
            string fullPath = Path.Combine(_directory, filename);

            return File.OpenRead(fullPath);
        }
    }
}
