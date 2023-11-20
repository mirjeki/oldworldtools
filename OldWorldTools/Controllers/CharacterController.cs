using LiteDB;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OldWorldTools.Models.WFRPCharacter;
using OldWorldTools.Models.WFRPCharacter.DTOs;
using System.Net;

namespace OldWorldTools.Controllers
{
    public class CharacterController : Controller
    {
        public IActionResult Skills()
        {
            Skill skill = new Skill();

            return View(skill);
        }

        [HttpGet]
        public void RandomiseCharacter()
        {
            CharacterSheet character = new CharacterSheet();

            character.Name = "Markus";
            character.Class = "Soldier";
            character.Skills = new List<CharacterSkill>();

        }

        [HttpPost]
        public IActionResult AddSkill(Skill skill)
        {
            SkillDTO newSkill = new SkillDTO();

            newSkill.Name = skill.Name;
            newSkill.LinkedCharacteristic = skill.GetCharacteristics().First(f => f.ShortName == skill.SelectedCharacteristic.Value);
            newSkill.Advanced = skill.Advanced;
            newSkill.Grouped = skill.Grouped;
            newSkill.Description = skill.Description;

            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
            {
                ILiteCollection<SkillDTO> skillDTOs = database.GetCollection<SkillDTO>();

                if (skillDTOs.Exists(e => e.Name == newSkill.Name))
                {

                }
                else
                {
                    skillDTOs.Insert(newSkill);
                }
            }

            return null;
        }
    }
}
