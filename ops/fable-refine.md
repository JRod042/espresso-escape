# Espresso Escape — refine & enhance audit (Fable, 2026-10-21)

Reference = Imagine handmade set: `DriveLook/imagine/285.jpg`, `frames/285-01..06.jpg` (hop), `frames/set-01..06.jpg` + `gen-2d7dcad9.mp4` (wide set: grinder, kraft bridge, flower tree), `gen-5ee82b85.jpg` / `gen-8ec4e4ae.jpg` (title on wall). The preview-reject recording is NOT the look. Unity-side plates: `Assets/Look/imagine-bean.jpg`, `imagine-set.jpg`, `Assets/Look/refs/`. Live view = `Assets/Look/refs/cam-shot.png` (= cam-k744).

Project facts: Unity 6000.6.4f1, Built-in RP (no SRP), Gamma color space, legacy Input Manager, uGUI 2.6. Not a git repo on disk (no `.git`). Proof baseline (Sep 17): physics apex 1.038, PackChips GREEN, L1–L3 spawn counts [1346, 5220, 5370].

Format per item: **Ref** (what the plates show) · **Now** (what the project does) · **Change** (concrete).

---

## 1. Visuals & graphics

### 1.1 Bean body
- **Ref**: one smooth felt egg, taller than wide (~1.35:1), slightly narrower at the top, uniform mid-brown, soft fuzzy silhouette, no lumps, no cleft, no two lobes.
- **Now**: `StitchedBean()` assembles Bean Core L/R, Bean Lobe L/R, Felt Nap L/R, Face Pad half-shell meshes, Bean Trench, Seam Plug, parked channels/shells/valley/crease, crown tips — a dual-lobe with a black cavity. `FeltFiber` adds 8-octave vertex displacement (lumpy silhouette) and a ±55% hash-noise albedo multiplier (`lerp(0.458,1.562,felt)`).
- **Change**: one procedural egg mesh (`CraftMesh.Egg`) on the `Imagine Bean` root, painted with `tex-felt-brown.png` (clean felt), no vertex displacement, noise ±7%. Remove lobes/pads/trench/plug/parked objects. Keep the CapsuleCollider (r 0.38, h 0.86, dir 1), Rigidbody (0.9 / 0.18 / 0.9 / interpolate / continuous / freeze X,Z rot), `BeanImpulse`, `LandFeel`, position (0, 0.76, 0.35), yaw 180.

### 1.2 Bean surface / white dashes
- **Ref**: felt nap only; no bright specks.
- **Now**: `PaintBean` uses `imagine-bean-albedo.png`, which is a dark speckled texture with white dashes baked in, then multiplies by `_BaseColor (1.226,1.176,1.040)`. The Felt Fleck / Stub Fleck loops are already disabled in code, so the dashes come from the albedo + shader noise, and from tan `Arm/Leg Stub Stitch` capsules.
- **Change**: `PaintBean` → `tex-felt-brown.png`, tint ≈ (1.02, 0.96, 0.90). Drop mid-strip / cleft-nest / yarn-nest shader hacks. No stub stitches on limbs.

### 1.3 Center stitch
- **Ref**: one pale running stitch down the front meridian, slightly recessed, thread colour ~ (0.85, 0.78, 0.62).
- **Now**: 12 `Dash Stitch` seeds at 0.006 scale inside a dark trench; shader `_YarnNestAmt` paints dark dashes.
- **Change**: shader `_SeamAmt/_SeamWidth` soft groove on object-space x≈0, front hemisphere; 12 `Dash Stitch` capsules placed on the egg meridian, pale thread colour, proud by ~4 mm.

### 1.4 Eyes & smile
- **Ref**: two glossy black beads with a specular highlight, ~9% of body width, slightly above centre, spacing ~0.42 body width; tiny dark thread smile below.
- **Now**: `BeadUnlit` is Queue Overlay, ZTest Always — eyes draw through trees and the bean itself; smile skips the middle (two half arcs on separate pads).
- **Change**: new `Escape/Bead` lit shader (Blinn-Phong highlight + fresnel, depth-tested). Eyes at local (±0.13, +0.06) on the egg surface, 0.062 scale, highlight dot; continuous 7-bead smile arc.

### 1.5 Arms & legs
- **Ref**: short rounded mitten stubs from the sides (~0.16 body width long), two short legs with rounded feet, no visible joint.
- **Now**: `Arm()/Foot()` each spawn Junction (0.28×0.22×0.26 sphere), Capsule, Mitt Lobe, SoftCap, Nap (1.15× capsule) + seam + 3 stitches = 9 objects per limb; junction and nap spheres are body-sized and fuse into the torso.
- **Change**: arm = 1 capsule sunk into the egg + 1 mitt sphere; leg = 1 capsule + 1 foot sphere; no junctions, no naps, no stitches. Mid-stride offsets kept (left forward).

