using LiteDB;
using OldWorldTools.Models.WFRPNames.DTOs;
using OldWorldTools.Models.WFRPNames;
using System.Xml.Serialization;
using OldWorldTools.Models.WFRPCharacter;
using OldWorldTools.Models.WFRPCharacter.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Org.BouncyCastle.Tls;
using System.Reflection;
using System.Security;

namespace OldWorldTools.API
{
    public class WFRPGenerator
    {
        public const string humanNameXMLSRC = "Resources/Names/HumanNames.xml";
        public const string dwarfNameXMLSRC = "Resources/Names/DwarfNames.xml";
        public const string halflingNameXMLSRC = "Resources/Names/HalflingNames.xml";
        public const string woodElfNameXMLSRC = "Resources/Names/WoodElfNames.xml";
        public const string highElfNameXMLSRC = "Resources/Names/HighElfNames.xml";
        public const string speciesXMLSRC = "Resources/Species/Species.xml";
        public const string talentsXMLSRC = "Resources/Talents/Talents.xml";
        public const string careersXMLSRC = "Resources/Careers/Careers.xml";

        public const string characterDefinitionsDB = "Data/WFRPCharacterDefinitions.db";
        public const string characterNamesDB = "Data/WFRPCharacterNames.db";
        static Random random = new Random();

        public void SetupData()
        {
            ImportCharacterNames();
            ImportCharacterSpecies();
            ImportCharacterCareers("Human");
            ImportCharacterCareers("Dwarf");
            ImportCharacterCareers("Halfling");
            ImportCharacterCareers("HighElf");
            ImportCharacterCareers("WoodElf");
            ImportCharacterTalents();

            SetupCharacteristics();
        }

        public CharacterSheet GetDefaultCharacter()
        {
            CharacterSheet characterSheet = new CharacterSheet
            {
                Name = "Enter name here...",
                Gender = GenderEnum.Male,
                Species = SpeciesEnum.Human,
                Region = RegionEnum.Reikland,
                RegionsAvailable = GetAvailableRegions(SpeciesEnum.Human)
            };

            characterSheet.Characteristics = GetCharacteristics();

            characterSheet.Characteristics = RandomiseCharacteristics(characterSheet);
            characterSheet = MapCareerToCharacterSheet(RandomiseCareer(SpeciesEnum.Human), characterSheet, TierEnum.Tier1);

            return characterSheet;
        }

        public CharacterSheet RandomiseCharacterName(CharacterSheet characterSheet)
        {
            string firstName = GetRandomCharacterName(characterSheet.Gender, characterSheet.Region, characterSheet.Species, NameTypeEnum.Forename);
            string lastName = GetRandomCharacterName(characterSheet.Gender, characterSheet.Region, characterSheet.Species, NameTypeEnum.Surname);
            characterSheet.Name = firstName + " " + lastName;

            return characterSheet;
        }

        public GenderEnum RandomiseGender()
        {
            return RandomEnumValue<GenderEnum>();
        }

        public SpeciesEnum RandomiseSpecies()
        {
            int diceRoll = RollD100();

            switch (diceRoll)
            {
                case <= 90:
                    return SpeciesEnum.Human;
                case <= 94:
                    return SpeciesEnum.Halfling;
                case <= 98:
                    return SpeciesEnum.Dwarf;
                case 99:
                    return SpeciesEnum.HighElf;
                case 100:
                    return SpeciesEnum.WoodElf;
                default:
                    return SpeciesEnum.Human;
            }
        }

        public RegionEnum RandomiseRegion(Dictionary<RegionEnum, string> regionsAvailable)
        {
            int randomResult = random.Next(regionsAvailable.Count());

            var result = regionsAvailable.ToList()[randomResult];

            return result.Key;
        }

        public CareerDTO RandomiseCareer(SpeciesEnum species)
        {
            int diceRoll = RollD100();

            var availableCareers = GetCareers(species);

            foreach (var career in availableCareers)
            {
                if (diceRoll >= career.oddsLower && diceRoll <= career.oddsUpper)
                {
                    return career;
                }
            }

            return availableCareers[0];
        }

        private int RollD10()
        {
            return random.Next(1, 10);
        }

        private int RollD100()
        {
            return random.Next(1, 100);
        }

