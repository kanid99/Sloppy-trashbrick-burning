# Sloppy-trashbrick-burning
A RimWorld mod (1.5 / 1.6) for burning trash bricks, and maybe more, for power.

## Requirements
- **Required:** [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) (and what it needs: Vanilla Expanded Framework, Biotech)
- **Optional:** Dubs Bad Hygiene (full or Lite). If it's loaded, the sludge pellet content switches on.
- **Optional:** Vanilla Furniture Expanded - Factory. If it's loaded, conveyor compatibility switches on (see below).

## What it adds
### Trashbrick pellet stove (Power tab)
- Research: Electricity + VRE *complex recycling* (the same research that unlocks the garbage compactor)
- Cost: 120 steel, 2 components, 2x2
- Makes **1000W**, burns **20 fuel/day**, holds 50
- Heats the room while it runs, like the wood-fired generator
- One garbage compactor makes 25 trashbricks a day, which is enough for about one stove

### Fuel hopper and auto-feeding
- Build any hopper against any side of the stove and the stove pulls fuel from it each rare tick until it reaches its target fuel level (the refuel slider).
- Works with **this mod's fuel hopper**, the **vanilla hopper**, and the **VFE Factory hopper**. Any building with `isHopper` counts.
- Fuel hopper: 1x1, 25 steel, holds 3 stacks, and its storage filter is locked to stove fuels at Important priority, so haulers keep it full.
- Each stove has a **Draw from hoppers** toggle.

With **VFE Factory**:
- **Belt -> hopper -> stove** (buffered): the belt pushes into the hopper, and the stove pulls from the hopper. Set the VFE factory hopper's storage filter to allow trashbricks, because its default filter is empty.
- **Belt -> stove** (no buffer): a belt pointed straight at the stove becomes a VFE refueling port. That's VFE's own behavior and needs nothing from this mod.
- Our fuel hopper is dropped from the architect menu in favor of VFE's factory hopper. Ones already built keep working and are tagged as factory hoppers so belts can pull from them.

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
Mods/DubsBadHygiene/Patches/             adds pellets to the stove and hopper filters
Mods/VFEFactory/Patches/                 factory-hopper tag, hides our hopper
Source/TrashbrickBurning/                CompHopperFeed (C#)
1.5/, 1.6/Assemblies/                    compiled DLLs
```

## Building the assembly
```sh
Source/build.sh   # needs mono's mcs, curl, unzip; fetches Krafs.Rimworld.Ref from NuGet
```

## Art
No custom textures yet. The stove uses the vanilla wood-fired generator sprite tinted grey, and the pellets use the vanilla kibble sprite tinted brown, and the fuel hopper uses the vanilla hopper sprite tinted grey.
