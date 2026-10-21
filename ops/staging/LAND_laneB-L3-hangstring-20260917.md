# LAND laneB-L3-hangstring-20260917

## SCOPE
- In `SpawnMillPath`, changed the L3 `HangString` length from `0.3f` to `0.34f`; the base position remains `bp + Vector3.up * 0.28f`.
- No held systems or forbidden areas were touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
