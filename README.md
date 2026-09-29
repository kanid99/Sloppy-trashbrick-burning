# SloppyMod Trash POWER!
A RimWorld mod (1.5 / 1.6) for burning trash bricks, and maybe more, for power.

## Requirements
- **Required:** Vanilla Expanded Framework (VRE already needs it; its PipeSystem runs the hot water network) and Harmony (VEF needs it too)
- **Required:** [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) (and what it needs: Vanilla Expanded Framework, Biotech)
- **Optional:** Dubs Bad Hygiene (in full or Lite mode). If it's loaded, sludge pellets and the plumbing modes switch on.
- **Optional:** Vanilla Furniture Expanded - Factory. If it's loaded, conveyor compatibility switches on (see below).

## What it adds
### Play modes (Mod settings)
**Advanced** (default): gasifiers are **burners** that feed **steam turbines** through pressurised hot water pipes.
**Simple**: gasifiers are self-contained generators, and the turbines and pipes aren't in the build menu.
Modes and power switch over straight away; the build menu updates after a restart.

### Trash gasifiers (Power tab)
The whole system is **trash gasification**. The **cobbled trash gasifier** is the early, jerry-rigged one. The **trash gasifier** is the baseline unit: cleaner, more efficient and safer.

| | Cobbled trash gasifier | Trash gasifier |
|---|---|---|
| Size | 2x2 | 2x2 |
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Cost | 120 steel, 2 components | 150 steel, 25 plasteel, 4 components |
| Pressure safety | **no safety valve**: at full pressure it bursts | safety valve vents |
| Pollution | while burning | none |
| Breakdowns | twice as often | normal |

**Advanced mode:** each burner has a **mode** button: eco, normal or high. Eco gets the most heat from each brick but makes the least; high makes the most, wastes the most fuel and heats the room most.

| Mode | Cobbled: heat on fuel/day | Trash gasifier: heat on fuel/day | Stirling power (cobbled / gasifier) | Water with DBH (cobbled / gasifier) |
|---|---|---|---|---|
| Eco | 600W on 40 (15W per brick) | 750W on 30 (**25W per brick**) | 75W / 100W | 16 / 20 a day |
| Normal | 1200W on 100 (12W) | **1500W on 75** (20W) | 150W / 250W | 32 / 40 a day |
| High | 1600W on 150 (10.7W) | 2000W on 112.5 (17.8W) | 300W / 500W | 43 / 53 a day |

- **Stirling engine:** with **no steam turbine on its network**, a burner's own Stirling engine makes a little power, using **three times its output in heat**. The rest of the heat still goes out on the pipe, so even in Stirling mode a burner needs an Overpressure Tank or radiators, or its pressure builds. As soon as a turbine is on the network, the Stirling engines stand down.
- **Sync burners** copies a burner's mode and hot water share to the other burners on its network, or on the map.

**Simple mode:** the cobbled gasifier makes **1000W** on 20 bricks a day; the trash gasifier makes **1200W** on 12.

### Large and industrial burners (advanced mode, Power tab)
Bigger burners with **no engine of their own**, so they need steam turbines. These will be rethought once the baseline chain has been playtested; for now they keep their numbers.

| | Large cobbled trash gasifier | Large trash gasifier | Industrial trash gasifier |
|---|---|---|---|
| Size | 3x4 | 3x4 | 3x6 |
| Eco / normal / high | 750 / 1000 / 1500W | 750 / 1000 / 1500W | 1500 / 2250 / 3000W |
| Fuel a day | 22 / 31 / 54 | 11 / 15 / 24 | 20 / 33 / 46 |
| Safety valve | no, it bursts | yes | yes |

### Steam turbines (advanced mode)
Two tiers, both 2x3, with **three gears**. A higher gear turns more of the heat into power but needs more heat to turn at all. **Below its gear's minimum a turbine stalls**: it still takes its share of the heat, wastes it, and makes nothing.

| Gear | Minimum heat | Steam turbine | Cobbled steam turbine |
|---|---|---|---|
| Low | 500W | 60% | 50% |
| Medium | 1500W | 70% | 60% |
| High | 3000W | 80% | 70% |

| | Cobbled steam turbine | Steam turbine |
|---|---|---|
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Most heat it takes | 4800W (3 cobbled on high) | 6000W (3 gasifiers on high) |
| Breakdowns | twice as often | normal |

