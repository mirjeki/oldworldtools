using LiteDB;
using Microsoft.AspNetCore.Mvc.Rendering;
using OldWorldTools.Models.WFRPCharacter.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;

namespace OldWorldTools.Models.WFRPCharacter
{
    public class Skill
    {
        public string Name { get; set; }
        [Display(Name = "Linked Characteristic")]
        public Characteristic LinkedCharacteristic { get; set; }

        public SelectListItem SelectedCharacteristic { get; set; }
        public IEnumerable<SelectListItem> CharacteristicsAvailable { get; set; }
        public bool Advanced { get; set; }
        public bool Grouped { get; set; }
        public string Description { get; set; }

        public Skill()
        {
            CharacteristicsAvailable = new List<SelectListItem>();

            CharacteristicsAvailable = GetCharacteristics().
                Select(s => new SelectListItem 
                {
                 Value = s.ShortName,
                 Text = s.Name
                });
        }

        public List<CharacteristicDTO> GetCharacteristics()
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
            {
                ILiteCollection<CharacteristicDTO> characteristicDTOs = database.GetCollection<CharacteristicDTO>();

                return characteristicDTOs.FindAll().ToList();
            }
        }
    }
}
