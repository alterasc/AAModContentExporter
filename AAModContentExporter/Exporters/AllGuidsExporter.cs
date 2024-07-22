using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;

internal static class AllGuidsExporter
{
    internal static void WriteAllNewGuids(List<BlueprintData> analyzed, TTTStyleBlueprints blueprintsJson = null)
    {
        var allGuidsPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}AllBlueprints.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# All mod blueprints");
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine("| Guid | Internal name | Display name |");
        sb.AppendLine("| --- | --- | --- |");
        var ordered = analyzed.OrderBy(x => (x.InternalName, x.Guid)).ToList();
        foreach (var newGuid in ordered)
        {
            sb.AppendLine($"| `{newGuid.Guid}` | {newGuid.InternalName} | {newGuid.DisplayName} |");
        }
        List<BlueprintGuid> add = [];
        if (blueprintsJson != null)
        {
            var itr = blueprintsJson.NewBlueprints
                .Where(x => !Exporter.ModBlueprints.ContainsKey(new BlueprintGuid(new Guid(x.Value))));
            foreach (var blueprint in itr)
            {
                var guid = new BlueprintGuid(new Guid(blueprint.Value));
                add.Add(guid);
                sb.AppendLine($"| `{guid}` | {blueprint.Key} | |");
            }
            var itr2 = blueprintsJson.DerivedBlueprintMasters
                .Where(x => !Exporter.ModBlueprints.ContainsKey(new BlueprintGuid(new Guid(x.Value))));
            foreach (var blueprint in itr2)
            {
                var guid = new BlueprintGuid(new Guid(blueprint.Value));
                add.Add(guid);
                sb.AppendLine($"| `{guid}` | {blueprint.Key} | |");
            }
            var itr3 = blueprintsJson.DerivedBlueprints
                .Where(x => !Exporter.ModBlueprints.ContainsKey(new BlueprintGuid(new Guid(x.Value))));
            foreach (var blueprint in itr3)
            {
                var guid = new BlueprintGuid(new Guid(blueprint.Value));
                add.Add(guid);
                sb.AppendLine($"| `{guid}` | {blueprint.Key} | |");
            }
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(allGuidsPath, sb.ToString());

        var binPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}blueprintguids.bin";
        var arr = ordered.Select(x => x.Guid).Concat(add).ToArray();
        GuidSerialization.SerializeGuids(arr, binPath);
    }
}