### 1.6 Camera
- **Ref**: wide diorama; bean ≈ 33% of frame height, centred, floor visible in front, clouds + twine at top, wall behind.
- **Now**: builder says 28 mm physical camera, but saved scene renders a face crop; `aperture/focusDistance` have no effect in BiRP.
- **Change**: non-physical camera, FOV 48, follow offset (0, 1.05, -2.9), look-at bean +0.25 with 0.35 forward look-ahead; `AudioListener` added. `SetCameraFollow` defaults updated.

### 1.7 Lighting & shadows
- **Ref**: soft warm top-front key, soft ground shadows under bean/trees, no speculars.
- **Now**: 7 lights; `FeltFiber` only reads `_LightColor0` (brightest directional) and has no shadow receive → Warm Fill, Warm Rim, Bounce, Table Fill, Bean Soft Key/Fill have no effect on felt. Quality tier "Ultra" (active) has shadows disabled. Fake `Contact Shadow` discs instead.
- **Change**: `FeltFiber` receives shadows (`SHADOW_COORDS/TRANSFER_SHADOW`), Warm Key soft shadows strength 0.6; `GameSession` forces `QualitySettings.shadows = All`, distance 30. Bean Soft Key / Soft Fill objects kept as-is (held), contact-shadow discs removed from the title set.

### 1.8 Kraft wall
- **Ref**: fine, even vertical corrugation, warm tan (~0.75,0.60,0.42), continuous behind the whole set.
- **Now**: 36 dark brown cubes (0.44 pitch) with gaps, fixed at z 8.4 — in the direction of travel; bean passes through it after a few hops.
- **Change**: one wide wall cube with kraft texture tiled (6,1) + 28 thin flutes for parallax, warm tint; whole backdrop group (`BackdropFollow`) tracks bean z at +8.5 so the plate framing persists during play.

### 1.9 Floor
- **Ref**: coarse burlap weave (~45 threads/m), warm tan.
- **Now**: 11×16 cube ending at z≈11.2 (goals go to z 42 → bean falls off world on L2+), tile 9.
- **Change**: 16×70 floor from z −6 to 64, burlap tile (16, 70) so weave scale stays ≈45/m; `LevelRunner` also resets the bean if y < −3.

### 1.10 Trees, clouds, props
- **Ref**: pines = 4–5 stacked felt tiers with brown pinecone dots; red tree = trunk + 7 red pompoms; clouds = 3-lobe white felt on twine; hanging yarn balls on strings; moss balls; roast beans; succulent; cardboard grinder; kraft bridge.
- **Now**: `Pine()` ≈ 220 spheres (72 yarn loops, 60 bumps, 28 tips…), `RedTree()` 28 blooms × 22 sub-spheres ≈ 640 objects; title scene = 20,787 objects / 15.8 MB; title text placed twice and overlapping.
- **Change**: Pine = trunk + 5 tiers + 4 cones; RedTree = trunk + 3 branches + 7 pompoms; clouds 4 spheres; single stitched title on the backdrop; grinder/bridge/moss/roast/succulent kept but simplified. Target ≈ 600 objects.

### 1.11 Materials / batching
- **Ref**: n/a.
- **Now**: every `PaintTex`/`PaintBean` call allocates a new Material (one per object); no instancing; no static batching.
- **Change**: material cache keyed by texture+tint+tile in both builder and spawner; `enableInstancing`; `multi_compile_instancing` in shaders; set props `isStatic`; spawner calls `StaticBatchingUtility.Combine` after spawn (play mode only); low-poly cached sphere for small props.

## 2. Gameplay feel
- **Ref**: `285-01..06` hop — crisp take-off, short airtime, visible squash on landing.
- **Now**: `BeanImpulse` fixed impulse (5.2 fwd / 6.4 up, cool 0.13) — kept. Any click anywhere hops, including UI buttons and on the title screen. `LandFeel` squash only; no stretch on take-off. No hop/land hooks for audio.
- **Change**: `BeanImpulse` ignores pointer-over-UI and exposes `OnHop`; it is enabled only in `Phase.Play`. `LandFeel` adds take-off stretch and `OnLand`. Camera look-ahead. Impulse values, gravity −22, collider untouched → physics proof unchanged (1.038).

