namespace OldWorldTools.Models.WFRPCharacter
{
    public class Characteristic
    {
        public string Name { get; set; }
        public CharacteristicEnum ShortName { get; set; }
        public int Rolled { get; set; }
        public int Initial { get; set; }
        public int SpeciesModifier { get; set; }
        public int OtherModifier { get; set; }
        public int Advances { get; set; }

        public int CurrentValue()
        {
            return Initial + Advances;
        }
    }
}
