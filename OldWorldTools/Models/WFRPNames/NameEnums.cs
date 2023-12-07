using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OldWorldTools.Models.WFRPNames
{
    public enum RegionEnum
    {
        Reikland,
        Middenland,
        Karak,
        Imperial,
        Moot,
        Asur,
        Asrai,
        Eonir
    }

    //descriptions necessary for spaces on PDF export
    public enum SpeciesEnum
    {
        [Description("Human")]
        Human,
        [Description("Dwarf")]
        Dwarf,
        [Description("Halfling")]
        Halfling,
        [Description("High Elf")]
        HighElf,
        [Description("Wood Elf")]
        WoodElf
    }

    public enum GenderEnum
    {
        Male,
        Female
    }

    public enum NameTypeEnum
    {
        Forename,
        Surname
    }
}
