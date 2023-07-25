using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    public static class Helpers
    {

        public static float RoundToDecimal(this double value, int decimalPlaces)
        {
            float multiplier = (float)Math.Pow(10, decimalPlaces);
            return (float)Math.Round(value * multiplier) / multiplier;
        }
    }
}
