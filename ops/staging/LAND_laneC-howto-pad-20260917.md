# LAND laneC-howto-pad-20260917

## SCOPE
- Moved Startup Options `How to play` from `Btn(..., new Vector2(0, -565), ...)` to `BtnBottom(..., 310f, ...)`, placing it above Search Levels (`210f`) and Back (`110f`).
- Soft Key `2.956`, SoftNap-X, ValleyShade, PadChannel, MidStrip wash, Overlay Always, Face Pad remesh, LevelSetSpawner, and Cacique remain untouched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
