using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Efectos que produce una decisión. Todos opcionales.</summary>
[Serializable]
public class ChoiceEffect
{
    public int health = 0;
    public int happiness = 0;
    public int smarts = 0;
    public int looks = 0;
    public long money = 0;
    public string flag = "";          // bandera que se activa al elegir
    public bool clearFlagInstead = false; // si true, la bandera se quita en vez de ponerse
    public bool addPartner = false;  // aparece una pareja
    public bool addChild = false;    // nace un hijo
    public int affectionPartner = 0; // cambio de afecto con la pareja
    public int affectionAll = 0;      // cambio de afecto con toda la familia
    public float deathChance = 0f;   // probabilidad de morir al elegir
    public string job = "";           // empleo obtenido
    public int salary = 0;            // sueldo anual
}

/// <summary>Una opción seleccionable de un evento.</summary>
[Serializable]
public class Choice
{
    public string text = "";
    public ChoiceEffect effects = new ChoiceEffect();
}

/// <summary>Un evento de vida con condiciones de aparición.</summary>
[Serializable]
public class LifeEvent
{
    public string id = "";
    public string title = "";
    public string text = "";
    public int minAge = 0;
    public int maxAge = 100;
    public int weight = 1;
    public string gender = "";          // "", "M" o "F"
    public string requiresFlag = "";   // el evento solo aparece si tienes esta bandera
    public string excludesFlag = "";   // ni aparece si tienes esta bandera
    public bool once = true;           // ocurre una sola vez por vida
    public Choice[] choices;
}

[Serializable]
public class EventFile
{
    public LifeEvent[] events;
}

/// <summary>Carga y filtra eventos desde Resources/Events/es.json.</summary>
public static class EventDatabase
{
    private static List<LifeEvent> all;
    private static HashSet<string> seenThisLife;

    public static void Reload()
    {
        all = new List<LifeEvent>();
        seenThisLife = new HashSet<string>();
        TextAsset asset = Resources.Load<TextAsset>("Events/es");
        if (asset == null)
        {
            Debug.LogError("No se encontró Resources/Events/es.json");
            return;
        }
        EventFile file = JsonUtility.FromJson<EventFile>(asset.text);
        if (file != null && file.events != null) all.AddRange(file.events);
        Debug.Log("Eventos cargados: " + all.Count);
    }

    /// <summary>Elige hasta 'count' eventos elegibles para este año.</summary>
    public static List<LifeEvent> PickForYear(Character ch, int count, System.Random rng)
    {
        if (all == null || all.Count == 0) Reload();
        if (seenThisLife == null) seenThisLife = new HashSet<string>();;
        List<LifeEvent> eligible = new List<LifeEvent>();
        foreach (LifeEvent e in all)
        {
            if (e == null || e.choices == null || e.choices.Length == 0) continue;
            if (ch.age < e.minAge || ch.age > e.maxAge) continue;
            if (!string.IsNullOrEmpty(e.gender) && e.gender != ch.gender) continue;
            if (!string.IsNullOrEmpty(e.requiresFlag) && !ch.HasFlag(e.requiresFlag)) continue;
            if (!string.IsNullOrEmpty(e.excludesFlag) && ch.HasFlag(e.excludesFlag)) continue;
            if (e.once && seenThisLife.Contains(e.id)) continue;
            eligible.Add(e);
        }
        List<LifeEvent> picked = new List<LifeEvent>();
        while (picked.Count < count && eligible.Count > 0)
        {
            int total = 0;
            foreach (LifeEvent e in eligible) total += Mathf.Max(1, e.weight);
            int ticket = rng.Next(total);
            int idx = 0;
            while (ticket >= Mathf.Max(1, eligible[idx].weight))
            {
                ticket -= Mathf.Max(1, eligible[idx].weight);
                idx++;
            }
            LifeEvent chosen = eligible[idx];
            picked.Add(chosen);
            seenThisLife.Add(chosen.id);
            eligible.RemoveAt(idx);
        }
        return picked;
    }

    public static void ResetSeen() { seenThisLife = null; }

    public static void MarkSeen(LifeEvent e)
    {
        if (seenThisLife == null) seenThisLife = new HashSet<string>();
        seenThisLife.Add(e.id);
    }
}
