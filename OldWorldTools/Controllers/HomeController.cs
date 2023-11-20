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

namespace OldWorldTools.Controllers
{
    public class HomeController : Controller
    {
        public const String charSheetSRC = "Resources/CharacterSheet.pdf";

        WFRPGenerator generator = new WFRPGenerator();

        public IActionResult Index()
        {
            generator.SetupData();
            CharacterSheet characterSheet = new CharacterSheet { 
                Name = "Enter name here...", 
                Gender = GenderEnum.Male, 
                Species = SpeciesEnum.Human, 
                Region = RegionEnum.Reikland,
                RegionsAvailable = generator.GetAvailableRegions(SpeciesEnum.Human)};

            //characterSheet.RegionsAvailable = generator.GetAvailableRegions(characterSheet.Species);
            //characterSheet.SelectedRegion = new SelectListItem { Value = "Reikland", Text = "Reikland" };

            return View(characterSheet);
        }

        [HttpPost]
        public IActionResult GenerateCharacter(CharacterSheet characterSheet, string submitButton)
        {
            ModelState.Clear();
            characterSheet.RegionsAvailable = generator.GetAvailableRegions(characterSheet.Species);

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
                    //fields.TryGetValue("language", out toSet);
                    //toSet.SetValue("English");
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

        public static IEnumerable<SelectListItem> EnumToSelectList<T>()
        {
            return (Enum.GetValues(typeof(T)).Cast<T>().Select(
                e => new SelectListItem() { Text = e.ToString(), Value = e.ToString() })).ToList();
        }
    }
}