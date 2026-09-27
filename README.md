# Sloppy-trashbrick-burning
A RimWorld mod (1.5 / 1.6) for burning trash bricks, and maybe more, for power.

## Requirements
- **Required:** [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) (and what it needs: Vanilla Expanded Framework, Biotech)
- **Optional:** Dubs Bad Hygiene (full or Lite). If it's loaded, the sludge pellet content switches on.

## What it adds
### Trashbrick pellet stove (Power tab)
- Research: Electricity + VRE *complex recycling* (the same research that unlocks the garbage compactor)
- Cost: 120 steel, 2 components, 2x2
- Makes **1000W**, burns **20 fuel/day**, holds 50
- Heats the room while it runs, like the wood-fired generator
- One garbage compactor makes 25 trashbricks a day, which is enough for about one stove

### Sludge pellets (only with Dubs Bad Hygiene)
- Recipe at the biofuel refinery: **75 fecal sludge -> 100 sludge pellets**
- The pellet stove also burns pellets
- Trade-off against DBH's own sludge-to-chemfuel recipe:

| Route | 75 sludge gives | Generator-days at 1000W | Storage risk |
|---|---|---|---|
| Chemfuel (DBH) | 35 chemfuel | ~7.8 | Explodes when damaged, very flammable |
| Sludge pellets | 100 pellets | 5 | Doesn't explode, low flammability, doesn't rot |

## Layout
```
About/About.xml                          mod metadata, VRE dependency
LoadFolders.xml                          loads Mods/DubsBadHygiene only if DBH is active
Defs/ThingDefs_Buildings/                the pellet stove
Mods/DubsBadHygiene/Defs/                sludge pellet item + recipe
Mods/DubsBadHygiene/Patches/             adds pellets to the stove's fuel filter
```

## Art
No custom textures yet. The stove uses the vanilla wood-fired generator sprite tinted grey, and the pellets use the vanilla kibble sprite tinted brown.
