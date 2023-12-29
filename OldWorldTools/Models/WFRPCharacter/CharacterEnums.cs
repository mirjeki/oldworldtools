using System.ComponentModel;

namespace OldWorldTools.Models.WFRPCharacter
{
    public enum CharacteristicEnum
    {
        WS,
        BS,
        S,
        T,
        I,
        Agi,
        Dex,
        Int,
        WP,
        Fel
    }
    public enum TierEnum
    {
        [Description("Tier 1")]
        Tier1,
        [Description("Tier 2")]
        Tier2,
        [Description("Tier 3")]
        Tier3,
        [Description("Tier 4")]
        Tier4
    }
}
