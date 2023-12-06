using LiteDB;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using OldWorldTools.Models.WFRPCharacter.DTOs;
using OldWorldTools.Models.WFRPNames;

namespace OldWorldTools.Models.WFRPCharacter
{
    public class CharacterSheet
    {
        public string Name { get; set; }
        public GenderEnum Gender { get; set; }
        public SpeciesEnum Species { get; set; }
        public RegionEnum Region { get; set; }
        public Dictionary<RegionEnum, string> RegionsAvailable { get; set; }
        public List<Characteristic> Characteristics { get; set; }
        public string Class { get; set; }
        public string Career { get; set; }
        public TierEnum Tier { get; set; }
        public string CareerPath { get; set; }
        public string Status { get; set; }
        public int Movement { get; set; }
        public int Walk { get; set; }
        public int Run { get; set; }
        public int Fate { get; set; }
        public int Fortune { get; set; }
        public int Resilience { get; set; }
        public int Resolve { get; set; }
        public List<CharacterSkill> SpeciesSkills { get; set; }
        public List<CharacterSkill> CareerSkills { get; set; }
        public List<string> Talents { get; set; }
        public List<string> Trappings { get; set; }
        //public List<Trapping> Trappings { get; set; }
    }
}
