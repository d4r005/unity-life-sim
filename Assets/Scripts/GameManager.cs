using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Corazón del juego: máquina de estados Nacer → Eventos → Resumen → Año siguiente → ... → Muerte → Legado.
/// Se coloca en un GameObject vacío en una escena nueva y todo lo demás se genera solo.
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum State { Create, Events, Summary, Death, Legacy }

    private SaveData data;
    private State state;
    private readonly System.Random rng = new System.Random();
    private List<LifeEvent> yearEvents = new List<LifeEvent>();
    private int eventIndex;
    private readonly List<string> log = new List<string>();

    private Canvas canvas;
    private Text hudName, hudStats, logText;
    private GameObject cardPanel, createPanel, deathPanel, familyPanel;
    private Transform choicesHolder, deathHolder, familyHolder;
    private Text cardTitle, cardText;
    private InputField nameInput;
    private string chosenGender = "M";

    // Paleta sobria
    private static readonly Color Bg = new Color(0.10f, 0.10f, 0.12f, 1f);
    private static readonly Color Card = new Color(0.16f, 0.16f, 0.19f, 1f);
    private static readonly Color Accent = new Color(0.22f, 0.45f, 0.75f, 1f);
    private static readonly Color Danger = new Color(0.70f, 0.22f, 0.22f, 1f);
    private static readonly Color Btn = new Color(0.24f, 0.28f, 0.36f, 1f);

    private void Awake()
    {
        EventDatabase.Reload();
        canvas = UIBuilder.CreateCanvas();
        BuildUI();
        SaveData loaded = SaveSystem.Load();
        if (loaded != null && loaded.character != null && loaded.character.alive)
        {
            data = loaded;
            LogLine("Partida cargada. " + data.character.name + ", " + data.character.age + " años.");
            BeginYear();
        }
        else
        {
            ShowCreate();
        }
    }

    // ----------------------------------------------------------------- UI

    private void BuildUI()
    {
        Transform root = canvas.transform;
        UIBuilder.Panel(root, "Fondo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Bg);

        // Barra superior
        UIBuilder.Panel(root, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -110f), new Vector2(0f, 0f), Card);
        hudName = UIBuilder.MakeText(root, "HudName", "", 30, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -104f), new Vector2(-20f, -66f));
        hudStats = UIBuilder.MakeText(root, "HudStats", "", 24, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -64f), new Vector2(-20f, -24f));

        // Tarjeta central (eventos y resúmenes)
        cardPanel = UIBuilder.Panel(root, "Card", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -860f), new Vector2(-16f, -130f), Card).gameObject;
        cardTitle = UIBuilder.MakeText(cardPanel.transform, "Title", "", 36, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -110f), new Vector2(-20f, -20f));
        cardText = UIBuilder.MakeText(cardPanel.transform, "Body", "", 29, new Color(0.88f, 0.88f, 0.92f), TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(24f, -430f), new Vector2(-24f, -150f));
        choicesHolder = UIBuilder.Panel(cardPanel.transform, "Choices", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 16f), new Vector2(-16f, -320f), new Color(0f, 0f, 0f, 0f)).transform;

        // Panel familiar
        familyPanel = UIBuilder.Panel(root, "Family", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -700f), new Vector2(-16f, -130f), Card).gameObject;
        familyHolder = UIBuilder.Panel(familyPanel.transform, "FamilyHolder", Vector2.zero, Vector2.one,
            new Vector2(16f, 16f), new Vector2(-16f, -16f), new Color(0f, 0f, 0f, 0f)).transform;

        // Panel de muerte / legado
        deathPanel = UIBuilder.Panel(root, "Death", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -800f), new Vector2(-16f, -160f), new Color(0.25f, 0.08f, 0.08f, 1f)).gameObject;
        UIBuilder.MakeText(deathPanel.transform, "DeathTitle", "FIN DE LA VIDA", 40, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -60f), new Vector2(-20f, -10f));
        deathHolder = UIBuilder.Panel(deathPanel.transform, "DeathHolder", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 16f), new Vector2(-16f, -520f), new Color(0f, 0f, 0f, 0f)).transform;

        // Registro
        UIBuilder.Panel(root, "LogPanel", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 96f), new Vector2(-16f, 300f), Card);
        logText = UIBuilder.MakeText(root, "Log", "", 22, new Color(0.7f, 0.75f, 0.8f), TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 106f), new Vector2(-32f, 292f));

        // Botones inferiores
        UIBuilder.MakeButton(root, "BtnFamily", "Familia", 24, Btn,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(220f, 84f), ToggleFamily);
        UIBuilder.MakeButton(root, "BtnSave", "Guardar", 24, Btn,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 16f), new Vector2(-16f, 84f),
            () => { SaveSystem.Save(data); LogLine("Partida guardada."); });
        UIBuilder.MakeButton(root, "BtnNew", "Nueva vida", 24, Danger,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-390f, 16f), new Vector2(-176f, 84f), () => {
                SaveSystem.Delete();
                ShowCreate();
            });

        // Panel de creación de personaje
        createPanel = UIBuilder.Panel(root, "Create", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -360f), new Vector2(300f, 360f), Card).gameObject;
        UIBuilder.MakeText(createPanel.transform, "Title", "GENERACIONES", 46, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -80f), new Vector2(-20f, -10f));
        UIBuilder.MakeText(createPanel.transform, "Sub", "Tu vida. Tus decisiones. Tu dinastía.", 24,
            new Color(0.7f, 0.75f, 0.8f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -130f), new Vector2(-20f, -90f));
        nameInput = UIBuilder.MakeInput(createPanel.transform, "NameInput", "Tu nombre...",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -250f), new Vector2(-30f, -190f));
        UIBuilder.MakeButton(createPanel.transform, "Male", "Niño", 30, Btn,
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(30f, -320f), new Vector2(-8f, -250f),
            () => { chosenGender = "M"; });
        UIBuilder.MakeButton(createPanel.transform, "Female", "Niña", 30, Btn,
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(8f, -320f), new Vector2(-30f, -250f),
            () => { chosenGender = "F"; });
        UIBuilder.MakeButton(createPanel.transform, "Start", "Nacer", 34, Accent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -410f), new Vector2(-30f, -330f),
            StartLifeFromCreate);
    }

    // ----------------------------------------------------------------- creación

    private void StartLifeFromCreate()
    {
        string surname = NameGenerator.RandomSurname(rng);
        string first = string.IsNullOrWhiteSpace(nameInput.text) ? NameGenerator.RandomName(rng, chosenGender)
                                                                 : nameInput.text.Trim();
        Character ch = new Character();
        ch.name = first + " " + surname;
        ch.gender = chosenGender;
        ch.country = NameGenerator.RandomCountry(rng);
        ch.age = 0;
        ch.health = rng.Next(85, 100);
        ch.happiness = rng.Next(75, 95);
        ch.smarts = rng.Next(35, 70);
        ch.looks = rng.Next(25, 75);
        ch.generation = 1;

        data = new SaveData();
        data.character = ch;
        data.family = new FamilyTree();
        data.family.CreateDefaultParents(surname);
        if (data.dynastyLegacy == 0) data.dynastyLegacy = 0;

        LogLine("Nace " + ch.name + " en " + ch.country + ". ¡Bienvenido a la vida!");
        SaveSystem.Save(data);
        BeginYear();
    }

    // ----------------------------------------------------------------- ciclo de vida

    private void BeginYear()
    {
        if (data.character.alive == false) return;
        state = State.Events;
        int howMany = rng.NextDouble() < 0.45 ? 2 : 1;
        yearEvents = EventDatabase.PickForYear(data.character, howMany, rng);
        eventIndex = 0;
        if (yearEvents.Count == 0) ShowSummary();
        else ShowEvent();
    }

    private void ShowEvent()
    {
        state = State.Events;
        createPanel.SetActive(false);
        deathPanel.SetActive(false);
        familyPanel.SetActive(false);
        cardPanel.SetActive(true);
        LifeEvent e = yearEvents[eventIndex];
        cardTitle.text = e.title;
        cardText.text = e.text;
        UIBuilder.Clear(choicesHolder);
        int n = e.choices.Length;
        float h = 84f; float gap = 12f;
        for (int i = 0; i < n; i++)
        {
            Choice c = e.choices[i];
            float fromTop = (n - 1 - i) * (h + gap);
            RectTransform b = UIBuilder.MakeButton(choicesHolder, "Choice" + i, c.text, 26, Btn,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(fromTop + h)), new Vector2(0f, -fromTop),
                () => OnChoice(e, c)).GetComponent<RectTransform>();
            b.anchorMin = new Vector2(0f, 1f);
        }
        UpdateHud();
    }

    private void OnChoice(LifeEvent e, Choice c)
    {
        Character ch = data.character;
        ChoiceEffect fx = c.effects != null ? c.effects : new ChoiceEffect();
        ch.AddStat("health", fx.health);
        ch.AddStat("happiness", fx.happiness);
        ch.AddStat("smarts", fx.smarts);
        ch.AddStat("looks", fx.looks);
        ch.money += fx.money;
        if (!string.IsNullOrEmpty(fx.flag))
        {
            if (fx.clearFlagInstead) ch.ClearFlag(fx.flag); else ch.SetFlag(fx.flag);
        }
        if (!string.IsNullOrEmpty(fx.job))
        {
            ch.job = fx.job;
            ch.salary = fx.salary;
        }
        if (fx.addPartner && data.family.Partner == null)
        {
            Person p = data.family.AddPartner(NameGenerator.RandomSurname(rng), ch.age);
            ch.SetFlag("pareja");
            LogLine("¡Ahora sales con " + p.name + "!");
        }
        if (fx.affectionPartner != 0)
        {
            Person p = data.family.Partner;
            if (p != null) p.affection = Math.Max(0, Math.Min(100, p.affection + fx.affectionPartner));
        }
        if (fx.affectionAll != 0)
        {
            foreach (Person p in data.family.members) if (p.alive)
                p.affection = Math.Max(0, Math.Min(100, p.affection + fx.affectionAll));
        }
        if (fx.addChild && data.family.Partner != null && data.family.Partner.alive)
        {
            Person child = data.family.AddChild(ch.name.Split(' ').Length > 1 ? ch.name.Split(' ')[1] : NameGenerator.RandomSurname(rng));
            ch.SetFlag("hijos");
            LogLine("¡Nace " + child.name + "! Ahora eres " + (ch.gender == "F" ? "madre" : "padre") + ".");
        }
        if (fx.deathChance > 0f && rng.NextDouble() < fx.deathChance)
        {
            Die("Murió trágicamente en un accidente inesperado.");
            return;
        }
        LogLine("Año " + (ch.age) + ": " + c.text);
        eventIndex++;
        if (eventIndex < yearEvents.Count) ShowEvent();
        else ShowSummary();
    }

    private void ShowSummary()
    {
        state = State.Summary;
        createPanel.SetActive(false);
        deathPanel.SetActive(false);
        familyPanel.SetActive(false);
        cardPanel.SetActive(true);
        Character ch = data.character;
        cardTitle.text = "Año de vida: " + ch.age;
        string body =
            "Edad: " + ch.age + " años\n" +
            "Dinero: $" + ch.money + (ch.job.Length > 0 ? "   (empleo: " + ch.job + ", $" + ch.salary + "/año)" : "") + "\n\n" +
            "Salud " + ch.health + "   Felicidad " + ch.happiness + "\n" +
            "Inteligencia " + ch.smarts + "   Aspecto " + ch.looks + "\n" +
            "Legado de la dinastía: " + data.dynastyLegacy;
        cardText.text = body;
        UIBuilder.Clear(choicesHolder);
        UIBuilder.MakeButton(choicesHolder, "NextYear", "Avanzar un año", 30, Accent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(0f, 0f), AdvanceYear);
        UpdateHud();
        SaveSystem.Save(data);
    }

    private void AdvanceYear()
    {
        Character ch = data.character;
        ch.age++;
        data.currentYear++;

        // Envejecer a la familia (con posibles defunciones)
        foreach (string death in data.family.AgeOneYear(rng)) LogLine(death);

        // Ingresos y deriva de stats
        if (ch.salary > 0) ch.money += ch.salary;
        if (ch.age > 40) ch.AddStat("health", -(ch.age - 40) / 10);
        if (ch.happiness > 85) ch.AddStat("happiness", -1);
        if (ch.happiness < 15) ch.AddStat("happiness", 1);

        // Hitos escolares automáticos
        if (ch.age == 6) { ch.SetFlag("escuela"); LogLine("¡Empiezas la primaria!"); }
        if (ch.age == 12) { ch.SetFlag("secundaria"); }
        if (ch.age == 15) { ch.SetFlag("prepa"); }
        if (ch.age == 18) { ch.ClearFlag("escuela"); ch.ClearFlag("secundaria"); ch.ClearFlag("prepa"); LogLine("¡Te gradúas de la prepa!"); }
        if (ch.age == 65 && ch.job.Length > 0) LogLine("Te jubilas de " + ch.job + ". El sueldo se reduce.");
        if (ch.age == 65 && ch.job.Length > 0) ch.salary = ch.salary / 2;

        // Probabilidad de muerte por edad y salud
        double deathChance = 0.0;
        if (ch.age > 55) deathChance += (ch.age - 55) * 0.008;
        if (ch.health < 15) deathChance += 0.08;
        if (ch.age > 90) deathChance += 0.05;
        if (rng.NextDouble() < deathChance)
        {
            Die("Falleció plácidamente a los " + ch.age + " años.");
            return;
        }
        BeginYear();
    }

    // ----------------------------------------------------------------- muerte y legado

    private void Die(string reason)
    {
        Character ch = data.character;
        ch.alive = false;
        long legacy = ch.money / 2 + data.family.AliveChildren.Count * 8000 + ch.age * 150 + ch.smarts * 40;
        data.dynastyLegacy += legacy;
        state = State.Death;
        LogLine(ch.name + " " + reason);
        ShowDeath(reason, legacy);
        SaveSystem.Save(data);
    }

    private void ShowDeath(string reason, long legacyGained)
    {
        createPanel.SetActive(false);
        cardPanel.SetActive(false);
        familyPanel.SetActive(false);
        deathPanel.SetActive(true);
        UIBuilder.Clear(deathHolder);

        UIBuilder.MakeText(deathHolder, "Reason", reason, 28, new Color(1f, 0.85f, 0.85f), TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(10f, -120f), new Vector2(-10f, -10f));
        UIBuilder.MakeText(deathHolder, "Legacy",
            "Legado ganado: " + legacyGained + "\nLegado total de la dinastía: " + data.dynastyLegacy,
            26, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(10f, -170f), new Vector2(-10f, -120f));

        // Herederos adultos
        int y = -260;
        List<Person> heirs = data.family.AliveChildren.FindAll(c => c.age >= 18);
        bool any = false;
        foreach (Person heir in heirs)
        {
            any = true;
            Person captured = heir;
            UIBuilder.MakeButton(deathHolder, "Heir_" + captured.name, "Continuar como " + captured.name +
                " (" + captured.age + " años)", 24, Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, y - 70), new Vector2(-10f, y),
                () => ContinueAs(captured));
            y -= 82;
        }
        if (!any)
        {
            UIBuilder.MakeText(deathHolder, "NoHeirs",
                "No dejaste herederos adultos. La dinastía empieza de nuevo.", 24,
                new Color(1f, 0.8f, 0.8f), TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(10f, y - 60), new Vector2(-10f, y));
            y -= 80;
        }
        UIBuilder.MakeButton(deathHolder, "NewDynasty", any ? "Empezar otra vida" : "Nueva dinastía", 24, Danger,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, y - 70), new Vector2(-10f, y),
            () => { SaveSystem.Delete(); ShowCreate(); });
        UpdateHud();
    }

    private void ContinueAs(Person heir)
    {
        Character old = data.character;
        Character ch = new Character();
        ch.name = heir.name;
        ch.gender = heir.gender;
        ch.age = heir.age;
        ch.health = rng.Next(70, 95);
        ch.happiness = rng.Next(60, 85);
        ch.smarts = Math.Max(35, Math.Min(90, old.smarts + rng.Next(-15, 16)));
        ch.looks = rng.Next(30, 80);
        ch.money = old.money / 2;                      // herencia
        ch.generation = old.generation + 1;
        ch.SetFlag("hijos"); if (old.HasFlag("pareja")) ch.SetFlag("pareja");

        // La familia se reordena: el difunto entra como ancestro, los hermanos quedan
        FamilyTree f = new FamilyTree();
        f.members.Add(new Person(old.name, old.gender == "F" ? "madre" : "padre", old.gender, old.age, 90) { alive = false });
        Person oldPartner = data.family.Partner;
        if (oldPartner != null)
            f.members.Add(new Person(oldPartner.name, oldPartner.gender == "F" ? "madre" : "padre",
                oldPartner.gender, oldPartner.age, oldPartner.affection));
        foreach (Person c in data.family.Children)
            if (c.alive && c.name != heir.name)
                f.members.Add(new Person(c.name, c.relation, c.gender, c.age, c.affection));

        data.character = ch;
        data.family = f;
        EventDatabase.ResetSeen();
        LogLine("La historia continúa con " + ch.name + ", generación " + ch.generation + ".");
        SaveSystem.Save(data);
        BeginYear();
    }

    // ----------------------------------------------------------------- paneles

    private void ToggleFamily()
    {
        if (state == State.Create) return;
        bool show = !familyPanel.activeSelf;
        if (show) RefreshFamily();
        familyPanel.SetActive(show);
        cardPanel.SetActive(!show && state != State.Death);
        deathPanel.SetActive(!show && state == State.Death);
    }

    private void RefreshFamily()
    {
        UIBuilder.Clear(familyHolder);
        int y = 0;
        foreach (Person p in data.family.members)
        {
            string line = p.name + "  ·  " + p.relation + ", " + p.age + " años" +
                          (p.alive ? "  ·  afecto " + p.affection : "  ·  †");
            UIBuilder.MakeText(familyHolder, "F_" + p.name + y, line, 25,
                p.alive ? Color.white : new Color(0.6f, 0.6f, 0.6f),
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(8f, y - 36), new Vector2(-8f, y));
            y -= 40;
        }
        if (data.family.members.Count == 0)
            UIBuilder.MakeText(familyHolder, "Empty", "Todavía no conoces a nadie.", 24,
                new Color(0.7f, 0.7f, 0.75f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -30), new Vector2(-8f, 0));
    }

    private void ShowCreate()
    {
        state = State.Create;
        data = null;
        cardPanel.SetActive(false);
        deathPanel.SetActive(false);
        familyPanel.SetActive(false);
        createPanel.SetActive(true);
        if (nameInput != null) nameInput.text = "";
        UpdateHud();
    }

    // ----------------------------------------------------------------- hud y log

    private void UpdateHud()
    {
        if (data == null || data.character == null || !data.character.alive)
        {
            hudName.text = "GENERACIONES";
            hudStats.text = "Simulador de vida familiar";
            return;
        }
        Character ch = data.character;
        hudName.text = ch.name + "  ·  " + ch.age + " años  ·  Gen " + ch.generation;
        hudStats.text = "Salud " + ch.health + "   Felicidad " + ch.happiness +
                        "   Inteligencia " + ch.smarts + "   Aspecto " + ch.looks +
                        "   $" + ch.money;
    }

    private void LogLine(string line)
    {
        log.Add(line);
        if (log.Count > 6) log.RemoveAt(0);
        logText.text = string.Join("\n", log.ToArray());
    }
}
