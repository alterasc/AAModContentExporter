using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AAModContentExporter.Exporters;
public static class ExporterUtils
{
    public static int WriteNameDescriptionList(string fileName, string title, List<BlueprintData> list)
    {
        if (list.Count == 0)
        {
            return 0;
        }
        var userPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}{fileName}.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        foreach (var feat in list)
        {
            sb.AppendLine($"## {feat.DisplayName}");
            sb.AppendLine();
            sb.AppendLine($"{feat.Description.Replace("\n", "  \n")}");
            sb.AppendLine();
            sb.AppendLine($"`{feat.Guid}`  ");
            sb.AppendLine($"`{feat.InternalName}`  ");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(userPath, sb.ToString());
        return list.Count;
    }
}
