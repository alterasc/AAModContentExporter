using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace AAModContentExporter.ModfinderIntegration;
public static class UpdateChecker
{
    public static void DownloadModfinderManifest(string directory)
    {
        string jsonUrl = "https://raw.githubusercontent.com/Pathfinder-WOTR-Modding-Community/ModFinder/main/ManifestUpdater/Resources/generated_manifest.json";
        string savePath = Path.Combine(directory, "generated_manifest.json"); // Path to save the JSON file
        string metadataPath = Path.Combine(directory, "modfinder_metadata.json"); // Path to save metadata file

        try
        {
            // Check if metadata file exists and read the last download time
            if (File.Exists(metadataPath))
            {
                string metadataContent = File.ReadAllText(metadataPath);
                JObject metadata = JObject.Parse(metadataContent);
                DateTime lastDownloadTime = metadata["lastDownloadTime"]?.ToObject<DateTime>() ?? DateTime.MinValue;
                if ((DateTime.Now - lastDownloadTime).TotalHours < 1)
                {
                    return;
                }
            }

            // Download the JSON file
            using (HttpClient client = new())
            {
                string jsonContent = client.GetStringAsync(jsonUrl).GetAwaiter().GetResult();
                File.WriteAllText(savePath, jsonContent);
                JObject metadata = new JObject
                {
                    ["lastDownloadTime"] = DateTime.Now
                };
                File.WriteAllText(metadataPath, metadata.ToString());
                Main.log.Log("Redownloaded ModFinder manifest");
            }
        }
        catch (Exception ex)
        {
            Main.log.Log("An error occurred:");
            Main.log.Log(ex.Message);
        }
    }

    public static Dictionary<string, string> ReadModfinderManifest()
    {
        string directory = Main.ModEntry.Path;
        DownloadModfinderManifest(directory);
        string manifestPath = Path.Combine(directory, "generated_manifest.json");
        if (!File.Exists(manifestPath))
        {
            return [];
        }
        var text = File.ReadAllText(manifestPath);
        var manifests = JsonConvert.DeserializeObject<List<JObject>>(text);
        var latestModVersions = manifests!.Select(x =>
        {
            var modId = x["Id"]!["Id"]!.ToString();
            var version = x["Version"]!["Latest"]!["Version"]!.ToString();
            return (modId, version);
        }).ToDictionary(x => x.modId, x => x.version);
        return latestModVersions;
    }
}


