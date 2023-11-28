using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Talents")]
    public class TalentCollection
    {
        [XmlElement("Talent")]
        public Talent[] Talents { get; set; }
    }

    [XmlRoot("Talent")]
    public class Talent
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
