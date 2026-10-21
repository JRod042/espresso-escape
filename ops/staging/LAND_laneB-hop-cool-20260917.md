# LAND laneB-hop-cool-20260917

## SCOPE
- In `Assets/Scripts/BeanImpulse.cs`, changed `coolSeconds` from `0.14f` to `0.13f` and tagged the line with this land id.
- `forwardImpulse` and `hopImpulse` remain untouched; no held or forbidden systems were touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
