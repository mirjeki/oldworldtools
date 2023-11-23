namespace OldWorldTools.Models.WFRPCharacter.DTOs
{
    public class CareerDTO
    {
        public int oddsLower { get; set; }
        public int oddsUpper { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public CareerPathDTO Path1 { get; set; }
        public CareerPathDTO Path2 { get; set; }
        public CareerPathDTO Path3 { get; set; }
        public CareerPathDTO Path4 { get; set; }
    }

    public class CareerPathDTO
    {
        public string Title { get; set; }
        public string AdvanceScheme { get; set; }
        public string Status { get; set; }
        public string Skills { get; set; }
        public string Talents { get; set; }
        public string Trappings { get; set; }
    }
}
