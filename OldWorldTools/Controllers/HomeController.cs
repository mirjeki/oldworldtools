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
        public const String charSheetSRC = "Resources/CharacterSheet.pdf";

        WFRPGenerator generator = new WFRPGenerator();

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
            var skills = generator.GetSkills();
            var currentCareer = generator.GetCareerByName(characterSheet.Career, characterSheet.Species);

            switch (submitButton)
            {
                case "Generate":
                    var pdfBytes = GeneratePdf(characterSheet);
                    string characterNameTrimmed = characterSheet.Name.Replace(" ", string.Empty);

                    // Return the PDF as a file download
                    return File(pdfBytes, "application/pdf", $"WFRP_Char_{characterNameTrimmed}.pdf");
                case "RandomiseName":
                    characterSheet = generator.RandomiseCharacterName(characterSheet);

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
                case "RandomiseCareer":
                    currentCareer = generator.RandomiseCareer(characterSheet.Species);
                    characterSheet = generator.MapCareerToCharacterSheet(currentCareer, characterSheet, TierEnum.Tier1);

                    return View("Index", characterSheet);
                case "RandomiseCharacteristics":
                    characterSheet.Characteristics = generator.RandomiseCharacteristics(characterSheet);
                    //characterSheet = generator.MapCareerToCharacterSheet(currentCareer, characterSheet, TierEnum.Tier1);

                    return View("Index", characterSheet);
                default:
                    break;
            }

            return View("Index", characterSheet);
        }

        private Byte[] GeneratePdf(CharacterSheet characterSheet)
        {
            using (ByteArrayOutputStream baos = new ByteArrayOutputStream())
            {
                using (PdfDocument pdf = new PdfDocument(new PdfReader(charSheetSRC), new PdfWriter(baos)))
                {
                    PdfAcroForm form = PdfAcroForm.GetAcroForm(pdf, true);
                    IDictionary<String, PdfFormField> fields = form.GetAllFormFields();
                    PdfFormField toSet;

                    fields.TryGetValue("Name", out toSet);
                    toSet.SetValue(characterSheet.Name);
                    fields.TryGetValue("Species", out toSet);
                    toSet.SetValue(characterSheet.Species.GetAttributeOfType<DescriptionAttribute>().Description);
                    fields.TryGetValue("Career", out toSet);
                    toSet.SetValue(characterSheet.Career);
                    fields.TryGetValue("Career Path", out toSet);
                    toSet.SetValue(characterSheet.CareerPath);
                    fields.TryGetValue("Career Tier", out toSet);
                    toSet.SetValue(characterSheet.Tier.GetAttributeOfType<DescriptionAttribute>().Description);
                    fields.TryGetValue("Status", out toSet);
                    toSet.SetValue(characterSheet.Status);
                    fields.TryGetValue("Class", out toSet);
                    toSet.SetValue(characterSheet.Class);

                    for (int i = 0; i < characterSheet.Characteristics.Count; i++)
                    {
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Initial", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].Initial.ToString());
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Advance", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].Advances.ToString());
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Current", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].CurrentValue().ToString());
                    }

                    for (int i = 0; i < characterSheet.Talents.Count; i++)
                    {
                        fields.TryGetValue($"Talent NameRow{i + 1}", out toSet);
                        toSet.SetValue(characterSheet.Talents[i]);
                        fields.TryGetValue($"Times takenRow{i + 1}", out toSet);
                        toSet.SetValue("1");

                        if (i >= 11)
                        {
                            break;
                        }
                    }

                    for (int i = 0; i < characterSheet.Trappings.Count; i++)
                    {
                        fields.TryGetValue($"Trappings{i+1}", out toSet);
                        toSet.SetValue(characterSheet.Trappings[i]);

                        if (i >= 11)
                        {
                            break;
                        }
                    }
                    //fields.TryGetValue("experience1", out toSet);
                    //toSet.SetValue("Off");
                    //fields.TryGetValue("experience2", out toSet);
                    //toSet.SetValue("Yes");
                    //fields.TryGetValue("experience3", out toSet);
                    //toSet.SetValue("Yes");
                    //fields.TryGetValue("shift", out toSet);
                    //toSet.SetValue("Any");
                    //fields.TryGetValue("info", out toSet);
                    //toSet.SetValue("I was 38 years old when I became an MI6 agent.");

                    pdf.Close();

                    return baos.ToArray();
                }
            }
        }
    }

    public static class EnumHelper
    {
        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        /// <example><![CDATA[string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;]]></example>
        public static T GetAttributeOfType<T>(this Enum enumVal) where T : System.Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }
    }
}