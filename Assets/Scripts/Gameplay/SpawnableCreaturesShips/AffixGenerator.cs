// AffixGenerator.cs
using System;
using System.Collections.Generic;
using System.Linq;

public static class AffixGenerator
{
    private static readonly System.Random Rng = new();

    public static List<AffixDefinition> RollAffixes(
        int areaLevel,
        string biome,
        IEnumerable<AffixDefinition> pool,
        int maxPrefixes,
        int maxSuffixes)
    {
        var valid = pool.Where(a =>
            areaLevel >= a.MinAreaLevel &&
            areaLevel <= a.MaxAreaLevel &&
            (a.AllowedBiomes.Count == 0 || a.AllowedBiomes.Contains(biome)));

        List<AffixDefinition> prefixes = valid.Where(a => a.IsPrefix).ToList();
        List<AffixDefinition> suffixes = valid.Where(a => !a.IsPrefix).ToList();

        return Roll(prefixes, maxPrefixes)
            .Concat(Roll(suffixes, maxSuffixes))
            .ToList();
    }

    /* ---------- helpers ---------- */

    private static IEnumerable<AffixDefinition> Roll(List<AffixDefinition> pool, int max)
    {
        int count = Rng.Next(max + 1); // 0…max
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int total = pool.Sum(a => a.Weight);
            int pick  = Rng.Next(total);

            int acc = 0;
            for (int n = 0; n < pool.Count; n++)
            {
                acc += pool[n].Weight;
                if (pick >= acc) continue;

                var chosen = pool[n];
                pool.RemoveAt(n);

                // Remove other affixes in the same exclusive group
                if (!string.IsNullOrEmpty(chosen.ExclusiveGroup))
                    pool.RemoveAll(a => a.ExclusiveGroup == chosen.ExclusiveGroup);

                yield return chosen;
                break;
            }
        }
    }
}