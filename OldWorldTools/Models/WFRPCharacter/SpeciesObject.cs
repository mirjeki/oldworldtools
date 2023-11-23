using OldWorldTools.Models.WFRPCharacter.DTOs;
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
        [XmlElement("AttributeModifiers")]
        public SpeciesCharacteristicsModifiers AttributeModifiers { get; set; }
        [XmlElement("Wounds")]
        public SpeciesWoundModifier Wounds { get; set; }
        public string Fate { get; set; }
        public string Resilience { get; set; }
        public string ExtraPoints { get; set; }
        public string Movement { get; set; }
        [XmlElement("Regions")]
        public SpeciesRegions SpeciesRegions { get; set; }
    }

    [XmlRoot("AttributeModifier")]
    public class SpeciesCharacteristicsModifiers
    {
        public string WS { get; set; }
        public string BS { get; set; }
        public string S { get; set; }
        public string T { get; set; }
        public string I { get; set; }
        public string Agi { get; set; }
        public string Dex { get; set; }
        public string Int { get; set; }
        public string WP { get; set; }
        public string Fel { get; set; }
    }

    [XmlRoot("Wounds")]
    public class SpeciesWoundModifier
    {
        public string SB { get; set; }
        public string TB { get; set; }
        public string WPB { get; set; }
    }

    [XmlRoot("Regions")]
    public class SpeciesRegions
    {
        [XmlElement("Region")]
        public Region[] Regions { get; set; }
    }

    [XmlRoot("Region")]
    public class Region
    {
        public string RegionName { get; set; }
        public string StartingSkills { get; set; }
        public string StartingTalents { get; set; }
    }
}