## 3. Levels
- **Now**: 5 `LevelDef`s; `LevelRunner` places a goal marker but never detects reaching it → `PlayerProgress.ClearBeat` never called, nothing unlocks. No collectibles though "How to play" promises honey beans. L4/L5 counts (orbs 9092, roast 9086, moss 9079, pines 2441, blooms 6655 / L5 blooms 18625 …) ≈ 240k+ objects.
- **Change**: `LevelRunner.Update` → goal reached when bean.z ≥ goalZ → `GameSession.CompleteLevel` → unlock + Level Clear overlay; fall reset. `Pickup` honey beans spawned under a separate `Level Pickups` root (spawner counts unchanged). L1–L3 spawn counts kept exactly ([1346, 5220, 5370]); L4/L5 reduced to sane ranges while keeping `LevelDensitySmoke` (L5 > L2, rising, distinct). `PineLite` restyled to tiers count-neutrally.

## 4. UI
- **Ref**: `gen-5ee82b85` — cream stitched title on the wall, minimal chrome, "Tap anywhere to hop".
- **Now**: uGUI legacy Text, kraft panels OK; HUD only "Tap to hop" + Menu; "· polish HOLD" badge shown to players on L4/L5 rows; no level name, no progress, no clear screen.
- **Change**: HUD shows level title, bean count, progress bar; Level Clear overlay (Next level / Title); hold badge removed; Options copy aligned to actual mechanics.

## 5. Audio
- **Now**: none. No AudioListener on the camera; only a Mute toggle.
- **Change**: `EscapeAudio` synthesises clips at runtime (hop, land, pickup, clear, soft ambient hum) — no asset dependency; AudioListener on Set Camera; mute pref respected.

## 6. Ship
- **Now**: Windows build succeeds (131 MB, 511 warnings). iOS export needs module. Bundle id `com.jrod042.espressoescape`. Quality tier Ultra has shadows off; no target frame rate; privacy/age-rating stubs.
- **Change (this pass)**: runtime quality/frame-rate boot in `GameSession`. **Left for later**: rebuild scene (Grok Build owns the Editor), re-run `EscapeCLI.RunProofs`, re-capture `cam-shot.png`, re-run Windows build, write privacy policy copy, decide on orientation lock (currently AutoRotation), init git.

## Files changed in this pass
`Assets/Look/FeltFiber.shader`, `Assets/Look/Bead.shader` (new), `Assets/Scripts/Editor/BuildEscapeScene.cs`, `Assets/Scripts/SetCameraFollow.cs`, `Assets/Scripts/BeanImpulse.cs`, `Assets/Scripts/LandFeel.cs`, `Assets/Scripts/Game/CraftMesh.cs` (new), `Assets/Scripts/Game/BackdropFollow.cs` (new), `Assets/Scripts/Game/Pickup.cs` (new), `Assets/Scripts/Game/EscapeAudio.cs` (new), `Assets/Scripts/Game/GameSession.cs`, `Assets/Scripts/Game/LevelRunner.cs`, `Assets/Scripts/Game/LevelSetSpawner.cs`, `Assets/Scripts/UI/TitleFlow.cs`, `Assets/Scripts/UI/SearchMenu.cs`.

Not touched: `Assets/Escape.unity`, `ProjectSettings/*`, `BeadUnlit.shader`, `BeadUnlitDepth.shader`, `SeamFelt.shader`, proofs (`PhysicsProof`, `EscapeCLI`, `LevelDensitySmoke*`, `PackChipsVerify`).

## Verification done without touching the open Editor
Runtime (`Assembly-CSharp`) and editor (`Assembly-CSharp-Editor`) scripts compiled clean with Unity 6000.6.4f1's own Roslyn (`Editor/Data/DotNetSdk/.../csc.dll`) against the engine module DLLs, output to `%TEMP%`. Shaders were reviewed by hand (ForwardBase + `multi_compile_fwdbase`, `SHADOW_COORDS/TRANSFER_SHADOW/SHADOW_ATTENUATION`, `FallBack "Diffuse"` for the ShadowCaster pass).

## Still to run (Grok Build / Jorge, in the open Editor)
1. Escape/Build Scene → regenerates `Assets/Escape.unity` with the egg bean, wide diorama camera, 70 m burlap, following kraft backdrop.
2. `EscapeCLI.RunProofs` → physics apex (expect ≈1.038 unchanged: capsule, mass/drag, impulses, gravity -22 untouched), PackChips, L1–L3 counts (L1–L3 loops untouched; expect [1346,5220,5370]).
3. `LevelDensitySmoke` → L5 should land ≈5.7k (> 5220), L4 well below.
4. Escape/Capture Look Shot → compare `Assets/Look/refs/cam-shot.png` against `DriveLook/imagine/285.jpg`.
