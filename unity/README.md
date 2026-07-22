# Reveal — Unity (iOS & Android)

Native rewrite of *Reveal*, a scratch-to-uncover puzzle game built for
ad-supported mobile. This is a real Unity project: open it, hit Play, build to
a device. The game is constructed **entirely from C#** (no hand-authored
scenes/prefabs) so the whole thing is reviewable and reproducible from source.

## Requirements

- **Unity 6** (`6000.5.4f1`, see `ProjectSettings/ProjectVersion.txt`). Any
  Unity 6 patch works; 2022.3 LTS also imports fine and may prompt an upgrade.
- iOS build: Xcode + an Apple Developer account.
- Android build: Android Build Support module (installed via Unity Hub).

## Open & run

1. Unity Hub → **Add** → select the `unity/` folder.
2. Open it. Unity generates `Library/`, `.meta` files and the remaining
   `ProjectSettings` on first import.
3. Open `Assets/Scenes/Main.unity` (it's intentionally empty — the game
   bootstraps itself in code) and press **Play**.

`GameBootstrap` runs `[RuntimeInitializeOnLoadMethod]` before the scene loads,
so it builds the camera, canvas, EventSystem, managers and UI automatically.
There is nothing to wire up in the Inspector.

## Project layout

```
Assets/Scripts/
  Core/    GameConfig, Scenes, SaveSystem, GameBootstrap   (tuning, data, entry)
  Game/    Board, BoardView, MotifPainter, GameManager     (gameplay + rendering)
  Meta/    Missions                                        (daily missions)
  UI/      UIFactory, GameUI                               (programmatic UI)
  Ads/     IAdService, AdManager, MockAdService,           (ad mediation)
           LevelPlayAdService, AdOverlay
  Audio/   Sfx                                             (procedural sound)
```

Gameplay parity with the web prototype: endowed head-start tiles, 3 hearts,
visible/avoidable bombs with a gentle ramp, hidden bonus (coin) tiles, a 70%
clear threshold, star ratings, chapter chests every 5 levels, near-miss
framing, coins soft-currency, daily reward + streak, daily missions, and the
picture collection/gallery.

## Ads (real money path)

Ads go through `IAdService`. Out of the box the game uses `MockAdService`
(simulated ads) so the full loop is playable immediately.

To wire real ads with **ironSource LevelPlay (AppLovin MAX)**:

1. Import the LevelPlay Unity SDK (Package Manager Git URL or `.unitypackage`).
2. **Edit → Project Settings → Player → Scripting Define Symbols**: add
   `REVEAL_LEVELPLAY` (for iOS and Android).
3. Open `Assets/Scripts/Ads/LevelPlayAdService.cs` and set `androidAppKey` /
   `iosAppKey` (and configure rewarded + interstitial ad units in the LevelPlay
   dashboard).
4. `AdManager` automatically switches to `LevelPlayAdService` when the symbol
   is defined.

Monetization pacing follows the CrazyLabs FTUE guidance — no interstitial
pressure in the first session (`NoAdsBeforeLevel`), then one every Nth loss
(`InterstitialEveryNLosses`). Rewarded video powers the opt-in *Continue* and
*Hint* features. Tune all of it in `GameConfig`.

## Building

- **Android**: File → Build Settings → Android → add `Main.unity` → Build
  (`.aab` for Play Store).
- **iOS**: File → Build Settings → iOS → Build → open the Xcode project → set
  your team/signing → Archive.

Set the bundle id, app name, icons and portrait orientation in **Player
Settings** before shipping.
