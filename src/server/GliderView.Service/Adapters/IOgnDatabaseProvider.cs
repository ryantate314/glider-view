using GliderView.Service.Models.OgnDatabaseProvider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Adapters
{
    /// <summary>
    /// A client to the Open Glider Network device database. It provides that ability to
    /// look up aircraft information based on tracker ID.
    /// </summary>
    public interface IOgnDeviceDatabaseProvider
    {
        Task<AircraftInfo?> GetAircraftInfo(string trackerId);
    }
}
