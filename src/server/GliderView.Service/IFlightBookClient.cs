namespace GliderView.Service
{
    public interface IFlightBookClient
    {
        int ReceiveBufferSize { get; set; }

        Task Connect();
        Task<Stream> DownloadIgcFile(string trackerId, DateTime? start = null, DateTime? end = null);
    }
}