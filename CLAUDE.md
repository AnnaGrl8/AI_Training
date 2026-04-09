# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A 2-player local Pong game built in Unity 6, based on the Unity Game Systems Cookbook. Part of the **LiftIA** training program (documentation is partially in French). Used as a lab for AI-assisted development with Claude Code and MCP for Unity.

## Build & Run

- Open the project in Unity 6 — target version `6000.4.0f1` (current ProjectVersion.txt shows 6000.3.10f1)
- Entry scene: `Assets/Scenes/Bootloader_Scene.unity`
- Game scenes: `Assets/PaddleBall/Scenes/PaddleBallClassic_Scene.unity`, `PaddleBallHockey_Scene.unity`, `PaddleBallFoosball_Scene.unity`
- Test framework (`com.unity.test-framework` 1.6.0) is installed but no test suites exist yet
- Input: W/S (Player 1), Arrow Up/Down (Player 2), R (Restart)

## Architecture

### Event-Driven ScriptableObject Pattern

All inter-component communication uses ScriptableObject-based event channels — components never reference each other directly. The base class is `GenericEventChannelSO<T>` in `Assets/Core/EventChannels/`, with concrete types for Bool, Int, Float, String, Vector2, Vector3, GameObject, PlayerID, and game-specific channels (PlayerScore, ScoreList). `VoidEventChannelSO` is a separate class inheriting directly from `DescriptionSO` (not the generic base) since it carries no payload.

To add a new event type: create a new SO inheriting `GenericEventChannelSO<T>`, add a corresponding Listener MonoBehaviour, and optionally a custom editor.

### Core vs Game separation

- **`Assets/Core/`** — Reusable framework: event channels, UI management (`UIManager`/`View` base), save/load system (`SaveManager` with `IDataSaver` strategy), scene loading, command pattern (`ICommand`/`CommandManager`), objective tracking, audio delegation, runtime sets
- **`Assets/PaddleBall/`** — Game-specific code: Ball, Paddle, Bouncer, ScoreGoal, managers (GameManager, GameSetup, ScoreManager), game data SOs, input handling, UI views
- **`Assets/Patterns/`** — Reusable gameplay component patterns (TeamID, GridSpawner, UI binding helpers)

### Key ScriptableObjects

- **`GameDataSO`** — All tunable game parameters (speeds, delays, bounce multiplier, player IDs)
- **`LevelLayoutSO`** — Level geometry data with JSON export/import for modding
- **`InputReaderSO`** — Bridges Unity's Input System actions to game code via UnityAction events
- **`PlayerIDSO`** — Enum-like identity pattern using SO reference equality
- **`ObjectiveSO`** / **`ScoreObjectiveSO`** — Win condition definitions

### Game Flow

```
GameManager broadcasts GameStarted
  → Ball.ServeBall() launches with random direction
  → Paddles listen to InputReaderSO for movement
  → Bouncer detects collisions, broadcasts bounce direction
  → ScoreGoal triggers on ball entry, broadcasts GoalHit
  → GameManager receives GoalHit, broadcasts PointScored
  → ScoreManager updates scores, ScoreObjectiveSO checks win condition
  → On win: WinScreen displays, GameManager.EndGame()
  → R key triggers GameRestarted → GameManager.OnReplay()
```

### Level Loading

`GameSetup` supports two modes: loading from `LevelLayoutSO` asset or from JSON file (exported via `LevelLayoutSOEditor`). It instantiates ball, paddles, walls, and goals, parenting them under a "Level" transform.

### UI System

Stack-based screen management via `UIManager` with `View` base class. Screens show/hide through event channel broadcasts. Uses UI Toolkit (UIElements).

### Rendering

Universal Render Pipeline (URP) v17.4.0. Pipeline settings in `Assets/PaddleBall/PipelineSettings/`.

## Packages of Note

- **com.coplaydev.unity-mcp** — MCP server integration for AI-assisted development
- **com.unity.inputsystem** — New Input System (action maps in `Assets/PaddleBall/Input/`)
- `PaddleBallControls.cs` is auto-generated from `PaddleBallControls.inputactions` — do not edit directly

## Code Style

Follow conventions in `Assets/Core/_StyleGuide/StyleExample.cs`:
- Pascal case for public members, camel case for locals/parameters
- `m_` prefix for private fields, `s_` for static, `k_` for constants
- Allman braces, 80–120 char lines
- `[Tooltip]` instead of comments on serialized fields
- `<summary>` tags on classes, structs, and ScriptableObjects
- SO suffix on ScriptableObject class names (e.g. `GameDataSO`)
- Namespaces: `GameSystemsCookbook` (core), `GameSystemsCookbook.Demos.Pong` (game-specific)
