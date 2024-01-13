using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Source;
using iText.Kernel.Pdf;
using OldWorldTools.Models.WFRPCharacter;
using System.ComponentModel;
using Microsoft.Extensions.ObjectPool;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

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
                    int weaponItems = 0;
                    int weaponsEnc = 0;
                    int armoursEnc = 0;
                    int trappingsEnc = 0;

                    for (int i = 0; i < characterSheet.Trappings.Count; i++)
                    {
                        fields.TryGetValue($"Trappings{i + 1}", out toSet);
                        toSet.SetValue(characterSheet.Trappings[i]);

                        var matchedTrapping = trappingsList.Where(w => w.Name == characterSheet.Trappings[i]).FirstOrDefault();

                        if (matchedTrapping != null)
                        {
                            fields.TryGetValue($"TrappingEncRow{i + 1}", out toSet);
                            toSet.SetValue(matchedTrapping.Enc);
                            if (Int32.TryParse(matchedTrapping.Enc, out int trappingEnc))
                            {
                                trappingsEnc += trappingEnc;
                            }

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
                                    if (Int32.TryParse(matchedTrapping.Enc, out int armourEnc))
                                    {
                                        armoursEnc += armourEnc;
                                        trappingsEnc -= armourEnc;
                                    }
                                    armourItems++;
                                }
                                if (matchedTrapping.Properties.Contains("Weapon"))
                                {
                                    var properties = HelperMethods.SeparateCSV(matchedTrapping.Properties);
                                    bool includesStrengthBonus = false;
                                    int damageBonus = 0;

                                    string qualities = "";

                                    foreach (var property in properties)
                                    {
                                        switch (property)
                                        {
                                            case "Basic":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Cavalry":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Fencing":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Brawling":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Flail":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Parry":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Polearm":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Two-handed":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Blackpowder":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Bow":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Crossbow":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Engineer":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Explosives":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Sling":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "Throwing":
                                                fields.TryGetValue($"GroupRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case "A":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Average");
                                                break;
                                            case "V":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Varies");
                                                break;
                                            case "VS":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Very Short");
                                                break;
                                            case "P":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Personal");
                                                break;
                                            case "L":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Long");
                                                break;
                                            case "VL":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Very Long");
                                                break;
                                            case "S":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Short");
                                                break;
                                            case "M":
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue("Massive");
                                                break;
                                            case string a when a.Contains("+"):
                                                fields.TryGetValue($"DamageRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property);
                                                break;
                                            case string a when a.Contains("Range"):
                                                fields.TryGetValue($"RangeReachRow{weaponItems + 1}", out toSet);
                                                toSet.SetValue(property.Replace("Range",string.Empty));
                                                break;
                                            case "Weapon":
                                                break;
                                            default:
                                                qualities += $"{property} ";
                                                break;
                                        }
                                    }

                                    fields.TryGetValue($"WeaponsNameRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(matchedTrapping.Name);
                                    fields.TryGetValue($"EncRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(matchedTrapping.Enc);
                                    fields.TryGetValue($"WeaponsQualitiesRow{armourItems + 1}", out toSet);
                                    toSet.SetValue(qualities);
                                    if (Int32.TryParse(matchedTrapping.Enc, out int weaponEnc))
                                    {
                                        weaponsEnc += weaponEnc;
                                        trappingsEnc -= weaponEnc;
                                    }
                                    weaponItems++;
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

                    //Wealth

                    switch (characterSheet.Status)
                    {
                        case string a when a.Contains("Brass"):
                            string trimmedBrassValue = a.Replace("Brass ", string.Empty);
                            int pennies = 0;
                            if (Int32.TryParse(trimmedBrassValue, out int brassValue))
                            {
                                pennies = (HelperMethods.RollD10() + HelperMethods.RollD10()) * brassValue;
                            }
                            fields.TryGetValue($"WEALTHd", out toSet);
                            toSet.SetValue(pennies.ToString());
                            break;
                        case string a when a.Contains("Silver"):
                            string trimmedSilverValue = a.Replace("Silver ", string.Empty);
                            int shillings = 0;
                            if (Int32.TryParse(trimmedSilverValue, out int silverValue))
                            {
                                shillings = (HelperMethods.RollD10()) * silverValue;
                            }
                            fields.TryGetValue($"WEALTHSS", out toSet);
                            toSet.SetValue(shillings.ToString());
                            break;
                        case string a when a.Contains("Gold"):
                            string trimmedGoldValue = a.Replace("Gold ", string.Empty);
                            int crowns = 0;
                            if (Int32.TryParse(trimmedGoldValue, out int goldValue))
                            {
                                crowns = goldValue;
                            }
                            fields.TryGetValue($"WEALTHGC", out toSet);
                            toSet.SetValue(crowns.ToString());
                            break;
                        default:
                            break;
                    }

                    //Encumbrance
                    fields.TryGetValue($"EncumbWeapons", out toSet);
                    toSet.SetValue((weaponsEnc).ToString());
                    fields.TryGetValue($"EncumbArmour", out toSet);
                    toSet.SetValue((armoursEnc).ToString());
                    fields.TryGetValue($"EncumbTrappings", out toSet);
                    toSet.SetValue((trappingsEnc).ToString());
                    fields.TryGetValue($"Total Max Enc", out toSet);
                    toSet.SetValue((StrengthBonus + ToughnessBonus).ToString());
                    fields.TryGetValue($"Total", out toSet);
                    toSet.SetValue((weaponsEnc + armoursEnc + trappingsEnc).ToString());

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
