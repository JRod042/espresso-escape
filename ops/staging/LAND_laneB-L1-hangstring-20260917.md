# LAND laneB-L1-hangstring-20260917

## SCOPE
- In `SpawnCafeYard`, changed the L1 `HangString` base length from `0.35f` to `0.39f`; random range remains `+ (float)rng.NextDouble() * 0.15f`.
- No held systems or forbidden areas were touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
