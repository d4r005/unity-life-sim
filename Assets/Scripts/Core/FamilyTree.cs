using System;
using System.Collections.Generic;

/// <summary>
/// Miembro de la familia / persona cercana al personaje jugable.
/// relation: padre, madre, hermano, hermana, pareja, hijo, hija, amigo, abuelo...
/// </summary>
[Serializable]
public class Person
{
    public string name = "";
    public string relation = "";
    public string gender = "M";
    public int age = 30;
    public bool alive = true;
    public int affection = 50;   // 0..100
    public int portraitSeed = 0; // semilla del retrato cartoon
    public string partnerOf = ""; // nombre de la persona con la que está casado/a

    public Person() { }

    public Person(string name, string relation, string gender, int age, int affection)
    {
        this.name = name;
        this.relation = relation;
        this.gender = gender;
        this.age = age;
        this.affection = affection;
    }
}

/// <summary>
/// El árbol familiar completo del personaje.
/// </summary>
[Serializable]
public class FamilyTree
{
    public List<Person> members = new List<Person>();

    public Person Partner
    {
        get { return members.Find(m => m.relation == "pareja" && m.alive); }
    }

    public List<Person> Children
    {
        get { return members.FindAll(m => (m.relation == "hijo" || m.relation == "hija")); }
    }

    public List<Person> AliveChildren
    {
        get { return members.FindAll(m => (m.relation == "hijo" || m.relation == "hija") && m.alive); }
    }

    public Person Parent1 { get { return members.Find(m => m.relation == "padre"); } }
    public Person Parent2 { get { return members.Find(m => m.relation == "madre"); } }

    public void Add(Person p) { members.Add(p); }

    /// <summary>Envejece a todos los vivos; los mayores pueden fallecer. Devuelve mensajes de defunción.</summary>
    public List<string> AgeOneYear(System.Random rng)
    {
        List<string> deaths = new List<string>();
        for (int i = 0; i < members.Count; i++)
        {
            Person m = members[i];
            if (!m.alive) continue;
            m.age++;
            // el afecto se enfría un poco si no hay eventos que lo cuiden
            m.affection = Math.Max(0, Math.Min(100, m.affection - 1));
            if (m.age > 68)
            {
                double chance = (m.age - 68) * 0.015 + 0.02;
                if (rng.NextDouble() < chance)
                {
                    m.alive = false;
                    deaths.Add(m.name + " (" + m.relation + ") falleció a los " + m.age + " años.");
                }
            }
        }
        return deaths;
    }

    public void CreateDefaultParents(string surname)
    {
        Person dad = new Person(NameGenerator.RandomMale(rngStatic) + " " + surname, "padre", "M", 28, 90);
        Person mom = new Person(NameGenerator.RandomFemale(rngStatic) + " " + surname, "madre", "F", 26, 90);
        dad.portraitSeed = rngStatic.Next(); mom.portraitSeed = rngStatic.Next();
        members.Add(dad); members.Add(mom);
        // un hermano con probabilidad
        if (rngStatic.NextDouble() < 0.6)
        {
            bool male = rngStatic.NextDouble() < 0.5;
            string n = male ? NameGenerator.RandomMale(rngStatic) : NameGenerator.RandomFemale(rngStatic);
            Person sib = new Person(n + " " + surname, male ? "hermano" : "hermana", male ? "M" : "F",
                rngStatic.Next(2, 8), 60);
            sib.portraitSeed = rngStatic.Next();
            members.Add(sib);
        }
    }

    public Person AddPartner(string surname, int playerAge, string playerGender, string orientation)
    {
        bool male;
        if (orientation == "B") male = rngStatic.NextDouble() < 0.5;      // bisexual: cualquiera
        else if (orientation == "G") male = playerGender == "M";          // gay: mismo genero
        else male = playerGender != "M";                                  // hetero: genero opuesto
        string n = (male ? NameGenerator.RandomMale(rngStatic) : NameGenerator.RandomFemale(rngStatic)) + " " + surname;
        Person p = new Person(n, "pareja", male ? "M" : "F", Math.Max(18, playerAge + rngStatic.Next(-4, 5)), 75);
        p.portraitSeed = rngStatic.Next();
        members.Add(p);
        return p;
    }

    public Person AddChild(string surname)
    {
        bool male = rngStatic.NextDouble() < 0.5;
        string n = (male ? NameGenerator.RandomMale(rngStatic) : NameGenerator.RandomFemale(rngStatic)) + " " + surname;
        Person p = new Person(n, male ? "hijo" : "hija", male ? "M" : "F", 0, 95);
        p.portraitSeed = rngStatic.Next();
        members.Add(p);
        return p;
    }

    private static readonly System.Random rngStatic = new System.Random();
}
