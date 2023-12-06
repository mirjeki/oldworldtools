using OldWorldTools.Models.WFRPCharacter.DTOs;

namespace OldWorldTools.Models.WFRPCharacter
{
    public class CharacterSkill
    {
        public SkillDTO Skill { get; set; }
        public int CharacteristicValue { get; set; }
        public int Advances { get; set; }
        public int CurrentTotal()
        {
            return CharacteristicValue + Advances;
        }
    }
}
