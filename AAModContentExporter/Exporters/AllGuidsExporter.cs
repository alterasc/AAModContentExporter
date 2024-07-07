using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;

internal static class AllGuidsExporter
{
    internal static void WriteAllNewGuids(List<BlueprintData> analyzed)
    {
        var allGuidsPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}AllBlueprints.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine("# All mod blueprints");
        sb.AppendLine();
        sb.AppendLine("| Guid | Internal name | Display name |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var newGuid in analyzed.OrderBy(x => (x.InternalName, x.Guid)))
        {
            sb.AppendLine($"| `{newGuid.Guid}` | {newGuid.InternalName} | {newGuid.DisplayName} |");
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(allGuidsPath, sb.ToString());
    }
}