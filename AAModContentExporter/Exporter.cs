using AAModContentExporter.Exporters;
using Kingmaker.Blueprints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityModManagerNet;

namespace AAModContentExporter;
public static class Exporter
{
    public static HashSet<BlueprintGuid> BaseGameGuids = [];
    public static Dictionary<BlueprintGuid, SimpleBlueprint> ModBlueprints = [];
    public static Dictionary<BlueprintGuid, BlueprintData> ModBlueprintData = [];

    public static ScanMetaData scanMetaData;

    private static string exportOutput;
    public static string ExportOutput
    {
        get
        {
            return exportOutput;
        }
        set
        {
            exportOutput = value;
        }
    }

    public static void Export()
    {
        var baseGame = $"{Main.ModEntry.Path}{Path.DirectorySeparatorChar}Base.guids";

        var exportMetaData = $"{Main.ExportRepoLocation}{Path.DirectorySeparatorChar}scanmetadata.json";

        var serializer = JsonSerializer.Create(Main.SerializerSettings);
        if (File.Exists(exportMetaData))
        {
            using StreamReader streamReader = File.OpenText(exportMetaData);
            using JsonReader jsonReader = new JsonTextReader(streamReader);
            scanMetaData = serializer.Deserialize<ScanMetaData>(jsonReader);
        }
        else
        {
            Main.log.Log($"Missing scanmetadata.json at ${Exporter.ExportOutput}");
            return;
        }
        var excludedMods = scanMetaData.ExcludedMods;

        Main.log.Log($"Excluded from analysis: {excludedMods.Count} mods");

        var activeMods = UnityModManager.modEntries
            .Where(modEntry => modEntry.Active)
            .ToList();

        if (!File.Exists(baseGame))
        {
            var unknownMods = activeMods.Where(x => !excludedMods.Contains(x.Info.Id)).ToList();
            if (unknownMods.Count > 0)
            {
                Main.log.Log($"Base game guid database is not built, because found active mods that are not marked as not content adding");
                foreach (var mod in unknownMods)
                {
                    Main.log.Log($"{mod.Info.Id} - {mod.Info.Version} - {mod.Info.DisplayName}");
                }
                return;
            }
            var keys = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.Keys.ToArray();
            var length = keys.Length;
            GuidSerialization.SerializeGuids(keys, baseGame);
            Main.log.Log("Exported base game guids");
            return;
        }

        var newMods = activeMods
            .Where(x => !excludedMods.Contains(x.Info.Id))
            .Where(x =>
            {
                return !scanMetaData.ProcessedMods.Any(y => y.ModId == x.Info.Id && y.Version == x.Info.Version);
            })
            .ToList();

        Main.log.Log($"New mods: {newMods.Count}");

        if (newMods.Count == 0)
        {
            return;
        }

        if (newMods.Count > 1)
        {
            Main.log.Log("Found more than one unprocessed mod, skipping export");
            foreach (var mod in newMods)
            {
                Main.log.Log($"{mod.Info.Id} - {mod.Info.Version} - {mod.Info.DisplayName}");
            }
            return;
        }
        var processingModInfo = newMods.First().Info;

        ExportOutput = $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{processingModInfo.Id}";
        if (!Directory.Exists(ExportOutput))
        {
            Directory.CreateDirectory(ExportOutput);
        }


        BaseGameGuids = new HashSet<BlueprintGuid>(GuidSerialization.DeserializeGuids(baseGame));

        var newBlueprints = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.Keys
            .Where(x => !BaseGameGuids.Contains(x))
            .Select(x => ResourcesLibrary.TryGetBlueprint(x))
            .ToList();

        foreach (var key in newBlueprints)
        {
            ModBlueprints[key.AssetGuid] = key;
        }

        Main.log.Log($"New blueprints: {newBlueprints.Count()}");

        BlueprintAnalyzer.Init();
        var analyzed = ModBlueprints.Values.Select(x => BlueprintAnalyzer.Analyze(x)).ToList();
        ModBlueprintData = analyzed.ToDictionary(x => x.Guid);

        AllGuidsExporter.WriteAllNewGuids(analyzed);

        var classesCount = CharacterClassExporter.WriteAllClasses(analyzed);
        var archetypeCount = ArchetypeExporter.WriteAllArchetypes(analyzed);
        var featCount = ExportersCollection.WriteAllFeats(analyzed);
        var spellsCount = SpellExporter.WriteAllSpells(analyzed);
        var mythicFeatCount = ExportersCollection.WriteAllMythicFeats(analyzed);
        var mythicAbilityCount = ExportersCollection.WriteAllMythicAbilities(analyzed);
        var domainsCount = ExportersCollection.WriteAllDomains(analyzed);
        var racialHeritagesCount = RacialHeritageExporter.WriteAllHeritages(analyzed);
        var itemCount = ExportersCollection.WriteAllItems(analyzed);
        var sorcererBloodlinesCount = ExportersCollection.WriteAllBloodlines(analyzed);
        var oracleMysteriesCount = ExportersCollection.WriteAllOracleMysteries(analyzed);
        var ragePowersCount = ExportersCollection.WriteAllRagePowers(analyzed);
        var rogueTalentsCount = ExportersCollection.WriteAllRogueTalents(analyzed);
        var slayerTalentsCount = ExportersCollection.WriteAllSlayerTalents(analyzed);
        var kiPowersCount = ExportersCollection.WriteAllKiPowers(analyzed);

        {
            ModFinderManifestEntry[] manifestEntries = [];
            ModFinderManifestEntry modManifest = null;
            try
            {
                if (File.Exists(Main.ModSettings.ModfinderInternalManifestPath))
                {
                    var newSer = JsonSerializer.Create();
                    using StreamReader streamReader = File.OpenText(exportMetaData);
                    using JsonReader jsonReader = new JsonTextReader(streamReader);
                    manifestEntries = newSer.Deserialize<ModFinderManifestEntry[]>(jsonReader);
                }
                modManifest = manifestEntries.FirstOrDefault(x => x.Id.Id == processingModInfo.Id);

            }
            catch (Exception e)
            {
                Main.log.Log($"Coulnd't get information from modfinder manifest: {e.Message}");
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[To list of mods](../README.md)");
            sb.AppendLine();
            sb.AppendLine($"# {processingModInfo.DisplayName}");
            sb.AppendLine();
            sb.AppendLine($"## Version: {processingModInfo.Version}");
            sb.AppendLine();
            sb.AppendLine($"## Author: {processingModInfo.Author}");
            sb.AppendLine();
            var homeUrl = string.IsNullOrEmpty(processingModInfo.HomePage) ? modManifest?.HomepageUrl : processingModInfo.HomePage;
            sb.AppendLine($"## Homepage: {homeUrl}");
            sb.AppendLine();
            if (modManifest != null)
            {
                sb.AppendLine($"## About: {modManifest.About}");
                sb.AppendLine();
            }
            sb.AppendLine($"## [All mod blueprints](./AllBlueprints.md)");
            sb.AppendLine();
            sb.AppendLine($"#### This unsorted list is the only one that's supposed to reliably show all blueprints that mod adds. If guid or name is not here, then highly likely something you're looking for is not added by this mod.");
            sb.AppendLine();
            sb.AppendLine($"## New Recognized Content");
            sb.AppendLine();
            sb.AppendLine("#### **Lists below are not a complete lists of mod what adds or does**. They are just some of the additions that can be easily automatically detected - and even then, mistakes could happen.");
            sb.AppendLine();
            if (classesCount > 0)
            {
                sb.AppendLine($"### [New classes](./Classes.md)");
                sb.AppendLine();
            }
            if (classesCount > 0)
            {
                sb.AppendLine($"### [New archetypes for base game classes](./Archetypes.md)");
                sb.AppendLine();
            }
            if (featCount > 0)
            {
                sb.AppendLine($"### [New feats](./Feats.md)");
                sb.AppendLine();
            }
            if (spellsCount > 0)
            {
                sb.AppendLine($"### [New spells](./Spells.md)");
                sb.AppendLine();
            }
            if (mythicFeatCount > 0)
            {
                sb.AppendLine($"### [New mythic feats](./MythicFeats.md)");
                sb.AppendLine();
            }
            if (mythicAbilityCount > 0)
            {
                sb.AppendLine($"### [New mythic abilities](./MythicAbilities.md)");
                sb.AppendLine();
            }
            if (domainsCount > 0)
            {
                sb.AppendLine($"### [New domains](./Domains.md)");
                sb.AppendLine();
            }
            if (racialHeritagesCount > 0)
            {
                sb.AppendLine($"### [New racial heritages](./RacialHeritages.md)");
                sb.AppendLine();
            }
            if (sorcererBloodlinesCount > 0)
            {
                sb.AppendLine($"### [New sorcerer bloodlines](./SorcererBloodlines.md)");
                sb.AppendLine();
            }
            if (oracleMysteriesCount > 0)
            {
                sb.AppendLine($"### [New oracle mysteries](./OracleMysteries.md)");
                sb.AppendLine();
            }
            if (ragePowersCount > 0)
            {
                sb.AppendLine($"### [New rage powers](./RagePowers.md)");
                sb.AppendLine();
            }
            if (rogueTalentsCount > 0)
            {
                sb.AppendLine($"### [New rogue talents](./RogueTalents.md)");
                sb.AppendLine();
            }
            if (slayerTalentsCount > 0)
            {
                sb.AppendLine($"### [New slayer talents](./SlayerTalents.md)");
                sb.AppendLine();
            }
            if (kiPowersCount > 0)
            {
                sb.AppendLine($"### [New ki powers](./KiPowers.md)");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("___");
            sb.AppendLine("[To list of mods](../README.md)");

            var readmepath = $"{ExportOutput}{Path.DirectorySeparatorChar}README.md";
            File.WriteAllText(readmepath, sb.ToString());
        }
        var a = new ProcessedMod
        {
            ModId = processingModInfo.Id,
            Version = processingModInfo.Version
        };

        //scanMetaData.ProcessedMods = scanMetaData.ProcessedMods
        //    .Where(x => x.ModId != processingModInfo.Id)
        //    .Concat([a])
        //    .OrderBy(x => x.ModId)
        //    .ToList();

        using StreamWriter streamWriter = new StreamWriter(exportMetaData);
        using JsonWriter jsonWriter = new JsonTextWriter(streamWriter);
        serializer.Serialize(jsonWriter, scanMetaData);
    }
}
