using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Ambitions")]
    public class AmbitionCollection
    {
        [XmlElement("Ambition")]
        public string[] Ambitions { get; set; }
    }
}
