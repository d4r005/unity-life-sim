# Generaciones · Unity Life Sim

Simulador de vida familiar **multigeneracional** para Android/iOS/PC, inspirado en
**BitLife** y **Family Go! Lifetime Sim**: vives la vida de un personaje año a año,
tomas decisiones, y cuando muere la historia continúa con uno de sus herederos.

## Características

- Creación de personaje (nombre, género, país aleatorio) y nacimiento a los 0 años
- Estilos de vida por stats: **Salud, Felicidad, Inteligencia, Aspecto y Dinero**
- Eventos con decisiones cada año (infancia, escuela, universidad, trabajo, pareja, hijos, vejez)
- Sistema de **banderas**: escuela → secundaria → prepa → universidad/trabajo → empleo → pareja → casado → hijos
- Familia viva: padres, hermanos, pareja e hijos envejecen contigo y fallecen
- **Legado**: al morir heredas dinero a un hijo adulto y la puntuación de la dinastía se acumula entre generaciones
- Guardado automático (JSON en `persistentDataPath`) y UI generada 100% por código

## Cómo abrir el proyecto

1. Instala **Unity 2022.3 LTS o superior** (Unity Hub → Installs → Install).
2. En Unity Hub: **Add** → selecciona esta carpeta (la que contiene `Assets/` y `ProjectSettings/`).
3. Espera a que importe el proyecto.
4. Crea una escena nueva: `File → New Scene → Basic (Built-in)` y guárdala como `Assets/Scenes/Main.unity`.
5. En la escena, crea un GameObject vacío (`GameObject → Create Empty`) y agrégale el
   componente **GameManager** (arrastra `Assets/Scripts/GameManager.cs`).
6. Pulsa **Play**. Toda la interfaz se construye sola.

## Compilar para Android

`File → Build Settings → Android → Switch Platform`, asigna la escena y pulsa **Build**.

## Estructura

```
Assets/
├── Scripts/
│   ├── GameManager.cs        — máquina de estados: nacer → eventos → año → muerte → legado
│   ├── Core/
│   │   ├── Character.cs      — personaje y stats serializables
│   │   ├── FamilyTree.cs     — miembros de la familia, envejecimiento y defunciones
│   │   ├── NameGenerator.cs  — nombres y apellidos en español
│   │   └── SaveSystem.cs     — guardado/carga JSON + SaveData
│   ├── Events/
│   │   └── LifeEvents.cs     — modelos de eventos y base de datos (filtro por edad/flags/género)
│   └── UI/
│       └── UIBuilder.cs      — canvas, paneles, botones e inputs generados por código
└── Resources/
    └── Events/es.json        — 40+ eventos de vida con decisiones (¡agrégale los tuyos!)
```

## Agregar eventos

Edita `Assets/Resources/Events/es.json`. Cada evento:

```json
{
  "id": "mi_evento",
  "title": "Título",
  "text": "Descripción de la situación.",
  "minAge": 20, "maxAge": 40,      // rango de edad en que puede ocurrir
  "weight": 5,                     // probabilidad relativa
  "gender": "F",                  // opcional: solo "M" o "F" (vacío = cualquiera)
  "requiresFlag": "empleo",       // opcional: solo si el jugador tiene esa bandera
  "excludesFlag": "casado",       // opcional: nunca si el jugador la tiene
  "once": true,                   // ocurre una sola vez por vida
  "choices": [
    { "text": "Opción A", "effects": { "happiness": 5, "money": -1000 } },
    { "text": "Opción B", "effects": { "flag": "mi_bandera", "job": "Cocinero", "salary": 90000 } }
  ]
}
```

Campos de `effects`: `health`, `happiness`, `smarts`, `looks`, `money` (deltas),
`flag` (+`clearFlagInstead`), `job`+`salary` (empleo), `addPartner`, `addChild`,
`affectionPartner`/`affectionAll` y `deathChance` (probabilidad de morir).

## Hoja de ruta

- [x] Núcleo de simulación de vida con eventos y decisiones
- [x] Familia y relaciones con envejecimiento
- [x] Muerte, herencia y continuidad generacional (legado)
- [x] Guardado automático
- [ ] Arte y retratos procedurales de personajes
- [ ] Interacciones con familiares (regalar, pelear, pedir dinero)
- [ ] Mascotas, amigos y eventos sociales
- [ ] Logros y tabla de legados
- [ ] Música y sonido
