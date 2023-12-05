using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OldWorldTools.Models.WFRPCharacter.DTOs
{
    public class SkillDTO
    {
        public string Name { get; set; }
        public CharacteristicEnum LinkedCharacteristic { get; set; }
        public bool Advanced { get; set; }
        public bool Grouped { get; set; }
    }
}
