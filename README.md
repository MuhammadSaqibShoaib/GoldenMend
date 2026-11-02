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

## Run it

1. Open this folder in Unity Hub with Unity 6000.3.8f1.
2. Open `Assets/_Game/Bootstrap/Game.unity`.
3. Set the Game view to a portrait resolution (for example 1080x1920) and press Play.

HUD: Restart, Hint (also pulses after 20 s idle), and level switching. "Bench peak" compares your busiest workbench with the solver's best.

## Architecture

The rules live in a plain C# core with no Unity dependencies; everything else is layered around it, and each layer is its own assembly so dependencies can only point inward.

| Folder | Assembly | Role |
| --- | --- | --- |
| `Assets/_Game/Core` | GoldenMend.Core | Rules engine (tap in, ordered events out), board state, pile rack, solver. `noEngineReferences: true`. |
| `Assets/_Game/Data` | GoldenMend.Data | Level specs (vessel shape and rack shape as text), level builder that keeps only solver-approved deals, level catalog. |
| `Assets/_Game/Presentation` | GoldenMend.Presentation | Board presenter, shard views, animation queue, generated vessel art and sound, HUD. |
| `Assets/_Game/Bootstrap` | Assembly-CSharp | Composition root and the game scene. |
| `Assets/_Game/Editor` | GoldenMend.Editor | **Golden Mend** menu: rebuild the game scene, capture level screenshots. |
| `Assets/Tests` | EditMode, PlayMode | Rule, rack, solver and level tests; presenter smoke tests that play every level to the win screen. |

## Tests

From Unity: **Window > General > Test Runner**, then Run All on the EditMode and PlayMode tabs.

From the command line (close the editor first; PlayMode and screenshots need graphics, so don't pass `-nographics`):

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe"
"$U" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
"$U" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-results.xml
"$U" -batchmode -quit -projectPath . -executeMethod GoldenMend.EditorTools.LevelScreenshots.CaptureAll
```

## Roadmap

- Gold seam shader and final art, haptics, full audio
- UI Toolkit screens: home, level intro, win, continue offer, shop
- Boosters (Magnifier, Brush, Glue Pot), frozen shards, continue for +2 bench slots
- Level editor window with one-click solving; 30+ hand-tuned levels
- Museum meta, Gold Purity win streak, lives
- Analytics, remote config, ads and in-app purchases
- Android release, then iOS

## Status

In active development.
