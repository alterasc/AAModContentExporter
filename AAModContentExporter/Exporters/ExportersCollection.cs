using System.Collections.Generic;
using System.Linq;

namespace AAModContentExporter.Exporters;
internal static class ExportersCollection
{
    internal static int WriteAllBloodlines(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsSorcererBloodline).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("SorcererBloodlines", "Sorcerer Bloodlines", list);
    }

    internal static int WriteAllDomains(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsDomain).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("Domains", "Domains", list);
    }

    internal static int WriteAllFeats(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsFeat).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("Feats", "Feats", list);
    }

    internal static int WriteAllItems(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsItem).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("Items", "Items", list);
    }

    internal static int WriteAllMythicAbilities(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsMythicAbility).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("MythicAbilities", "Mythic Abilities", list);
    }

    internal static int WriteAllMythicFeats(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsMythicFeat).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("MythicFeats", "Mythic Feats", list);
    }

    internal static int WriteAllRagePowers(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsRagePower).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("RagePowers", "Rage Powers", list);
    }

    internal static int WriteAllRogueTalents(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsRogueTalent).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("RogueTalents", "Rogue Talents", list);
    }

    internal static int WriteAllSlayerTalents(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsSlayerTalent).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("SlayerTalents", "Slayer Talents", list);
    }

    internal static int WriteAllKiPowers(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsKiPower).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("KiPowers", "Ki Powers", list);
    }

    internal static int WriteAllOracleMysteries(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsOracleMystery).OrderBy(x => x.DisplayName).ToList();
        return ExporterUtils.WriteNameDescriptionList("OracleMysteries", "Oracle Mysteries", list);
    }
}
