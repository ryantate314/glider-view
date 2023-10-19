using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Utilities
{
    internal static class UnitUtils
    {
        public static double MetersToFeet(double meters)
        {
            return meters * 3.281;
        }

        public static double FeetToMeters(double feet)
        {
            return feet / 3.281;
        }
    }
}
