# Liquid Glass UI panel (Espresso Escape)

Apple "Liquid Glass"-style frosted panel for **uGUI** on **URP**.

Files
- `LiquidGlass.shader` - UI shader `UI/LiquidGlass`: rounded corners, white rim highlight, light inner gradient, tint/opacity, optional backdrop blur. Supports uGUI Mask/RectMask2D (stencil + clip rect).
- `LiquidGlass.mat` - default material (rim, gradient, saturation, fallback opacity live here).
- `LiquidGlassPanel.cs` - component (`Add Component > UI > Effects > Liquid Glass Panel`): corner radius, blur, tint, opacity, blur mode. Panels share one material and still batch.
- `LiquidGlassPanel.prefab` - a 600x400 Image using the material + component.

Use on menus / HUD
1. Drag `LiquidGlassPanel.prefab` under any Canvas (or add an `Image`, set its Material to `LiquidGlass`, leave Source Image empty, and add `Liquid Glass Panel`).
2. Put buttons, text and Phosphor icons (`Assets/ThirdParty/PhosphorIcons`) as children.
3. Tweak Corner Radius / Blur / Tint / Opacity on the component; rim + gradient on the material.
   The component turns on the canvas's TexCoord1/TexCoord2 shader channels automatically.
4. Runtime-built UI (TitleFlow/FeltUI): `img.material = glassMat; img.gameObject.AddComponent<LiquidGlassPanel>();`

Blur vs fallback
- Real blur samples URP's `_CameraOpaqueTexture`. It needs **both**:
  - Opaque Texture enabled (URP asset `EscapeURP` -> Rendering -> Opaque Texture, or per-camera override), and
  - the Canvas in **Screen Space - Camera** or **World Space** (Screen Space - Overlay draws after the camera, so there is no backdrop to sample).
- Otherwise (Blur Mode = Auto) the panel uses the **tinted translucent fallback**: same shape, rim and gradient, no blur.
  Espresso Escape currently has Opaque Texture OFF and an Overlay canvas, so you will see the fallback until both are changed.
- Blur only shows world geometry (opaque + skybox), not other UI or transparent objects behind the panel - same limit as any URP grab approach.

iOS / Metal performance
- Blur mode = 16 texture taps per pixel on the panel area plus URP's opaque copy each frame (with `Opaque Downsampling` 2x bilinear, which also softens it for free). Keep glass panels small/few on HUD, prefer the fallback on in-game HUD and blur on pause/menus.
- Fill-rate is the cost: large full-screen glass on older iPhones (A12-A13) can cost ~1-2 ms. Lower `Blur` doesn't reduce taps - it narrows them; switch to `ForceFallback` on low quality tiers if needed.
- Opaque Texture adds a copy for every camera that requests it - enable it only if you use blur.