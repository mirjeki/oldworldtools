using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Source;
using iText.Kernel.Pdf;
using OldWorldTools.Models.WFRPCharacter;
using System.ComponentModel;
using Microsoft.Extensions.ObjectPool;

namespace OldWorldTools.API
{
    public class PDFMapperWFRP
    {
        public const String charSheetSRC = "Resources/CharacterSheet.pdf";
        WFRPGenerator generator = new WFRPGenerator();


        public Byte[] GeneratePdf(CharacterSheet characterSheet)
        {
            int StrengthBonus = characterSheet.Characteristics.Where(w => w.ShortName == CharacteristicEnum.S).First().CurrentBonus();
            int ToughnessBonus = characterSheet.Characteristics.Where(w => w.ShortName == CharacteristicEnum.T).First().CurrentBonus();
            int WillPowerBonus = characterSheet.Characteristics.Where(w => w.ShortName == CharacteristicEnum.WP).First().CurrentBonus();

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

                    fields.TryGetValue("Fate", out toSet);
                    toSet.SetValue(characterSheet.Fate.ToString());
                    fields.TryGetValue("Fortune", out toSet);
                    toSet.SetValue(characterSheet.Fortune.ToString());

                    fields.TryGetValue("ResilienceRow1", out toSet);
                    toSet.SetValue(characterSheet.Resilience.ToString());
                    fields.TryGetValue("ResolveRow1", out toSet);
                    toSet.SetValue(characterSheet.Resolve.ToString());
                    fields.TryGetValue("MotivationRow1", out toSet);
                    toSet.SetValue(characterSheet.Motivation);

                    fields.TryGetValue("AmbitionsShortTerm", out toSet);
                    toSet.SetValue(characterSheet.ShortTermAmbition);

                    fields.TryGetValue("AmbitionsLongTerm", out toSet);
                    toSet.SetValue(characterSheet.LongTermAmbition);

                    fields.TryGetValue("Movement", out toSet);
                    toSet.SetValue(characterSheet.Movement.ToString());
                    fields.TryGetValue("Walk", out toSet);
                    toSet.SetValue(characterSheet.Walk.ToString());
                    fields.TryGetValue("Run", out toSet);
                    toSet.SetValue(characterSheet.Run.ToString());

                    for (int i = 0; i < characterSheet.Characteristics.Count; i++)
                    {
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Initial", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].Initial.ToString());
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Advance", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].Advances.ToString());
                        fields.TryGetValue($"{characterSheet.Characteristics[i].ShortName.ToString()}Current", out toSet);
                        toSet.SetValue(characterSheet.Characteristics[i].CurrentValue().ToString());
                    }

                    int advSkillRow = 1;

                    foreach (var skill in characterSheet.SpeciesSkills)
                    {
                        //the MapSkill method will increase the advSkillRow

                        advSkillRow = MapSkill(fields, skill, toSet, advSkillRow);
                    }

                    foreach (var skill in characterSheet.CareerSkills)
                    {
                        advSkillRow = MapSkill(fields, skill, toSet, advSkillRow);
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

                    var trappingsList = generator.GetTrappings();
                    int armourItems = 0;

                    for (int i = 0; i < characterSheet.Trappings.Count; i++)
                    {
                        fields.TryGetValue($"Trappings{i + 1}", out toSet);
                        toSet.SetValue(characterSheet.Trappings[i]);

                        var matchedTrapping = trappingsList.Where(w => w.Name == characterSheet.Trappings[i]).FirstOrDefault();

                        if (matchedTrapping != null)
                        {
                            fields.TryGetValue($"TrappingEncRow{i + 1}", out toSet);
                            toSet.SetValue(matchedTrapping.Enc);

                            if (matchedTrapping.Properties != null)
                            {
                                if (matchedTrapping.Properties.Contains("Armour"))
                                {
                                    var properties = HelperMethods.SeparateCSV(matchedTrapping.Properties);
                                    bool affectsArms = false;
                                    bool affectsLegs = false;
                                    bool affectsBody = false;
                                    bool affectsHead = false;
                                    int armourValue = 0;

                                    string qualities = "";
                                    string locations = "";

                                    foreach (var property in properties)
                                    {
                                        switch (property)
                                        {
                                            case "AP1":
                                                armourValue = 1;
                                                break;
                                            case "AP2":
                                                armourValue = 2;
                                                break;
                                            case "A":
                                                affectsArms = true;
                                                locations += "Arms ";
                                                break;
                                            case "B":
                                                affectsBody = true;
                                                locations += $"Body ";
                                                break;
                                            case "L":
                                                affectsLegs = true;
                                                locations += $"Legs ";
                                                break;
                                            case "H":
                                                affectsHead = true;
                                                locations += $"Head ";
                                                break;
                                            case "Armour":
                                                break;
                                            default:
                                                qualities += $"{property} ";
                                                break;
                                        }
                                    }

                                    int currentValue;

                                    if (affectsHead)
                                    {
                                        fields.TryGetValue($"Head", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }
                                    }
                                    if (affectsBody)
                                    {
                                        fields.TryGetValue($"Body", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }
                                    }
                                    if (affectsArms)
                                    {
                                        fields.TryGetValue($"LArm", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }

                                        fields.TryGetValue($"RArm", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }
                                    }
                                    if (affectsLegs)
                                    {
                                        fields.TryGetValue($"LLeg", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }

                                        fields.TryGetValue($"RLeg", out toSet);
                                        Int32.TryParse(toSet.GetValueAsString(), out currentValue);
                                        if (currentValue <= armourValue)
                                        {
                                            //only write if current armour's value is higher than currently applied armour
                                            toSet.SetValue(armourValue.ToString());
                                        }
                                    }

                                    fields.TryGetValue($"ArmorNameRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(matchedTrapping.Name);
                                    fields.TryGetValue($"LocationsRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(locations);
                                    fields.TryGetValue($"ArmorEncRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(matchedTrapping.Enc);
                                    fields.TryGetValue($"APRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(armourValue.ToString());
                                    fields.TryGetValue($"QualitiesRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(qualities);
                                    armourItems++;
                                }
                            }
                        }

                        if (i >= 11)
                        {
                            break;
                        }
                    }

                    //Corruption & Mutation
                    fields.TryGetValue($"CThreshold", out toSet);
                    toSet.SetValue((ToughnessBonus + WillPowerBonus).ToString());
                    fields.TryGetValue($"PLimit", out toSet);
                    toSet.SetValue((ToughnessBonus).ToString());
                    fields.TryGetValue($"MLimit", out toSet);
                    toSet.SetValue((WillPowerBonus).ToString());

                    //Wounds
                    fields.TryGetValue($"SBField", out toSet);
                    toSet.SetValue((StrengthBonus).ToString());
                    fields.TryGetValue($"TBx2", out toSet);
                    toSet.SetValue((ToughnessBonus * 2).ToString());
                    fields.TryGetValue($"WPBField", out toSet);
                    toSet.SetValue((WillPowerBonus).ToString());


                    if (characterSheet.Talents.Contains("Hardy"))
                    {
                        fields.TryGetValue($"HardyField", out toSet);
                        toSet.SetValue((ToughnessBonus).ToString());
                        fields.TryGetValue($"Wounds", out toSet);
                        toSet.SetValue((StrengthBonus + (ToughnessBonus * 2) + WillPowerBonus + ToughnessBonus).ToString());
                    }
                    else
                    {
                        fields.TryGetValue($"HardyField", out toSet);
                        toSet.SetValue(0.ToString());
                        fields.TryGetValue($"Wounds", out toSet);
                        toSet.SetValue((StrengthBonus + (ToughnessBonus * 2) + WillPowerBonus).ToString());
                    }


                    pdf.Close();

                    return baos.ToArray();
                }
            }
        }

        private int MapSkill(IDictionary<string, PdfFormField> fields, CharacterSkill characterSkill, PdfFormField toSet, int advSkillRow)
        {
            string[] nameParts = characterSkill.Skill.Name.Split(' ');

            if (characterSkill.Skill.Advanced)
            {
                fields.TryGetValue($"SkillNameRow{advSkillRow}", out toSet);
                toSet.SetValue(characterSkill.Skill.Name);
                fields.TryGetValue($"CharRow{advSkillRow}", out toSet);
                toSet.SetValue(characterSkill.Skill.LinkedCharacteristic.ToString());
                fields.TryGetValue($"CharacteristicRow{advSkillRow}", out toSet);
                toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                fields.TryGetValue($"AdvRow{advSkillRow}", out toSet);
                toSet.SetValue(characterSkill.Advances.ToString());
                fields.TryGetValue($"SkillRow{advSkillRow}", out toSet);
                toSet.SetValue(characterSkill.CurrentTotal().ToString());

                advSkillRow++;
            }
            else
            {
                switch (nameParts[0])
                {
                    case "Art":
                        fields.TryGetValue($"{nameParts[0]}Group", out toSet);
                        toSet.SetValue(nameParts[1]);
                        fields.TryGetValue($"{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                        fields.TryGetValue($"Adv{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.Advances.ToString());
                        fields.TryGetValue($"Skill{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        break;
                    case "Entertain":
                        fields.TryGetValue($"{nameParts[0]}Group", out toSet);
                        toSet.SetValue(nameParts[1]);
                        fields.TryGetValue($"{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                        fields.TryGetValue($"Adv{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.Advances.ToString());
                        fields.TryGetValue($"Skill{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        break;
                    case "Melee":
                        if (nameParts.Length > 1 && !nameParts[1].Contains("Basic"))
                        {
                            fields.TryGetValue($"{nameParts[0]}Group", out toSet);
                            toSet.SetValue(nameParts[1]);
                            fields.TryGetValue($"{nameParts[0]}", out toSet);
                            toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                            fields.TryGetValue($"Adv{nameParts[0]}", out toSet);
                            toSet.SetValue(characterSkill.Advances.ToString());
                            fields.TryGetValue($"Skill{nameParts[0]}", out toSet);
                            toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        }
                        else
                        {
                            fields.TryGetValue($"MeleeBasic", out toSet);
                            toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                            fields.TryGetValue($"AdvMeleeBasic", out toSet);
                            toSet.SetValue(characterSkill.Advances.ToString());
                            fields.TryGetValue($"SkillMeleeBasic", out toSet);
                            toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        }
                        break;
                    case "Ride":
                        fields.TryGetValue($"{nameParts[0]}Group", out toSet);
                        toSet.SetValue(nameParts[1]);
                        fields.TryGetValue($"{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                        fields.TryGetValue($"Adv{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.Advances.ToString());
                        fields.TryGetValue($"Skill{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        break;
                    case "Stealth":
                        fields.TryGetValue($"{nameParts[0]}Group", out toSet);
                        toSet.SetValue(nameParts[1]);
                        fields.TryGetValue($"{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                        fields.TryGetValue($"Adv{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.Advances.ToString());
                        fields.TryGetValue($"Skill{nameParts[0]}", out toSet);
                        toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        break;
                    default:
                        string nameWithoutSpaces = characterSkill.Skill.Name.Replace(" ", String.Empty);
                        fields.TryGetValue($"{nameWithoutSpaces}", out toSet);
                        toSet.SetValue(characterSkill.CharacteristicValue.ToString());
                        fields.TryGetValue($"Adv{nameWithoutSpaces}", out toSet);
                        toSet.SetValue(characterSkill.Advances.ToString());
                        fields.TryGetValue($"Skill{nameWithoutSpaces}", out toSet);
                        toSet.SetValue(characterSkill.CurrentTotal().ToString());
                        break;
                }
            }

            return advSkillRow;
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
