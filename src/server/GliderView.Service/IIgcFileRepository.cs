namespace GliderView.Service
{
    public interface IIgcFileRepository
    {
        FileStream GetFile(string filename);
        IEnumerable<string> GetFiles(string airfield, string trackerId, DateTime date);
        Task<string> SaveFile(Stream igcFile, string airfield, string registration, string trackerId, DateTime eventDate);
    }
}