using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;

internal static class SpellExporter
{
    internal static int WriteAllSpells(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsSpell).OrderBy(x => x.DisplayName).ToList();
        if (list.Count == 0)
        {
            return 0;
        }
        var userPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}Spells.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# New spells");
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        foreach (var spell in analyzed.Where(x => x.IsSpell).OrderBy(x => x.DisplayName))
        {
            var books = string.Join(", ", spell.SpellEntries);
            sb.AppendLine($"## {spell.DisplayName}");
            sb.AppendLine();
            sb.AppendLine($"{spell.Description.Replace("\n", "  \n")}");
            sb.AppendLine();
            sb.AppendLine(books);
            sb.AppendLine();
            sb.AppendLine($"`{spell.Guid}`  ");
            sb.AppendLine($"`{spell.InternalName}`  ");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(userPath, sb.ToString());
        return list.Count;
    }
}