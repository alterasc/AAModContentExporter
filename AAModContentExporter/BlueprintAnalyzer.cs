using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System.Collections.Generic;
using System.Linq;

namespace AAModContentExporter;
public static class BlueprintAnalyzer
{
    static HashSet<BlueprintGuid> AddedClasses = [];
    static HashSet<BlueprintGuid> AddedArchetypes = [];

    static Dictionary<BlueprintGuid, HashSet<SpellEntry>> AllSpells = [];
    static Dictionary<BlueprintGuid, HashSet<ClassLevelEntry>> ClassFeatures = [];
    static HashSet<BlueprintGuid> AllFeats = [];
    public static void Init()
    {
        var featSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("247a4068296e8be42890143f451b4b45");
        AllFeats = featSelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();
        AddedClasses = ContentTracker.modBlueprints.Values
            .Where(x => x is BlueprintCharacterClass)
            .Select(x => x.AssetGuid)
            .ToHashSet();

        AddedArchetypes = ContentTracker.modBlueprints.Values
            .Where(x => x is BlueprintArchetype)
            .Select(x => x.AssetGuid)
            .ToHashSet();

        LoadClassSpells();
        LoadDirectClassFeatures();
    }

    private static void LoadClassSpells()
    {
        var playerClasses = BlueprintRoot.Instance.Progression.m_CharacterClasses;

        List<string> exClasses = [
            "f5b8c63b141b2f44cbb8c2d7579c34f5", // eldritch scion class
            "b3a505fb61437dc4097f43c3f8f9a4cf", // sorcerer
            "52dbfd8505e22f84fad8d702611f60b7", // arcanist
            "6afa347d804838b48bda16acb0573dc0", // skald,
            "20ce9bf8af32bee4c8557a045ab499b1", // oracle,
            ];
        var excludedClasses = exClasses.Select(x => new BlueprintGuid(new System.Guid(x))).ToHashSet();
        var spellsWithClass = playerClasses.Select(x => x.Get())
            .Where(x => !x.PrestigeClass)
            .Where(x => !excludedClasses.Contains(x.AssetGuid))
            .Where(x => x.Spellbook != null)
            .Select(x => (x.LocalizedName.ToString(), x.Spellbook.SpellList));
        foreach (var sp in spellsWithClass)
        {
            var className = sp.Item1;
            var spellList = sp.Item2;
            foreach (var spellListEntry in spellList.SpellsByLevel)
            {
                var spelllevel = spellListEntry.SpellLevel;
                foreach (var spell in spellListEntry.m_Spells)
                {
                    AllSpells.TryGetValue(spell.Guid, out var set);
                    if (set == null)
                    {
                        set = [];
                        AllSpells[spell.Guid] = set;
                    }
                    set.Add(new SpellEntry(className, spelllevel));
                }
            }
        }

        List<string> exArchetypes = [
            "57f93dd8423c97c49989501281296c4a", //eldritch scoundrel uses Wizard Spell list
            "26185cfb81b34e778ad370407300de9a", //nature mage uses Druid Spell list
            "44f3ba33839a87f48a66b2b9b2f7c69b", //unlettered arcanist uses Witch Spell list
            ];
        var excludedArchetypes = exArchetypes.Select(x => new BlueprintGuid(new System.Guid(x))).ToHashSet();
        var spellsWithArchetype = playerClasses.Select(x => x.Get())
            .Where(x => !x.PrestigeClass)
            .SelectMany(x => x.Archetypes)
            .Where(x => !excludedArchetypes.Contains(x.AssetGuid))
            .Where(x => x.m_ReplaceSpellbook != null
                    && x.LocalizedName != null
                    && x.Spellbook != null
                    && (x.GetParentClass().Spellbook == null || x.GetParentClass().Spellbook.SpellList.AssetGuid != x.Spellbook.SpellList.AssetGuid)
                    )
            .Select(x => (x?.LocalizedName?.ToString(), x.Spellbook.SpellList));
        foreach (var sp in spellsWithArchetype)
        {
            var className = sp.Item1;
            var spellList = sp.Item2;
            foreach (var spellListEntry in spellList.SpellsByLevel)
            {
                var spelllevel = spellListEntry.SpellLevel;
                foreach (var spell in spellListEntry.m_Spells)
                {
                    AllSpells.TryGetValue(spell.Guid, out var set);
                    if (set == null)
                    {
                        set = [];
                        AllSpells[spell.Guid] = set;
                    }
                    set.Add(new SpellEntry(className, spelllevel));
                }
            }
        }
    }

