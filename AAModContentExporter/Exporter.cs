using AAModContentExporter.Exporters;
using AAModContentExporter.ModfinderIntegration;
using Kingmaker.Blueprints;
using Kingmaker.Modding;
using Kingmaker.Utility;
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
    public static HashSet<BlueprintGuid> KnownGuids = [];
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
            Main.log.Log($"Missing scanmetadata.json at ${ExportOutput}");
            return;
        }
        var latestModVersions = UpdateChecker.ReadModfinderManifest();
        foreach (var modManifest in latestModVersions)
        {
            var modId = modManifest.Key;
            var manifestVersion = modManifest.Value;
            var scanned = scanMetaData.ProcessedMods.FirstOrDefault(x => x.ModId == modId);
            if (scanned != null)
            {
                if (manifestVersion != scanned.Version)
                {
                    Main.log.Log($"{scanned.DisplayName} ({scanned.ModId}) processed version: {scanned.Version}, latest: {manifestVersion}");
                }
                continue;
            }
            var ex = scanMetaData.ExcludedMods.FirstOrDefault(x => x == modId);
            if (ex != null)
            {
                continue;
            }
            Main.log.Log($"Mod {modId} is in Modfinder manifest, not scanned or excluded.");
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

        var owlMods = OwlcatModificationsManager.Instance
            .AppliedModifications
            .Where(x =>
            {
                return !scanMetaData.ProcessedMods.Any(y => y.ModId == x.Manifest.UniqueName && y.Version == x.Manifest.Version);
            })
            .ToList();

        var newModsTotal = newMods.Count + owlMods.Count;
        Main.log.Log($"New UMM mods: {newMods.Count}");
        Main.log.Log($"New Owl mods: {owlMods.Count}");

        if (newModsTotal == 0)
        {
            return;
        }

        if (newModsTotal > 1)
        {
            Main.log.Log("Found more than one unprocessed mod, skipping export");
            foreach (var mod in newMods)
            {
                Main.log.Log($"{mod.Info.Id} - {mod.Info.Version} - {mod.Info.DisplayName}");
            }
            foreach (var mod in owlMods)
            {
                Main.log.Log($"{mod.Manifest.UniqueName} - {mod.Manifest.Version} - {mod.Manifest.DisplayName}");
            }
            return;
        }
        var modToAnalyze = new ProcessedMod
        {
            ModId = newMods.Count == 1 ? newMods.First().Info.Id : owlMods.First().Manifest.UniqueName,
            Version = newMods.Count == 1 ? newMods.First().Info.Version : owlMods.First().Manifest.Version,
            DisplayName = newMods.Count == 1 ? newMods.First().Info.DisplayName : owlMods.First().Manifest.DisplayName,
        };
        var author = newMods.Count == 1 ? newMods.First().Info.Author : owlMods.First().Manifest.Author;
        var homePage = newMods.Count == 1 ? newMods.First().Info.HomePage : owlMods.First().Manifest.HomePage;

        var blueprintModCount = 0;
        TTTStyleBlueprints blueprintsJson = null;

        if (newMods.Count == 1)
        {
            var modPath = newMods.First().Path;
            var blueprintsPath = $"{modPath}{Path.DirectorySeparatorChar}UserSettings{Path.DirectorySeparatorChar}Blueprints.json";
            if (File.Exists(blueprintsPath))
            {
                try
                {
                    using StreamReader streamReader = File.OpenText(blueprintsPath);
                    using JsonReader jsonReader = new JsonTextReader(streamReader);
                    blueprintsJson = serializer.Deserialize<TTTStyleBlueprints>(jsonReader);
                    blueprintModCount = blueprintsJson.NewBlueprints.Count + blueprintsJson.DerivedBlueprints.Count + blueprintsJson.DerivedBlueprintMasters.Count;
                    Main.log.Log($"Mod Blueprints.json announce {blueprintModCount} new blueprints");
                }
                catch (Exception ex)
                {
                    Main.log.Log($"Failed to read Blueprints.json: {ex.Message}");
                }
            }
        }

        ExportOutput = $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{modToAnalyze.ModId}";



        KnownGuids = new HashSet<BlueprintGuid>(GuidSerialization.DeserializeGuids(baseGame));

        foreach (var key in KnownGuids)
        {
            ResourcesLibrary.TryGetBlueprint(key);
        }

        foreach (var procMod in scanMetaData.ProcessedMods)
        {
            if (procMod.ModId == modToAnalyze.ModId)
            {
                continue;
            }
            var path = $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{procMod.ModId}{Path.DirectorySeparatorChar}blueprintguids.bin";
            if (File.Exists(path))
            {
                var processed = GuidSerialization.DeserializeGuids(path);
                KnownGuids.AddRange(processed);
            }
        }

        var bpkeys = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.Keys.Where(x => !KnownGuids.Contains(x)).ToList();
        var newBlueprints = bpkeys
            .Select(x => ResourcesLibrary.TryGetBlueprint(x))
            .ToList();

        if (newBlueprints.Count == 0)
        {
            scanMetaData.ExcludedMods.Add(modToAnalyze.ModId);
            scanMetaData.ExcludedMods.Sort();
        }
        else
        {
            if (!Directory.Exists(ExportOutput))
            {
                Directory.CreateDirectory(ExportOutput);
            }
            foreach (var key in newBlueprints)
            {
                if (key == null)
                {
                    Main.log.Log($"What the fuck, null key");
                }
                else
                {
                    ModBlueprints[key.AssetGuid] = key;
                }
            }

            Main.log.Log($"New blueprints: {newBlueprints.Count}");

            if (blueprintModCount > 0 && newBlueprints.Count != blueprintModCount)
            {
                Main.log.Log($"Blueprint detection differ from blueprint cache");
            }

            BlueprintAnalyzer.Init();
            var analyzed = ModBlueprints.Values.Select(x => BlueprintAnalyzer.Analyze(x)).ToList();
            ModBlueprintData = analyzed.ToDictionary(x => x.Guid);

            AllGuidsExporter.WriteAllNewGuids(analyzed, blueprintsJson);

            {
                ModFinderManifestEntry[] manifestEntries = [];
                ModFinderManifestEntry modManifest = null;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"# {modToAnalyze.DisplayName}");
                sb.AppendLine();
                sb.AppendLine("[Back to site homepage](../README.md)");
                sb.AppendLine();
                sb.AppendLine($"## Version: {modToAnalyze.Version}");
                sb.AppendLine();
                sb.AppendLine($"## Author: {author}");
                sb.AppendLine();
                var homeUrl = homePage;
                sb.AppendLine($"## Homepage: [{homeUrl}]({homeUrl})");
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
                if (CharacterClassExporter.WriteAllClasses(analyzed) > 0)
                {
                    sb.AppendLine($"### [New classes](./Classes.md)");
                    sb.AppendLine();
                }
                if (ArchetypeExporter.WriteAllArchetypes(analyzed) > 0)
                {
                    sb.AppendLine($"### [New archetypes for base game classes](./Archetypes.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllFeats(analyzed) > 0)
                {
                    sb.AppendLine($"### [New feats](./Feats.md)");
                    sb.AppendLine();
                }
                if (SpellExporter.WriteAllSpells(analyzed) > 0)
                {
                    sb.AppendLine($"### [New spells](./Spells.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllMythicFeats(analyzed) > 0)
                {
                    sb.AppendLine($"### [New mythic feats](./MythicFeats.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllMythicAbilities(analyzed) > 0)
                {
                    sb.AppendLine($"### [New mythic abilities](./MythicAbilities.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllDomains(analyzed) > 0)
                {
                    sb.AppendLine($"### [New domains](./Domains.md)");
                    sb.AppendLine();
                }
                if (RacialHeritageExporter.WriteAllHeritages(analyzed) > 0)
                {
                    sb.AppendLine($"### [New racial heritages](./RacialHeritages.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllBloodlines(analyzed) > 0)
                {
                    sb.AppendLine($"### [New sorcerer bloodlines](./SorcererBloodlines.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllOracleMysteries(analyzed) > 0)
                {
                    sb.AppendLine($"### [New oracle mysteries](./OracleMysteries.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllRagePowers(analyzed) > 0)
                {
                    sb.AppendLine($"### [New rage powers](./RagePowers.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllRogueTalents(analyzed) > 0)
                {
                    sb.AppendLine($"### [New rogue talents](./RogueTalents.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllSlayerTalents(analyzed) > 0)
                {
                    sb.AppendLine($"### [New slayer talents](./SlayerTalents.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllKiPowers(analyzed) > 0)
                {
                    sb.AppendLine($"### [New ki powers](./KiPowers.md)");
                    sb.AppendLine();
                }
                if (ExportersCollection.WriteAllItems(analyzed) > 0)
                {
                    sb.AppendLine($"### [New items](./Items.md)");
                    sb.AppendLine();
                }
                sb.AppendLine();
                sb.AppendLine("___");
                sb.AppendLine("[Back to site homepage](../README.md)");

                var readmepath = $"{ExportOutput}{Path.DirectorySeparatorChar}README.md";
                File.WriteAllText(readmepath, sb.ToString());
            }

            scanMetaData.ProcessedMods = scanMetaData.ProcessedMods
                .Where(x => x.ModId != modToAnalyze.ModId) // that line is for when we process new version of some mod
                .Concat([modToAnalyze])
                .OrderBy(x => x.ModId)
                .ToList();


        }
        using StreamWriter streamWriter = new StreamWriter(exportMetaData);
        using JsonWriter jsonWriter = new JsonTextWriter(streamWriter);
        serializer.Serialize(jsonWriter, scanMetaData);

        RegenerateReadme(scanMetaData);
        MakeAllGuidsFile();
    }

    internal static void MakeAllGuidsFile()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var procMod in scanMetaData.ProcessedMods)
        {
            var path = $"{Main.ModSettings.OutputFolder}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}{procMod.ModId}{Path.DirectorySeparatorChar}blueprintguids.bin";
            if (File.Exists(path))
            {
                var processed = GuidSerialization.DeserializeGuids(path);
                foreach (var mod in processed)
                {
                    stringBuilder.AppendLine($"{mod} - {procMod.DisplayName}");
                }
            }
        }
        var readmepath = $"{Main.ExportRepoLocation}{Path.DirectorySeparatorChar}all_blueprints.txt";
        File.WriteAllText(readmepath, stringBuilder.ToString());
    }

    internal static void RegenerateReadme(ScanMetaData scanMetaData)
    {
        var start = """
            # Database of Pathfinder Wrath of the Righteous mod content

            ### Disclaimer

            - Below are only those mods that create new blueprints
            - Information is collected automatically with no manual oversight and no filtering beyond by group. If in classes you see creature only class - that's not a list error.
            - Provides no guarantees of correctness
            - May be outdated by the time you read it
            - Does not encompass all mods out there and is not in any way replacement of mod readme

            **Table of Contents**
            - [List of content by mod](#list-of-content-by-mod)
            - [List of content by category](#list-of-content-by-category)
            - [Raw list of blueprints for troubleshooting](#raw-list-of-blueprints-for-troubleshooting)

            ## List of content by mod
            
            """;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(start);
        foreach (var mod in scanMetaData.ProcessedMods.OrderBy(x => x.DisplayName))
        {
            sb.AppendLine($"### [{mod.DisplayName}](./{mod.ModId}/README.md) {mod.Version}");
            sb.AppendLine();
        }
        sb.AppendLine();
        var mid = """
            ## List of content by category

            ### [New classes](./Classes.md)
            
            ### [New archetypes for base game classes](./Archetypes.md)
            
            ### [New feats](./Feats.md)
            
            ### [New spells](./Spells.md)
            
            ### [New mythic feats](./MythicFeats.md)
            
            ### [New mythic abilities](./MythicAbilities.md)
            
            ### [New domains](./Domains.md)
            
            ### [New racial heritages](./RacialHeritages.md)
            
            ### [New sorcerer bloodlines](./SorcererBloodlines.md)
            
            ### [New oracle mysteries](./OracleMysteries.md)
            
            ### [New rage powers](./RagePowers.md)
            
            ### [New rogue talents](./RogueTalents.md)
            
            ### [New slayer talents](./SlayerTalents.md)
            
            ### [New ki powers](./KiPowers.md)
            
            ### [New items](./Items.md)

            ## Raw list of blueprints for troubleshooting

            This is for when you have in log missing error blueprint with some guid, and you want to know which mod it is from.  
            Copy guid, open the link below and just Ctrl-F (search) on the page

            ### [all_blueprints.txt](https://raw.githubusercontent.com/alterasc/alterasc.github.io/main/all_blueprints.txt)
            """;
        sb.AppendLine(mid);
        var readmeToC = $"{Main.ExportRepoLocation}{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}README.md";
        File.WriteAllText(readmeToC, sb.ToString());
    }
}
