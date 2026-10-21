# LAND laneC-pause-resume-pad-20260917

## SCOPE
- In `TitleFlow`, kept Pause overlay Resume centered at `new Vector2(0, 40)` and moved Title to the responsive `BtnBottom` anchor with a 110f bottom pad for hop-safe placement.
- Soft Key 2.956, SoftNap-X, ValleyShade, PadChannel, MidStrip wash, Overlay Always, Face Pad remesh, and `LevelSetSpawner` were not touched.

## VERIFY
- PackChipsVerify: PASS
- PhysicsProof: PASS, apex1=1.038, apex2=1.038, delta=0.000
- LEVEL_SMOKE_L1L3: PASS, counts=[1346,5220,5370]
