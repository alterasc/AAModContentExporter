using HarmonyLib;
using Kingmaker;

namespace AAModContentExporter;
[HarmonyPatch]
internal class ContentTracker
{
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Export()
    {
        Exporter.Export();
    }
}
