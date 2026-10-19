using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Corazón del juego: nacer → eventos → resumen → año siguiente → ... → muerte → legado,
/// con menús de acción estilo BitLife: Ocupación, Actividades, Relaciones, Bienes y Delitos.
/// Interfaz estilo cartoon claro (tipo Family Go): contornos gruesos y pastillas.
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
    private Text hudName, hudSub, hudMoney, logText;
    private readonly Text[] statTexts = new Text[4];
    private readonly Image[] statBars = new Image[4];
    private Image avatarImage;
    private Button advanceButton;
    private Image advanceImage;
    private Button autoButton;
    private Image autoImage;
    private bool autoPlay = false;
    private float autoTimer = 0f;
    private static readonly float AUTO_INTERVAL = 4f;
    private int lastPortraitSeed = -1;
    private static readonly string[] StatNames = { "Salud", "Felicidad", "Intel", "Aspecto" };
    private GameObject cardPanel, createPanel, deathPanel, menuPanel;
    private Transform choicesHolder, deathHolder, menuHolder;
    private Text cardTitle, cardText, menuTitle;
    private InputField nameInput;
    private string chosenGender = "M";
    private string chosenOrientation = "H";
    private readonly UnityEngine.UI.Image[] orientationImgs = new UnityEngine.UI.Image[3];

    // ---- Paleta estilo Family Go: fondo claro, tinta oscura, contornos gruesos
    private static Color Bg => Theme.Bg;
    private static Color Paper => Theme.Paper;
    private static Color Ink => Theme.Ink;
    private static Color Sub => Theme.Sub;
    private static Color Accent => Theme.Accent;
    private static Color Cream => Theme.Cream;
    private static Color Danger => Theme.Danger;
    private static Color Off => Theme.Off;
    private static Color AutoOn => Theme.AutoOn;
    private static Color LogBg => Theme.LogBg;

    private void Awake()
    {
        Theme.Dark = PlayerPrefs.GetInt("tema_oscuro", 0) == 1;
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

        // ---- Encabezado: avatar, nombre, apellido familiar
        UIBuilder.Panel(root, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -148f), new Vector2(0f, -16f), Paper);
        GameObject avatarGo = new GameObject("Avatar", typeof(Image));
        avatarGo.transform.SetParent(root, false);
        RectTransform art = avatarGo.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f); art.anchorMax = new Vector2(0f, 1f);
        art.offsetMin = new Vector2(30f, -132f); art.offsetMax = new Vector2(114f, -48f);
        avatarImage = avatarGo.GetComponent<Image>();

        hudName = UIBuilder.MakeText(root, "HudName", "", 26, Ink, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(122f, -88f), new Vector2(-160f, -46f),
            FontStyle.Bold);
        hudSub = UIBuilder.MakeText(root, "HudSub", "", 18, Sub, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(122f, -126f), new Vector2(-160f, -92f));
        UIBuilder.MakeButton(root, "BtnTheme", "Tema", 16, Cream,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-244f, -146f), new Vector2(-154f, -108f),
            ToggleTheme);
        UIBuilder.MakeButton(root, "BtnNewLife", "Nueva vida", 16, Danger,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-142f, -146f), new Vector2(-16f, -108f),
            () => { SaveSystem.Delete(); ShowCreate(); });

        // ---- Pastillas de stats (4 stats + dinero)
        Color[] statColors = {
            new Color(0.33f, 0.66f, 0.36f), new Color(0.93f, 0.74f, 0.22f),
            new Color(0.28f, 0.58f, 0.85f), new Color(0.88f, 0.44f, 0.60f)
        };
        for (int i = 0; i < 4; i++)
        {
            float x0 = 16f + i * 136f;
            UIBuilder.Panel(root, "Chip" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0, -246f), new Vector2(x0 + 128f, -162f), Paper);
            UIBuilder.Plain(root, "Dot" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -196f), new Vector2(x0 + 24f, -184f), statColors[i]);
            statTexts[i] = UIBuilder.MakeText(root, "StatTxt" + i, "", 16, Ink, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 30f, -202f), new Vector2(x0 + 122f, -180f),
                FontStyle.Bold);
            UIBuilder.Plain(root, "BarBg" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -232f), new Vector2(x0 + 116f, -218f), new Color(0.88f, 0.87f, 0.82f, 1f));
            Image fill = UIBuilder.Plain(root, "BarFill" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x0 + 12f, -232f), new Vector2(x0 + 116f, -218f), statColors[i]);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            statBars[i] = fill;
        }
        float mx = 16f + 4 * 136f;
        UIBuilder.Panel(root, "ChipMoney", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(mx, -246f), new Vector2(mx + 128f, -162f), Paper);
        UIBuilder.Plain(root, "DotMoney", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(mx + 12f, -196f), new Vector2(mx + 24f, -184f), new Color(0.89f, 0.70f, 0.24f));
        hudMoney = UIBuilder.MakeText(root, "HudMoney", "", 17, Ink, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(mx + 28f, -208f), new Vector2(mx + 124f, -172f),
            FontStyle.Bold);

        // ---- Tarjeta central (eventos y resúmenes)
        cardPanel = UIBuilder.Panel(root, "Card", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 444f), new Vector2(-16f, -260f), Paper).gameObject;
        cardTitle = UIBuilder.MakeText(cardPanel.transform, "Title", "", 30, Ink, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(32f, -336f), new Vector2(-32f, -276f), FontStyle.Bold);
        cardText = UIBuilder.MakeText(cardPanel.transform, "Body", "", 23, Ink, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(36f, -576f), new Vector2(-36f, -352f));
        choicesHolder = UIBuilder.Panel(cardPanel.transform, "Choices", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(20f, 16f), new Vector2(-20f, 300f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Panel de menús
        menuPanel = UIBuilder.Panel(root, "Menu", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 444f), new Vector2(-16f, -260f), Paper).gameObject;
        menuTitle = UIBuilder.MakeText(menuPanel.transform, "MenuTitle", "", 28, Ink, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(24f, -236f), new Vector2(-24f, -186f), FontStyle.Bold);
        menuHolder = UIBuilder.Panel(menuPanel.transform, "MenuHolder", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(12f, 16f), new Vector2(-12f, -248f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Panel de muerte / legado
        deathPanel = UIBuilder.Panel(root, "Death", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 444f), new Vector2(-16f, -260f), Theme.DeathFill).gameObject;
        UIBuilder.MakeText(deathPanel.transform, "DeathTitle", "FIN DE LA VIDA", 38, Danger, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -300f), new Vector2(-20f, -244f), FontStyle.Bold);
        deathHolder = UIBuilder.Panel(deathPanel.transform, "DeathHolder", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 20f), new Vector2(-16f, -260f), new Color(0f, 0f, 0f, 0f)).transform;

        // ---- Registro de vida
        UIBuilder.Panel(root, "LogPanel", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 244f), new Vector2(-16f, 420f), LogBg);
        logText = UIBuilder.MakeText(root, "Log", "", 19, Sub, TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 258f), new Vector2(-28f, 406f));

        // ---- Botón grande de avanzar año + botón AUTO (avanza solo)
        advanceButton = UIBuilder.MakeButton(root, "BtnAdvance", "Avanzar un año  +", 30, Accent,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(104f, 132f), new Vector2(-16f, 236f),
            AdvanceYear);
        advanceImage = advanceButton.GetComponent<Image>();
        autoButton = UIBuilder.MakeButton(root, "BtnAuto", "AUTO", 24, Off,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 132f), new Vector2(92f, 236f),
            ToggleAuto);
        autoImage = autoButton.GetComponent<Image>();
        autoImage.sprite = UIBuilder.OutlineSprite(autoPlay ? AutoOn : Off);

        // ---- Barra de menús inferior
        string[] menuNames = { "Familia", "Ocupación", "Actividad", "Bienes", "Delitos" };
        Action[] menuActions = {
            () => OpenTreeMenu(),
            () => OpenOccupationMenu(),
            () => OpenActivitiesMenu(),
            () => OpenAssetsMenu(),
            () => OpenCrimesMenu()
        };
        for (int i = 0; i < menuNames.Length; i++)
        {
            int idx = i;
            UIBuilder.MakeButton(root, "Menu_" + menuNames[i], menuNames[i], 21, Cream,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f + idx * 139f, 16f), new Vector2(148f + idx * 139f, 104f),
                () => menuActions[idx]());
        }

        // ---- Panel de creación de personaje
        createPanel = UIBuilder.Panel(root, "Create", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -380f), new Vector2(300f, 380f), Paper).gameObject;
        UIBuilder.MakeText(createPanel.transform, "Title", "GENERACIONES", 46, Ink, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -84f), new Vector2(-20f, -14f), FontStyle.Bold);
        UIBuilder.MakeText(createPanel.transform, "Sub", "Tu vida. Tus decisiones. Tu dinastía.", 24, Sub,
            TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(20f, -134f), new Vector2(-20f, -94f));
        nameInput = UIBuilder.MakeInput(createPanel.transform, "NameInput", "Tu nombre...",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -254f), new Vector2(-30f, -194f));
        UIBuilder.MakeButton(createPanel.transform, "Male", "Niño", 30, Cream,
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(30f, -324f), new Vector2(-8f, -254f),
            () => { chosenGender = "M"; });
        UIBuilder.MakeButton(createPanel.transform, "Female", "Niña", 30, Cream,
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(8f, -324f), new Vector2(-30f, -254f),
            () => { chosenGender = "F"; });
        string[] orientLabels = { "Hetero", "Gay", "Bisexual" };
        string[] orientValues = { "H", "G", "B" };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            UnityEngine.UI.Button ob = UIBuilder.MakeButton(createPanel.transform, "Orient_" + i,
                orientLabels[i], 22, i == 0 ? Accent : Cream,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f + idx * 190f, -394f), new Vector2(200f + idx * 190f, -334f),
                () => { chosenOrientation = orientValues[idx]; RefreshOrientationButtons(); });
            orientationImgs[idx] = ob.GetComponent<UnityEngine.UI.Image>();
        }
        UIBuilder.MakeButton(createPanel.transform, "Start", "Nacer", 34, Accent,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -474f), new Vector2(-30f, -404f),
            StartLifeFromCreate);
    }

    private void RefreshOrientationButtons()
    {
        string[] orientValues = { "H", "G", "B" };
        for (int i = 0; i < 3; i++)
        {
            if (orientationImgs[i] != null)
                orientationImgs[i].sprite = UIBuilder.OutlineSprite(orientValues[i] == chosenOrientation ? Accent : Cream);
        }
    }

    private void SetAdvanceVisible(bool visible)
    {
        advanceButton.gameObject.SetActive(visible);
    }

    private void SetAdvanceEnabled(bool enabled)
    {
        advanceButton.interactable = enabled;
        advanceImage.color = Color.white; // el sprite lleva el color
        advanceImage.sprite = UIBuilder.OutlineSprite(enabled ? Accent : Off);
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
        ch.orientation = chosenOrientation;
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
        float h = 84f; float gap = 10f;
        for (int i = 0; i < n; i++)
        {
            Choice c = e.choices[i];
            float fromTop = (n - 1 - i) * (h + gap);
            UIBuilder.MakeButton(choicesHolder, "Choice" + i, c.text, 24, Cream,
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
            Person p = data.family.AddPartner(NameGenerator.RandomSurname(rng), ch.age, ch.gender, ch.orientation);
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
            bool adopted = data.family.Partner.gender == ch.gender;   // parejas del mismo género adoptan
            LogLine(adopted ? "¡Adoptaste a " + child.name + "!" : "¡Nace " + child.name + "!");
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
        string assetsLine = data.assets.Count > 0 ? string.Join(", ", data.assets.ToArray()) : "—";
        cardText.text =
            "Edad " + ch.age + " años   ·   " + FmtMoney(ch.money) + "\n" +
            (ch.job.Length > 0 ? "Empleo: " + ch.job + " (" + FmtMoney(ch.salary) + "/año)" : "Sin empleo") + "\n" +
            "Salud " + ch.health + " · Felicidad " + ch.happiness + "\n" +
            "Intel " + ch.smarts + " · Aspecto " + ch.looks + "\n" +
            "Bienes: " + assetsLine + "\n" +
            "Legado de la dinastía: " + data.dynastyLegacy + "\n" +
            "Usa los menús de abajo y «Avanzar un año» para seguir.";
        UIBuilder.Clear(choicesHolder);
        SetAdvanceVisible(true);
        SetAdvanceEnabled(true);
        UpdateHud();
        SaveSystem.Save(data);
    }

    private void Update()
    {
        // AUTO: avanza los años solo cuando estamos en el resumen anual
        if (autoPlay && state == State.Summary && data != null && data.character.alive)
        {
            autoTimer -= Time.deltaTime;
            if (autoTimer <= 0f)
            {
                autoTimer = AUTO_INTERVAL;
                AdvanceYear();
            }
        }
    }

    private void ToggleAuto()
    {
        autoPlay = !autoPlay;
        autoTimer = AUTO_INTERVAL;
        autoImage.sprite = UIBuilder.OutlineSprite(autoPlay ? AutoOn : Off);
        LogLine(autoPlay ? "Auto-avance activado: un año cada 4 segundos." : "Auto-avance pausado.");
    }

    private void AdvanceYear()
    {
        Character ch = data.character;
        autoTimer = AUTO_INTERVAL;
        ch.age++;
        data.currentYear++;

        foreach (string death in data.family.AgeOneYear(rng)) LogLine(death);

        TryFamilyMarriages();

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

    private string lastDeathReason = "";
    private long lastDeathLegacy = 0;

    private void ShowDeath(string reason, long legacyGained)
    {
        lastDeathReason = reason;
        lastDeathLegacy = legacyGained;
        createPanel.SetActive(false);
        cardPanel.SetActive(false);
        menuPanel.SetActive(false);
        deathPanel.SetActive(true);
        SetAdvanceVisible(false);
        UIBuilder.Clear(deathHolder);

        UIBuilder.MakeText(deathHolder, "Reason", reason, 26, Ink, TextAnchor.UpperCenter,
            Vector2.zero, Vector2.one, new Vector2(10f, -66f), new Vector2(-10f, -10f));
        UIBuilder.MakeText(deathHolder, "Legacy",
            "Legado ganado: " + legacyGained + "\nLegado total de la dinastía: " + data.dynastyLegacy,
            22, Ink, TextAnchor.UpperCenter,
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
                "No dejaste herederos adultos.\nLa dinastía empieza de nuevo.", 24, Ink, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(10f, y - 70), new Vector2(-10f, y));
            y -= 92;
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

        // El arbol persiste: se reetiqueta todo desde la perspectiva del heredero
        List<Person> members = data.family.members;
        foreach (Person p in members)
        {
            if (p.relation == "pareja")
                p.relation = (p.gender == "F") ? "madre" : "padre";          // tu otro padre/madre
            else if ((p.relation == "yerno" || p.relation == "nuera") && p.partnerOf == heir.name)
                p.relation = "pareja";                                        // tu esposo/a
            else if (p.relation == "padre" || p.relation == "madre")
                p.relation = (p.gender == "F") ? "abuela" : "abuelo";        // abuelos
            else if (p.relation == "hermano" || p.relation == "hermana")
                p.relation = (p.gender == "F") ? "tía" : "tío";   // tios
            else if (p.relation == "hijo" || p.relation == "hija")
                p.relation = (p.gender == "F") ? "hermana" : "hermano";      // hermanos
        }
        members.RemoveAll(mp => ReferenceEquals(mp, heir));
        members.Add(new Person(old.name, old.gender == "F" ? "madre" : "padre", old.gender, old.age, 100)
            { alive = false, portraitSeed = old.portraitSeed });

        data.character = ch;
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

        // rejilla de dos columnas, hasta 8 filas
        int shown = 0;
        foreach (MenuRow row in rows)
        {
            if (shown >= 8) break;
            MenuRow captured = row;
            int col = shown % 2;
            int line = shown / 2;
            float x = 4f + col * 342f;
            float fromTop = line * 62f;
            UIBuilder.MakeButton(menuHolder, "Row" + shown, captured.label, 18,
                captured.enabled ? Cream : Off,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -(fromTop + 56f)), new Vector2(x + 330f, -fromTop),
                captured.enabled ? (UnityEngine.Events.UnityAction)(() => { captured.onClick(); }) : null);
            shown++;
        }
        UIBuilder.MakeButton(menuHolder, "Back", "Volver", 22, Danger,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 16f), new Vector2(-4f, 78f), CloseMenu);
        UpdateHud();
    }

    private void CloseMenu()
    {
        menuPanel.SetActive(false);
        if (menuFromEvents && eventIndex < yearEvents.Count) ShowEvent();
        else ShowSummary();
    }

    // ---- familia y relaciones

    // ---- arbol genealogico (vista estilo Family Go)

    private static Color TreeLineCol => Theme.TreeLine;

    private class TreeUnit
    {
        public Person person;
        public bool isYou;
        public Person inlaw;
        public float cx, inlawCx, width;
    }

    private void OpenTreeMenu()
    {
        if (state == State.Create || state == State.Death || data == null) return;
        if (state != State.Menu) menuFromEvents = (state == State.Events);
        state = State.Menu;
        createPanel.SetActive(false);
        cardPanel.SetActive(false);
        deathPanel.SetActive(false);
        menuPanel.SetActive(true);
        UIBuilder.Clear(menuHolder);
        Character ch = data.character;
        menuTitle.text = "Arbol familiar";

        List<Person> abuelos = new List<Person>(), padres = new List<Person>(), tios = new List<Person>(),
                     hermanos = new List<Person>(), hijos = new List<Person>();
        Person pareja = null;
        foreach (Person p in data.family.members)
        {
            switch (p.relation)
            {
                case "abuelo": case "abuela": abuelos.Add(p); break;
                case "padre": case "madre": padres.Add(p); break;
                case "tío": case "tía": tios.Add(p); break;
                case "hermano": case "hermana": hermanos.Add(p); break;
                case "hijo": case "hija": hijos.Add(p); break;
                case "pareja": pareja = p; break;
            }
        }

        List<TreeUnit> row1 = BuildUnits(abuelos, 4);
        List<TreeUnit> row2 = BuildUnits(padres, 2);
        row2.AddRange(BuildUnits(tios, 3));
        List<TreeUnit> row3 = BuildUnits(hermanos, 3);
        TreeUnit you = new TreeUnit { person = null, isYou = true, inlaw = pareja,
            width = 48f + (pareja != null ? 64f : 0f) };
        row3.Add(you);
        List<TreeUnit> row4 = BuildUnits(hijos, 5);

        LayoutRow(row1);
        LayoutRow(row2);
        LayoutRow(row3);
        LayoutRow(row4);

        Transform t = menuHolder;

        // ---- lineas de conexion (se dibujan detras de los nodos)
        if (row1.Count > 0)
        {
            TreeLine(t, row1[0].cx, 64f, row1[row1.Count - 1].cx, 64f);
            TreeLine(t, 344f, 64f, 344f, 72f);
        }
        if (row2.Count > 0)
        {
            TreeLine(t, row2[0].cx, 136f, row2[row2.Count - 1].cx, 136f);
            if (row1.Count > 0)
                foreach (TreeUnit u in row2)
                    if (u.person.relation == "tío" || u.person.relation == "tía")
                        TreeLine(t, u.cx, 64f, u.cx, 72f);
        }
        float midX = (you.cx + (pareja != null ? you.inlawCx : you.cx)) / 2f;
        TreeLine(t, midX, 136f, midX, 144f);
        if (pareja != null) TreeLine(t, you.cx + 26f, 166f, you.inlawCx - 22f, 166f);
        foreach (TreeUnit u in row3)
            if (!u.isYou && u.inlaw != null) TreeLine(t, u.cx + 22f, 166f, u.inlawCx - 22f, 166f);
        if (row4.Count > 0)
        {
            TreeLine(t, midX, 166f, midX, 212f);
            float c0 = Math.Min(row4[0].cx, row4[0].inlaw != null ? row4[0].inlawCx : row4[0].cx);
            TreeUnit lastU = row4[row4.Count - 1];
            float c1 = Math.Max(lastU.cx, lastU.inlaw != null ? lastU.inlawCx : lastU.cx);
            TreeLine(t, c0, 212f, c1, 212f);
            foreach (TreeUnit u in row4)
            {
                TreeLine(t, u.cx, 212f, u.cx, 220f);
                if (u.inlaw != null) TreeLine(t, u.cx + 22f, 242f, u.inlawCx - 22f, 242f);
            }
        }

        // ---- nodos
        foreach (TreeUnit u in row1) DrawUnit(t, u, 0f, 44f);
        foreach (TreeUnit u in row2) DrawUnit(t, u, 72f, 44f);
        foreach (TreeUnit u in row3) DrawUnit(t, u, 144f, u.isYou ? 48f : 44f);
        foreach (TreeUnit u in row4) DrawUnit(t, u, 220f, 44f);

        // ---- botones laterales
        UIBuilder.MakeButton(t, "BtnVolver", "Volver", 20, Cream,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, -44f), new Vector2(676f, 0f), CloseMenu);
        if (ch.age >= 18 && pareja == null)
            UIBuilder.MakeButton(t, "BtnDate", "Buscar pareja", 18, Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, -118f), new Vector2(676f, -72f), DoDating);
        if (ch.age >= 6 && CountFriends() < 3)
            UIBuilder.MakeButton(t, "BtnFriend", "Hacer amigo", 18, Cream,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, -192f), new Vector2(676f, -146f), DoMakeFriend);
        UpdateHud();
    }

    private List<TreeUnit> BuildUnits(List<Person> people, int cap)
    {
        List<TreeUnit> units = new List<TreeUnit>();
        foreach (Person p in people)
        {
            if (units.Count >= cap) break;
            TreeUnit u = new TreeUnit();
            u.person = p;
            u.inlaw = FindInlaw(p);
            u.width = 44f + (u.inlaw != null ? 64f : 0f);
            units.Add(u);
        }
        return units;
    }

    private Person FindInlaw(Person p)
    {
        foreach (Person q in data.family.members)
            if (q.partnerOf == p.name &&
                (q.relation == "yerno" || q.relation == "nuera" || q.relation == "cuñado" || q.relation == "cuñada"))
                return q;
        return null;
    }

    private void LayoutRow(List<TreeUnit> units)
    {
        if (units.Count == 0) return;
        float total = 0f;
        foreach (TreeUnit u in units) total += u.width;
        total += 18f * (units.Count - 1);
        float x = 344f - total / 2f;
        foreach (TreeUnit u in units)
        {
            float half = u.isYou ? 24f : 22f;
            u.cx = x + half;
            u.inlawCx = u.inlaw != null ? x + u.width - half : u.cx;
            x += u.width + 18f;
        }
    }

    private void DrawUnit(Transform t, TreeUnit u, float topY, float size)
    {
        if (u.isYou)
        {
            Character ch = data.character;
            TreeNode(t, u.cx, topY, size, ch.portraitSeed, ShortName(ch.name), "TU · " + ch.age + " años", false, null);
            if (u.inlaw != null)
                TreeNode(t, u.inlawCx, topY, size, u.inlaw.portraitSeed, ShortName(u.inlaw.name),
                    u.inlaw.relation + ", " + u.inlaw.age + (u.inlaw.alive ? "" : " †"), !u.inlaw.alive,
                    () => OpenMemberMenu(u.inlaw));
            return;
        }
        TreeNode(t, u.cx, topY, size, u.person.portraitSeed, ShortName(u.person.name),
            u.person.relation + ", " + u.person.age + (u.person.alive ? "" : " †"), !u.person.alive,
            () => OpenMemberMenu(u.person));
        if (u.inlaw != null)
            TreeNode(t, u.inlawCx, topY, size, u.inlaw.portraitSeed, ShortName(u.inlaw.name),
                u.inlaw.relation + ", " + u.inlaw.age + (u.inlaw.alive ? "" : " †"), !u.inlaw.alive,
                () => OpenMemberMenu(u.inlaw));
    }

    private void TryFamilyMarriages()
    {
        List<Person> members = data.family.members;
        for (int i = 0; i < members.Count; i++)
        {
            Person p = members[i];
            if (!p.alive || p.age < 21 || p.age > 70) continue;
            bool isChild = p.relation == "hijo" || p.relation == "hija";
            bool isSibling = p.relation == "hermano" || p.relation == "hermana";
            if (!isChild && !isSibling) continue;
            if (HasSpouse(p)) continue;
            double chance = isChild ? 0.08 : 0.05;
            if (rng.NextDouble() < chance)
            {
                bool male = rng.NextDouble() < 0.5;
                string rel = isChild ? (male ? "yerno" : "nuera") : (male ? "cuñado" : "cuñada");
                string n = NameGenerator.RandomName(rng, male ? "M" : "F") + " " + NameGenerator.RandomSurname(rng);
                members.Add(new Person(n, rel, male ? "M" : "F", Math.Max(19, p.age + rng.Next(-4, 5)), 60)
                    { portraitSeed = rng.Next(), partnerOf = p.name });
                LogLine("¡Tu " + p.relation + " " + p.name + " se casó con " + n + "!");
            }
        }
    }

    private bool HasSpouse(Person p)
    {
        foreach (Person q in data.family.members)
            if (q.partnerOf == p.name &&
                (q.relation == "yerno" || q.relation == "nuera" || q.relation == "cuñado" || q.relation == "cuñada"))
                return true;
        return false;
    }

    private void TreeLine(Transform parent, float x1, float y1, float x2, float y2)
    {
        float x = Math.Min(x1, x2), y = Math.Min(y1, y2);
        float w = Math.Max(4f, Math.Abs(x2 - x1)), h = Math.Max(4f, Math.Abs(y2 - y1));
        UIBuilder.Plain(parent, "TreeLine", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(x, -(y + h)), new Vector2(x + w, -y), TreeLineCol);
    }

    private void TreeNode(Transform parent, float cx, float topY, float size, int seed, string name, string sub, bool dead, Action onClick)
    {
        GameObject node = new GameObject("Node", typeof(Image));
        node.transform.SetParent(parent, false);
        RectTransform rt = node.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(cx - size / 2f, -topY - size);
        rt.offsetMax = new Vector2(cx + size / 2f, -topY);
        Image img = node.GetComponent<Image>();
        img.sprite = UIBuilder.Avatar(seed);
        if (onClick != null)
        {
            Button btn = node.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
        }
        if (dead)
        {
            GameObject veilGO = new GameObject("Veil", typeof(Image));
            veilGO.transform.SetParent(node.transform, false);
            RectTransform v = veilGO.GetComponent<RectTransform>();
            v.anchorMin = Vector2.zero; v.anchorMax = Vector2.one;
            v.offsetMin = Vector2.zero; v.offsetMax = Vector2.zero;
            veilGO.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 0.55f);
        }
        UIBuilder.MakeText(parent, "NodeName", name, 15, Ink, TextAnchor.UpperCenter,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx - 62f, -topY - size - 20f), new Vector2(cx + 62f, -topY - size), FontStyle.Bold);
        UIBuilder.MakeText(parent, "NodeSub", sub, 12, Sub, TextAnchor.UpperCenter,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx - 62f, -topY - size - 36f), new Vector2(cx + 62f, -topY - size - 20f));
    }

    private static string ShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "...";
        string[] parts = fullName.Split(' ');
        return parts.Length > 1 ? parts[0] : fullName;
    }

    private void OpenMemberMenu(Person p)
    {
        List<MenuRow> rows = new List<MenuRow>();
        if (!p.alive)
        {
            rows.Add(new MenuRow(p.name + " fallecio a los " + p.age + " anos.", null, false));
            OpenMenu("En memoria", rows);
            return;
        }
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
        p.affection = Math.Max(0, Math.Min(100, p.affection - 8));
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
            Person p = data.family.AddPartner(NameGenerator.RandomSurname(rng), ch.age, ch.gender, ch.orientation);
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
                Math.Max(6, ch.age + rng.Next(-4, 5)), 55) { portraitSeed = rng.Next() });
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
            rows.Add(new MenuRow("Pedir aumento\n(sueldo " + FmtMoney(ch.salary) + ")", DoAskRaise));
            rows.Add(new MenuRow("Trabajar duro\n(+posible ascenso)", DoWorkHard));
            rows.Add(new MenuRow("Renunciar a\n" + ch.job, DoQuitJob));
        }

        if (ch.age >= 18 && !ch.HasFlag("universidad") && !ch.HasFlag("titulado"))
            rows.Add(new MenuRow("Inscribirse a la\nuniversidad (-$50K)", DoEnrollUniversity, ch.money >= 50000));

        if (ch.HasFlag("universidad") && !ch.HasFlag("titulado"))
            rows.Add(new MenuRow("Estudiar para el\nexamen (intel +3)", DoStudy));

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
            if (shown >= 3) break;
            JobDef captured = j;
            rows.Add(new MenuRow("Solicitar: " + j.name + "\n" + FmtMoney(j.salary) + "/año", () => DoApplyJob(captured)));
            shown++;
        }
        if (rows.Count == 0)
            rows.Add(new MenuRow("No hay nada disponible\na tu edad todavía.", null, false));
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
            LogLine("¡Te contrataron como " + j.name + "! Sueldo: " + FmtMoney(ch.salary) + "/año.");
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
            LogLine("Tu esfuerzo dio frutos: te promovieron. Sueldo ahora " + FmtMoney(ch.salary) + "/año.");
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
            string label = a.name + (a.cost > 0 ? "\n-$" + a.cost : "\ngratis");
            rows.Add(new MenuRow(label, () => DoActivity(captured), ch.money >= a.cost));
        }
        if (rows.Count == 0) rows.Add(new MenuRow("Todavía eres muy\npequeño para esto.", null, false));
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
                rows.Add(new MenuRow("Vender: " + a.name + "\n+" + FmtMoney(a.price / 2), () => DoSellAsset(captured)));
            else
                rows.Add(new MenuRow("Comprar: " + a.name + "\n-" + FmtMoney(a.price),
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
        LogLine("¡Compraste " + a.name + "!");
        SaveSystem.Save(data);
        CloseMenu();
    }

    private void DoSellAsset(AssetDef a)
    {
        Character ch = data.character;
        ch.money += a.price / 2;
        data.assets.Remove(a.name);
        ch.AddStat("happiness", -2);
        LogLine("Vendiste " + a.name + " por " + FmtMoney(a.price / 2) + ".");
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
        if (rows.Count == 0) rows.Add(new MenuRow("Todavía no tienes edad\npara estas travesuras.", null, false));
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
        chosenOrientation = "H";
        RefreshOrientationButtons();
        if (nameInput != null) nameInput.text = "";
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (data == null || data.character == null || !data.character.alive)
        {
            hudName.text = "GENERACIONES";
            hudSub.text = "Tu vida. Tu dinastía.";
            hudMoney.text = "";
            for (int i = 0; i < 4; i++) { statTexts[i].text = "—"; statBars[i].fillAmount = 0f; }
            return;
        }
        Character ch = data.character;
        string surname = ch.name.Contains(" ") ? ch.name.Substring(ch.name.LastIndexOf(' ') + 1) : ch.name;
        hudName.text = ch.name + "  ·  " + ch.age + " años";
        hudSub.text = "Familia " + surname + "  ·  Gen " + ch.generation + "  ·  " + ch.country;
        hudMoney.text = FmtMoney(ch.money);
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

    /// <summary>Cambia entre tema claro y oscuro y reconstruye la interfaz.</summary>
    private void ToggleTheme()
    {
        Theme.Dark = !Theme.Dark;
        PlayerPrefs.SetInt("tema_oscuro", Theme.Dark ? 1 : 0);
        PlayerPrefs.Save();

        State previous = state;
        if (previous == State.Menu) previous = menuFromEvents ? State.Events : State.Summary;

        Destroy(canvas.gameObject);
        canvas = UIBuilder.CreateCanvas();
        BuildUI();

        switch (previous)
        {
            case State.Create: ShowCreate(); break;
            case State.Events: ShowEvent(); break;
            case State.Death: ShowDeath(lastDeathReason, lastDeathLegacy); break;
            default: ShowSummary(); break;
        }
        LogLine(Theme.Dark ? "Tema oscuro activado." : "Tema claro activado.");
    }

    private static string FmtMoney(long v)
    {
        if (v >= 1000000) return "$" + (v / 1000000) + "." + ((v % 1000000) / 100000) + "M";
        if (v >= 10000) return "$" + (v / 1000) + "K";
        return "$" + v.ToString("N0");
    }
}
