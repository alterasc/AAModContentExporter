using Kingmaker.Blueprints.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;
internal static class RacialHeritageExporter
{
    internal static int WriteAllHeritages(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsRacialHeritage).OrderBy(x => (x.Parent, x.DisplayName)).ToList();
        if (list.Count == 0)
        {
            return 0;
        }
        var userPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}RacialHeritages.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine("# Racial heritages");
        sb.AppendLine();
        foreach (var heritage in list)
        {
            var racialHeritageBp = heritage.Bp as BlueprintFeature;
            sb.AppendLine($"## {heritage.DisplayName} ({heritage.Parent})");
            sb.AppendLine();
            sb.AppendLine($"{heritage.Description.Replace("\n", "  \n")}");
            sb.AppendLine();
            sb.AppendLine($"`{heritage.Guid}`  ");
            sb.AppendLine($"`{heritage.InternalName}`  ");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(userPath, sb.ToString());
        return list.Count;
    }
}
