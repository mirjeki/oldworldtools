using LiteDB;
using OldWorldTools.Models.WFRPNames.DTOs;
using OldWorldTools.Models.WFRPNames;
using System.Xml.Serialization;
using OldWorldTools.Models.WFRPCharacter;
using OldWorldTools.Models.WFRPCharacter.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OldWorldTools.API
{
    public class WFRPGenerator
    {
        public const String humanNameXMLSRC = "Resources/Names/HumanNames.xml";
        public const String dwarfNameXMLSRC = "Resources/Names/DwarfNames.xml";
        public const String halflingNameXMLSRC = "Resources/Names/HalflingNames.xml";
        public const String woodElfNameXMLSRC = "Resources/Names/WoodElfNames.xml";
        public const String highElfNameXMLSRC = "Resources/Names/HighElfNames.xml";
        public const String speciesXMLSRC = "Resources/Species/Species.xml";
        static Random random = new Random();

        public void SetupData()
        {
            ImportCharacterNames();
            ImportCharacterSpecies();
            SetupCharacteristics();
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
            //return RandomEnumValue<SpeciesEnum>();
        }

        private int RollD100()
        {
            return random.Next(1, 100);
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

            //IEnumerable<SelectListItem> regionsAvailable = GetRegions(species).
            //    Select(s => new SelectListItem
            //    {
            //        Value = s.Region,
            //        Text = s.Region
            //    });

            return regionsAvailable;
        }

        public RegionEnum RandomiseRegion(Dictionary<RegionEnum, string> regionsAvailable)
        {
            int randomResult = random.Next(regionsAvailable.Count());

            var result = regionsAvailable.ToList()[randomResult];

            //var number = (int)((RegionEnum)Enum.Parse(typeof(RegionEnum), result.Value));

            return result.Key;
        }

        private List<SpeciesDTO> GetRegions(SpeciesEnum species)
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
            {
                ILiteCollection<SpeciesDTO> speciesDTOs = database.GetCollection<SpeciesDTO>();

                var allSpecies = speciesDTOs.FindAll().ToList();

                return speciesDTOs.FindAll().Where(w => w.SpeciesType == species.ToString()).ToList();
            }
        }

        private string GetRandomCharacterName(GenderEnum gender, RegionEnum region, SpeciesEnum species, NameTypeEnum nameType)
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterNames.db"))
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

        private void ImportCharacterNames()
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterNames.db"))
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
            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
            {
                ILiteCollection<SpeciesDTO> species = database.GetCollection<SpeciesDTO>();
                //clear DB for fresh import
                species.DeleteAll();

                List<SpeciesDTO> speciesToAdd = new List<SpeciesDTO>();

                SpeciesCollection speciesTypes = DeserializeXMLFileToObject<SpeciesCollection>(speciesXMLSRC);

                foreach (var speciesType in speciesTypes.SpeciesTypes)
                {
                    foreach (var region in speciesType.Regions)
                    {
                        speciesToAdd.Add(new SpeciesDTO { SpeciesType = speciesType.Value, Region = region});
                    }
                }

                species.Insert(speciesToAdd);
            }
        }

        private void ClearCharacteristics()
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
            {
                ILiteCollection<CharacteristicDTO> characteristicDTOs = database.GetCollection<CharacteristicDTO>();

                characteristicDTOs.DeleteAll();
            }
        }

        private void SetupCharacteristics()
        {
            using (var database = new LiteDatabase("Data/WFRPCharacterDefinitions.db"))
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
