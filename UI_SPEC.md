# UI Spec: PokeIdle — Interfaz de Usuario

Estilo visual de referencia: **Pokémon FireRed / LeafGreen (GBA)**
Plataforma: Godot 4 (móvil landscape + web)

---

## 🏗️ Mapa de Pantallas y Flujo de Navegación

```
MainMenu
  ├── [Jugar]         → StarterSelection → GameplayScene
  ├── [Continuar]     → StarterSelection (con run guardada) → GameplayScene
  ├── [Gacha]         → GachaScene
  ├── [Modo Extra]    → (TBD)
  └── [Estadísticas]  → GlobalStatsScene

GameplayScene
  ├── BattlePanel     (siempre visible)
  ├── TeamPanel       (botón → overlay)
  ├── BagPanel        (botón → overlay)
  └── GymScene        (automático al llegar al boss gym)
        └── PauseModal
              └── RunSummaryScene   (derrota)
```

---

## 1. Menú Principal (`MainMenu.tscn`)

**Layout:** Pantalla completa. Logo del juego arriba, botones en columna centrada. Fondo animado (sprite de agua o paisaje de ruta en pixel art).

### Botones

| Botón        | Acción                                               | Condición                |
| ------------ | ---------------------------------------------------- | ------------------------ |
| JUGAR        | Abre modal de selección de modo → `StarterSelection` | Siempre visible          |
| CONTINUAR    | Carga la run guardada → `GameplayScene`              | Solo si hay run guardada |
| GACHA        | Abre `GachaScene`                                    | Siempre visible          |
| MODO EXTRA   | TBD                                                  | Oculto hasta implementar |
| ESTADÍSTICAS | Abre `GlobalStatsScene`                              | Siempre visible          |

### Modal de Selección de Modo (antes de iniciar run)

Se muestra antes de pasar al `GameplayScene`. Opciones:

- **MODO IDLE** (auto-combat activo, velocidad configurable)
- **MODO MANUAL** (el jugador elige movimientos cada turno)
- _(Modos adicionales TBD)_

---

## 2. Selección de Inicial (`StarterSelection.tscn`)

### Descripción

Una pantalla estilo Pokédex donde el jugador elige su Pokémon inicial para la run. Solo se pueden elegir: primera forma evolutiva, Pokémon de forma única, paradoja, singulares, pseudo legendarios (en su primera forma evolutiva) y legendarios (en su primera forma evolutiva si aplica).

### Layout

- **Panel izquierdo:** Grid de sprites de Pokémon. Los no desbloqueados = silueta negra.
- **Panel derecho:** Detalle del Pokémon seleccionado.

### Panel de Detalle

```
[Sprite del Pokémon seleccionado]
Nombre  |  Tipo(s)
─────────────────────────────
Gráfica hexagonal de stats (HP, ATK, DEF, SPATK, SPDEF, SPE)
Dropdown: Naturaleza  [Seleccionar]
Boton: Shiny  [True o False]
─────────────────────────────
Movimientos base: [M1] [M2] [M3] [M4]
─────────────────────────────
Toggle: [STATS NORMALES / IVs Y HUEVO]  ← toggle
  • En modo IV: muestra números de IV individuales
  • En modo Huevo: muestra los egg moves desbloqueados
─────────────────────────────
[ELEGIR ESTE POKÉMON]
```

### Sistema de Desbloqueo

- **Por defecto:** Los 3 iniciales de cada generación (Gen 1–9) = 27 iniciales.
- **Captura:** Al capturar un Pokémon por primera vez → se agrega al pool de iniciales.
- **Gacha:** Desbloquea Pokémon adicionales, shinies y egg moves.
- **Duplicado:** Al capturar un duplicado, el inicial hereda:
  - El IV más alto histórico por stat
  - La naturaleza del duplicado se añade a la lista de naturalezas elegibles

### Shinies

- Se desbloquean capturándolos o vía gacha.
- Tienen animación de entrada diferente (destellos).

---

## 3. Pantalla de Gameplay (`GameplayScene.tscn`)

### Layout General (inspirado en FireRed GBA)

