# LAND laneB-cam-offsety-20260917

## SCOPE
- In `Assets/Scripts/SetCameraFollow.cs`, changed `new Vector3(0.2f, 1.15f, -2.4f)` to `new Vector3(0.2f, 1.18f, -2.4f)` and tagged the line with this land id.
- `posDamp` remains `8.5f`; `lookDamp` remains `10.5f`. No held or forbidden systems were touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
