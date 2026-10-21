# LAND laneB-landfeel-recover-20260917

## SCOPE
- In `Assets/Scripts/LandFeel.cs`, changed `public float recover = 10f;` to `public float recover = 11f;` and tagged the line with this land id.
- `squash` remains `0.14f`. No held or forbidden systems were touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
