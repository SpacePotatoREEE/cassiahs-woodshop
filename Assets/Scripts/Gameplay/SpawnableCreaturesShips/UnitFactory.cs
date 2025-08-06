// UnitFactory.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UnitFactory
{
    /// <summary>Spawns a fully‑rolled unit and returns the instance.</summary>
    public static GameObject SpawnUnit(
        SpawnTable.Entry entry,
        AreaProfile       areaProfile,
        Vector3           position,
        Quaternion        rotation)
    {
        // 1. Choose affixes
        var allAffixes = Resources.LoadAll<AffixDefinition>(""); // swap to Addressables later
        int maxAff = Random.Range(entry.Template.BaseStats.Count > 0 ? areaProfile.SpawnTables.Min(t=>t.MinAffixes) : 0,
                                  areaProfile.SpawnTables.Max(t=>t.MaxAffixes)+1);

        var affixes = AffixGenerator.RollAffixes(
            areaProfile.AreaLevel,
            entry.Template.BiomeTag,
            allAffixes,
            maxAff, maxAff);                    // prefixes & suffixes share cap here

        // 2. Instantiate prefab
        GameObject go = Object.Instantiate(entry.Template.Prefab, position, rotation);

        // 3. Stats
        var stats = go.GetComponent<UnitStats>() ?? go.AddComponent<UnitStats>();
        stats.AddModifiers(entry.Template.BaseStats);
        stats.AddModifiers(affixes.SelectMany(a => a.Modifiers));

        // 4. Nameplate -----------------------------------------------------------
        NameplateUI plate = go.GetComponentInChildren<NameplateUI>(true);

        if (plate == null)
        {
            // Create a fresh plate 1.2 m above the pivot
            GameObject holder = new GameObject("Nameplate");
            holder.transform.SetParent(go.transform, false);
            holder.transform.localPosition = Vector3.up * 1.2f;

            var tmp = holder.AddComponent<TMPro.TextMeshPro>();  // 3‑D variant
            tmp.fontSize   = 3;
            tmp.alignment  = TMPro.TextAlignmentOptions.Center;

            plate = holder.AddComponent<NameplateUI>();
        }

        plate.SetName(BuildName(entry.Template.UnitName, affixes));

        // 5. Lifetime
        if (areaProfile.UnitLifetime > 0)
        {
            var td = go.AddComponent<TimedDespawn>();
            td.Lifetime = areaProfile.UnitLifetime;
        }

        return go;
    }

    /* ---------- helpers ---------- */

    private static string BuildName(string baseName, List<AffixDefinition> affixes)
    {
        var prefixes = affixes.Where(a => a.IsPrefix).ToList();
        var suffixes = affixes.Where(a => !a.IsPrefix).ToList();

        string txt = "";
        prefixes.ForEach(p => txt += $"<color=#{ColorUtility.ToHtmlStringRGB(p.DisplayColor)}>{p.AffixName}</color> ");
        txt += baseName;
        if (suffixes.Count > 0)
        {
            txt += " of ";
            txt += string.Join(" & ",
                suffixes.Select(s =>
                    $"<color=#{ColorUtility.ToHtmlStringRGB(s.DisplayColor)}>{s.AffixName}</color>"));
        }
        return txt;
    }
}
