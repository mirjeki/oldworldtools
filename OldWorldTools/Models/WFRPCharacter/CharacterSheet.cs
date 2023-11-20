using LiteDB;
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
        //public SelectListItem SelectedRegion { get; set; }
        //public IEnumerable<SelectListItem> RegionsAvailable { get; set; }

        public Dictionary<RegionEnum, string> RegionsAvailable { get; set; }
        public string Class { get; set; }
        public List<CharacterSkill> Skills { get; set; }
    }
}