One trash gasifier on normal (1500W) runs a turbine in medium gear. High gear takes two. Several turbines on one network fill one at a time. The turbine's readout says when it has stalled and which gear would turn; a **Steam turbine stalled** alert warns you too. Turbines spin a cooling fan and hum while they make power.

- **Pipes:** our own network on Vanilla Expanded Framework's PipeSystem, the same system VE's chemfuel pipes use. That gives the usual overlay, plus a visible pipe, a **hidden pipe** and a **valve**, all at the Electricity tier. The visible and hidden pipe share **one architect button** (a dropdown). They sit in the Power tab, or in VE's **pipe networks** tab when a mod adds it (Vanilla Chemfuel Expanded). A building joins the network where a pipe runs under it.

### The heat network (advanced mode)
A burner's heat goes, in order:
1. With Dubs Bad Hygiene, its **hot water share** to the plumbing (see below).
2. Its own **Stirling engine**, only if no turbine is on the network.
3. **Steam turbines**, one at a time, each up to its maximum.
4. **Radiators**: 1x1, heating their room to the target temperature you set through vanilla's own temperature system. The **cobbled radiator** (Electricity: 20 steel, 10 wood) takes up to 150W, leaks about three times as often and breaks down; the **cast-iron radiator** (Microelectronics: 45 steel) takes up to 250W. Each has a valve to shut it off.
5. **Overpressure Tanks**: 2x2, hold 36 kWh, charging and discharging at up to 3000W. A tank takes whatever the turbines and radiators don't. It gives it back to **radiators** the burners can't satisfy, and **tops a stalling turbine up** to its gear's minimum when the burners fall short, through a breakdown or a refuel. With Dubs it also feeds the plumbing's hot water tanks and radiators.
6. **Dubs Bad Hygiene hot water and heating**, from what's left.
7. Whatever's left **builds pressure**.

### Hazards (can be switched off)
- **Overpressure:** heat with nowhere to go builds pressure over about half a day at 500W. The **trash gasifier's safety valve** vents it harmlessly in puffs of steam. The **cobbled gasifier has no working safety valve**: at full pressure it **bursts**. A **1000°C steam cloud** fills everything within 5 tiles in line of sight: rooms are heated by the share of them the cloud fills, and anyone caught in it is scalded, worst at the centre. The gasifier then breaks down and needs repairing. A critical alert warns you from 70% pressure.
- **Overpressure Tank explosions:** a tank more than 20% full can let go, with a blast and a 1000°C steam cloud out to 3-7 tiles depending on how full it is. The risk rises with the square of the fill, with the days since it was last bled (up to x3), and four times over while a cobbled gasifier (no safety valve) is piped to its network. When full and freshly bled on a gasifier-only network it averages one explosion in 60 days. Its readout shows the risk a day, and an **Overpressure Tank at risk** alert fires above 2% a day. **Bleed** (right-click to choose 0/25/50/75%) has a colonist (basic work) open the blow-off valve: the heat goes into the room and the wear resets. Or turn on **Auto-release** and pick a level (25/50/75/100%): above it the tank vents on its own, up to 3000W, straight into its room - safe, but it gets hot fast indoors.
- **Steam leaks:** pipes, turbines, radiators and Overpressure Tanks spring a leak now and then while hot water flows. On average once every 20 days per network, whatever its size, and adjustable in settings. A leaking part is damaged and sprays scalding steam on the cells around it until a colonist repairs it. Parts badly damaged some other way (raids, fire) leak too.

### Ash
Burners fill an ash pan as they burn (cobbled 0.25 ash per brick, gasifier 0.1). Like the SloppyMods Riimba station's waste, whole ash items come out **automatically** behind the burner, the side opposite its intake. It never blocks: if the spot's taken, the ash goes on the nearest free cell, and a hopper or storage there catches it for haulers or a conveyor. A **Rake out ash** button empties the pan early.
- **Ashcrete blocks:** 30 ash makes 20 blocks at a stonecutter's table. A cheap, fireproof, stony building material, weaker and plainer than cut stone.

### Other fuels
Burners also take **wood** (a log is worth 0.4 of a trashbrick) and **chemfuel** (a unit is worth 2.5). A Harmony patch makes refuelling count each fuel at its own value. Switch it off in settings.

