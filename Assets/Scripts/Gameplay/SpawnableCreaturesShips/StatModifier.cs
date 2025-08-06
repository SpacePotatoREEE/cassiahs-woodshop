using UnityEngine;

using UnityEngine;

[System.Serializable]
public struct StatModifier
{
    public StatType Stat;
    public float Flat;     // +10
    public float Percent;  // 0.25 = +25 %
}