    public static void LoadDirectClassFeatures()
    {
        var playerClasses = BlueprintRoot.Instance.Progression.m_CharacterClasses;

        List<string> exClasses = [
            "f5b8c63b141b2f44cbb8c2d7579c34f5", // eldritch scion class
            ];
        var excludedClasses = exClasses.Select(x => new BlueprintGuid(new System.Guid(x))).ToHashSet();
        var filteredClasses = playerClasses.Select(x => x.Get())
            .Where(x => !excludedClasses.Contains(x.AssetGuid))
            .Where(x => !AddedClasses.Contains(x.AssetGuid))
            .ToList();
        foreach (var cClass in filteredClasses) {
            var progression = cClass.Progression;
            var className = cClass.LocalizedName;
            foreach(var entry in progression.LevelEntries)
            {
                foreach(var f in entry.m_Features)
                {
                    ClassFeatures.TryGetValue(f.Guid, out var classEntries);
                    if (classEntries == null)
                    {
                        classEntries = [];
                        ClassFeatures[f.Guid] = classEntries;
                    }
                    classEntries.Add(new ClassLevelEntry(className, entry.Level));
                }
            }
        }

        List<string> exArchetypes = [];
        var excludedArchetypes = exArchetypes.Select(x => new BlueprintGuid(new System.Guid(x))).ToHashSet();
        var archetypes = playerClasses.Select(x => x.Get())
            .SelectMany(x => x.Archetypes)
            .Where(x => !excludedArchetypes.Contains(x.AssetGuid))
            .Where(x => !AddedArchetypes.Contains(x.AssetGuid))
            .ToList();
        foreach (var archetype in archetypes)
        {
            var progression = archetype.AddFeatures;
            var className = archetype.LocalizedName;
            foreach (var entry in progression)
            {
                foreach (var f in entry.m_Features)
                {
                    ClassFeatures.TryGetValue(f.Guid, out var classEntries);
                    if (classEntries == null)
                    {
                        classEntries = [];
                        ClassFeatures[f.Guid] = classEntries;
                    }
                    classEntries.Add(new ClassLevelEntry(className, entry.Level));
                }
            }
        }
    }
    
    public static BlueprintData Analyze(SimpleBlueprint bp)
    {
        var result = new BlueprintData();
        result.Bp = bp;
        result.InternalName = bp.name;
        if (bp is BlueprintUnitFact unitFact)
        {
            result.IsUnitFact = true;
            if (unitFact.m_DisplayName != null)
            {
                result.LocalizedName = unitFact.m_DisplayName.ToString();
            }
            if (bp is BlueprintAbility ability)
            {
                AllSpells.TryGetValue(ability.AssetGuid, out var spellEntries);
                if (spellEntries != null)
                {
                    result.IsSpell = true;
                    result.SpellEntries = spellEntries;
                }
            }
            else if (bp is BlueprintFeature feature)
            {
                result.IsFeat = AllFeats.Contains(feature.AssetGuid);
                ClassFeatures.TryGetValue(feature.AssetGuid, out var classEntries);
                if (classEntries != null)
                {
                    result.IsClassFeature = true;
                    result.ClassLevelEntries = classEntries;
                }
            }
        }
        return result;
    }
}

public class BlueprintData
{
    public SimpleBlueprint Bp;
    public BlueprintGuid Guid => Bp.AssetGuid;
    public string InternalName;
    public string LocalizedName;
    public bool IsUnitFact;
    public bool IsFeat;
    public bool IsSpell;
    public bool IsClassFeature;
    public HashSet<SpellEntry> SpellEntries;
    public HashSet<ClassLevelEntry> ClassLevelEntries;
}

public class SpellEntry
{
    public string className;
    public int level;

    public SpellEntry(string className, int level)
    {
        this.className = className;
        this.level = level;
    }

    public override bool Equals(object obj)
    {
        return obj is SpellEntry entry &&
               className == entry.className &&
               level == entry.level;
    }

    public override int GetHashCode()
    {
        int hashCode = 88113433;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(className);
        hashCode = hashCode * -1521134295 + level.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        return $"{className} {level}";
    }
}

public class ClassLevelEntry
{
    public string className;
    public int level;

    public ClassLevelEntry(string className, int level)
    {
        this.className = className;
        this.level = level;
    }

    public override bool Equals(object obj)
    {
        return obj is ClassLevelEntry entry &&
               className == entry.className &&
               level == entry.level;
    }

    public override int GetHashCode()
    {
        int hashCode = 88113433;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(className);
        hashCode = hashCode * -1521134295 + level.GetHashCode();
        return hashCode;
    }

    public override string ToString()
    {
        return $"{className} {level}";
    }
}