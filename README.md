# Sloppy-trashbrick-burning
A RimWorld mod (1.5 / 1.6) for burning trash bricks, and maybe more, for power.

## Requirements
- **Required:** Vanilla Expanded Framework (VRE already needs it; its PipeSystem runs the hot water network) and Harmony (VEF needs it too)
- **Required:** [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) (and what it needs: Vanilla Expanded Framework, Biotech)
- **Optional:** Dubs Bad Hygiene (in full or Lite mode). If it's loaded, sludge pellets and the plumbing modes switch on.
- **Optional:** Vanilla Furniture Expanded - Factory. If it's loaded, conveyor compatibility switches on (see below).

## What it adds
### Play modes (Mod settings)
**Advanced** (default): stoves are **burners** that feed **steam turbines** through pressurised hot water pipes.
**Simple**: stoves are self-contained generators, and the turbines and pipes aren't in the build menu.
Burn rates and power switch over straight away; the build menu updates after a restart.

### The two stoves (Power tab)
Both are 2x2 Stirling-engine stoves that burn trashbricks. The high-tech gasifier's advantage is fuel efficiency: it uses about half the fuel for the same heat, and it doesn't pollute.

| | Cobbled pellet stove | Trashbrick gasifier |
|---|---|---|
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Cost | 120 steel, 2 components | 150 steel, 25 plasteel, 4 components |
| Pollution | ~3 cells/day at simple rate, scales with fuel burnt, **only while burning** | none, catalytic filters |

**Simple mode:** the cobbled stove makes **1000W** on 20 bricks a day and heats the room; the gasifier makes **1200W** on 12 a day and is insulated.

**Advanced mode:** each stove is a burner with a **burn rate** gizmo. Hotter burns cost more fuel per watt and dump more waste heat into the room:

| Burn rate | Cobbled: fuel/day | Gasifier: fuel/day | Room heat (cobbled / gasifier) | Built-in engine, no turbine |
|---|---|---|---|---|
| 250W | 8 (32 per kW) | 4 (16 per kW) | 4 / 1.5 | 100W |
| 350W | 12 (34 per kW) | 6 (17 per kW) | 6 / 2.5 | 200W |
| 500W | 20 (40 per kW) | 9 (18 per kW) | 10 / 4 | 300W |

A burner's heat goes to **turbines first**. Once a burner is connected to a turbine, its own small engine shuts off.

### Steam turbines (advanced mode)
Two tiers, both 2x3. Each turns the heat piped to it into power, from as many burners as it takes. With more than one turbine on a network, the heat is shared by capacity.

| | Cobbled steam turbine | Steam turbine |
|---|---|---|
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Cost | 200 steel, 3 components | 250 steel, 20 plasteel, 6 components |
| Heat taken | up to 1000W | up to 1500W |
| Conversion | 75%, **750W max** | 100%, **1500W max** |
| Burners to fill it | 2 at 500W, or 4 at 250W | 3 at 500W, or 6 at 250W |

- **Pipes:** our own network on Vanilla Expanded Framework's PipeSystem, the same system VE's chemfuel pipes use. That gives the usual overlay, plus a visible pipe, a **hidden pipe** and a **valve**, all at the Electricity tier. A building joins the network where a pipe runs under it.
- **Status:** each burner shows its heat, fuel use and where its heat is going; each turbine shows the heat it's getting, its capacity and its conversion rate.

For scale: a full steam turbine takes 48–60 bricks a day from cobbled stoves, or 24–27 from gasifiers. One garbage compactor makes 25.

### The heat network (advanced mode)
A burner's heat goes, in order:
1. **Steam turbines**, up to their capacity. An accumulator tops them up if the burners fall short.
2. **Radiators**: 1x1, heating their room to the target temperature you set through vanilla's own temperature system. The **cobbled radiator** (Electricity: 20 steel, 10 wood) takes up to 150W and leaks about three times as often; the **cast-iron radiator** (Microelectronics: 45 steel) takes up to 250W.
3. **Heat accumulators**: 2x2, store 24 kWh, charging and discharging at up to 750W. A battery for steam that keeps turbines turning while burners are refuelled or switched off.
4. **Dubs Bad Hygiene hot water and heating**, if installed. With Dubs, each burner's hot water share is offered to the plumbing *before* step 1 (see below).
5. Whatever's left **builds pressure**.

With nothing piped to it, a burner runs its own small engine instead (100/200/300W), and never builds pressure.

