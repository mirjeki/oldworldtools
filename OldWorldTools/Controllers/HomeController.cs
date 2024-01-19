using Microsoft.AspNetCore.Mvc;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.IO.Source;
using OldWorldTools.Models.WFRPCharacter;
using OldWorldTools.API;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using OldWorldTools.Models.WFRPNames;
using System.ComponentModel;

namespace OldWorldTools.Controllers
{
    public class HomeController : Controller
    {
        WFRPGenerator generator = new WFRPGenerator();
        PDFMapperWFRP mapperWFRP = new PDFMapperWFRP();

        public IActionResult Index()
        {
            generator.SetupData();
            CharacterSheet characterSheet = generator.GetDefaultCharacter();

            return View(characterSheet);
        }

        [HttpPost]
        public IActionResult GenerateCharacter(CharacterSheet characterSheet, string submitButton)
        {
            ModelState.Clear();
            characterSheet.RegionsAvailable = generator.GetAvailableRegions(characterSheet.Species);
            //var careers = generator.GetCareers(characterSheet.Species);
            //var speciesModifiers = generator.GetSpeciesModifiersByRegion(characterSheet.Region);
            //var skills = generator.GetSkills();
            var currentCareer = generator.GetCareerByName(characterSheet.Career, characterSheet.Species);

            switch (submitButton)
            {
                case "RandomiseName":
                    characterSheet = generator.RandomiseCharacterName(characterSheet);

                    return View("Index", characterSheet);
                case "RandomiseMotivation":
                    characterSheet.Motivation = generator.RandomiseMotivation();

                    return View("Index", characterSheet);
                case "RandomiseShortTermAmbition":
                    characterSheet.ShortTermAmbition = generator.RandomiseShortTermAmbition();

                    return View("Index", characterSheet);
                case "RandomiseLongTermAmbition":
                    characterSheet.LongTermAmbition = generator.RandomiseLongTermAmbition();

                    return View("Index", characterSheet);
                case "RandomiseGender":
                    characterSheet.Gender = generator.RandomiseGender();

                    return View("Index", characterSheet);
                case "RandomiseSpecies":
                    characterSheet.Species = generator.RandomiseSpecies();
                    characterSheet.RegionsAvailable = generator.GetAvailableRegions(characterSheet.Species);

                    return View("Index", characterSheet);
                case "RandomiseRegion":
                    characterSheet.Region = generator.RandomiseRegion(characterSheet.RegionsAvailable);

                    return View("Index", characterSheet);
                case "RandomiseCharacteristics":
                    characterSheet.Characteristics = generator.RandomiseCharacteristics(characterSheet);
                    //characterSheet = generator.MapCareerToCharacterSheet(currentCareer, characterSheet, TierEnum.Tier1);

                    ViewData["PageScroll"] = "characteristics";
                    return View("Index", characterSheet);
                case "RandomiseCareer":
                    currentCareer = generator.RandomiseCareer(characterSheet.Species);
                    characterSheet = generator.MapCareerToCharacterSheet(currentCareer, characterSheet, TierEnum.Tier1);
                    characterSheet = generator.MapSpeciesSkillsToCharacterSheet(characterSheet);
                    characterSheet = generator.MapCareerSkillsToCharacterSheet(currentCareer, characterSheet, characterSheet.Tier);

                    ViewData["PageScroll"] = "career";
                    return View("Index", characterSheet);
                case "RandomiseSkills":
                    characterSheet = generator.MapSpeciesSkillsToCharacterSheet(characterSheet);
                    characterSheet = generator.MapCareerSkillsToCharacterSheet(currentCareer, characterSheet, characterSheet.Tier);

                    ViewData["PageScroll"] = "skills";
                    return View("Index", characterSheet);
                case "Generate":
                    var pdfBytes = mapperWFRP.GeneratePdf(characterSheet);
                    string characterNameTrimmed = characterSheet.Name.Replace(" ", string.Empty);

                    // Return the PDF as a file download
                    return File(pdfBytes, "application/pdf", $"WFRP_Char_{characterNameTrimmed}.pdf");
                default:
                    break;
            }

            return View("Index", characterSheet);
        }
    }
}