using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Corazón del juego: nacer → eventos → resumen → año siguiente → ... → muerte → legado,
/// con menús de acción estilo BitLife: Ocupación, Actividades, Relaciones, Bienes y Delitos.
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum State { Create, Events, Summary, Death, Menu }

    private class MenuRow
    {
        public string label;
        public bool enabled = true;
        public Action onClick;
        public MenuRow(string label, Action onClick, bool enabled = true)
        { this.label = label; this.onClick = onClick; this.enabled = enabled; }
    }

    private SaveData data;
    private State state;
    private readonly System.Random rng = new System.Random();
    private List<LifeEvent> yearEvents = new List<LifeEvent>();
    private int eventIndex;
    private readonly List<string> log = new List<string>();
    private bool menuFromEvents = false;

    private Canvas canvas;
    private Text hudName, hudMoney, logText;
    private readonly Text[] statTexts = new Text[4];
    private readonly Image[] statBars = new Image[4];
    private Image avatarImage;
    private Button advanceButton;
    private Image advanceImage;
    private int lastPortraitSeed = -1;
    private static readonly string[] StatNames = { "Salud", "Felicidad", "Intel", "Aspecto" };
    private GameObject cardPanel, createPanel, deathPanel, menuPanel;
    private Transform choicesHolder, deathHolder, menuHolder;
    private Text cardTitle, cardText, menuTitle;
    private InputField nameInput;
    private string chosenGender = "M";

    // Paleta sobria
    private static readonly Color Bg = new Color(0.10f, 0.10f, 0.12f, 1f);
    private static readonly Color Card = new Color(0.16f, 0.16f, 0.19f, 1f);
    private static readonly Color Accent = new Color(0.22f, 0.45f, 0.75f, 1f);
    private static readonly Color Danger = new Color(0.70f, 0.22f, 0.22f, 1f);
    private static readonly Color Btn = new Color(0.24f, 0.28f, 0.36f, 1f);
    private static readonly Color BtnOff = new Color(0.18f, 0.18f, 0.20f, 1f);
    private static readonly Color Ok = new Color(0.25f, 0.45f, 0.25f, 1f);

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

        // ---- Encabezado: avatar, nombre, dinero y chips de stats
        UIBuilder.Panel(root, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -150f), new Vector2(0f, 0f), Card);
        GameObject avatarGo = new GameObject("Avatar", typeof(Image));
        avatarGo.transform.SetParent(root, false);
        RectTransform art = avatarGo.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f); art.anchorMax = new Vector2(0f, 1f);
        art.offsetMin = new Vector2(20f, -122f); art.offsetMax = new Vector2(110f, -32f);
        avatarImage = avatarGo.GetComponent<Image>();

        hudName = UIBuilder.MakeText(root, "HudName", "", 26, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(116f, -70f), new Vector2(-190f, -34f),
            FontStyle.Bold);
        hudMoney = UIBuilder.MakeText(root, "HudMoney", "", 26, new Color(1f, 0.9f, 0.55f), TextAnchor.MiddleRight,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -70f), new Vector2(-20f, -34f),
            FontStyle.Bold);
        UIBuilder.MakeButton(root, "BtnNewLife", "Nueva vida", 18, Danger,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -140f), new Vector2(-16f, -104f),
            () => { SaveSystem.Delete(); ShowCreate(); });

        Color[] statColors = {
            new Color(0.30f, 0.72f, 0.38f), new Color(0.92f, 0.74f, 0.25f),
            new Color(0.35f, 0.75f, 0.92f), new Color(0.92f, 0.45f, 0.62f)
        };
        for (int i = 0; i < 4; i++)
        {
            float x0 = 20f + i * 166f;
            UIBuilder.Panel(root, "Chip" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0, -140f), new Vector2(x0 + 158f, -84f), new Color(0.13f, 0.14f, 0.18f, 1f));
            UIBuilder.Plain(root, "Dot" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -106f), new Vector2(x0 + 24f, -94f), statColors[i]);
            statTexts[i] = UIBuilder.MakeText(root, "StatTxt" + i, "", 17, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 30f, -108f), new Vector2(x0 + 150f, -92f));
            UIBuilder.Plain(root, "BarBg" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -130f), new Vector2(x0 + 146f, -118f), new Color(0.08f, 0.08f, 0.10f, 1f));
            Image fill = UIBuilder.Plain(root, "BarFill" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -130f), new Vector2(x0 + 146f, -118f), statColors[i]);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            statBars[i] = fill;
        }

        // ---- Tarjeta central (eventos y resúmenes)
        cardPanel = UIBuilder.Panel(root, "Card", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -820f), new Vector2(-16f, -166f), Card).gameObject;
        cardTitle = UIBuilder.MakeText(cardPanel.transform, "Title", "", 32, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(24f, -232f), new Vector2(-24f, -180f), FontStyle.Bold);
        cardText = UIBuilder.MakeText(cardPanel.transform, "Body", "", 27, new Color(0.88f, 0.88f, 0.92f), TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(28f, -460f), new Vector2(-28f, -250f));
        choicesHolder = UIBuilder.Panel(cardPanel.transform, "Choices", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 18f), new Vector2(-16f, 340f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Panel de menús
        menuPanel = UIBuilder.Panel(root, "Menu", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -820f), new Vector2(-16f, -166f), Card).gameObject;
        menuTitle = UIBuilder.MakeText(menuPanel.transform, "MenuTitle", "", 32, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(24f, -232f), new Vector2(-24f, -180f), FontStyle.Bold);
        menuHolder = UIBuilder.Panel(menuPanel.transform, "MenuHolder", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(12f, 18f), new Vector2(-12f, -96f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Panel de muerte / legado
        deathPanel = UIBuilder.Panel(root, "Death", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -820f), new Vector2(-16f, -166f), new Color(0.25f, 0.08f, 0.08f, 1f)).gameObject;
        UIBuilder.MakeText(deathPanel.transform, "DeathTitle", "FIN DE LA VIDA", 40, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -236f), new Vector2(-20f, -184f), FontStyle.Bold);
        deathHolder = UIBuilder.Panel(deathPanel.transform, "DeathHolder", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 18f), new Vector2(-16f, -190f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Registro
        UIBuilder.Panel(root, "LogPanel", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -1052f), new Vector2(-16f, -836f), Card);
        logText = UIBuilder.MakeText(root, "Log", "", 20, new Color(0.68f, 0.72f, 0.78f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -1044f), new Vector2(-30f, -848f));

        // ---- Botón grande de avanzar año
        advanceButton = UIBuilder.MakeButton(root, "BtnAdvance", "Avanzar un año  +", 32, Accent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -1160f), new Vector2(-16f, -1068f),
            AdvanceYear);
        advanceImage = advanceButton.GetComponent<Image>();

        // ---- Barra de menús inferior
        string[] menuNames = { "Familia", "Ocupación", "Actividad", "Bienes", "Delitos" };
        Action[] menuActions = {
            () => OpenFamilyMenu(),
            () => OpenOccupationMenu(),
            () => OpenActivitiesMenu(),
            () => OpenAssetsMenu(),
            () => OpenCrimesMenu()
        };
        for (int i = 0; i < menuNames.Length; i++)
        {
            int idx = i;
            UIBuilder.MakeButton(root, "Menu_" + menuNames[i], menuNames[i], 22, Btn,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f + idx * 139f, 16f), new Vector2(148f + idx * 139f, 100f),
                () => menuActions[idx]());
        }

        // ---- Panel de creación de personaje
        createPanel = UIBuilder.Panel(root, "Create", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -360f), new Vector2(300f, 360f), Card).gameObject;
        UIBuilder.MakeText(createPanel.transform, "Title", "GENERACIONES", 46, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -80f), new Vector2(-20f, -10f), FontStyle.Bold);
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

    private void SetAdvanceVisible(bool visible)
    {
        advanceButton.gameObject.SetActive(visible);
    }

    private void SetAdvanceEnabled(bool enabled)
    {
        advanceButton.interactable = enabled;
        advanceImage.color = enabled ? Accent : BtnOff;
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
        ch.portraitSeed = rng.Next(1, 1000000);

        data = new SaveData();
        data.character = ch;
        data.family = new FamilyTree();
        data.family.CreateDefaultParents(surname);

        LogLine("Nace " + ch.name + " en " + ch.country + ". ¡Bienvenido a la vida!");
        SaveSystem.Save(data);
        BeginYear();
    }

    // ----------------------------------------------------------------- ciclo de vida

    private void BeginYear()
    {
        if (!data.character.alive) return;
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
        menuPanel.SetActive(false);
        cardPanel.SetActive(true);
        SetAdvanceVisible(true);
        SetAdvanceEnabled(false);
        LifeEvent e = yearEvents[eventIndex];
        cardTitle.text = e.title;
        cardText.text = e.text;
        UIBuilder.Clear(choicesHolder);
        int n = e.choices.Length;
        float h = 92f; float gap = 10f;
        for (int i = 0; i < n; i++)
        {
            Choice c = e.choices[i];
            float fromTop = (n - 1 - i) * (h + gap);
            UIBuilder.MakeButton(choicesHolder, "Choice" + i, c.text, 26, Btn,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(fromTop + h)), new Vector2(0f, -fromTop),
                () => OnChoice(e, c));
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
            Person child = data.family.AddChild(ch.name.Contains(" ") ? ch.name.Split(' ')[1] : NameGenerator.RandomSurname(rng));
            ch.SetFlag("hijos");
            LogLine("¡Nace " + child.name + "!");
        }
        if (fx.deathChance > 0f && rng.NextDouble() < fx.deathChance)
        {
            Die("Murió trágicamente en un accidente inesperado.");
            return;
        }
        LogLine("Año " + ch.age + ": " + c.text);
        eventIndex++;
        SaveSystem.Save(data);
        if (eventIndex < yearEvents.Count) ShowEvent();
        else ShowSummary();
    }

    private void ShowSummary()
    {
        state = State.Summary;
        createPanel.SetActive(false);
        deathPanel.SetActive(false);
        menuPanel.SetActive(false);
        cardPanel.SetActive(true);
        Character ch = data.character;
        cardTitle.text = "Año de vida: " + ch.age;
        string assetsLine = data.assets.Count > 0 ? string.Join(", ", data.assets.ToArray()) : "ninguno";
        cardText.text =
            "Edad: " + ch.age + " años\n" +
            "Dinero: $" + ch.money + (ch.job.Length > 0 ? "\nEmpleo: " + ch.job + " ($" + ch.salary + "/año)" : "") + "\n\n" +
            "Salud " + ch.health + "   Felicidad " + ch.happiness + "\n" +
            "Inteligencia " + ch.smarts + "   Aspecto " + ch.looks + "\n" +
            "Bienes: " + assetsLine + "\n\n" +
            "Legado de la dinastía: " + data.dynastyLegacy;
        UIBuilder.Clear(choicesHolder);
        UIBuilder.MakeText(choicesHolder, "Hint",
            "Usa los menús de abajo para actuar.\nCuando estés listo, pulsa el botón azul.",
            24, new Color(0.6f, 0.65f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, 0f));
        SetAdvanceVisible(true);
        SetAdvanceEnabled(true);
        UpdateHud();
        SaveSystem.Save(data);
    }

    private void AdvanceYear()
    {
        Character ch = data.character;
        ch.age++;
        data.currentYear++;

        foreach (string death in data.family.AgeOneYear(rng)) LogLine(death);

        if (ch.salary > 0) ch.money += ch.salary;
        if (ch.age > 40) ch.AddStat("health", -(ch.age - 40) / 10);
        if (ch.happiness > 85) ch.AddStat("happiness", -1);
        if (ch.happiness < 15) ch.AddStat("happiness", 1);

        if (ch.age == 6) { ch.SetFlag("escuela"); LogLine("¡Empiezas la primaria!"); }
        if (ch.age == 12) { ch.SetFlag("secundaria"); }
        if (ch.age == 15) { ch.SetFlag("prepa"); }
        if (ch.age == 18) { ch.ClearFlag("escuela"); ch.ClearFlag("secundaria"); ch.ClearFlag("prepa"); LogLine("¡Te gradúas de la prepa!"); }
        if (ch.age == 65 && ch.job.Length > 0) { ch.salary = ch.salary / 2; LogLine("Te jubilas de " + ch.job + "."); }

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
        menuPanel.SetActive(false);
        deathPanel.SetActive(true);
        SetAdvanceVisible(false);
        UIBuilder.Clear(deathHolder);

        UIBuilder.MakeText(deathHolder, "Reason", reason, 28, new Color(1f, 0.85f, 0.85f), TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(10f, -70f), new Vector2(-10f, -10f));
        UIBuilder.MakeText(deathHolder, "Legacy",
            "Legado ganado: " + legacyGained + "\nLegado total de la dinastía: " + data.dynastyLegacy,
            26, Color.white, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(10f, -130f), new Vector2(-10f, -74f));

        int y = -160;
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
        ch.money = old.money / 2;
        ch.generation = old.generation + 1;
        ch.portraitSeed = rng.Next(1, 1000000);

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

    // ----------------------------------------------------------------- menús estilo BitLife

    private void OpenMenu(string title, List<MenuRow> rows)
    {
        if (state == State.Create || state == State.Death) return;
        if (state != State.Menu) menuFromEvents = (state == State.Events);
        state = State.Menu;
        createPanel.SetActive(false);
        deathPanel.SetActive(false);
        cardPanel.SetActive(false);
        menuPanel.SetActive(true);
        menuTitle.text = title;
        UIBuilder.Clear(menuHolder);

        float y = 0f;
        int shown = 0;
        foreach (MenuRow row in rows)
        {
            if (shown >= 9) break;
            MenuRow captured = row;
            UIBuilder.MakeButton(menuHolder, "Row" + shown, captured.label, 22,
                captured.enabled ? Btn : BtnOff,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(8f, -(y + 54f)), new Vector2(-8f, -y),
                captured.enabled ? (UnityEngine.Events.UnityAction)(() => { captured.onClick(); }) : null);
            y += 62f;
            shown++;
        }
        UIBuilder.MakeButton(menuHolder, "Back", "Volver", 22, Danger,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(8f, -(y + 54f)), new Vector2(-8f, -y), CloseMenu);
        UpdateHud();
    }

    private void CloseMenu()
    {
        menuPanel.SetActive(false);
        if (menuFromEvents && eventIndex < yearEvents.Count) ShowEvent();
        else ShowSummary();
    }

    // ---- familia y relaciones

    private void OpenFamilyMenu()
    {
        Character ch = data.character;
        List<MenuRow> rows = new List<MenuRow>();

        if (ch.age >= 18 && data.family.Partner == null)
            rows.Add(new MenuRow("Buscar pareja (app de citas)", DoDating));
        if (ch.age >= 6 && CountFriends() < 3)
            rows.Add(new MenuRow("Buscar un amigo", DoMakeFriend));

        foreach (Person p in data.family.members)
        {
            Person captured = p;
            string label = p.name + "  ·  " + p.relation + ", " + p.age + " años" +
                           (p.alive ? "  ·  afecto " + p.affection : "  ·  †");
            rows.Add(new MenuRow(label, () => OpenMemberMenu(captured)));
        }
        OpenMenu("Familia y relaciones", rows);
    }

    private void OpenMemberMenu(Person p)
    {
        List<MenuRow> rows = new List<MenuRow>();
        rows.Add(new MenuRow("Conversar (afecto +3)", () => TalkTo(p, 3, "Pasaste un rato agradable con " + p.name + ".")));
        rows.Add(new MenuRow("Dar un cumplido (afecto +4)", () => TalkTo(p, 4, p.name + " agradeció tu cumplido.")));
        rows.Add(new MenuRow("Regalar $2,000 (afecto +8)", () => GiftTo(p), data.character.money >= 2000));
        rows.Add(new MenuRow("Pedir dinero", () => AskMoney(p), p.alive && p.affection >= 55));
        rows.Add(new MenuRow("Insultar (afecto -12)", () => TalkTo(p, -12, "Le gritaste a " + p.name + ". Se molestó.")));
        OpenMenu(p.name, rows);
    }

    private void TalkTo(Person p, int delta, string message)
    {
        p.affection = Math.Max(0, Math.Min(100, p.affection + delta));
        data.character.AddStat("happiness", delta > 0 ? 1 : -1);
        LogLine(message);
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void GiftTo(Person p)
    {
        data.character.money -= 2000;
        p.affection = Math.Max(0, Math.Min(100, p.affection + 8));
        LogLine("Le regalaste algo especial a " + p.name + ". Afecto +8.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void AskMoney(Person p)
    {
        long amount = 3000 + rng.Next(0, 3000);
        data.character.money += amount;
        p.affection = Math.Max(0, p.affection - 8);
        LogLine(p.name + " te prestó $" + amount + ".");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoDating()
    {
        Character ch = data.character;
        double chance = 0.15 + ch.looks * 0.007;
        if (rng.NextDouble() < chance && data.family.Partner == null)
        {
            Person p = data.family.AddPartner(NameGenerator.RandomSurname(rng), ch.age);
            ch.SetFlag("pareja");
            LogLine("¡Genial! " + p.name + " aceptó salir contigo.");
        }
        else LogLine("Nadie respondió tus mensajes... por ahora.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoMakeFriend()
    {
        Character ch = data.character;
        if (rng.NextDouble() < 0.4 + ch.happiness * 0.004)
        {
            bool male = rng.NextDouble() < 0.5;
            string n = (male ? NameGenerator.RandomMale(rng) : NameGenerator.RandomFemale(rng)) +
                       " " + NameGenerator.RandomSurname(rng);
            data.family.members.Add(new Person(n, male ? "amigo" : "amiga", male ? "M" : "F",
                Math.Max(6, ch.age + rng.Next(-4, 5)), 55));
            LogLine("Hiciste una nueva amistad: " + n + ".");
        }
        else LogLine("Nadie pareció interesado en platicar contigo hoy.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private int CountFriends()
    {
        int n = 0;
        foreach (Person p in data.family.members)
            if ((p.relation == "amigo" || p.relation == "amiga") && p.alive) n++;
        return n;
    }

    // ---- ocupación

    private void OpenOccupationMenu()
    {
        Character ch = data.character;
        List<MenuRow> rows = new List<MenuRow>();

        if (ch.job.Length > 0)
        {
            rows.Add(new MenuRow("Pedir aumento (sueldo: $" + ch.salary + ")", DoAskRaise));
            rows.Add(new MenuRow("Trabajar duro (+posible ascenso)", DoWorkHard));
            rows.Add(new MenuRow("Renunciar a " + ch.job, DoQuitJob));
        }

        if (ch.age >= 18 && !ch.HasFlag("universidad") && !ch.HasFlag("titulado"))
            rows.Add(new MenuRow("Inscribirse a la universidad (-$50,000)", DoEnrollUniversity, ch.money >= 50000));

        if (ch.HasFlag("universidad") && !ch.HasFlag("titulado"))
            rows.Add(new MenuRow("Estudiar para el examen (inteligencia +3)", DoStudy));

        bool wantPartTime = ch.age < 18;
        List<JobDef> eligible = new List<JobDef>();
        foreach (JobDef j in Catalog.Jobs)
        {
            if (ch.age < j.minAge) continue;
            if (!string.IsNullOrEmpty(j.reqFlag) && !ch.HasFlag(j.reqFlag)) continue;
            if (j.partTime != wantPartTime) continue;
            if (ch.job == j.name) continue;
            eligible.Add(j);
        }
        eligible.Sort((a, b) => b.salary.CompareTo(a.salary));
        int shown = 0;
        foreach (JobDef j in eligible)
        {
            if (shown >= 5) break;
            JobDef captured = j;
            rows.Add(new MenuRow("Solicitar: " + j.name + " ($" + j.salary + "/año)", () => DoApplyJob(captured)));
            shown++;
        }
        if (rows.Count == 0)
            rows.Add(new MenuRow("No hay nada disponible a tu edad todavía.", null, false));
        OpenMenu("Ocupación", rows);
    }

    private void DoApplyJob(JobDef j)
    {
        Character ch = data.character;
        double chance = Math.Max(0.15, Math.Min(0.95, 0.25 + (ch.smarts - j.reqSmarts) * 0.01));
        if (rng.NextDouble() < chance)
        {
            ch.job = j.name;
            ch.salary = j.salary;
            ch.SetFlag("empleo");
            ch.AddStat("happiness", 5);
            LogLine("¡Te contrataron como " + j.name + "! Sueldo: $" + j.salary + "/año.");
        }
        else
        {
            LogLine("No quedaste en el puesto de " + j.name + ". Suerte la próxima.");
        }
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoAskRaise()
    {
        Character ch = data.character;
        double chance = Math.Max(0.10, Math.Min(0.85, 0.2 + ch.smarts * 0.006));
        if (rng.NextDouble() < chance)
        {
            int raise = (int)(ch.salary * 0.12);
            ch.salary += raise;
            LogLine("¡Tu jefe te dio un aumento de $" + raise + "!");
        }
        else LogLine("Tu jefe dijo que 'ahora no es buen momento'.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoWorkHard()
    {
        Character ch = data.character;
        ch.AddStat("happiness", -2);
        if (rng.NextDouble() < 0.3 + ch.smarts * 0.004)
        {
            int raise = (int)(ch.salary * 0.20);
            ch.salary += raise;
            LogLine("Tu esfuerzo dio frutos: te promovieron. Sueldo ahora $" + ch.salary + "/año.");
        }
        else LogLine("Trabajaste hasta tarde. Estás exhausto pero tu jefe te notó.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoQuitJob()
    {
        Character ch = data.character;
        LogLine("Renunciaste a tu puesto de " + ch.job + ".");
        ch.job = "";
        ch.salary = 0;
        ch.AddStat("happiness", 2);
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoEnrollUniversity()
    {
        Character ch = data.character;
        ch.money -= 50000;
        ch.SetFlag("universidad");
        ch.AddStat("smarts", 4);
        LogLine("¡Entraste a la universidad! -$50,000 de inscripción.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoStudy()
    {
        Character ch = data.character;
        ch.AddStat("smarts", 3);
        ch.AddStat("happiness", -1);
        if (ch.age >= 22 && ch.smarts >= 65 && rng.NextDouble() < 0.4)
        {
            ch.SetFlag("titulado");
            LogLine("¡Te titulaste de la universidad!");
        }
        else LogLine("Estudiaste toda la tarde. Inteligencia +3.");
        SaveSystem.Save(data);
        CloseMenu();
    }

    // ---- actividades

    private void OpenActivitiesMenu()
    {
        Character ch = data.character;
        List<MenuRow> rows = new List<MenuRow>();
        foreach (ActivityDef a in Catalog.Activities)
        {
            if (ch.age < a.minAge) continue;
            ActivityDef captured = a;
            string label = a.name + (a.cost > 0 ? "  ·  -$" + a.cost : "  ·  gratis");
            rows.Add(new MenuRow(label, () => DoActivity(captured), ch.money >= a.cost));
        }
        if (rows.Count == 0) rows.Add(new MenuRow("Todavía eres muy pequeño para esto.", null, false));
        OpenMenu("Actividades", rows);
    }

    private void DoActivity(ActivityDef a)
    {
        Character ch = data.character;
        ch.money -= a.cost;
        ch.AddStat("health", a.health);
        ch.AddStat("happiness", a.happiness);
        ch.AddStat("smarts", a.smarts);
        ch.AddStat("looks", a.looks);
        LogLine("Actividad: " + a.name + ".");
        SaveSystem.Save(data);
        CloseMenu();
    }

    // ---- bienes

    private void OpenAssetsMenu()
    {
        Character ch = data.character;
        List<MenuRow> rows = new List<MenuRow>();
        foreach (AssetDef a in Catalog.Assets)
        {
            AssetDef captured = a;
            if (data.assets.Contains(a.name))
                rows.Add(new MenuRow("Vender: " + a.name + " (+$" + a.price / 2 + ")", () => DoSellAsset(captured)));
            else
                rows.Add(new MenuRow("Comprar: " + a.name + "  ·  -$" + a.price,
                    () => DoBuyAsset(captured), ch.money >= a.price));
        }
        OpenMenu("Bienes", rows);
    }

    private void DoBuyAsset(AssetDef a)
    {
        Character ch = data.character;
        ch.money -= a.price;
        ch.AddStat("happiness", a.happiness);
        data.assets.Add(a.name);
        LogLine("¡Compraste " + a.name + " por $" + a.price + "!");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoSellAsset(AssetDef a)
    {
        Character ch = data.character;
        ch.money += a.price / 2;
        ch.AddStat("happiness", -3);
        data.assets.Remove(a.name);
        LogLine("Vendiste " + a.name + " por $" + a.price / 2 + ".");
        SaveSystem.Save(data);
        CloseMenu();
    }

    // ---- delitos

    private void OpenCrimesMenu()
    {
        Character ch = data.character;
        List<MenuRow> rows = new List<MenuRow>();
        foreach (CrimeDef c in Catalog.Crimes)
        {
            if (ch.age < c.minAge) continue;
            CrimeDef captured = c;
            rows.Add(new MenuRow(captured.name, () => DoCrime(captured)));
        }
        if (rows.Count == 0) rows.Add(new MenuRow("Todavía no tienes edad para estas travesuras.", null, false));
        OpenMenu("Delitos", rows);
    }

    private void DoCrime(CrimeDef c)
    {
        Character ch = data.character;
        double chance = Math.Max(0.10, Math.Min(0.85, 0.25 + (ch.smarts - c.reqSmarts) * 0.008));
        if (rng.NextDouble() < chance)
        {
            ch.money += c.reward;
            ch.AddStat("happiness", 3);
            LogLine(c.successLog + " Ganaste $" + c.reward + ".");
        }
        else
        {
            ch.money -= c.fine;
            ch.AddStat("happiness", -c.happinessHit);
            ch.SetFlag("antecedentes");
            LogLine(c.caughtLog + " Perdiste $" + c.fine + ".");
        }
        SaveSystem.Save(data);
        CloseMenu();
    }

    // ----------------------------------------------------------------- crear y hud

    private void ShowCreate()
    {
        state = State.Create;
        data = null;
        cardPanel.SetActive(false);
        deathPanel.SetActive(false);
        menuPanel.SetActive(false);
        createPanel.SetActive(true);
        SetAdvanceVisible(false);
        if (nameInput != null) nameInput.text = "";
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (data == null || data.character == null || !data.character.alive)
        {
            hudName.text = "GENERACIONES";
            hudMoney.text = "Tu vida. Tu dinastía.";
            return;
        }
        Character ch = data.character;
        hudName.text = ch.name + "  ·  " + ch.age + " años  ·  Gen " + ch.generation;
        hudMoney.text = "$" + ch.money.ToString("N0");
        int[] vals = { ch.health, ch.happiness, ch.smarts, ch.looks };
        for (int i = 0; i < 4; i++)
        {
            statTexts[i].text = StatNames[i] + " " + vals[i];
            statBars[i].fillAmount = vals[i] / 100f;
        }
        if (ch.portraitSeed != lastPortraitSeed)
        {
            lastPortraitSeed = ch.portraitSeed;
            avatarImage.sprite = UIBuilder.Avatar(ch.portraitSeed);
        }
    }

    private void LogLine(string line)
    {
        log.Add(line);
        if (log.Count > 6) log.RemoveAt(0);
        logText.text = string.Join("\n", log.ToArray());
    }
}