```
┌──────────────────────────────────────┐
│  [Zona: Ruta 1 — 4/10 batallas]  ⚙ │  ← HUD superior
├─────────────────────┬────────────────┤
│                     │ HP ████░░  45/78│
│  [Sprite enemigo]   │ Nv.12 STARLY   │
│                     │ ⬛⬛⬛⬛ (tipo) │
├─────────────────────┴────────────────┤
│                                      │
│     [Dialog Box]                     │
│     "Starly usó Ala de Acero..."    │  ← texto del turno
│                                      │
├──────┬───────────────────────────────┤
│ HP   │                               │
│ ████ │  [Sprite jugador - back]      │
│ Nv18 │                               │
│BULB. │                               │
├──────┴──────────┬──────┬─────────────┤
│  [M1] [M2]      │  🎒  │   ⚡×1 ×2  │
│  [M3] [M4]      │ TEAM │   ×3 ×4 → │
└─────────────────┴──────┴────────────┘
```

### Componentes del HUD

| Componente                | Detalle                                                         |
| ------------------------- | --------------------------------------------------------------- |
| **Zona y progreso**       | "Ruta 1 — 4/10 batallas" con barra de progreso                  |
| **Panel enemigo**         | Sprite, nombre, nivel, tipos, barra HP (sin número)             |
| **Panel jugador**         | Sprite de espalda, nombre, nivel, barra HP (con número)         |
| **Dialog Box**            | Texto del último turno, fondo blanco con borde negro retro      |
| **Botones de movimiento** | Grilla 2x2. En auto-mode: visibles pero opacos (no clickeables) |
| **Botón mochila (🎒)**    | Abre `BagPanel`                                                 |
| **Botón equipo (TEAM)**   | Abre `TeamPanel`                                                |
| **Botones de velocidad**  | ×1 ×2 ×3 ×4 — solo en IDLE mode. Controlan `ActionCooldown`     |
| **Botón Pokéball**        | Visible. Usa la ball seleccionada en el inventario              |

### Modos de Juego (mid-run toggle)

- **IDLE:** Auto-combat. Botones de velocidad activos. Movimientos opacos.
- **MANUAL:** El jugador clickea el movimiento de cada turno. Sin velocidad.
- El toggle es persistente en la run (se puede cambiar en pausa).

### Avance de Zona

- Cada X batallas ganadas → avance automático a la siguiente zona.
- El último Pokémon de la zona es un **Boss** (stat boost automático).
- Al llegar al Gym → transición automática a `GymScene`.

---

## 4. Escena de Gimnasio (`GymScene.tscn`)

### Flujo

1. Fade-in → aparece el sprite del Líder de Gimnasio (grande, centrado).
2. Dialog box con el diálogo de introducción del Líder.
3. [CONFIRMAR] → el combate inicia (mismo layout que `GameplayScene`).
4. **Victoria:**
   - Dialog de victoria del Líder.
   - Animación de medalla + descripción del buff que otorga.
   - Continúa al siguiente bloque de zonas.
5. **Derrota:**
   - Dialog de derrota del Líder.
   - [CONFIRMAR] → `RunSummaryScene`.

---

## 5. Panel del Equipo (`TeamPanel.tscn`)

Overlay de pantalla completa (estilo menú Pokémon GBA). Se abre con el botón TEAM.

### Vista de Lista (6 slots)

```
┌─────────────────────────────────┐
│ [Sprite]  BULBASAUR    Nv. 18   │
│           HP ██████░░  45/78   │
│           XP ███░░░░░          │
│           [M:Látigo Cepa] [M:...]│
├─────────────────────────────────┤
│ [Sprite]  STARLY       Nv. 12   │
│           HP ███████░  67/73   │
│           XP █░░░░░░░          │
│         ✦ [FAINTED]             │
└─────────────────────────────────┘
      [MOVER]          [VOLVER]
```

### Vista de Detalle (al clickear un Pokémon)

- Sprite grande
- Stats completos (con IVs y naturaleza)
- Habilidad pasiva
- Movimientos con PP
- Botones: [MOVER] [USAR ÍTEM] [VOLVER]

### Sistema de Mover

- Botón MOVER → selecciona un Pokémon → selecciona la posición destino → intercambio.

---

## 6. Mochila / Inventario (`BagPanel.tscn`)

Overlay estilo bolsa Pokémon GBA.

### Pestañas / Bolsillos

