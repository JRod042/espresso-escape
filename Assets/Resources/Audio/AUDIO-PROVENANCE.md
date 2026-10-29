# Audio provenance (Grok Bot, 2026-10-28)

Plan: /workspace/casa-brand/exports/escape-kraft/audio-kit/unity-sound-plan/SOUND-PLAN.md. **Rule:** where sources overlap, the P0 kit wins.
Peak lock: every file in this folder has a measured sample peak of -3.0 dBFS or lower (ffmpeg volumedetect, after encode). True peak (inter-sample) was not measured.
Edit chain for Kenney/OGA: decode -> gain (only if peak > -3 dBFS; iterated until the encoded file measured <= -3.0) -> 44.1 kHz -> OGG Vorbis (UI mono q6, music stereo q4). No EQ, HPF or loop editing was applied.

| File | Source | License | Source peak | Gain | Out peak |
|---|---|---|---|---|---|
| P0/sfx_hop, sfx_land, sfx_bean, sfx_whoosh, sfx_stamp, sfx_steam, bed_room (= beds/room.wav) | Casa P0 kit v7 (p0-for-pr21) | Casa original | -3.0 | 0 (copied unchanged) | -3.0 |
| UI/select_001 | Kenney Interface Sounds 1.0 | CC0 | -1.1 | -5.2 dB | -3.3 |
| UI/back_001 | same | CC0 | -1.0 | -2.3 | -3.3 |
| UI/toggle_001 | same | CC0 | -0.9 | -2.5 | -3.3 |
| UI/toggle_002 | same | CC0 | -0.9 | -2.3 | -3.0 |
| UI/open_001 | same | CC0 | -0.9 | -2.4 | -3.5 |
| UI/close_001 | same | CC0 | -0.9 | -2.2 | -3.2 |
| UI/error_004 | same | CC0 | -1.3 | -5.1 | -3.4 |
| UI/confirmation_001 | same | CC0 | -0.9 | -2.4 | -3.3 |
| UI/glass_005 | same | CC0 | -0.9 | -2.4 | -3.2 |
| UI/glass_002 | same | CC0 | -1.0 | -2.3 | -3.4 |
| UI/jingles_PIZZI07 (level clear) | Kenney Music Jingles | CC0 | -5.7 | -0.7 (mono sum overshoot) | -3.5 |
| UI/jingles_PIZZI00 (star, +2 semitones per star) | Kenney Music Jingles | CC0 | -6.1 | 0 | -3.1 |
| Music/music_menu (little_cafe_0.wav, 156.0 s) | OGA, OatCog | CC0 | -2.6 | -0.7 | -3.1 |
| Music/music_L1 (chill_guitar.wav, 133.9 s) | OGA, Pro Sensory (Alex McCulloch) | CC0 | 0.0 | -4.6 | -3.2 |
| Music/music_L2 (LofiLoop_2.ogg, 127.9 s) | OGA, omfgdude | CC0 | -2.0 | -1.4 | -3.2 |
| Music/music_L3 (in_the_lobby.wav, 104.7 s) | OGA, Umplix | CC0 | -0.1 | -3.3 | -3.4 |
| Music/music_L4 (calm_fireplace_guitar_song_0.mp3, 103.0 s) | OGA, ShggothSlave | CC0 | -5.3 | 0 | -5.5 |
| Music/music_L5 (jazz_improv_looped_2.mp3, 131.9 s) | OGA, Pro Sensory (Alex McCulloch) | CC0 | -1.2 | -2.6 | -3.2 |

Kenney sources: zips in /tmp/kenney on the Grok box, sha256 matched SOUND-PLAN.md Appendix A (interface-sounds f2193d07…, music-jingles b729ba57…). kenney.nl blocked the box earlier this session, so the hash-verified zips were used instead of a fresh download-link pull.
OGA sources: re-downloaded 2026-10-28 from opengameart.org/sites/default/files/* (HTTP 200, byte-identical to the Sound bot's copies); each content page's License(s) field read as CC0 the same day.

## Not imported (Freesound needs a login; not signed in)
All section-2 ambiences (L1 #437461/#509950/#96906, L2 #344764 [CC-BY, never wanted], #201600 Gutek CC0 grinder swap, #249503, #32913, L3 #553390/#527658/#553887/#592036, L4 #163997/#187588, L5 #637083/#204843/#402845/#320145/#746366) and the section-4 Freesound items (#460241, #154403, #46631).
Stand-ins: the P0 room bed plays under every level; L5's grinder wake uses P0 whoosh.

## Unverified (needs a human listen)
- Loop seams: OatCog, omfgdude, Umplix and the L5 track are described as loops by their authors; L1 and L4 are not, so they restart with an audible seam. No crossfade loop was built.
- Whether each track suits its level, the mix balance (music 0.55, bed 0.12, -12 dB jingle duck, -6 dB pause duck), and LUFS targets. No loudness measurement or phone-speaker check was done.
- The P0 AHAP haptics are not wired (they need a native Core Haptics plugin). Haptics.cs still uses Handheld.Vibrate.
