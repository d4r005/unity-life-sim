using System.Collections.Generic;

/// <summary>Catálogos de contenido: empleos, actividades, bienes y delitos.</summary>
public class JobDef
{
    public string name;
    public int salary;
    public int minAge;
    public string reqFlag;     // "" ninguno, "universidad", "titulado"
    public int reqSmarts;
    public bool partTime;

    public JobDef(string name, int salary, int minAge, string reqFlag, int reqSmarts, bool partTime)
    { this.name = name; this.salary = salary; this.minAge = minAge; this.reqFlag = reqFlag; this.reqSmarts = reqSmarts; this.partTime = partTime; }
}

public class ActivityDef
{
    public string name;
    public int minAge;
    public long cost;
    public int health, happiness, smarts, looks;

    public ActivityDef(string name, int minAge, long cost, int h, int ha, int s, int l)
    { this.name = name; this.minAge = minAge; this.cost = cost; health = h; happiness = ha; smarts = s; looks = l; }
}

public class AssetDef
{
    public string name;
    public long price;
    public int happiness;

    public AssetDef(string name, long price, int happiness) { this.name = name; this.price = price; this.happiness = happiness; }
}

public class CrimeDef
{
    public string name;
    public int minAge;
    public int reqSmarts;
    public long reward;
    public string successLog;
    public string caughtLog;
    public long fine;
    public int happinessHit;

    public CrimeDef(string name, int minAge, int reqSmarts, long reward, long fine, int happinessHit, string sLog, string cLog)
    { this.name = name; this.minAge = minAge; this.reqSmarts = reqSmarts; this.reward = reward; this.fine = fine;
      happinessHit = happinessHit; successLog = sLog; caughtLog = cLog; }
}

public static class Catalog
{
    public static readonly List<JobDef> Jobs = new List<JobDef>
    {
        new JobDef("Mesero de medio tiempo", 60000, 16, "", 0, true),
        new JobDef("Repartidor", 70000, 16, "", 0, true),
        new JobDef("Cajero de tienda", 95000, 18, "", 25, false),
        new JobDef("Mesero profesional", 95000, 18, "", 0, false),
        new JobDef("Asistente de oficina", 130000, 18, "", 40, false),
        new JobDef("Vendedor", 140000, 18, "", 35, false),
        new JobDef("Técnico de soporte", 180000, 19, "universidad", 50, false),
        new JobDef("Programador junior", 240000, 20, "universidad", 55, false),
        new JobDef("Contador", 260000, 22, "titulado", 60, false),
        new JobDef("Analista senior", 300000, 24, "titulado", 65, false),
        new JobDef("Ingeniero industrial", 320000, 24, "titulado", 65, false),
        new JobDef("Abogado", 400000, 25, "titulado", 75, false),
        new JobDef("Médico residente", 420000, 26, "titulado", 85, false)
    };

    public static readonly List<ActivityDef> Activities = new List<ActivityDef>
    {
        new ActivityDef("Meditar (gratis)", 5, 0, 0, 4, 0, 0),
        new ActivityDef("Jugar en el parque", 3, 0, 1, 3, 0, 0),
        new ActivityDef("Biblioteca (gratis)", 6, 0, 0, 1, 3, 0),
        new ActivityDef("Cine con amigos", 8, 400, 0, 4, 0, 0),
        new ActivityDef("Gimnasio", 14, 1800, 4, 1, 0, 1),
        new ActivityDef("Ir al doctor", 3, 6000, 6, 0, 0, 0),
        new ActivityDef("Fiesta con amigos", 18, 2500, -2, 7, 0, 0),
        new ActivityDef("Barbería / estilista", 12, 600, 0, 1, 0, 2),
        new ActivityDef("Vacaciones en la playa", 18, 45000, 3, 10, 0, 1),
        new ActivityDef("Cirugía estética", 20, 90000, -2, 4, 0, 9)
    };

    public static readonly List<AssetDef> Assets = new List<AssetDef>
    {
        new AssetDef("Bicicleta nueva", 8000, 3),
        new AssetDef("Auto usado", 80000, 5),
        new AssetDef("Auto nuevo", 200000, 9),
        new AssetDef("Casa pequeña", 500000, 14),
        new AssetDef("Casa grande", 1500000, 20),
        new AssetDef("Mansión", 5000000, 30)
    };

    public static readonly List<CrimeDef> Crimes = new List<CrimeDef>
    {
        new CrimeDef("Robar una cartera", 14, 0, 4000, 5000, 5,
            "Robaste una cartera sin que nadie te viera.", "Te atraparon robando una cartera. Multa y vergüenza."),
        new CrimeDef("Shoplifting", 14, 10, 3000, 6000, 6,
            "Saliste de la tienda con la chamarra sin pagar.", "El guardia te atrapó. Pagaste la multa."),
        new CrimeDef("Atraco a una tienda", 18, 40, 25000, 30000, 15,
            "El atraco salió perfecto. Nadie salió lastimado.", "Te arrestaron por el atraco. Pagaste fianza y multa."),
        new CrimeDef("Estafa por internet", 18, 60, 60000, 50000, 12,
            "La estafa funcionó: cayó un incauto.", "La policía rastreó tu estafa. Multa récord.")
    };
}