        public CharacterSheet MapCareerToCharacterSheet(CareerDTO career, CharacterSheet characterSheet, TierEnum tier)
        {
            characterSheet.Career = career.Name;
            characterSheet.Class = career.Class;

            switch (tier)
            {
                case TierEnum.Tier1:
                    characterSheet.CareerPath = career.Path1.Title;
                    characterSheet.Status = career.Path1.Status;
                    characterSheet.Talents = RandomiseStarterTalents(characterSheet.Region, SeparateAndFormatCSV(career.Path1.Talents));
                    //characterSheet.Talents = SeparateAndFormatCSV(career.Path1.Talents);
                    characterSheet.Trappings = SeparateAndFormatCSV(career.Path1.Trappings);
                    break;
                case TierEnum.Tier2:
                    break;
                case TierEnum.Tier3:
                    characterSheet.CareerPath = career.Path3.Title;
                    characterSheet.Status = career.Path3.Status;
                    //randomly determine chosen talents from list
                    characterSheet.Talents = SeparateAndFormatCSV(career.Path3.Talents);
                    characterSheet.Trappings = SeparateAndFormatCSV(career.Path3.Trappings);
                    break;
                case TierEnum.Tier4:
                    break;
                default:
                    break;
            }

            characterSheet = ApplyTalentModifiersToCharacterSheet(characterSheet);

            foreach (var characteristic in characterSheet.Characteristics)
            {
                CalculateInitialCharacteristic(characteristic, characterSheet.Region);
            }

            return characterSheet;
        }

        private void ResetCharacteristicModifiers(CharacterSheet characterSheet)
        {
            foreach (var characteristic in characterSheet.Characteristics)
            {
                characteristic.OtherModifier = 0;
            }
        }

        private List<string> RandomiseStarterTalents(RegionEnum region, List<string> pathTalents)
        {
            List<string> talentsToReturn = new List<string>();

            var speciesTalents = GetSpeciesModifiersByRegion(region).StartingTalents;

            talentsToReturn = SeparateAndFormatCSV(speciesTalents);

            talentsToReturn.Add(GetUniquePathTalent(talentsToReturn, pathTalents));

            List<string> talentsToRemove = new List<string>();
            List<string> talentsToAdd = new List<string>();

            foreach (var talent in talentsToReturn)
            {
                if (talent.Contains("?"))
                {
                    var options = talent.Split('?', StringSplitOptions.TrimEntries);
                    var result = RollD100();

                    if (result <= 50)
                    {
                        talentsToAdd.Add(options[0]);
                    }
                    else
                    {
                        talentsToAdd.Add(options[1]);
                    }

                    talentsToRemove.Add(talent);
                }

                if (talent == "2 Random")
                {
                    talentsToAdd.Add(GetUniqueStarterTalent(talentsToReturn, talentsToAdd));
                    talentsToAdd.Add(GetUniqueStarterTalent(talentsToReturn, talentsToAdd));

                    talentsToRemove.Add(talent);
                }

                if (talent == "3 Random")
                {
                    talentsToAdd.Add(GetUniqueStarterTalent(talentsToReturn, talentsToAdd));
                    talentsToAdd.Add(GetUniqueStarterTalent(talentsToReturn, talentsToAdd));
                    talentsToAdd.Add(GetUniqueStarterTalent(talentsToReturn, talentsToAdd));

                    talentsToRemove.Add(talent);
                }
            }

            foreach (var talent in talentsToRemove)
            {
                talentsToReturn.Remove(talent);
            }

            foreach (var talent in talentsToAdd)
            {
                talentsToReturn.Add(talent);
            }

            return talentsToReturn;
        }

        private string GetUniquePathTalent(List<string> talentsToReturn, List<string> pathTalents)
        {
            string newTalent = pathTalents[random.Next(pathTalents.Count)];
            while (talentsToReturn.Contains(newTalent))
            {
                newTalent = pathTalents[random.Next(pathTalents.Count)];
            }
            return newTalent;
        }

        private string GetUniqueStarterTalent(List<string> talentsToReturn, List<string> talentsToAdd)
        {
            string newTalent = GetRandomStartingTalent();
            while (talentsToReturn.Contains(newTalent) || talentsToAdd.Contains(newTalent))
            {
                newTalent = GetRandomStartingTalent();
            }
            return newTalent;
        }

