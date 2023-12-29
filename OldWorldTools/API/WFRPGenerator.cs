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
using System.Linq;
using iText.StyledXmlParser.Jsoup.Parser;

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
        public const string skillsXMLSRC = "Resources/Skills/Skills.xml";
        public const string talentsXMLSRC = "Resources/Talents/Talents.xml";
        public const string trappingsXMLSRC = "Resources/Trappings/Trappings.xml";
        public const string motivationsXMLSRC = "Resources/Motivations/Motivations.xml";
        public const string shortTermAmbitionsXMLSRC = "Resources/Ambitions/ShortTermAmbitions.xml";
        public const string longTermAmbitionsXMLSRC = "Resources/Ambitions/LongTermAmbitions.xml";
        public const string careersXMLSRC = "Resources/Careers/Careers.xml";

        public const string characterDefinitionsDB = "Data/WFRPCharacterDefinitions.db";
        public const string characterFluffDB = "Data/WFRPCharacterFluff.db";
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
            ImportCharacterSkills();
            ImportCharacterTalents();
            ImportCharacterTrappings();
            ImportCharacterMotivations();
            ImportCharacterShortTermAmbitions();
            ImportCharacterLongTermAmbitions();

            SetupCharacteristics();
        }

        public CharacterSheet GetDefaultCharacter()
        {
            CharacterSheet characterSheet = new CharacterSheet
            {
                Name = "Herr Kartoffeln",
                Gender = GenderEnum.Male,
                Species = SpeciesEnum.Human,
                Region = RegionEnum.Reikland,
                RegionsAvailable = GetAvailableRegions(SpeciesEnum.Human),
                SpeciesSkills = new List<CharacterSkill>(),
                CareerSkills = new List<CharacterSkill>()
            };

            characterSheet.Characteristics = GetCharacteristics();
            characterSheet.Motivation = RandomiseMotivation();
            characterSheet.ShortTermAmbition = RandomiseShortTermAmbition();
            characterSheet.LongTermAmbition = RandomiseLongTermAmbition();

            characterSheet.Characteristics = RandomiseCharacteristics(characterSheet);
            characterSheet = MapCareerToCharacterSheet(RandomiseCareer(SpeciesEnum.Human), characterSheet, TierEnum.Tier1);
            characterSheet = MapSpeciesSkillsToCharacterSheet(characterSheet);
            var currentCareer = GetCareerByName(characterSheet.Career, characterSheet.Species);
            characterSheet = MapCareerSkillsToCharacterSheet(currentCareer, characterSheet, TierEnum.Tier1);

            return characterSheet;
        }

        public string RandomiseMotivation()
        {
            var motivations = GetMotivations();

            return motivations[random.Next(0, motivations.Count)].Name;
        }

        public string RandomiseShortTermAmbition()
        {
            var ambitions = GetShortTermAmbitions();

            return ambitions[random.Next(0, ambitions.Count)].Description;
        }

        public string RandomiseLongTermAmbition()
        {
            var ambitions = GetLongTermAmbitions();

            return ambitions[random.Next(0, ambitions.Count)].Description;
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
            return HelperMethods.RandomEnumValue<GenderEnum>();
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

        public CharacterSheet MapSpeciesSkillsToCharacterSheet(CharacterSheet characterSheet)
        {
            characterSheet.SpeciesSkills = new List<CharacterSkill>();
            var allSkills = GetSkills();

            var speciesModifiers = GetSpeciesModifiersByRegion(characterSheet.Region);

            characterSheet.Movement = speciesModifiers.Movement;
            characterSheet.Walk = characterSheet.Movement * 2;
            characterSheet.Run = characterSheet.Movement * 4;

            characterSheet.Fate = speciesModifiers.Fate;
            characterSheet.Fortune = characterSheet.Fate;

            characterSheet.Resilience = speciesModifiers.Resilience;
            characterSheet.Resolve = characterSheet.Resilience;

            var speciesSkillsRaw = HelperMethods.SeparateAndFormatCSV(speciesModifiers.StartingSkills);

            List<SkillDTO> skillsAvailable = new List<SkillDTO>();

            foreach (var skillName in speciesSkillsRaw)
            {
                var skillNameParts = skillName.Split(' ');

                SkillDTO matchingSkill = allSkills.Where(w => w.Name.Contains(skillNameParts[0])).FirstOrDefault();

                if (matchingSkill != null)
                {
                    SkillDTO skillToAdd = new SkillDTO
                    {
                        Name = skillName,
                        Advanced = matchingSkill.Advanced,
                        Grouped = matchingSkill.Grouped,
                        LinkedCharacteristic = matchingSkill.LinkedCharacteristic
                    };

                    skillsAvailable.Add(skillToAdd);
                }
            }

            ////As there are always more than 6 skills available for each starting species, this shouldn't go outside the range of the index

            //add 3 skills at 5 adv
            for (int i = 0; i < 3; i++)
            {
                int randomNum = random.Next(skillsAvailable.Count);

                characterSheet.SpeciesSkills.Add
                    (new CharacterSkill
                    {
                        Advances = 5,
                        Skill = skillsAvailable[randomNum],
                        CharacteristicValue = characterSheet.Characteristics.Where(w => w.ShortName == skillsAvailable[randomNum].LinkedCharacteristic).First().CurrentValue()
                    });

                skillsAvailable.RemoveAt(randomNum);
            }

            //add 3 skills at 3 adv
            for (int i = 0; i < 3; i++)
            {
                int randomNum = random.Next(skillsAvailable.Count);
                characterSheet.SpeciesSkills.Add
                    (new CharacterSkill
                    {
                        Advances = 3,
                        Skill = skillsAvailable[randomNum],
                        CharacteristicValue = characterSheet.Characteristics.Where(w => w.ShortName == skillsAvailable[randomNum].LinkedCharacteristic).First().CurrentValue()
                    });

                skillsAvailable.RemoveAt(randomNum);
            }

            return characterSheet;
        }

        public CharacterSheet MapCareerSkillsToCharacterSheet(CareerDTO career, CharacterSheet characterSheet, TierEnum tier)
        {
            switch (tier)
            {
                case TierEnum.Tier1:
                    return DetermineFirstLevelCareerSkills(career, characterSheet);
                case TierEnum.Tier2:
                    break;
                case TierEnum.Tier3:
                    break;
                case TierEnum.Tier4:
                    break;
                default:
                    break;
            }

            return DetermineFirstLevelCareerSkills(career, characterSheet);
        }

        private CharacterSheet DetermineFirstLevelCareerSkills(CareerDTO career, CharacterSheet characterSheet)
        {
            int skillPoints = 40;

            characterSheet.CareerSkills = new List<CharacterSkill>();
            var allSkills = GetSkills();

            var careerSkillsRaw = HelperMethods.SeparateAndFormatCSV(career.Path1.Skills);

            List<SkillDTO> skillsAvailable = new List<SkillDTO>();

            foreach (var skillName in careerSkillsRaw)
            {
                var skillNameParts = skillName.Split(' ');

                SkillDTO matchingSkill = allSkills.Where(w => w.Name.Contains(skillNameParts[0])).FirstOrDefault();

                if (matchingSkill != null)
                {
                    SkillDTO skillToAdd = new SkillDTO
                    {
                        Name = skillName,
                        Advanced = matchingSkill.Advanced,
                        Grouped = matchingSkill.Grouped,
                        LinkedCharacteristic = matchingSkill.LinkedCharacteristic
                    };

                    skillsAvailable.Add(skillToAdd);
                }
            }

            while (skillPoints > 0)
            {
                int randomSkill = random.Next(skillsAvailable.Count);

                int randomSkillPointAllocation;

                if (skillsAvailable.Count == 1)
                {
                    randomSkillPointAllocation = skillPoints;
                }
                else if (skillPoints > 10)
                {
                    randomSkillPointAllocation = random.Next(5, 10);
                }
                else
                {
                    randomSkillPointAllocation = random.Next(1, skillPoints);
                }

                var skill = skillsAvailable[randomSkill];

                var matchingSpeciesSkill = characterSheet.SpeciesSkills.Where(w => w.Skill.Name == skill.Name).FirstOrDefault();

                if (matchingSpeciesSkill != null)
                {
                    characterSheet.SpeciesSkills.Where(f => f.Skill == matchingSpeciesSkill.Skill).First().Advances += randomSkillPointAllocation;
                }
                else
                {
                    characterSheet.CareerSkills.Add
                        (new CharacterSkill
                        {
                            Advances = randomSkillPointAllocation,
                            Skill = skill,
                            CharacteristicValue = characterSheet.Characteristics.Where(w => w.ShortName == skill.LinkedCharacteristic).First().CurrentValue()
                        });
                }

                skillsAvailable.RemoveAt(randomSkill);
                skillPoints -= randomSkillPointAllocation;
            }

            return characterSheet;
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
                    characterSheet.Talents = RandomiseStarterTalents(characterSheet.Region, HelperMethods.SeparateAndFormatCSV(career.Path1.Talents));
                    characterSheet.Trappings = DetermineChoices(HelperMethods.SeparateAndFormatCSV(career.Path1.Trappings));
                    break;
                case TierEnum.Tier2:
                    break;
                case TierEnum.Tier3:
                    characterSheet.CareerPath = career.Path3.Title;
                    characterSheet.Status = career.Path3.Status;
                    //randomly determine chosen talents from list
                    characterSheet.Talents = HelperMethods.SeparateAndFormatCSV(career.Path3.Talents);
                    characterSheet.Trappings = DetermineChoices(HelperMethods.SeparateAndFormatCSV(career.Path3.Trappings));
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

        private List<string> DetermineChoices(List<string> list)
        {
            List<string> itemsToReturn = new List<string>();

            foreach (var item in list)
            {
                if (item.Contains("?"))
                {
                    var options = item.Split('?', StringSplitOptions.TrimEntries);

                    int randomOption = random.Next(options.Length);

                    itemsToReturn.Add(options[randomOption]);
                }
                else
                {
                    itemsToReturn.Add(item);
                }
            }

            return itemsToReturn;
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

            talentsToReturn = HelperMethods.SeparateAndFormatCSV(speciesTalents);

            talentsToReturn.Add(GetUniquePathTalent(talentsToReturn, pathTalents));

            List<string> talentsToRemove = new List<string>();
            List<string> talentsToAdd = new List<string>();

            foreach (var talent in talentsToReturn)
            {
                if (talent.Contains("?"))
                {
                    var options = talent.Split('?', StringSplitOptions.TrimEntries);

                    int randomOption = random.Next(options.Length);

                    talentsToAdd.Add(options[randomOption]);

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

        public List<SkillDTO> GetSkills()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<SkillDTO> skillDTOs = database.GetCollection<SkillDTO>();

                return skillDTOs.FindAll().ToList();
            }
        }

        public List<TrappingDTO> GetTrappings()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<TrappingDTO> trappingDTOs = database.GetCollection<TrappingDTO>();

                return trappingDTOs.FindAll().ToList();
            }
        }

        public List<MotivationDTO> GetMotivations()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<MotivationDTO> motivations = database.GetCollection<MotivationDTO>();

                return motivations.FindAll().ToList();
            }
        }

        public List<AmbitionDTO> GetShortTermAmbitions()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<AmbitionDTO> ambitions = database.GetCollection<AmbitionDTO>("ShortTermAmbitions");

                return ambitions.FindAll().ToList();
            }
        }

        public List<AmbitionDTO> GetLongTermAmbitions()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<AmbitionDTO> ambitions = database.GetCollection<AmbitionDTO>("LongTermAmbitions");

                return ambitions.FindAll().ToList();
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

                NameCollection humanNames = HelperMethods.DeserializeXMLFileToObject<NameCollection>(humanNameXMLSRC);

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

                NameCollection dwarfNames = HelperMethods.DeserializeXMLFileToObject<NameCollection>(dwarfNameXMLSRC);

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

                NameCollection halflingNames = HelperMethods.DeserializeXMLFileToObject<NameCollection>(halflingNameXMLSRC);

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

                NameCollection woodElfNames = HelperMethods.DeserializeXMLFileToObject<NameCollection>(woodElfNameXMLSRC);

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

                NameCollection highElfNames = HelperMethods.DeserializeXMLFileToObject<NameCollection>(highElfNameXMLSRC);

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

                SpeciesCollection speciesCollection = HelperMethods.DeserializeXMLFileToObject<SpeciesCollection>(speciesXMLSRC);

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

        private void ImportCharacterSkills()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<SkillDTO> skills = database.GetCollection<SkillDTO>();
                //clear DB for fresh import
                skills.DeleteAll();

                List<SkillDTO> skillsToAdd = new List<SkillDTO>();

                SkillCollection skillsCollection = HelperMethods.DeserializeXMLFileToObject<SkillCollection>(skillsXMLSRC);

                foreach (var skill in skillsCollection.Skills)
                {
                    List<string> tags = HelperMethods.SeparateCSV(skill.Tags);

                    CharacteristicEnum linkedCharacteristic;

                    switch (tags[0])
                    {
                        case "WS":
                            linkedCharacteristic = CharacteristicEnum.WS;
                            break;
                        case "BS":
                            linkedCharacteristic = CharacteristicEnum.BS;
                            break;
                        case "S":
                            linkedCharacteristic = CharacteristicEnum.S;
                            break;
                        case "T":
                            linkedCharacteristic = CharacteristicEnum.T;
                            break;
                        case "I":
                            linkedCharacteristic = CharacteristicEnum.I;
                            break;
                        case "Agi":
                            linkedCharacteristic = CharacteristicEnum.Agi;
                            break;
                        case "Dex":
                            linkedCharacteristic = CharacteristicEnum.Dex;
                            break;
                        case "Int":
                            linkedCharacteristic = CharacteristicEnum.Int;
                            break;
                        case "WP":
                            linkedCharacteristic = CharacteristicEnum.WP;
                            break;
                        case "Fel":
                            linkedCharacteristic = CharacteristicEnum.Fel;
                            break;
                        default:
                            linkedCharacteristic = CharacteristicEnum.WS;
                            break;
                    }

                    bool isAdvanced = tags[1] == "A" ? true : false;
                    bool isGrouped = false;

                    if (tags.Count == 3)
                    {
                        isGrouped = tags[2] == "G" ? true : false;
                    }


                    skillsToAdd.Add(new SkillDTO { Name = skill.Name, LinkedCharacteristic = linkedCharacteristic, Advanced = isAdvanced, Grouped = isGrouped });
                }

                skills.Insert(skillsToAdd);
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

                TalentCollection talentsCollection = HelperMethods.DeserializeXMLFileToObject<TalentCollection>(talentsXMLSRC);

                foreach (var talent in talentsCollection.Talents)
                {
                    talentsToAdd.Add(new TalentDTO { Name = talent.Name, Description = talent.Description });
                }

                talents.Insert(talentsToAdd);
            }
        }

        private void ImportCharacterTrappings()
        {
            using (var database = new LiteDatabase(characterDefinitionsDB))
            {
                ILiteCollection<TrappingDTO> trappings = database.GetCollection<TrappingDTO>();
                //clear DB for fresh import
                trappings.DeleteAll();

                List<TrappingDTO> trappingsToAdd = new List<TrappingDTO>();

                TrappingsCollection trappingsCollection = HelperMethods.DeserializeXMLFileToObject<TrappingsCollection>(trappingsXMLSRC);

                foreach (var trapping in trappingsCollection.Trappings)
                {
                    trappingsToAdd.Add(new TrappingDTO { Name = trapping.Name, Enc = trapping.Enc, Properties = trapping.Properties });
                }

                trappings.Insert(trappingsToAdd);
            }
        }

        private void ImportCharacterMotivations()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<MotivationDTO> motivations = database.GetCollection<MotivationDTO>();
                //clear DB for fresh import
                motivations.DeleteAll();

                List<MotivationDTO> motivationsToAdd = new List<MotivationDTO>();

                MotivationCollection motivationsCollection = HelperMethods.DeserializeXMLFileToObject<MotivationCollection>(motivationsXMLSRC);

                foreach (var motivation in motivationsCollection.Motivations)
                {
                    motivationsToAdd.Add(new MotivationDTO { Name = motivation });
                }

                motivations.Insert(motivationsToAdd);
            }
        }

        private void ImportCharacterShortTermAmbitions()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<AmbitionDTO> ambitions = database.GetCollection<AmbitionDTO>("ShortTermAmbitions");
                //clear DB for fresh import
                ambitions.DeleteAll();

                List<AmbitionDTO> ambitionsToAdd = new List<AmbitionDTO>();

                AmbitionCollection ambitionsCollection = HelperMethods.DeserializeXMLFileToObject<AmbitionCollection>(shortTermAmbitionsXMLSRC);

                foreach (var ambition in ambitionsCollection.Ambitions)
                {
                    ambitionsToAdd.Add(new AmbitionDTO { Description = ambition });
                }

                ambitions.Insert(ambitionsToAdd);
            }
        }

        private void ImportCharacterLongTermAmbitions()
        {
            using (var database = new LiteDatabase(characterFluffDB))
            {
                ILiteCollection<AmbitionDTO> ambitions = database.GetCollection<AmbitionDTO>("LongTermAmbitions");
                //clear DB for fresh import
                ambitions.DeleteAll();

                List<AmbitionDTO> ambitionsToAdd = new List<AmbitionDTO>();

                AmbitionCollection ambitionsCollection = HelperMethods.DeserializeXMLFileToObject<AmbitionCollection>(longTermAmbitionsXMLSRC);

                foreach (var ambition in ambitionsCollection.Ambitions)
                {
                    ambitionsToAdd.Add(new AmbitionDTO { Description = ambition });
                }

                ambitions.Insert(ambitionsToAdd);
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

                CareerCollection careerTypes = HelperMethods.DeserializeXMLFileToObject<CareerCollection>(careersXMLSRC);

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
    }
}
