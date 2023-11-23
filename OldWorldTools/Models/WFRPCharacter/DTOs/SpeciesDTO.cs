namespace OldWorldTools.Models.WFRPCharacter.DTOs
{
    public class SpeciesDTO
    {
        public string SpeciesType { get; set; }
        public string Region { get; set; }
        public List<CharacteristicModifierDTO> CharacteristicsModifiers { get; set; }
        public WoundModifierDTO WoundModifier { get; set; }

        public int Fate { get; set; }
        public int Resilience { get; set; }
        public int ExtraPoints { get; set; }
        public int Movement { get; set; }
        public string StartingSkills { get; set; }
        public string StartingTalents { get; set; }
    }

    public class CharacteristicModifierDTO
    {
        public CharacteristicEnum Characteristic { get; set; }
        public int Modifier { get; set; }
    }

    public class WoundModifierDTO
    {
        public bool AddStrengthBonus { get; set; }
        public int ToughnessBonusMultiplier { get; set; }
        public bool AddWillPowerBonus { get; set; }
    }
}
