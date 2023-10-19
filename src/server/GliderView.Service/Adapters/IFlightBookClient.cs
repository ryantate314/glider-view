using GliderView.Service.Models.FlightBook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GliderView.Service.Adapters
{
    public interface IFlightBookClient
    {
        int ReceiveBufferSize { get; set; }

        Task Connect();
        Task<Stream> DownloadIgcFile(string trackerId, DateTime? start = null, DateTime? end = null);
        Task<IEnumerable<AircraftLocationUpdate>> GetFleet(string faaId);
    }
}