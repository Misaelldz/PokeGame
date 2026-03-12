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

---

## 🎨 Fase 6: Integración de Assets (pokerogue-assets)

Fuente: [pagefaultgames/pokerogue-assets](https://github.com/pagefaultgames/pokerogue-assets) — branch `beta`

### Fase A — Sprites de Pokémon (🔴 Alta prioridad)
- [ ] Sparse-checkout del repo para bajar solo las carpetas necesarias.
- [ ] Copiar sprites a `Assets/Sprites/Pokemon/Front/`, `Back/`, `Shiny/Front/`, `Shiny/Back/`.
- [ ] Crear `Core/Services/SpriteLoader.cs` que cargue sprites por `PokedexId`.

### Fase B — Fondos de Batalla / Arenas (🔴 Alta prioridad)
- [ ] Copiar `images/arenas/` → `Assets/Sprites/Arenas/`.
- [ ] Mapear cada `ZoneData` a su nombre de arena correspondiente.
- [ ] Cargar fondo dinámicamente en `GameplayScene` y `GymScene`.

### Fase C — Audio completo (🟡 Media prioridad)
- [ ] Copiar `audio/cry/` → `Assets/Audio/Cries/`.
- [ ] Copiar `audio/bgm/` → `Assets/Audio/BGM/`.
- [ ] Copiar `audio/se/` + `audio/ui/` → `Assets/Audio/SE/` y `Assets/Audio/UI/`.
- [ ] Crear `Core/Autoloads/AudioManager.cs` con `PlayCry(int id)`, `PlayBGM(string zone)`, `PlaySFX(string name)`.

### Fase D — UI Sprites (🟢 Baja prioridad)
- [ ] Copiar `images/pokeball/` → `Assets/Sprites/Pokeballs/`.
- [ ] Copiar `images/items/` + `items.png` → `Assets/Sprites/Items/` (spritesheet con AtlasTexture).
- [ ] Copiar `images/ui/` → `Assets/Sprites/UI/` (marcos retro, dialog boxes).

### Fase E — Trainer Sprites (🟢 Baja prioridad)
- [ ] Copiar `images/trainer/` → `Assets/Sprites/Trainers/`.
- [ ] Mapear cada gimnasio a su sprite de líder.

---

## 🎭 Fase 7: Sistema de Variantes de Sprites (Eventos y Cosméticos)

Permite desbloquear sprites alternativos (retro, eventos, otros) por misiones o logros.

### Estructura de archivos
```
Assets/Sprites/Pokemon/Variants/
  {pokedexId}_{variantName}.png       ← ej: 25_gen1.png, 25_christmas.png
  {pokedexId}_{variantName}_shiny.png
```

### Flujo de desbloqueo
```
Misión completada → GameManager.UnlockVariant(pokedexId, variantName)
                  → guardado en save file
                  → SpriteLoader detecta variante activa automáticamente
```

### Tareas
- [ ] Ampliar `SpriteLoader.cs` para buscar variante activa antes del sprite base.
- [ ] Agregar `UnlockedVariants: Dictionary<int, string>` al save del jugador en `GameManager`.
- [ ] Crear UI de selector de variante en el panel de detalle de `StarterSelection`.
- [ ] Definir primer evento de prueba (ej. "Desbloquea el sprite Gen 1 de Pikachu ganando 10 batallas con él").
