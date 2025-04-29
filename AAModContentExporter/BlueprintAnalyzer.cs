using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AAModContentExporter;
public static class BlueprintAnalyzer
{
    static HashSet<BlueprintGuid> PlayerClasses = [];
    static HashSet<BlueprintGuid> AddedClasses = [];
    static HashSet<BlueprintGuid> AddedArchetypes = [];

    static Dictionary<BlueprintGuid, HashSet<SpellEntry>> AllSpells = [];
    static HashSet<BlueprintGuid> AllFeats = [];
    static HashSet<BlueprintGuid> MythicAbilities = [];
    static HashSet<BlueprintGuid> MythicFeats = [];
    static HashSet<BlueprintGuid> Domains = [];
    static HashSet<BlueprintGuid> SorcererBloodlines = [];
    static HashSet<BlueprintGuid> RogueTalents = [];
    static HashSet<BlueprintGuid> RagePowers = [];
    static HashSet<BlueprintGuid> KiPowers = [];
    static HashSet<BlueprintGuid> OracleMysteries = [];
    static HashSet<BlueprintGuid> SlayerTalents = [];
    static Dictionary<BlueprintGuid, BlueprintRace> RacialHeritages = [];

    public static Dictionary<string, Dictionary<int, HashSet<BlueprintGuid>>> NewClassFeatures = [];
    public static void Init()
    {
        var featSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("247a4068296e8be42890143f451b4b45");
        AllFeats = featSelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        var mythicAbilitySelection = Utils.GetBlueprint<BlueprintFeatureSelection>("ba0e5a900b775be4a99702f1ed08914d");
        MythicAbilities = mythicAbilitySelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        var mythicFeatSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("9ee0f6745f555484299b0a1563b99d81");
        MythicFeats = mythicFeatSelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        var domainSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("48525e5da45c9c243a343fc6545dbdb9");
        Domains = domainSelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        var sorcBloodlineSeletion = Utils.GetBlueprint<BlueprintFeatureSelection>("24bef8d1bee12274686f6da6ccbc8914");
        SorcererBloodlines = sorcBloodlineSeletion.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        RogueTalents = Utils.GetBlueprint<BlueprintFeatureSelection>("c074a5d615200494b8f2a9c845799d93").m_AllFeatures.Select(x => x.Guid).ToHashSet();

        RagePowers = Utils.GetBlueprint<BlueprintFeatureSelection>("28710502f46848d48b3f0d6132817c4e").m_AllFeatures.Select(x => x.Guid).ToHashSet();

        KiPowers = Utils.GetBlueprint<BlueprintFeatureSelection>("3049386713ff04245a38b32483362551").m_AllFeatures.Select(x => x.Guid).ToHashSet();

        OracleMysteries = Utils.GetBlueprint<BlueprintFeatureSelection>("5531b975dcdf0e24c98f1ff7e017e741").m_AllFeatures.Select(x => x.Guid).ToHashSet();

        SlayerTalents = Utils.GetBlueprint<BlueprintFeatureSelection>("913b9cf25c9536949b43a2651b7ffb66").m_AllFeatures.Select(x => x.Guid).ToHashSet();


        PlayerClasses = BlueprintRoot.Instance.Progression.m_CharacterClasses
            .Select(x => x.Guid).ToHashSet();

        AddedClasses = Exporter.ModBlueprints.Values
            .Where(x => x is BlueprintCharacterClass)
            .Select(x => x.AssetGuid)
            .Where(x => PlayerClasses.Contains(x))
            .ToHashSet();

        AddedArchetypes = Exporter.ModBlueprints.Values
            .Where(x => x is BlueprintArchetype)
            .Select(x => x.AssetGuid)
            .ToHashSet();

        LoadClassSpells();

        var raceSelections = BlueprintRoot.Instance.Progression
            .CharacterRaces
            .SelectMany(x => x.m_Features)
            .Select(x => x.Get())
            .Where(x => x is BlueprintFeatureSelection)
            .ToList();
        foreach (var race in BlueprintRoot.Instance.Progression.CharacterRaces)
        {
            var basicFeatSelection = new BlueprintGuid(new System.Guid("247a4068296e8be42890143f451b4b45"));
            var moddedHeritages = race.m_Features
                .Select(x => x.Get())
                .Where(x => x is BlueprintFeatureSelection)
                .Where(x => x.AssetGuid != basicFeatSelection)
                .Select(x => x as BlueprintFeatureSelection)
                .SelectMany(x => x.m_AllFeatures)
                .Where(x => Exporter.ModBlueprints.Keys.Contains(x.Guid));
            foreach (var mod in moddedHeritages)
            {
                RacialHeritages[mod.Guid] = race;
            }

        }

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

    public static BlueprintData Analyze(SimpleBlueprint bp)
    {
        var result = new BlueprintData();
        result.Bp = bp;
        result.InternalName = bp.name ?? string.Empty;
        if (bp is BlueprintCharacterClass cClass)
        {
            if (PlayerClasses.Contains(bp.AssetGuid))
            {
                result.IsCharacterClass = true;
                result.DisplayName = cClass.LocalizedName;
                result.Description = (cClass.LocalizedDescription ?? cClass.LocalizedDescriptionShort).ToString() ?? string.Empty;
            }
        }
        else if (bp is BlueprintArchetype archetype)
        {
            var origClass = BlueprintRoot.Instance.Progression.m_CharacterClasses
                .Select(x => x.Get())
                .FirstOrDefault(x =>
                {
                    var hasArch = x.m_Archetypes.Select(yy => yy.Guid).Any(yy => yy == bp.AssetGuid);
                    return hasArch;
                });
            if (origClass == null)
            {
                Main.log.Log($"Archetype with no class: {archetype.LocalizedName}");
            }
            if (origClass != null && Exporter.KnownGuids.Contains(origClass.AssetGuid))
            {
                result.IsArchetypeForBaseClass = true;
                result.DisplayName = archetype.LocalizedName;
                result.Description = (archetype.LocalizedDescription ?? archetype.LocalizedDescriptionShort).ToString() ?? string.Empty;
                result.Parent = origClass.LocalizedName ?? string.Empty;
            }
        }
        else if (bp is BlueprintRace race)
        {
            result.IsRace = true;
            result.DisplayName = race.m_DisplayName.ToString();
            result.Description = race.m_Description.ToString();
        }
        else if (bp is BlueprintUnitFact unitFact)
        {
            result.IsUnitFact = true;
            if (unitFact.m_DisplayName != null)
            {
                try
                {
                    result.DisplayName = unitFact.m_DisplayName.ToString();
                }
                catch (System.Exception ex)
                {
                    result.DisplayName = "";
                    Main.log.Log($"Couldn't process name for {unitFact.name}, guid {unitFact.AssetGuid}");
                }
            }
            try
            {
                result.Description = unitFact.m_Description != null ? unitFact.Description : string.Empty;
            }
            catch (System.Exception)
            {
                Main.log.Log($"Couldn't find description for {unitFact.name}");
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

                result.IsMythicAbility = MythicAbilities.Contains(feature.AssetGuid);

                result.IsMythicFeat = MythicFeats.Contains(feature.AssetGuid);

                result.IsDomain = Domains.Contains(feature.AssetGuid);

                result.IsSorcererBloodline = SorcererBloodlines.Contains(feature.AssetGuid);

                result.IsOracleMystery = OracleMysteries.Contains(feature.AssetGuid);

                result.IsRogueTalent = RogueTalents.Contains(feature.AssetGuid);

                result.IsSlayerTalent = SlayerTalents.Contains(feature.AssetGuid);

                result.IsRagePower = RagePowers.Contains(feature.AssetGuid);

                result.IsKiPower = KiPowers.Contains(feature.AssetGuid);

                if (RacialHeritages.ContainsKey(feature.AssetGuid))
                {
                    result.Parent = RacialHeritages[feature.AssetGuid].m_DisplayName;
                    result.IsRacialHeritage = true;
                }

            }
        }
        else if (bp is BlueprintItem item)
        {
            if (item is BlueprintItemWeapon weapon)
            {
                if (weapon.IsNatural)
                {
                    return result;
                }
            }
            result.IsItem = true;
            result.DisplayName = item.Name;
            try
            {
                result.Description = item.Description;
            }
            catch (System.Exception)
            {
                result.Description = "";
                Main.log.Log($"Couldn't process name for {item.name}, guid {item.AssetGuid}");
            }
        }
        return result;
    }

    public static string StripHTML(this string str)
    {
        return Regex.Replace(str, "<.*?>", string.Empty);
    }
    public static string StripEncyclopediaTags(this string str)
    {
        return Regex.Replace(str, "{.*?}", string.Empty);
    }
}

public class BlueprintData
{
    public SimpleBlueprint Bp;
    public BlueprintGuid Guid => Bp.AssetGuid;

    public string Description { get => description; set => description = value.StripHTML().StripEncyclopediaTags(); }

    public string InternalName;
    public string DisplayName;
    private string description;
    public string Parent;
    public bool IsUnitFact;
    public bool IsItem;
    public bool IsCharacterClass;
    public bool IsArchetypeForBaseClass;
    public bool IsFeat;
    public bool IsMythicFeat;
    public bool IsMythicAbility;
    public bool IsSpell;
    public bool IsClassFeature;
    public bool IsDomain;
    public bool IsSorcererBloodline;
    public bool IsRace;
    public bool IsRacialHeritage;
    public bool IsRogueTalent;
    public bool IsSlayerTalent;
    public bool IsRagePower;
    public bool IsOracleMystery;
    public bool IsKiPower;
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