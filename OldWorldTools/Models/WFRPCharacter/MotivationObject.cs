using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Motivations")]
    public class MotivationCollection
    {
        [XmlElement("Motivation")]
        public string[] Motivations { get; set; }
    }
}
