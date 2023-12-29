using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames.DTOs;
using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Trappings")]
    public class TrappingsCollection
    {
        [XmlElement("Trapping")]
        public Trapping[] Trappings { get; set; }
    }

    [XmlRoot("Trapping")]
    public class Trapping
    {
        public string Name { get; set; }
        public string Enc { get; set; }
        public string Properties { get; set; }
    }
}
