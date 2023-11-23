using System.Xml.Serialization;

namespace OldWorldTools.Models.WFRPCharacter
{
    [XmlRoot("Careers")]
    public class CareerCollection
    {
        [XmlElement("CareerSpecies")]
        public CareerSpecies[] CareerSpecies { get; set; }
    }

    [XmlRoot("CareerSpecies")]
    public class CareerSpecies
    {
        [XmlAttribute]
        public string Value { get; set; }
        [XmlElement("Odds")]
        public Odds[] Odds { get; set; }
    }

    [XmlRoot("Odds")]
    public class Odds
    {
        [XmlAttribute]
        public int Lower { get; set; }
        [XmlAttribute]
        public int Upper { get; set; }
        [XmlElement("Career")]
        public Career Career { get; set; }
    }

    [XmlRoot("Career")]
    public class Career
    {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("Class")]
        public string Class { get; set; }
        [XmlElement("Path1")]
        public Path Path1 { get; set; }
        [XmlElement("Path2")]
        public Path Path2 { get; set; }
        [XmlElement("Path3")]
        public Path Path3 { get; set; }
        [XmlElement("Path4")]
        public Path Path4 { get; set; }
    }

    [XmlRoot("Path")]
    public class Path
    {
        [XmlElement("Title")]
        public string Title { get; set; }
        [XmlElement("AdvanceScheme")]
        public string AdvanceScheme { get; set; }
        [XmlElement("Status")]
        public string Status { get; set; }
        [XmlElement("Skills")]
        public string Skills { get; set; }
        [XmlElement("Talents")]
        public string Talents { get; set; }
        [XmlElement("Trappings")]
        public string Trappings { get; set; }
    }
}
