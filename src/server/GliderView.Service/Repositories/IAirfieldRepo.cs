using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Repositories
{
    public interface IAirfieldRepo
    {
        Task<Airfield?> GetAirfield(string faaId);
    }
}
