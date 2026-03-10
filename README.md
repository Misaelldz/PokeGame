# PokeIdle

Un juego idle/roguelike de combate Pokémon construido en **Godot 4 + C#**.

> Inspirado visualmente en Pokémon FireRed (GBA) con mecánicas de idle game y meta-progresión entre runs.

---

## 🎮 Concepto

Cada partida es una **run roguelike**: eliges un Pokémon inicial, avanzas por zonas derrotando Pokémon salvajes en auto-combat, capturas criaturas para mejorar tu pool de iniciales y desafías líderes de gimnasio hasta llegar a la **Elite Four**.

Al ganar una run desbloqueas la siguiente región con mayor dificultad. Lo capturado queda registrado permanentemente: cada Pokémon mejora sus IVs y naturalezas disponibles en futuras runs.

---

## 🗂️ Estructura del Proyecto

```
pokeidle/
├── Core/
│   ├── Autoloads/       # GameManager (singleton global)
│   ├── Engines/         # CombatEngine, CaptureEngine, XpEngine, TypeChart
│   ├── Models/          # PokemonData, MoveData, ItemData, ZoneData, BattleState…
│   ├── Services/        # DatabaseService, EncounterService
│   └── Systems/         # BattleSystem, EvolutionSystem, InventorySystem
├── UI/                  # BattleUI, TeamUI (scripts de interfaz)
├── Debug/               # DebugPanel + PokemonInjector (solo en debug builds)
├── Assets/
│   └── Data/            # CSVs: pokémon, moves, items, zones, evolutions…
├── ROADMAP.md           # Estado del proyecto por fases
└── UI_SPEC.md           # Especificación completa de interfaces
```

---

## 🚀 Estado Actual

| Fase | Descripción                                | Estado         |
| ---- | ------------------------------------------ | -------------- |
| 1    | Setup del proyecto Godot, estructura base  | ✅ Completa    |
| 2    | Data models + Engines de cálculo           | ✅ Completa    |
| 3    | Loop de batalla, sistemas, scripts de UI   | ✅ Completa    |
| 4    | Escenas visuales (menú, gameplay, modales) | 🔄 En progreso |

---

## ⚙️ Tecnologías

- **Godot 4.x** — Motor de juego
- **C# (.NET 8)** — Lógica del juego
- **CSV** — Base de datos de Pokémon, movimientos, ítems y zonas

---

## 🧩 Características Principales

- **Auto-combat con velocidad ajustable** (×1, ×2, ×3, ×4)
- **Modo manual** para elegir movimientos cada turno
- **Sistema de captura** basado en fórmulas Gen VI
- **Meta-progresión**: IVs históricos y naturalezas acumuladas por especie
- **Evoluciones** automáticas post-batalla
- **Gimnasios** con diálogos de líder y medallas con buffs globales
- **Panel de debug** integrado para testing (solo en builds de desarrollo)

---

## 📋 Documentación

- [`ROADMAP.md`](./ROADMAP.md) — Fases completadas y pendientes
- [`UI_SPEC.md`](./UI_SPEC.md) — Especificación de todas las pantallas del juego
