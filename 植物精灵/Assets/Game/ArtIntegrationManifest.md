# Supplied Art Integration

Source: `C:/Users/30373/Downloads/美术.zip`

## Imported

- 13 player frames: `AttackA` and `AttackB`
- 37 vine enemy frames: idle, walk, run, swing, bite, hit, and death
- 26 mushroom enemy frames: idle/spit, walk, attack, hit, and death
- 4 spore projectile frames
- 6 impact burst frames
- 82 underground ruin environment pieces

Total runtime PNG assets: 168.

## Runtime Mapping

- Player `AttackA`: normal attack; the neutral frames also provide the current idle visual.
- Player `AttackB`: skill cast.
- Vine and mushroom sequences: movement, attack, damage, and death state visuals.
- Spore sequence: player seed projectile and mushroom projectile.
- Burst sequence: projectile and melee impacts plus portal energy.
- Ruin pieces: level platforms, background walls, columns, props, portal frame, and graft pickups.

## Kept Outside The Build

- 86 duplicate tightly-cropped animation frames. The equal-canvas frames are used to prevent animation jitter.
- 17 original generation sheets, 17 frame overview JPGs, 16 preview GIFs, and the source slicing JSON.

These 137 source/reference files remain in the original ZIP and are not duplicated into the Unity build.

## Missing Source Art

- No beetle animation set was supplied. The beetle keeps a distinct program placeholder.
- No separate player idle, run, jump, fall, dash, hit, or death sheets were supplied. Movement currently uses the neutral player pose with procedural bob and tilt.
- No dedicated portal, graft icon, HUD, menu, or audio assets were supplied. Ruin and VFX pieces are reused where appropriate.
