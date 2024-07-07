using Kingmaker.Blueprints.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;
internal static class ArchetypeExporter
{
    internal static int WriteAllArchetypes(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsArchetypeForBaseClass).OrderBy(x => (x.Parent, x.DisplayName)).ToList();
        if (list.Count == 0)
        {
            return 0;
        }
        var userPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}Archetypes.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine("# Archetypes");
        sb.AppendLine();
        foreach (var archetype in list)
        {
            var archetypeBp = archetype.Bp as BlueprintArchetype;
            sb.AppendLine($"## {archetype.DisplayName} ({archetype.Parent})");
            sb.AppendLine();
            sb.AppendLine($"{archetype.Description.Replace("\n", "  \n")}");
            sb.AppendLine();
            sb.AppendLine($"`{archetype.Guid}`  ");
            sb.AppendLine($"`{archetype.InternalName}`  ");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(userPath, sb.ToString());
        return list.Count;
    }
}
