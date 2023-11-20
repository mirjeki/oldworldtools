using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Species")]
    public class SpeciesCollection
    {
        [XmlElement("SpeciesType")]
        public SpeciesType[] SpeciesTypes { get; set; }
    }


    [XmlRoot("SpeciesType")]
    public class SpeciesType
    {
        [XmlAttribute]
        public string Value { get; set; }
        [XmlElement("Region")]
        public string[] Regions { get; set; }
    }
}
