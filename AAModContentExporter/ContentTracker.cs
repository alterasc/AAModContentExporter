using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter;
[HarmonyPatch]
internal class ContentTracker
{
    public static Dictionary<BlueprintGuid, SimpleBlueprint> modBlueprints = new Dictionary<BlueprintGuid, SimpleBlueprint>();

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    public static void Record(BlueprintGuid guid, SimpleBlueprint bp)
    {
        modBlueprints[guid] = bp;
    }

    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Export()
    {
        BlueprintAnalyzer.Init();
        var analyzed = modBlueprints.Values.Select(x => BlueprintAnalyzer.Analyze(x)).ToList();

        WriteAllNewGuids(analyzed);

        WriteAllFeats(analyzed);

        WriteAllSpells(analyzed);
    }

    private static void WriteAllSpells(List<BlueprintData> analyzed)
    {
        var userPath = $"{Main.ExportOutput}{Path.DirectorySeparatorChar}Spells.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("| Guid | Name | Spellbooks |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var spell in analyzed.Where(x => x.IsSpell))
        {
            var books = string.Join(", ", spell.SpellEntries);
            sb.AppendLine($"| {spell.Guid} | {spell.LocalizedName} | {books} |");
        }
        File.WriteAllText(userPath, sb.ToString());
    }

    private static void WriteAllFeats(List<BlueprintData> analyzed)
    {
        var userPath = $"{Main.ExportOutput}{Path.DirectorySeparatorChar}Feats.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("| Guid | Name |");
        sb.AppendLine("| --- | --- |");

        foreach (var feat in analyzed.Where(x => x.IsFeat))
        {
            sb.AppendLine($"| {feat.Guid} | {feat.LocalizedName} |");
        }
        File.WriteAllText(userPath, sb.ToString());
    }

    private static void WriteAllNewGuids(List<BlueprintData> analyzed)
    {
        var allGuidsPath = $"{Main.ExportOutput}{Path.DirectorySeparatorChar}AllGuids.txt";
        StringBuilder allGuidsSb = new StringBuilder();
        foreach (var newGuid in analyzed)
        {
            allGuidsSb.Append($"{newGuid.Guid} - {newGuid.InternalName}");
        }
        File.WriteAllText(allGuidsPath, allGuidsSb.ToString());
    }
}
