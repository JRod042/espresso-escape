# LAND laneC-opt-tip-font-20260917

## SCOPE
- In `Assets/Scripts/UI/TitleFlow.cs`, increased the Startup Options hop-lane tip Label at `y=-610` from font `20` to `22`.
- The second tip Label remains font `18`; no held/forbidden systems or bottom pads were changed.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