| Bolsillo           | Contenido                                                            |
| ------------------ | -------------------------------------------------------------------- |
| Pokéballs          | Poké Ball, Great Ball, Ultra Ball, Master Ball                       |
| Medicinas          | Poción, Súper Poción, Hiper Poción, Full Restore, Revive, Max Revive |
| Objetos de Batalla | X-Ataque, X-Defensa, etc.                                            |
| Objetos Especiales | Piedras evolutivas, MTs, Mega Stones                                 |

### Uso de Ítems

- Los ítems de curación **consumen un turno** de batalla.
- Las piedras evolutivas, MTs y mega stones **no consumen turno**.
- Al seleccionar un ítem que requiere objetivo → se abre selector de Pokémon del equipo.

---

## 7. Escena de Gacha (`GachaScene.tscn`)

TBD — Se definirá en detalle cuando se planifique el sistema de gacha. Pendiente de lluvia de ideas.

**Lo que sí sabemos:**

- Da Pokémon (nuevos o duplicados que mejoran IVs/naturalezas).
- Da egg moves para los Pokémon del pool.
- Da shinies.

---

## 8. Resumen de Run (`RunSummaryScene.tscn`)

Se muestra al perder (derrota en gimnasio o equipo completo fainted).

### Contenido

- Tiempo de la run
- Zona/Gimnasio más lejos alcanzado
- Pokémon capturados
- Batallas ganadas
- Pokémon mejor nivel
- Medallas obtenidas
- Comparativa con récord personal

### Botones

| Botón          | Acción                      |
| -------------- | --------------------------- |
| NUEVA RUN      | Vuelve a `StarterSelection` |
| MENÚ PRINCIPAL | Vuelve a `MainMenu`         |

---

## 9. Estadísticas Globales (`GlobalStatsScene.tscn`)

Historial de todas las runs del jugador. Destacados:

- Total de runs
- Mejor run (zonas completadas)
- Run más larga (tiempo)
- Total de Pokémon capturados
- Total de batallas ganadas
- Pokémon más alto nivel conseguido
- Records personales

---

## 10. Menú de Pausa (`PauseMenu.tscn`)

Overlay sobre el gameplay. Botones:
| Botón | Acción |
|---|---|
| CONTINUAR | Cierra pausa |
| MODO: IDLE/MANUAL | Toggle del modo de batalla |
| CONFIGURACIÓN | Volumen música, volumen SFX |
| ABANDONAR RUN | Confirmación → `RunSummaryScene` |

---

## 11. Cuando un Pokémon queda Fainted

- El Pokémon se marca como fainted en el equipo (no desaparece).
- **IDLE mode:** El sistema intenta usar Revive automáticamente si está en inventario Y el Pokémon está marcado como prioritario (o es el único del equipo).
- **MANUAL mode:** Se muestra una notificación → el jugador elige si usar Revive o ignorarlo.
- Si **todo el equipo queda fainted** → derrota inmediata → `RunSummaryScene`.

---

## 12. Notas de Estilo Visual (FireRed)

- **Fuente:** Pixel font estilo GBA (similar a la usada en FireRed).
- **Paleta:** Colores retro GBA. Fondos claros, texto negro, bordes gruesos negros.
- **Sprites:** Pokémon sprites de generaciones correspondientes (front para enemigo, back para jugador).
- **Dialog Box:** Rectángulo blanco con borde negro de 2px, texto animado letra a letra.
- **HPs:** Barras de color (verde/amarillo/rojo según porcentaje), sin degradado, pixel-perfect.
- **Transiciones:** Fade to black entre escenas (como en los juegos GBA).
- **Sonido:** SFX de botones pixelados, cry de Pokémon al aparecer, música de ruta/batalla.

---

## Escenas a Crear (Orden de Implementación)

```
Prioridad 1 (flujo mínimo jugable):
  [ ] MainMenu.tscn
  [ ] StarterSelection.tscn
  [ ] GameplayScene.tscn

Prioridad 2 (completar gameplay):
  [ ] GymScene.tscn
  [ ] TeamPanel.tscn
  [ ] BagPanel.tscn
  [ ] PauseMenu.tscn
  [ ] RunSummaryScene.tscn

Prioridad 3 (meta-progresión):
  [ ] GlobalStatsScene.tscn
  [ ] GachaScene.tscn   ← requiere diseño de sistema
```