### Hazards (can be switched off)
- **Overpressure:** heat with nowhere to go builds pressure over about half a day at 500W. The **gasifier's safety valve** vents it harmlessly in puffs of steam. The **cobbled stove has no working safety valve**: at full pressure it **bursts**. A **1000°C steam cloud** fills everything within 5 tiles in line of sight: rooms are heated by the share of them the cloud fills, and anyone caught in it is scalded, worst at the centre. The stove then breaks down and needs repairing. A critical alert warns you from 70% pressure.
- **Steam leaks:** pipes, turbines, radiators and accumulators spring a leak now and then while hot water flows. On average once every 20 days per network, whatever its size, and adjustable in settings. A leaking part is damaged and sprays scalding steam on the cells around it until a colonist repairs it. Parts badly damaged some other way (raids, fire) leak too.

### Ash
Burners fill an ash pan as they burn (cobbled 0.25 ash per brick, gasifier 0.1). Like the SloppyMods Riimba station's waste, whole ash items come out **automatically** behind the burner, the side opposite its intake. It never blocks: if the spot's taken, the ash goes on the nearest free cell, and a hopper or storage there catches it for haulers or a conveyor. A **Rake out ash** button empties the pan early.
- **Ashcrete blocks:** 30 ash makes 20 blocks at a stonecutter's table. A cheap, fireproof, stony building material, weaker and plainer than cut stone.

### Other fuels
Burners also take **wood** (a log is worth 0.4 of a trashbrick) and **chemfuel** (a unit is worth 2.5). A Harmony patch makes refuelling count each fuel at its own value. Switch it off in settings.

### Polish
- **Effects:** smoke from the cobbled flue, a flickering firebox glow, steam wisps from turbines and the gasifier's stack, and the gasifier's safety valve visibly venting.
- **Sound:** the jerry-rigged machines play a rattling running loop (`Sounds/STB/CobbledRattle.wav`, synthesised by `Source/Audio/make_rattle.py`).
- **Alerts:** burner overpressure (critical), steam turbine getting no heat, burner out of fuel.
- **Status:** turbines show how many burners feed them; accumulators show a charge bar and kWh stored.

### Settings
Play mode, hazards on/off, other fuels on/off, and sliders for fuel use, power output, ash and leak frequency.

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
**Sludge pellets:** a biofuel refinery recipe turns 75 fecal sludge into 100 sludge pellets, and both stoves burn them. That's less energy than DBH's sludge-to-chemfuel recipe (35 chemfuel, about 7.8 generator-days), but pellets don't explode when damaged, catch fire less easily, and don't rot.

**Plumbing:** both stoves connect to DBH plumbing.
- **Advanced mode:** a stove on the plumbing is also a DBH boiler. Each burner has a **Hot water** share (0–100% in 10% steps; left-click steps, right-click picks). That share of its heat is offered to the plumbing's hot-water tanks and radiators **first**, one boiler unit per watt. Whatever the plumbing doesn't actually draw goes on to the turbines, so a high share only costs power when the tanks and radiators really want the heat. Anything the network leaves over is offered to the plumbing too. With no turbine, what the plumbing draws comes off the built-in engine's power, down to nothing.
- **Simple mode:** an engine mode gizmo adds **water-cooled** (120% power while water flows, drawing 50 or 40 water a day) and **heat recovery** (a 1600-unit boiler for hot water and heating, at 50% power for the cobbled stove or 60% for the gasifier).

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
Source/TrashbrickBurning/                mod settings, CompStirlingEngine (burner, pressure, ash), HeatNetwork + turbine/radiator/accumulator, Hazards (burst, leaks), Ash, CompMachineEffects, CompLooseParts, Alerts, HarmonyPatches (fuel values), CompHopperFeed, PlaceWorker_ShowIntake, CompPowerPlantStirling, CompBurnPollution
Source/TrashbrickBurning.DBH/            CompStirlingWater, CompStirlingBoiler
Source/Art/                              draws, verifies and measures every texture
Source/Audio/make_rattle.py              synthesises the rattle loop
Sounds/STB/                              the rattle loop
```

## Building
```sh
Source/build.sh   # needs mono's mcs, curl, unzip and git
```
This compiles against the Krafs.Rimworld.Ref reference assemblies from NuGet, VEF's `PipeSystem.dll`, and (for the DBH bridge) DBH's own `BadHygiene.dll`, the last two from their public repos. The game doesn't need to be installed.

## Art
Every sprite is drawn by `Source/Art/draw_sprites.py`, following the rules the SloppyMods mending and Riimba art settled on: four real views per machine, black only on the silhouette, height from walls and shadows, one meaningful accent colour, and straight pipe runs. The low-tech machines share a home-brew, jerry-rigged look (scrap-plate decks, timber chocks, bolted-on salvage, a car battery, tape, a drip bucket), and their loose parts (dangling cables, a pipe on one clamp, gauges on wobbly stalks, loose nuts) **rattle while they run**, drawn and shaken by `CompLooseParts`. The high-tech ones are neat and bolted. `verify_art.py` checks the art against the C#. See [`Source/Art/README.md`](Source/Art/README.md).
