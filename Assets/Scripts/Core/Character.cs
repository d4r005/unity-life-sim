using System;
using System.Collections.Generic;

/// <summary>
/// Personaje jugable. Clase de datos serializable (compatible con JsonUtility).
/// </summary>
[Serializable]
public class Character
{
    public string name = "";
    public string gender = "M";          // "M" o "F"
    public string country = "Mexico";
    public int age = 0;
    public int health = 100;
    public int happiness = 80;
    public int smarts = 50;
    public int looks = 50;
    public long money = 0;
    public string job = "";
    public int salary = 0;
    public List<string> flags = new List<string>();
    public bool alive = true;
    public int generation = 1;
    public long legacyScore = 0;
    public int portraitSeed = 0;   // semilla del retrato procedural

    public bool HasFlag(string f) { return flags != null && flags.Contains(f); }

    public void SetFlag(string f)
    {
        if (!HasFlag(f)) flags.Add(f);
    }

    public void ClearFlag(string f) { if (HasFlag(f)) flags.Remove(f); }

    public int GetStat(string stat)
    {
        switch (stat)
        {
            case "health": return health;
            case "happiness": return happiness;
            case "smarts": return smarts;
            case "looks": return looks;
            default: return 0;
        }
    }

    public void AddStat(string stat, int delta)
    {
        switch (stat)
        {
            case "health": health = Clamp(health + delta); break;
            case "happiness": happiness = Clamp(happiness + delta); break;
            case "smarts": smarts = Clamp(smarts + delta); break;
            case "looks": looks = Clamp(looks + delta); break;
        }
    }

    private static int Clamp(int v) { return Math.Max(0, Math.Min(100, v)); }
}
