using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System.Collections.Generic;
using System.Linq;
using static AAModContentExporter.BlueprintAnalyzer;

namespace AAModContentExporter;
public static class BlueprintAnalyzer
{
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

    static Dictionary<BlueprintGuid, HashSet<SpellEntry>> AllSpells = [];
    static HashSet<BlueprintGuid> AllFeats = [];
    public static void Init()
    {
        var featSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("247a4068296e8be42890143f451b4b45");
        AllFeats = featSelection.m_AllFeatures.Select(x => x.Guid).ToHashSet();

        var playerClasses = BlueprintRoot.Instance.Progression.m_CharacterClasses;
        var spellsWithClass = playerClasses.Select(x => x.Get())
            .Where(x => !x.PrestigeClass)
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
        var spellsWithArchetype = playerClasses.Select(x => x.Get())
            .Where(x => !x.PrestigeClass)
            .SelectMany(x => x.Archetypes)
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
    public HashSet<SpellEntry> SpellEntries;
}
