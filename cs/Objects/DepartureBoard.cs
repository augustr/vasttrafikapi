using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VasttrafikSharp.Objects
{
    [XmlRoot("DepartureBoard")]
    public class DepartureBoard
    {
        [XmlElement("Departure")]
        public List<Departure> Departures { get; set; }
    }
}
