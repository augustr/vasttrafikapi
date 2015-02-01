using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VasttrafikSharp.Objects
{
    public class Departure
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("stopid")]
        public string StopId { get; set; }

        [XmlAttribute("stop")]
        public string Stop { get; set; }

        [XmlAttribute("time")]
        public string Time { get; set; }

        [XmlAttribute("date")]
        public string Date { get; set; }

        [XmlAttribute("journeyid")]
        public string JourneyId { get; set; }

        [XmlAttribute("direction")]
        public string Direction { get; set; }

        [XmlAttribute("track")]
        public string Track { get; set; }

        [XmlAttribute("rtTime")]
        public string RealtimeTime { get; set; }

        [XmlAttribute("rtDate")]
        public string RealtimeDate { get; set; }

        [XmlAttribute("fgColor")]
        public string ForegroundColor { get; set; }

        [XmlAttribute("bgColor")]
        public string BackgroundColor { get; set; }

        [XmlAttribute("stroke")]
        public string Stroke { get; set; }

        [XmlElement("ref")]
        public JourneyDetailRef JourneyDetailReference { get; set; }
    }
}
