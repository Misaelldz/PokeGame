# Roadmap: PokeIdle Godot Migration (C#)

Este documento describe el plan de acción para migrar de React a Godot 4 con C#. El proyecto ahora reside en este Workspace organizado y preparado para Godot.

---

## 🗺️ Fase 1: Arquitectura Base y Setup

- [✔️] Definir estructura de directorios en el proyecto Godot. (¡HECHO!)
- [✔️] Configurar `project.godot`:
  - Resolución base para móviles (1280x720).
  - Modo `landscape` (sensor_landscape).
  - Activar 'Pixel snap' y Nearest filtering.
- [x] Crear Autoload `GameManager` (C#) para mantener el estado global del juego (equipo, dinero, stats).

## 🧮 Fase 2: Lógica Core (Modelos y Sistemas en C#)

- [x] **Data Models:** Escribir las clases C# fundamentales (`Pokemon`, `Move`, `Item`, `StatusCondition`).
- [x] **Service Layer:** Migrar la estática (Diccionarios, Regiones, Gimnasios).
- [x] **Engines de Cálculo:**
  - [x] Migrar fórmulas de daño (`combat.engine`).
  - [x] Migrar fórmulas de captura (`capture.engine`).
  - [x] Migrar XP y subida de niveles (`xp.engine`).

## 🕹️ Fase 3: Loop de Batalla (Scripts y Señales)

- [x] Mudar `useEngineTick` a un script maestro como `BattleSystem.cs` orquestado por timers y eventos.
  - **Alta Prioridad (el juego no corre sin esto):**
    - [x] `Core/Models/BattleState.cs` — Modelo: describe el estado de un combate en memoria (HP, turno, fase...).
    - [x] `Core/Services/EncounterService.cs` — Genera el Pokémon salvaje según la zona activa.
    - [x] `Core/Systems/BattleSystem.cs` — Node/Controller: loop de combate con `_Process(delta)`, orquesta los Engines.
  - **Media Prioridad:**
    - [x] `Core/Systems/EvolutionSystem.cs` — Detecta y ejecuta evoluciones tras la batalla.
    - [x] `Core/Systems/InventorySystem.cs` — Manejo de ítems: agregar, usar, contar.
  - **Baja Prioridad (para la Fase 4 de UI):**
    - [x] `UI/BattleUI.cs` — Script del nodo visual de batalla.
    - [x] `UI/TeamUI.cs` — Script del panel del equipo.
- [x] Sistema generador de encounters según la Zona actual.
- [x] Lógica de fases (Espera -> Seleccionar Acción -> Ejecutar turno Enemigo).
- [x] Implementar **Panel de Debug** temporal (basado en `OS.IsDebugBuild()`) para los betatesters.
  - [x] `Debug/DebugPanel.cs` — 6 pestañas (General, Equipo, Ítems, Progreso, Mega, Estado)
  - [x] `Debug/PokemonInjector.cs` — Laboratorio de Clonación (inyector de Pokémon)

## 🖼️ Fase 4: Vistas y Recursos Visuales

- [ ] **Sistemas de UI Control Nodes:** Emplear VBoxContainer y TextureRects para crear las interfaces adaptables.
- [ ] Animaciones de Sprite mediante el uso de nodos `Tween`.
- [ ] **Modales Godot:** Recrear las mochilas y popups usando ventanas Control y superposiciones.

## 📦 Fase 5: Exportación y Efectos Finales

- [ ] Añadir SFX y BGM con el nodo `AudioStreamPlayer`.
- [ ] Testing intensivo de los scripts en builds.
- [ ] Construir versión HTML5 (web).
