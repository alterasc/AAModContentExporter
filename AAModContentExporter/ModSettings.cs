using System.Collections.Generic;

namespace AAModContentExporter;
public class ModSettings
{
    public string ModfinderInternalManifestPath { get; set; }
    public string OutputFolder { get; set; }
}

public class ScanMetaData
{
    public List<ProcessedMod> ProcessedMods { get; set; } = [];
    public List<string> ExcludedMods { get; set; } = [];
}

public class ProcessedMod
{
    public string ModId { get; set; }
    public string Version { get; set; }
    public string DisplayName { get; set; }
}

public class ModFinderManifestEntry
{
    public ModFinderManifestEntryId Id { get; set; }
    public string About { get; set; }
    public string HomepageUrl { get; set; }
}

public class ModFinderManifestEntryId
{
    public string Id { get; set; }
    public string Type { get; set; }
}

public class TTTStyleBlueprints
{
    public Dictionary<string, string> NewBlueprints { get; set; } = [];
    public Dictionary<string, string> DerivedBlueprintMasters { get; set; } = [];
    public Dictionary<string, string> DerivedBlueprints { get; set; } = [];
}