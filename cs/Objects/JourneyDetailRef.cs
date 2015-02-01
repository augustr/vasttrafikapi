using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VasttrafikSharp.Objects
{
    public class JourneyDetailRef
    {
        [XmlAttribute("ref")]
        public string ReferenceUrl { get; set; }
    }
}