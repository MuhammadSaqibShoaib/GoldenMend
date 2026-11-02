# Golden Mend

A calm puzzle game about kintsugi, the Japanese art of mending broken pottery with gold. Rebuild each vessel shard by shard; every join fills with a gold seam, and every finished piece goes on display in your museum.

Mobile, portrait, one-handed. Built with Unity 6 (6000.3.8f1), URP 2D and the Input System.

## How it plays

- **The vessel** sits at the top with one gold-framed anchor shard in place. Faint outlines show where the rest belong.
- **The rack** at the bottom holds every shard, all visible. A shard is open (bright, framed) when it has a clear path to the top of the rack; locked shards are dimmed with a padlock until the shards in their way are taken.
- **Tap an open shard.** If its socket touches the part already rebuilt, it locks in with a gold seam. If not, it waits on the **5-slot workbench**, then flies home by itself as soon as a neighbour lands, which can set off chains.
- **Win** by filling every socket. **Lose** when a shard needs a bench slot and all five are full.
- **Twists:** cracked shards (dark X) need two placed neighbours; walls in the rack force longer routes; some levels start with a bench slot locked until you place three shards.

Every level is checked by a solver to be winnable without boosters, and its difficulty is the fewest bench slots a win needs.

## Game flow

- **Home:** coins, settings, the next vessel waiting to be restored, progress, Play and the Museum.
- **Museum:** every vessel as a card. Restored pieces show with gold seams and their stars; replay any of them.
- **Level:** pause, level plate and hint at the top; a short intro card names the vessel. Level 1 teaches with a one-line tip.
- **Level complete:** 1–3 stars (bench peak against the solver's best), a coin reward, then the next vessel.
- **Workbench full:** continue with two extra slots for 60 coins, try again, or go home.
- **Pause and Settings:** resume, restart, home; sound and haptics toggles; credits.

Progress, coins and settings are saved to a JSON file in the persistent data folder.

## Run it

1. Open this folder in Unity Hub with Unity 6000.3.8f1.
2. Open `Assets/_Game/Bootstrap/Game.unity`.
3. Set the Game view to a portrait resolution (for example 1080x1920) and press Play.

The UI is built in uGUI from code with the Kenney UI Pack, Kenney Game Icons and Zen Maru Gothic. It scales from tall phones to tablets (width-matched on phones, height-matched on wider screens) and stays inside the device safe area.

## Architecture

The rules live in a plain C# core with no Unity dependencies; everything else is layered around it, and each layer is its own assembly so dependencies can only point inward.

| Folder | Assembly | Role |
| --- | --- | --- |
| `Assets/_Game/Core` | GoldenMend.Core | Rules engine (tap in, ordered events out), board state, pile rack, solver. `noEngineReferences: true`. |
| `Assets/_Game/Data` | GoldenMend.Data | Level specs (vessel shape and rack shape as text), level builder that keeps only solver-approved deals, level catalog. |
| `Assets/_Game/Meta` | GoldenMend.Meta | Save data, progress and stars, coins, settings. Plain C#. |
| `Assets/_Game/Presentation` | GoldenMend.Presentation | Board presenter, shard views, animation queue, generated vessel art and sound. `UI/`: widget builder, safe area, adaptive scaler, every screen and popup, driven by a `UiTheme` asset. |
| `Assets/_Game/Flow` | GoldenMend.Flow | Game flow state machine, JSON save file, composition of the whole game. |
| `Assets/_Game/Bootstrap` | Assembly-CSharp | Scene entry point and the game scene. |
| `Assets/_Game/Editor` | GoldenMend.Editor | **Golden Mend** menu: rebuild the game scene and UI theme, capture every screen at phone, tall-phone and tablet sizes. Kenney sprite import settings. |
| `Assets/ThirdParty` | — | Kenney UI Pack and Game Icons (CC0), Zen Maru Gothic (OFL). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). |
| `Assets/Tests` | EditMode, PlayMode | Rule, rack, solver, continue, level, progress, coin and settings tests; presenter smoke tests that play every level to the win screen. |

## Tests

From Unity: **Window > General > Test Runner**, then Run All on the EditMode and PlayMode tabs.

From the command line (close the editor first; PlayMode and screenshots need graphics, so don't pass `-nographics`):

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe"
"$U" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
"$U" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-results.xml
"$U" -batchmode -quit -projectPath . -executeMethod GoldenMend.EditorTools.LevelScreenshots.CaptureAll   # all screens, 3 device sizes
```

## Roadmap

- Gold seam shader and final art, haptics, full audio
- Shop and booster purchases
- Boosters (Magnifier, Brush, Glue Pot), frozen shards
- Level editor window with one-click solving; 30+ hand-tuned levels
- Museum wings, Gold Purity win streak, lives
- Analytics, remote config, ads and in-app purchases
- Android release, then iOS

## Status

In active development.
