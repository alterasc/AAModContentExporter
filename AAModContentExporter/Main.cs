using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace AAModContentExporter;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;
    internal static ModEntry ModEntry;

    public static ModSettings ModSettings;
    public static string ExportRepoLocation => ModSettings.OutputFolder;

    private static JsonSerializerSettings cachedSettings;
    public static JsonSerializerSettings SerializerSettings
    {
        get
        {
            if (cachedSettings == null)
            {
                cachedSettings = new JsonSerializerSettings
                {
                    CheckAdditionalContent = false,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    FloatParseHandling = FloatParseHandling.Double,
                    Formatting = Formatting.Indented,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    StringEscapeHandling = StringEscapeHandling.Default,
                    Culture = CultureInfo.InvariantCulture
                };
            }
            return cachedSettings;
        }
    }
    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        log = modEntry.Logger;
#if DEBUG
        modEntry.OnUnload = OnUnload;
#endif
        var settingsPath = $"{ModEntry.Path}{Path.DirectorySeparatorChar}ModSettings.json";
        if (File.Exists(settingsPath))
        {
            var serializer = JsonSerializer.Create(SerializerSettings);
            using (StreamReader streamReader = File.OpenText(settingsPath))
            using (JsonReader jsonReader = new JsonTextReader(streamReader))
            {

                ModSettings = serializer.Deserialize<ModSettings>(jsonReader);
            }

        }
        else
        {
            ModSettings = new ModSettings
            {
                OutputFolder = Environment.GetEnvironmentVariable("WrathModDBRepoPath")
            };
            log.Log($"Output path: {ModSettings.OutputFolder}");
            if (string.IsNullOrEmpty(ModSettings.OutputFolder))
            {
                return true;
            }
        }
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }
#if DEBUG
    public static bool OnUnload(UnityModManager.ModEntry modEntry) {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
#endif
}