        public Dictionary<RegionEnum, string> GetAvailableRegions(SpeciesEnum species)
        {
            Dictionary<RegionEnum, string> regionsAvailable = new Dictionary<RegionEnum, string>();

            var regions = GetRegions(species);

            foreach (var region in regions)
            {
                RegionEnum newRegion;
                Enum.TryParse<RegionEnum>(region.Region, out newRegion);
                regionsAvailable.Add(newRegion, region.Region);
            }

            return regionsAvailable;
        }

        private List<Characteristic> GetCharacteristics()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CharacteristicDTO> characteristicDTOs = database.GetCollection<CharacteristicDTO>();

                List<Characteristic> characteristicsToReturn = new List<Characteristic>();

                var allCharacteristics = characteristicDTOs.FindAll().ToList();

                foreach (var characteristic in allCharacteristics)
                {
                    CharacteristicEnum shortNameEnum = (CharacteristicEnum)Enum.Parse(typeof(CharacteristicEnum), characteristic.ShortName);
                    characteristicsToReturn.Add(new Characteristic { Name = characteristic.Name, ShortName = shortNameEnum });
                }

                return characteristicsToReturn;
            }
        }

        private List<SpeciesDTO> GetRegions(SpeciesEnum species)
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<SpeciesDTO> speciesDTOs = database.GetCollection<SpeciesDTO>();

                return speciesDTOs.FindAll().Where(w => w.SpeciesType == species.ToString()).ToList();
            }
        }

        public SpeciesDTO GetSpeciesModifiersByRegion(RegionEnum region)
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<SpeciesDTO> speciesDTOs = database.GetCollection<SpeciesDTO>();

                return speciesDTOs.FindOne(f => f.Region == region.ToString());

                //return speciesDTOs.FindAll().Where(w => w.SpeciesType == species.ToString()).FirstOrDefault();
            }
        }

        public List<CareerDTO> GetCareers(SpeciesEnum species)
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CareerDTO> careerDTOs = database.GetCollection<CareerDTO>($"{species.ToString()}Careers");

                return careerDTOs.FindAll().ToList();
            }
        }

        public CareerDTO GetCareerByName(string careerName, SpeciesEnum species)
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CareerDTO> careerDTOs = database.GetCollection<CareerDTO>($"{species.ToString()}Careers");

                return careerDTOs.FindOne(f => f.Name == careerName);
            }
        }

        private string GetRandomCharacterName(GenderEnum gender, RegionEnum region, SpeciesEnum species, NameTypeEnum nameType)
        {
            using (var database = new LiteDatabase(characterNamesDB))
            {
                ILiteCollection<NameDTO> names = database.GetCollection<NameDTO>();

                List<NameDTO> nameDTOList = names.FindAll().ToList();

                switch (species)
                {
                    case SpeciesEnum.Human:
                        nameDTOList = nameDTOList.Where(w => w.Species == "Human").ToList();
                        break;
                    case SpeciesEnum.Dwarf:
                        nameDTOList = nameDTOList.Where(w => w.Species == "Dwarf").ToList();
                        break;
                    case SpeciesEnum.Halfling:
                        nameDTOList = nameDTOList.Where(w => w.Species == "Halfling").ToList();
                        break;
                    case SpeciesEnum.WoodElf:
                        nameDTOList = nameDTOList.Where(w => w.Species == "WoodElf").ToList();
                        break;
                    case SpeciesEnum.HighElf:
                        nameDTOList = nameDTOList.Where(w => w.Species == "HighElf").ToList();
                        break;
                    default:
                        break;
                }

                switch (region)
                {
                    case RegionEnum.Reikland:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Reikland").ToList();
                        break;
                    case RegionEnum.Middenland:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Middenland").ToList();
                        break;
                    case RegionEnum.Karak:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Karak").ToList();
                        break;
                    case RegionEnum.Imperial:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Imperial").ToList();
                        break;
                    case RegionEnum.Moot:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Moot").ToList();
                        break;
                    case RegionEnum.Asur:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Asur").ToList();
                        break;
                    case RegionEnum.Asrai:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Asrai").ToList();
                        break;
                    case RegionEnum.Eonir:
                        nameDTOList = nameDTOList.Where(w => w.Region == "Eonir").ToList();
                        break;
                    default:
                        break;
                }

                switch (nameType)
                {
                    case NameTypeEnum.Forename:
                        nameDTOList = nameDTOList.Where(w => w.NameType.Contains("Forename")).ToList();
                        break;
                    case NameTypeEnum.Surname:
                        nameDTOList = nameDTOList.Where(w => w.NameType.Contains("Surname")).ToList();
                        break;
                    default:
                        break;
                }

                if (nameType == NameTypeEnum.Forename || region == RegionEnum.Karak)
                {
                    switch (gender)
                    {
                        case GenderEnum.Male:
                            nameDTOList = nameDTOList.Where(w => w.NameType.Contains("Male")).ToList();
                            break;
                        case GenderEnum.Female:
                            nameDTOList = nameDTOList.Where(w => w.NameType.Contains("Female")).ToList();
                            break;
                        default:
                            break;
                    }
                }

                var random = new Random();

                int randomIndex = random.Next(nameDTOList.Count);

                return nameDTOList[randomIndex].Value;
            }
        }

        private string GetRandomStartingTalent()
        {
            int diceRoll = RollD100();

            switch (diceRoll)
            {
                case <= 3:
                    return "Acute Sense (Any one)";
                case <= 6:
                    return "Ambidextrous";
                case <= 9:
                    return "Animal Affinity";
                case <= 12:
                    return "Artistic";
                case <= 15:
                    return "Attractive";
                case <= 18:
                    return "Coolheaded";
                case <= 21:
                    return "Craftsman (Any one)";
                case <= 24:
                    return "Flee!";
                case <= 28:
                    return "Hardy";
                case <= 31:
                    return "Lightning Reflexes";
                case <= 34:
                    return "Linguistics";
                case <= 38:
                    return "Luck";
                case <= 41:
                    return "Marksman";
                case <= 44:
                    return "Mimic";
                case <= 47:
                    return "Night Vision";
                case <= 50:
                    return "Nimble Fingered";
                case <= 52:
                    return "Noble Blood";
                case <= 55:
                    return "Orientation";
                case <= 58:
                    return "Perfect Pitch";
                case <= 62:
                    return "Pure Soul";
                case <= 65:
                    return "Read/Write";
                case <= 68:
                    return "Resistance (Any one)";
                case <= 71:
                    return "Savvy";
                case <= 74:
                    return "Sharp";
                case <= 78:
                    return "Sixth Sense";
                case <= 81:
                    return "Strong Legs";
                case <= 84:
                    return "Sturdy";
                case <= 87:
                    return "Suave";
                case <= 91:
                    return "Super Numerate";
                case <= 94:
                    return "Very Resilient";
                case <= 97:
                    return "Very Strong";
                case <= 100:
                    return "Warrior Born";
                default:
                    return "Acute Sense (Any one)";
            }

            //using (var database = new LiteDatabase(characterDefinitionsDB))
            //{
            //    ILiteCollection<TalentDTO> names = database.GetCollection<TalentDTO>();

            //    List<TalentDTO> talentDTOList = names.FindAll().ToList();

            //    switch (diceRoll)
            //    {
            //        case <= 3:
            //            return talentDTOList.Where(w => w.Name.Contains("Acute Sense")).First();
            //        case <= 6:
            //            return talentDTOList.Where(w => w.Name.Contains("Ambidextrous")).First();
            //        case <= 9:
            //            return talentDTOList.Where(w => w.Name.Contains("Animal Affinity")).First();
            //        case <= 12:
            //            return talentDTOList.Where(w => w.Name.Contains("Artistic")).First();
            //        case <= 15:
            //            return talentDTOList.Where(w => w.Name.Contains("Attractive")).First();
            //        case <= 18:
            //            return talentDTOList.Where(w => w.Name.Contains("Coolheaded")).First();
            //        case <= 21:
            //            return talentDTOList.Where(w => w.Name.Contains("Craftsman")).First();
            //        case <= 24:
            //            return talentDTOList.Where(w => w.Name.Contains("Flee")).First();
            //        case <= 28:
            //            return talentDTOList.Where(w => w.Name.Contains("Hardy")).First();
            //        case <= 31:
            //            return talentDTOList.Where(w => w.Name.Contains("Lightning Reflexes")).First();
            //        case <= 34:
            //            return talentDTOList.Where(w => w.Name.Contains("Linguistics")).First();
            //        case <= 38:
            //            return talentDTOList.Where(w => w.Name.Contains("Luck")).First();
            //        case <= 41:
            //            return talentDTOList.Where(w => w.Name.Contains("Marksman")).First();
            //        case <= 44:
            //            return talentDTOList.Where(w => w.Name.Contains("Mimic")).First();
            //        case <= 47:
            //            return talentDTOList.Where(w => w.Name.Contains("Night Vision")).First();
            //        case <= 50:
            //            return talentDTOList.Where(w => w.Name.Contains("Nimble Fingered")).First();
            //        case <= 52:
            //            return talentDTOList.Where(w => w.Name.Contains("Noble Blood")).First();
            //        case <= 55:
            //            return talentDTOList.Where(w => w.Name.Contains("Orientation")).First();
            //        case <= 58:
            //            return talentDTOList.Where(w => w.Name.Contains("Perfect Pitch")).First();
            //        case <= 62:
            //            return talentDTOList.Where(w => w.Name.Contains("Pure Soul")).First();
            //        case <= 65:
            //            return talentDTOList.Where(w => w.Name.Contains("Read/Write")).First();
            //        case <= 68:
            //            return talentDTOList.Where(w => w.Name.Contains("Resistance")).First();
            //        case <= 71:
            //            return talentDTOList.Where(w => w.Name.Contains("Savvy")).First();
            //        case <= 74:
            //            return talentDTOList.Where(w => w.Name.Contains("Sharp")).First();
            //        case <= 78:
            //            return talentDTOList.Where(w => w.Name.Contains("Sixth Sense")).First();
            //        case <= 81:
            //            return talentDTOList.Where(w => w.Name.Contains("Strong Legs")).First();
            //        case <= 84:
            //            return talentDTOList.Where(w => w.Name.Contains("Sturdy")).First();
            //        case <= 87:
            //            return talentDTOList.Where(w => w.Name.Contains("Suave")).First();
            //        case <= 91:
            //            return talentDTOList.Where(w => w.Name.Contains("Super Numerate")).First();
            //        case <= 94:
            //            return talentDTOList.Where(w => w.Name.Contains("Very Resilient")).First();
            //        case <= 97:
            //            return talentDTOList.Where(w => w.Name.Contains("Very Strong")).First();
            //        case <= 100:
            //            return talentDTOList.Where(w => w.Name.Contains("Warrior Born")).First();
            //        default:
            //            return talentDTOList.Where(w => w.Name.Contains("Acute Sense")).First();
            //    }
            //}
        }

        public List<Characteristic> RandomiseCharacteristics(CharacterSheet characterSheet)
        {
            List<Characteristic> characteristics = characterSheet.Characteristics;

            foreach (var characteristic in characteristics)
            {
                characteristic.Rolled = RollD10() + RollD10();

                CalculateInitialCharacteristic(characteristic, characterSheet.Region);
            }

            return characteristics;
        }

        private void CalculateInitialCharacteristic(Characteristic characteristic, RegionEnum region)
        {
            var speciesModifiers = GetSpeciesModifiersByRegion(region);
            characteristic.SpeciesModifier = speciesModifiers.CharacteristicsModifiers.Find(f => f.Characteristic == characteristic.ShortName).Modifier;
            characteristic.Initial = characteristic.Rolled + characteristic.SpeciesModifier + characteristic.OtherModifier;
        }

        public CharacterSheet ApplyTalentModifiersToCharacterSheet(CharacterSheet characterSheet)
        {
            ResetCharacteristicModifiers(characterSheet);

            foreach (var talent in characterSheet.Talents)
            {
                switch (talent)
                {
                    case "Coolheaded":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.WP).OtherModifier = 5;
                        break;
                    case "Fleet Footed":
                        characterSheet.Movement += 1;
                        break;
                    case "Lightning Reflexes":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.Agi).OtherModifier = 5;
                        break;
                    case "Luck":
                        characterSheet.Fortune += 1;
                        break;
                    case "Marksman":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.BS).OtherModifier = 5;
                        break;
                    case "Nimble Fingered":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.Dex).OtherModifier = 5;
                        break;
                    case "Savvy":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.Int).OtherModifier = 5;
                        break;
                    case "Sharp":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.I).OtherModifier = 5;
                        break;
                    case "Sprinter":
                        characterSheet.Run += 1;
                        break;
                    case "Strong-minded":
                        characterSheet.Resolve += 1;
                        break;
                    case "Suave":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.Fel).OtherModifier = 5;
                        break;
                    case "Very Resilient":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.T).OtherModifier = 5;
                        break;
                    case "Very Strong":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.S).OtherModifier = 5;
                        break;
                    case "Warrior Born":
                        characterSheet.Characteristics.Find(f => f.ShortName == CharacteristicEnum.WS).OtherModifier = 5;
                        break;
                    default:
                        break;
                }
            }

            return characterSheet;
        }

        private void ImportCharacterNames()
        {
            using (var database = new LiteDatabase(characterNamesDB))
            {
                ILiteCollection<NameDTO> names = database.GetCollection<NameDTO>();
                //clear DB for fresh import
                names.DeleteAll();

                List<NameDTO> namesToAdd = new List<NameDTO>();

                NameCollection humanNames = DeserializeXMLFileToObject<NameCollection>(humanNameXMLSRC);

                foreach (var region in humanNames.NameRegions)
                {
                    foreach (var nameType in region.NameTypes)
                    {
                        foreach (var name in nameType.Names)
                        {
                            namesToAdd.Add(new NameDTO { Value = name, NameType = nameType.Value, Region = region.Value, Species = "Human" });
                        }
                    }
                }

                NameCollection dwarfNames = DeserializeXMLFileToObject<NameCollection>(dwarfNameXMLSRC);

                foreach (var region in dwarfNames.NameRegions)
                {
                    foreach (var nameType in region.NameTypes)
                    {
                        foreach (var name in nameType.Names)
                        {
                            namesToAdd.Add(new NameDTO { Value = name, NameType = nameType.Value, Region = region.Value, Species = "Dwarf" });
                        }
                    }
                }

                NameCollection halflingNames = DeserializeXMLFileToObject<NameCollection>(halflingNameXMLSRC);

                foreach (var region in halflingNames.NameRegions)
                {
                    foreach (var nameType in region.NameTypes)
                    {
                        foreach (var name in nameType.Names)
                        {
                            namesToAdd.Add(new NameDTO { Value = name, NameType = nameType.Value, Region = region.Value, Species = "Halfling" });
                        }
                    }
                }

                NameCollection woodElfNames = DeserializeXMLFileToObject<NameCollection>(woodElfNameXMLSRC);

                foreach (var region in woodElfNames.NameRegions)
                {
                    foreach (var nameType in region.NameTypes)
                    {
                        foreach (var name in nameType.Names)
                        {
                            namesToAdd.Add(new NameDTO { Value = name, NameType = nameType.Value, Region = region.Value, Species = "WoodElf" });
                        }
                    }
                }

                NameCollection highElfNames = DeserializeXMLFileToObject<NameCollection>(highElfNameXMLSRC);

                foreach (var region in highElfNames.NameRegions)
                {
                    foreach (var nameType in region.NameTypes)
                    {
                        foreach (var name in nameType.Names)
                        {
                            namesToAdd.Add(new NameDTO { Value = name, NameType = nameType.Value, Region = region.Value, Species = "HighElf" });
                        }
                    }
                }

                names.Insert(namesToAdd);
            }
        }

        private void ImportCharacterSpecies()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<SpeciesDTO> species = database.GetCollection<SpeciesDTO>();
                //clear DB for fresh import
                species.DeleteAll();

                List<SpeciesDTO> speciesToAdd = new List<SpeciesDTO>();

                SpeciesCollection speciesCollection = DeserializeXMLFileToObject<SpeciesCollection>(speciesXMLSRC);

                foreach (var speciesType in speciesCollection.SpeciesTypes)
                {
                    foreach (var region in speciesType.SpeciesRegions.Regions)
                    {
                        List<CharacteristicModifierDTO> characteristicModifiers = new List<CharacteristicModifierDTO>
                        {
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.WS, Modifier = Int32.Parse(speciesType.AttributeModifiers.WS)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.BS, Modifier = Int32.Parse(speciesType.AttributeModifiers.BS)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.S, Modifier = Int32.Parse(speciesType.AttributeModifiers.S)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.T, Modifier = Int32.Parse(speciesType.AttributeModifiers.T)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.I, Modifier = Int32.Parse(speciesType.AttributeModifiers.I)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.Agi, Modifier = Int32.Parse(speciesType.AttributeModifiers.Agi)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.Dex, Modifier = Int32.Parse(speciesType.AttributeModifiers.Dex)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.Int, Modifier = Int32.Parse(speciesType.AttributeModifiers.Int)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.WP, Modifier = Int32.Parse(speciesType.AttributeModifiers.WP)},
                            new CharacteristicModifierDTO { Characteristic = CharacteristicEnum.Fel, Modifier = Int32.Parse(speciesType.AttributeModifiers.Fel)}
                        };

                        SpeciesDTO speciesDTO = new SpeciesDTO();

                        speciesDTO.SpeciesType = speciesType.Value;
                        speciesDTO.CharacteristicsModifiers = characteristicModifiers;
                        speciesDTO.WoundModifier = new WoundModifierDTO
                        {
                            AddStrengthBonus = speciesType.Wounds.SB == "True" ? true : false,
                            ToughnessBonusMultiplier = Int32.Parse(speciesType.Wounds.TB),
                            AddWillPowerBonus = speciesType.Wounds.WPB == "True" ? true : false
                        };
                        speciesDTO.Fate = Int32.Parse(speciesType.Fate);
                        speciesDTO.Resilience = Int32.Parse(speciesType.Resilience);
                        speciesDTO.ExtraPoints = Int32.Parse(speciesType.ExtraPoints);
                        speciesDTO.Movement = Int32.Parse(speciesType.Movement);
                        speciesDTO.Region = region.RegionName;
                        speciesDTO.StartingSkills = region.StartingSkills;
                        speciesDTO.StartingTalents = region.StartingTalents;

                        speciesToAdd.Add(speciesDTO);
                    }
                }

                species.Insert(speciesToAdd);
            }
        }

        private void ImportCharacterTalents()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<TalentDTO> talents = database.GetCollection<TalentDTO>();
                //clear DB for fresh import
                talents.DeleteAll();

                List<TalentDTO> talentsToAdd = new List<TalentDTO>();

                TalentCollection talentsCollection = DeserializeXMLFileToObject<TalentCollection>(talentsXMLSRC);

                foreach (var talent in talentsCollection.Talents)
                {
                    talentsToAdd.Add(new TalentDTO { Name = talent.Name, Description = talent.Description });
                }

                talents.Insert(talentsToAdd);
            }
        }

        private void ImportCharacterCareers(string speciesToImport)
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CareerDTO> careers = database.GetCollection<CareerDTO>($"{speciesToImport}Careers");
                //clear DB for fresh import
                careers.DeleteAll();

                List<CareerDTO> careersToAdd = new List<CareerDTO>();

                CareerCollection careerTypes = DeserializeXMLFileToObject<CareerCollection>(careersXMLSRC);

                foreach (var careerSpecies in careerTypes.CareerSpecies)
                {
                    if (careerSpecies.Value == speciesToImport)
                    {
                        foreach (var odds in careerSpecies.Odds)
                        {
                            careersToAdd.Add(new CareerDTO
                            {
                                oddsLower = odds.Lower,
                                oddsUpper = odds.Upper,
                                Name = odds.Career.Name,
                                Class = odds.Career.Class,
                                Path1 = new CareerPathDTO
                                {
                                    Title = odds.Career.Path1.Title,
                                    AdvanceScheme = odds.Career.Path1.AdvanceScheme,
                                    Status = odds.Career.Path1.Status,
                                    Skills = odds.Career.Path1.Skills,
                                    Talents = odds.Career.Path1.Talents,
                                    Trappings = odds.Career.Path1.Trappings
                                },
                                Path2 = new CareerPathDTO
                                {
                                    Title = odds.Career.Path2.Title,
                                    AdvanceScheme = odds.Career.Path2.AdvanceScheme,
                                    Status = odds.Career.Path2.Status,
                                    Skills = odds.Career.Path2.Skills,
                                    Talents = odds.Career.Path2.Talents,
                                    Trappings = odds.Career.Path2.Trappings
                                },
                                Path3 = new CareerPathDTO
                                {
                                    Title = odds.Career.Path3.Title,
                                    AdvanceScheme = odds.Career.Path3.AdvanceScheme,
                                    Status = odds.Career.Path3.Status,
                                    Skills = odds.Career.Path3.Skills,
                                    Talents = odds.Career.Path3.Talents,
                                    Trappings = odds.Career.Path3.Trappings
                                },
                                Path4 = new CareerPathDTO
                                {
                                    Title = odds.Career.Path4.Title,
                                    AdvanceScheme = odds.Career.Path4.AdvanceScheme,
                                    Status = odds.Career.Path4.Status,
                                    Skills = odds.Career.Path4.Skills,
                                    Talents = odds.Career.Path4.Talents,
                                    Trappings = odds.Career.Path4.Trappings
                                }
                            });
                        }
                    }
                }

                careers.Insert(careersToAdd);
            }
        }

        private void ClearCharacteristics()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CharacteristicDTO> characteristicDTOs = database.GetCollection<CharacteristicDTO>();

                characteristicDTOs.DeleteAll();
            }
        }

        private void SetupCharacteristics()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<CharacteristicDTO> characteristicDTOs = database.GetCollection<CharacteristicDTO>();

                string[] characteristicsArray = new string[] {
                "WS",
                "BS",
                "S",
                "T",
                "I",
                "Agi",
                "Dex",
                "Int",
                "WP",
                "Fel"
                };

                for (int i = 0; i < characteristicsArray.Length; i++)
                {
                    string cShorthand = characteristicsArray[i];
                    if (characteristicDTOs.Exists(c => c.ShortName == cShorthand))
                    {
                        //this characteristic has already been entered
                        continue;
                    }
                    else
                    {
                        CharacteristicDTO newCharacteristicDTO = new CharacteristicDTO();
                        newCharacteristicDTO.ShortName = cShorthand;

                        switch (newCharacteristicDTO.ShortName)
                        {
                            case "WS":
                                newCharacteristicDTO.Name = "Weapon Skill";
                                break;
                            case "BS":
                                newCharacteristicDTO.Name = "Ballistic Skill";
                                break;
                            case "S":
                                newCharacteristicDTO.Name = "Strength";
                                break;
                            case "T":
                                newCharacteristicDTO.Name = "Toughness";
                                break;
                            case "I":
                                newCharacteristicDTO.Name = "Initiative";
                                break;
                            case "Agi":
                                newCharacteristicDTO.Name = "Agility";
                                break;
                            case "Dex":
                                newCharacteristicDTO.Name = "Dexterity";
                                break;
                            case "Int":
                                newCharacteristicDTO.Name = "Intelligence";
                                break;
                            case "WP":
                                newCharacteristicDTO.Name = "Will Power";
                                break;
                            case "Fel":
                                newCharacteristicDTO.Name = "Fellowship";
                                break;
                            default:
                                break;
                        }

                        characteristicDTOs.Insert(newCharacteristicDTO);
                    }
                }
            }
        }



        private static List<string> SeparateAndFormatCSV(string csvContents)
        {
            var strings = csvContents.Split(',');

            List<string> result = new List<string>();

            foreach (var s in strings)
            {
                result.Add(AddSpacesToSentence(s));
            }

            return result;
        }

        private static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ' && text[i - 1] != '(' && text[i - 1] != '/')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        private static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(random.Next(v.Length));
        }

        private static T DeserializeXMLFileToObject<T>(string XmlFilename)
        {
            T returnObject = default(T);
            if (string.IsNullOrEmpty(XmlFilename)) return default(T);

            try
            {
                StreamReader xmlStream = new StreamReader(XmlFilename);
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                returnObject = (T)serializer.Deserialize(xmlStream);
            }
            catch (Exception ex)
            {
                //handle exceptions!
            }
            return returnObject;
        }
    }
}
