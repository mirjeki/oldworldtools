using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPNames
{
    [XmlRoot("Names")]
    public class NameCollection
    {
        [XmlElement("NameRegion")]
        public NameRegion[] NameRegions { get; set; }
    }


    [XmlRoot("NameRegion")]
    public class NameRegion
    {
        [XmlAttribute]
        public string Value { get; set; }
        [XmlElement("NameType")]
        public NameType[] NameTypes { get; set; }
    }

    [XmlRoot("NameType")]
    public class NameType
    {
        [XmlAttribute]
        public string Value { get; set; }
        [XmlElement("Name")]
        public string[] Names { get; set; }
    }
}
