# Sloppy-trashbrick-burning
A RimWorld mod (1.5 / 1.6) for burning trash bricks, and maybe more, for power.

## Requirements
- **Required:** Vanilla Expanded Framework (VRE already needs it; its PipeSystem runs the hot water network)
- **Required:** [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) (and what it needs: Vanilla Expanded Framework, Biotech)
- **Optional:** Dubs Bad Hygiene (in full or Lite mode). If it's loaded, sludge pellets and the plumbing modes switch on.
- **Optional:** Vanilla Furniture Expanded - Factory. If it's loaded, conveyor compatibility switches on (see below).

## What it adds
### Generators (Power tab)
Both are Stirling engines on a 2x2 footprint that burn trashbricks. The high-tech build's main advantage is efficiency, with a little more output.

| | Cobbled pellet stove | Trashbrick gasifier |
|---|---|---|
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Cost | 120 steel, 2 components | 150 steel, 25 plasteel, 4 components |
| Output | 1000W | 1200W |
| Fuel/day | 20 | 12 |
| **Power per brick** | **50 W-days** | **100 W-days** |
| Fuel capacity | 50 | 60 |
| Heat | 8/s, heats the room | 3/s, insulated |
| Pollution | ~3 cells/day, **only while burning** | none, catalytic filters |

One garbage compactor (25 bricks/day) runs one cobbled stove, or two gasifiers.

### One intake, no output
Each machine has a single intake, marked with a green in-arrow on the edge it faces. The stove only takes fuel from a hopper on the cells in front of that intake. Those cells are outlined in green while you place or select it.

- **Hoppers:** any building with `isHopper` works on the intake: the vanilla hopper, the VFE Factory hopper, or this mod's fuel hopper. Each machine has a **Draw from hoppers** toggle.
- **Fuel hopper:** 1x1, 25 steel, holds 3 stacks. Its filter is locked to stove fuels at Important priority, so haulers keep it full. Point its spout at the machine.
- **With VFE Factory:** VFE's factory hopper is used and ours leaves the build menu; hoppers already built keep working. A belt pushes into the hopper and the stove pulls from it, so **belt -> hopper -> stove** buffers the fuel. Set the factory hopper's filter to allow trashbricks, since it starts empty. A belt pointed straight at a stove also refuels it, from any side; that's VFE's own behavior.

### Steam turbine (pressurised hot water)
Each stove's **engine mode** gizmo has a **turbine feed** setting. In it the stove stops making its own power and sends its heat down a **pressurised hot water pipe** to a **trashbrick steam turbine**, which converts it more efficiently.

| | Own Stirling engine | Feeding a turbine |
|---|---|---|
| Cobbled pellet stove | 1000W | 1400W |
| Trashbrick gasifier | 1200W | 1700W |

- **Turbine:** 2x3, up to **5000W**, and several stoves can feed one. With more than one turbine on a network, the heat is shared by capacity. Needs Microelectronics and VRE complex recycling; costs 250 steel, 20 plasteel and 6 components.
- **Pipes:** our own pipe network built on Vanilla Expanded Framework's PipeSystem, the same system VE's chemfuel pipes use. That gives the usual overlay, plus a visible pipe, a **hidden pipe** (buried, VE style) and a **valve** to split or cut lines. A building joins the network where a pipe runs under it.
- **Status:** a stove in turbine feed with no turbine on its network says so, and the turbine shows how much heat it is getting against its capacity.

No Dubs mod is needed for any of this.

### With Dubs Bad Hygiene
**Sludge pellets:** a biofuel refinery recipe turns 75 fecal sludge into 100 sludge pellets, and both machines burn them. That's less energy than DBH's sludge-to-chemfuel recipe (35 chemfuel, about 7.8 generator-days), but pellets don't explode when damaged, catch fire less easily, and don't rot.

**Plumbing and engine modes:** both machines connect to DBH plumbing, and the engine mode gizmo gains two more settings:

| Mode | Power | Plumbing |
|---|---|---|
| Water-cooled | 120% while water flows | draws 50/day (cobbled) or 40/day (gasifier) |
| Heat recovery | 50% (cobbled) or 60% (gasifier) | works as a 1600-unit DBH boiler for hot water and central heating |

Works with DBH's Lite mode too: that's a setting inside DBH, not a separate mod.

## Layout
```
About/                                   metadata, preview, icon
LoadFolders.xml                          loads Mods/<mod> only when that mod is active
Defs/ThingDefs_Buildings/                both machines, the fuel hopper, the hot water network, pipes, valve and turbine
1.5/, 1.6/Assemblies/                    TrashbrickBurning.dll
Mods/DubsBadHygiene/Defs, Patches/       sludge pellets, plumbing and engine modes
Mods/DubsBadHygiene/1.5, 1.6/Assemblies/ TrashbrickBurning.DBH.dll (references BadHygiene.dll)
Mods/VFEFactory/Patches/                 factory-hopper tag, hides our hopper
Source/TrashbrickBurning/                CompHopperFeed, PlaceWorker_ShowIntake, CompStirlingEngine (modes), CompPowerPlantStirling, CompSteamTurbine, CompBurnPollution
Source/TrashbrickBurning.DBH/            CompStirlingWater, CompStirlingBoiler
Source/Art/                              draws, verifies and measures every texture
```

## Building
```sh
Source/build.sh   # needs mono's mcs, curl, unzip and git
```
This compiles against the Krafs.Rimworld.Ref reference assemblies from NuGet, VEF's `PipeSystem.dll`, and (for the DBH bridge) DBH's own `BadHygiene.dll`, the last two from their public repos. The game doesn't need to be installed.

## Art
Every sprite is drawn by `Source/Art/draw_sprites.py`, following the rules the SloppyMods mending and Riimba art settled on: four real views per machine, black only on the silhouette, height from walls and shadows, one meaningful accent colour, and straight pipe runs. `verify_art.py` checks the art against the C#. See [`Source/Art/README.md`](Source/Art/README.md).
