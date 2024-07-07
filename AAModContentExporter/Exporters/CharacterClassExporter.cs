using Kingmaker.Blueprints.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAModContentExporter.Exporters;
internal static class CharacterClassExporter
{
    internal static int WriteAllClasses(List<BlueprintData> analyzed)
    {
        var list = analyzed.Where(x => x.IsCharacterClass).OrderBy(x => x.DisplayName).ToList();
        if (list.Count == 0)
        {
            return 0;
        }
        var userPath = $"{Exporter.ExportOutput}{Path.DirectorySeparatorChar}Classes.md";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        sb.AppendLine();
        sb.AppendLine("# Classes");
        sb.AppendLine();
        foreach (var characterClass in list)
        {
            var classBp = characterClass.Bp as BlueprintCharacterClass;
            sb.AppendLine($"## {classBp.LocalizedName}");
            sb.AppendLine();
            sb.AppendLine($"{characterClass.Description.Replace("\n", "  \n")}");
            sb.AppendLine();
            if (classBp.m_Archetypes.Length > 0)
            {
                sb.AppendLine($"Archetypes:  ");
                foreach (var arch in classBp.Archetypes)
                {
                    sb.AppendLine($" - {arch.LocalizedName}");
                }
                sb.AppendLine();
            }
            sb.AppendLine($"`{characterClass.Guid}`  ");
            sb.AppendLine($"`{characterClass.InternalName}`  ");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("### [Back to mod overview](./README.md)");
        File.WriteAllText(userPath, sb.ToString());
        return list.Count;
    }
}
