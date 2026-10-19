using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Estado completo de la partida, serializable a JSON.</summary>
[Serializable]
public class SaveData
{
    public Character character = new Character();
    public FamilyTree family = new FamilyTree();
    public List<string> assets = new List<string>();
    public long dynastyLegacy = 0;     // legado acumulado de toda la dinastía
    public int currentYear = 1;         // años vividos del personaje actual
}

/// <summary>Guardado y carga en Application.persistentDataPath.</summary>
public static class SaveSystem
{
    private static string Path() { return System.IO.Path.Combine(Application.persistentDataPath, "lifesim-save.json"); }

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(Path(), JsonUtility.ToJson(data));
        }
        catch (Exception e) { Debug.LogWarning("No se pudo guardar: " + e.Message); }
    }

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(Path())) return null;
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path()));
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo cargar: " + e.Message);
            return null;
        }
    }

    public static void Delete()
    {
        try { if (File.Exists(Path())) File.Delete(Path()); } catch { }
    }

    public static bool Exists() { return File.Exists(Path()); }
}
