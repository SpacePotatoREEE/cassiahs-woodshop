using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

public class UnitStats : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public StatType Type;
        public float Base;
        [HideInInspector] public float Current;
    }

    [Header("Base stats (author‑time)")]
    public List<Stat> Stats = new List<Stat>();

    private readonly List<StatModifier> _modifiers = new();

    /* ---------- PUBLIC API ---------- */

    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        Recalculate();
    }

    public void AddModifiers(IEnumerable<StatModifier> mods)
    {
        _modifiers.AddRange(mods);
        Recalculate();
    }

    public float GetStat(StatType type)
    {
        var stat = Stats.Find(s => s.Type == type);
        return stat != null ? stat.Current : 0f;
    }

    /* ---------- INTERNAL ---------- */

    private void Recalculate()
    {
        foreach (var s in Stats)
        {
            float flat   = 0f;
            float percent = 0f;
            foreach (var m in _modifiers)
                if (m.Stat == s.Type) { flat += m.Flat; percent += m.Percent; }

            s.Current = (s.Base + flat) * (1f + percent);
        }
    }
}
