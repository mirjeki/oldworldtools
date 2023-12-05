using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Skills")]
    public class SkillCollection
    {
        [XmlElement("Skill")]
        public Skill[] Skills { get; set; }
    }

    [XmlRoot("Skill")]
    public class Skill
    {
        public string Name { get; set; }
        public string Tags { get; set; }
    }
}
