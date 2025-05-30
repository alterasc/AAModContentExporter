﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AAModContentExporter;

public static class Utils
{
    public static BlueprintGuid ParseToBPGuid(string str)
    {
        var r = Guid.Parse(str);
        if (r == null || r == Guid.Empty)
        {
            throw new ArgumentException("Invalid guid");
        }
        return new BlueprintGuid(r);
    }

    public class InvalidReferenceException : ApplicationException
    {
        public InvalidReferenceException(string id) : base($"Invalid reference: {id}")
        {
        }
    }

    public static T GetBlueprint<T>(string id) where T : SimpleBlueprint
    {
        if (ResourcesLibrary.TryGetBlueprint(ParseToBPGuid(id)) is not T obj)
        {
            throw new InvalidReferenceException(id);
        }
        return obj;
    }

    public static T GetBlueprint<T>(BlueprintGuid id) where T : SimpleBlueprint
    {
        if (ResourcesLibrary.TryGetBlueprint(id) is not T obj)
        {
            throw new InvalidReferenceException(id.ToString());
        }
        return obj;
    }

    public static T GetBlueprint<T>(Guid guid) where T : SimpleBlueprint
    {
        if (guid == null || guid == Guid.Empty)
        {
            throw new ArgumentException("Invalid guid");
        }
        if (ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(guid)) is not T obj)
        {
            throw new InvalidReferenceException(guid.ToString());
        }
        return obj;
    }

    public static T GetBlueprintReference<T>(string id) where T : BlueprintReferenceBase
    {
        T val = Activator.CreateInstance<T>();
        val.deserializedGuid = ParseToBPGuid(id);
        return val;
    }

    public static void SetComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
    {
        HashSet<string> hashSet = [];
        foreach (BlueprintComponent blueprintComponent in components)
        {
            if (string.IsNullOrEmpty(blueprintComponent.name))
            {
                blueprintComponent.name = "$" + blueprintComponent.GetType().Name;
            }

            if (!hashSet.Add(blueprintComponent.name))
            {
                int num = 0;
                string name;
                while (!hashSet.Add(name = $"{blueprintComponent.name}${num}"))
                {
                    num++;
                }

                blueprintComponent.name = name;
            }
        }

        obj.ComponentsArray = components;
        obj.OnEnable();
    }

    public static void SetComponents(this BlueprintScriptableObject obj, IEnumerable<BlueprintComponent> components)
    {
        obj.SetComponents(components.ToArray());
    }

    public static void RemoveComponents<T>(this BlueprintScriptableObject obj) where T : BlueprintComponent
    {
        T[] array = obj.GetComponents<T>().ToArray();
        foreach (T value in array)
        {
            obj.SetComponents(obj.ComponentsArray.RemoveFromArray(value));
        }
    }

    internal static T[] RemoveFromArray<T>(this T[] array, T value)
    {
        List<T> list = array.ToList();
        if (!list.Remove(value))
        {
            return array;
        }

        return list.ToArray();
    }

    public static void AddComponent(this BlueprintScriptableObject obj, BlueprintComponent component)
    {
        obj.SetComponents(obj.ComponentsArray.AppendToArray(component));
    }

    public static void AddComponent<T>(this BlueprintScriptableObject obj, Action<T> init = null) where T : BlueprintComponent, new()
    {
        obj.SetComponents(obj.ComponentsArray.AppendToArray(Create(init)));
    }

    public static T Create<T>(Action<T> init = null) where T : new()
    {
        T val = new();
        init?.Invoke(val);
        return val;
    }
    internal static T[] AppendToArray<T>(this T[] array, T value)
    {
        int num = array != null ? array.Length : 0;
        T[] array2 = new T[num + 1];
        if (num > 0)
        {
            Array.Copy(array, array2, num);
        }

        array2[num] = value;
        return array2;
    }

    public static T ReplaceBlueprint<T>(string id, Action<T> init = null) where T : SimpleBlueprint, new()
    {
        var original = GetBlueprint<SimpleBlueprint>(id);
        T val = new()
        {
            name = original.name,
            AssetGuid = original.AssetGuid,
        };
        T val2 = val;
        ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(val2.AssetGuid, val);
        val2.OnEnable();
        SetRequiredBlueprintFields(val2);
        init?.Invoke(val2);
        return val2;
    }

    internal static void AddBlueprint(SimpleBlueprint blueprint, BlueprintGuid assetId)
    {
        SimpleBlueprint simpleBlueprint = ResourcesLibrary.TryGetBlueprint(assetId);
        if (simpleBlueprint == null)
        {
            ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(assetId, blueprint);
            blueprint.OnEnable();
        }
    }

    private static void SetRequiredBlueprintFields(SimpleBlueprint blueprint)
    {
        BlueprintBuff blueprintBuff = blueprint as BlueprintBuff;
        if (blueprintBuff == null)
        {
            BlueprintFeature blueprintFeature = blueprint as BlueprintFeature;
            if (blueprintFeature != null)
            {
                blueprintFeature.IsClassFeature = true;
            }
        }
        else
        {
            blueprintBuff.FxOnStart = new PrefabLink();
            blueprintBuff.FxOnRemove = new PrefabLink();
            blueprintBuff.IsClassFeature = true;
        }
    }

    public static void ApplyForAll<T>(string[] ids, System.Action<T> action) where T : SimpleBlueprint
    {
        foreach (var id in ids)
        {
            var bp = Utils.GetBlueprint<T>(id);
            action.Invoke(bp);
        }
    }
}