### Polish
- **Effects:** smoke from the cobbled flue and the gasifier's stack, a flickering firebox glow, steam wisps from turbines, the gasifier's safety valve visibly venting, and a spinning cooling fan on each turbine while it makes power.
- **Sound:** synthesised running loops: the cobbled machines rattle (`Source/Audio/make_rattle.py`), the trash gasifier chuffs like a steam engine and the steam turbine whines (`Source/Audio/make_steam.py`).
- **Alerts:** burner overpressure (critical), steam turbine getting no heat, steam turbine stalled, burner out of fuel, Overpressure Tank at risk.
- **Status:** burners show their mode, Stirling output and any heat with nowhere to go; turbines their gear, heat and whether they've stalled; tanks a charge bar, kWh stored, explosion risk and auto-release.

### Build shortcuts
Selecting any building from this mod shows build buttons for **every other one**: hopper, gasifiers, pipes, valve, turbines, Overpressure Tank and radiators, in that order, from fuel to power. You can lay out a whole chain without going back to the architect menu. They're the architect's own buttons, so each appears only once researched, and the heat network's stay hidden in simple mode.

### Settings
Play mode, hazards on/off, other fuels on/off, and sliders for fuel use, power output, ash and leak frequency.

### One intake, no output
Each machine has a single intake, marked with a green in-arrow on the edge it faces. The gasifier only takes fuel from a hopper on the cells in front of that intake. Those cells are outlined in green while you place or select it.

- **Hoppers:** any building with `isHopper` works on the intake: the vanilla hopper, the VFE Factory hopper, or this mod's fuel hopper. Each machine has a **Draw from hoppers** toggle.
- **Fuel hopper:** 1x1, 25 steel, holds 3 stacks. Its filter is locked to gasifier fuels at Important priority, so haulers keep it full. Point its spout at the machine.
- **With VFE Factory:** VFE's factory hopper is used and ours leaves the build menu; hoppers already built keep working. A belt pushes into the hopper and the gasifier pulls from it, so **belt -> hopper -> gasifier** buffers the fuel. Set the factory hopper's filter to allow trashbricks, since it starts empty. A belt pointed straight at a gasifier also refuels it, from any side; that's VFE's own behavior.

### With Dubs Bad Hygiene
**Sludge pellets:** a biofuel refinery recipe turns 75 fecal sludge into 100 sludge pellets, and every burner burns them. That's less energy than DBH's sludge-to-chemfuel recipe (35 chemfuel, about 7.8 generator-days), but pellets don't explode when damaged, catch fire less easily, and don't rot.

**Plumbing:** every burner connects to DBH plumbing.
- **Advanced mode:** a burner on the plumbing is also a DBH boiler. Each burner has a **Hot water** share (0–100% in 10% steps; left-click steps, right-click picks). That share of its heat is offered to the plumbing's hot-water tanks and radiators **first**, one boiler unit per watt. Whatever the plumbing doesn't actually draw goes on to the turbines, so a high share only costs power when the tanks and radiators really want the heat. Anything the network leaves over is offered to the plumbing too. What the plumbing draws of the share comes before the Stirling engine, so it can cut the Stirling's power.
- **Simple mode:** an engine mode gizmo adds **water-cooled** (120% power while water flows, drawing 50 or 40 water a day) and **heat recovery** (a 1600-unit boiler for hot water and heating, at 50% power for the cobbled gasifier or 60% for the trash gasifier).

**Overpressure Tank as a boiler:** on DBH plumbing, the Overpressure Tank is also a DBH boiler. It feeds the plumbing's hot water tanks and radiators from the heat it holds, offering only what they're short of, so a satisfied plumbing network doesn't drain it. 1 DBH heating unit = 1W, as DBH's own electric boiler.

**Water (advanced mode):** a burner **needs** water from DBH plumbing to burn, by its mode (see the table above). With no plumbing, or no water on it, it won't light: its readout says *NO WATER* and a **Burner has no water** alert fires. A mod setting turns the requirement off.

The large and industrial burners join DBH plumbing too, with boilers of 4800 and 9600 units.

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
Source/TrashbrickBurning/                mod settings, CompBuildShortcuts, CompStirlingEngine (burner, pressure, ash), HeatNetwork + turbine/radiator/accumulator, AccumulatorRisk (explosion, bleed job), Hazards (burst, leaks), Ash, CompMachineEffects, CompLooseParts, Alerts, HarmonyPatches (fuel values), CompHopperFeed, PlaceWorker_ShowIntake, CompPowerPlantStirling, CompBurnPollution
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
