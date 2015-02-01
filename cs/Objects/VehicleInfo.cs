using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VasttrafikSharp.Objects
{
    public class VehicleInfo
    {
        public string Number { get; set; }
        public string Destination { get; set; }
        public string NextMin { get; set; }
        public string NextNextMin { get; set; }
        public string BackgroundColor { get; set; }
        public string ForegroundColor { get; set; }
    }
}
