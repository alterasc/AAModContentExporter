using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AAModContentExporter;
internal static class AggregatedTotalsMaker
{
    internal static void RecreateTotals(ScanMetaData scanMetaData)
    {
        RecreateGenericTotal(scanMetaData, "Classes", "New classes");
        RecreateGenericTotal(scanMetaData, "Archetypes", "New archetypes for base classes");
        RecreateGenericTotal(scanMetaData, "Races", "New races");
        RecreateGenericTotal(scanMetaData, "Feats", "New feats");
        RecreateGenericTotal(scanMetaData, "Spells", "New spells");
        RecreateGenericTotal(scanMetaData, "MythicFeats", "New mythic feats");
        RecreateGenericTotal(scanMetaData, "MythicAbilities", "New mythic abilities");
        RecreateGenericTotal(scanMetaData, "Domains", "New domains");
        RecreateGenericTotal(scanMetaData, "RacialHeritages", "New racial heritages");
        RecreateGenericTotal(scanMetaData, "SorcererBloodlines", "New sorcerer Bloodlines");
        RecreateGenericTotal(scanMetaData, "OracleMysteries", "New oracle mysteries");
        RecreateGenericTotal(scanMetaData, "RagePowers", "New rage powers");
        RecreateGenericTotal(scanMetaData, "RogueTalents", "New rogue talents");
        RecreateGenericTotal(scanMetaData, "SlayerTalents", "New slayer talents");
        RecreateGenericTotal(scanMetaData, "KiPowers", "New ki powers");
        RecreateGenericTotal(scanMetaData, "Items", "New items");
    }

    internal static void RecreateGenericTotal(ScanMetaData scanMetaData, string name, string title)
    {
        List<string> start = [
            $"# {title}",
            string.Empty,
            "### [Back to site homepage](./README.md)",
            string.Empty
            ];
        var allLines = scanMetaData.ProcessedMods
            .SelectMany(x => ReadGenericModExport(x, name));
        List<string> end = [
            string.Empty,
            "### [Back to site homepage](./README.md)"
            ];
        var result = start.Concat(allLines).Concat(end);
        File.WriteAllLines(AggregatedPath(name), result);
    }

    internal static IEnumerable<string> ReadGenericModExport(ProcessedMod mod, string category)
    {
        var filePath = ModPath(mod.ModId, category);
        if (!File.Exists(filePath))
        {
            yield break;
        }
        var allLines = File.ReadAllLines(filePath);
        yield return $"## {mod.DisplayName}";
        for (int i = 3; i < allLines.Length - 2; i++)
        {
            var line = allLines[i];
            if (line.StartsWith("#"))
            {
                yield return "#" + line;
            }
            else
            {
                yield return line;
            }
        }
    }

    internal static string ModPath(string modId, string fileName)
    {
        return $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{modId}{Path.DirectorySeparatorChar}{fileName}.md";
    }

    internal static string AggregatedPath(string fileName)
    {
        return $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{fileName}.md";
    }
}

