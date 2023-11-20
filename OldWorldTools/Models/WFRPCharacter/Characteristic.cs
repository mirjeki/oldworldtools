namespace OldWorldTools.Models.WFRPCharacter
{
    public class Characteristic
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int Initial { get; set; }
        public int Advances { get; set; }

        public int CurrentValue()
        {
            return Initial + Advances;
        }
    }
}
