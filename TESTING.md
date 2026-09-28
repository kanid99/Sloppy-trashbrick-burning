# Playtest checklist

Nothing here has run in RimWorld yet. It all compiles against the real 1.5 and 1.6 game, VEF and DBH
assemblies, but this is the first contact with the game itself. Work top to bottom: each section
leans on the one before it.

## Install

1. Put the repo in `RimWorld/Mods/SloppyModTrashPower/` (clone it there, or download the branch
   `claude/trash-brick-fuel-power-f6lia3` as a zip). The repo root *is* the mod folder: `About/`,
   `Defs/`, `1.5/`, `1.6/` and the rest sit directly in it. The compiled DLLs are committed, so
   there's nothing to build.
2. Mod list, in this order: **Harmony**, Core, Biotech, **Vanilla Expanded Framework**,
   **Vanilla Recycling Expanded**, then optionally **Dubs Bad Hygiene** and **VFE - Factory**, then
   **SloppyMod Trash POWER!**.
3. Turn on **Development mode** (Options > General). You'll want the debug log and debug actions.

**First check:** on the main menu, open the debug log (the icon top right). Any red lines
mentioning `STB_`, `TrashbrickBurning`, `PipeSystem` or `STB/` are ours: please copy them to me as-is.
Yellow warnings are worth sending too.

## 1. Loading and settings

- [ ] Options > Mod settings > **SloppyMod Trash POWER!** opens: the play mode checkbox, hazards, other
  fuels, four sliders and a reset button.
- [ ] Start a new colony (or dev quickstart). No red errors in the log.
- [ ] Debug actions > **Research all** (or research Electricity, Microelectronics and VRE complex
  recycling). The **Power** tab has cobbled pellet stove, trashbrick gasifier, fuel hopper, both
  turbines, heat accumulator, pressurised hot water pipe and hidden pipe, and valve. The
  **Temperature** tab has cobbled radiator and hot water radiator.

## 2. A cobbled stove on its own (advanced mode, the default)

God mode on, then build a **cobbled pellet stove**. Give it trashbricks (debug spawn
`VRecyclingE_TrashBrick`) or wood.

- [ ] While placing it, a **green outline** shows the two cells in front of its intake (the side
  with the green arrow). Rotate it: the outline and the arrow move together in all four facings.
- [ ] Lit, its inspect panel reads roughly *Burning at 250W of heat, 8 fuel a day* and *Nothing
  piped to it: built-in engine up to 100W*. Its power readout shows **100W**.
- [ ] The **Burn rate** button cycles 250 → 350 → 500W; power goes 100 → 200 → 300W, and fuel use
  8 → 12 → 20 a day.
- [ ] **Smoke** puffs from the flue, the **fire window flickers**, the loose cables and gauges
  **rattle**, and you can hear the **rattle loop**. All of it stops when you switch the stove off,
  and freezes when you pause.
- [ ] The room warms up (more at 500W than at 250W).
- [ ] Refuel from a **fuel hopper** built on the intake cells: it pulls fuel as it burns. A hopper on
  any other side does nothing.
- [ ] With several burners, **Sync burners** copies the selected one's burn rate and hot water share to the rest on its network (or the map); a message says how many.
- [ ] Wood is worth less than trashbricks: 10 logs fill it less than 10 bricks do.
- [ ] After a while **ash** appears on the cell **behind** the stove. **Rake out ash** drops the rest
  now.
- [ ] A stonecutter's table has **Make ashcrete blocks** (30 ash → 20 blocks), and walls can be built
  from ashcrete.

## 3. Pipes and a turbine

Build a **cobbled steam turbine** and lay **pressurised hot water pipe** from under the stove to under
the turbine.

- [ ] The pipe overlay shows while placing pipe, and the pipes link up (straights, corners, tees).
- [ ] The stove now reads *To the hot water network: 250W* and its own power drops to **0W**.
- [ ] The turbine reads *Heat supplied: 250W of 1000W capacity, converted at 75%* and *Fed by 1
  burners*, and outputs about **188W** (250 × 0.75).
- [ ] Add a second stove at 500W on the same pipe: the turbine gets 750W → about **560W**.
- [ ] The cobbled turbine's loose parts rattle while it's producing.
- [ ] Switch the stoves off: **Steam turbine getting no heat** alert.
- [ ] The **hidden pipe** also links, and a **valve** switched off cuts the turbine off.
- [ ] Rotate the turbines through all four facings; the art should look like the same machine
  each way.

## 3b. Large and industrial burners

- [ ] The Power tab has **large cobbled stove** and **large trashbrick gasifier** (3x4) and **industrial trashbrick gasifier** (3x6). Place each in all four facings: the green outline covers the whole 3-cell front, and the art faces the same way.
- [ ] None of them has a power readout or joins the power grid. Unpiped, the inspect says *no engine on board, pipe it to a steam turbine* and pressure climbs.
- [ ] A large burner at 1500W on a steam turbine gives it its full 1500W. An industrial at 3000W fills two turbines.
- [ ] The large cobbled stove smokes, flickers and rattles like the small one; the gasifiers vent steam from their stacks.
- [ ] In simple mode none of the three is in the build menu.

## 4. Pressure and the steam burst (hazards on)

Leave a **cobbled stove at 500W** connected to a turbine, then switch the **turbine off**.

- [ ] The stove's inspect shows **Pressure** climbing (about 12% an hour of game time at 500W).
- [ ] From 70%: the red **Burner overpressure** alert, and the inspect says *no safety valve - will
  burst!*
- [ ] At 100%: a **Steam burst** letter, a big smoke cloud, pawns within 5 tiles in line of sight get
  burns, a small enclosed room jumps towards 1000°C (check with the temperature overlay), and the
  stove is **broken down** and needs a component.
- [ ] Repeat with a **gasifier**: it stops at 85%, **vents** puffs of steam from its stack, and never
  bursts.
- [ ] Turn **hazards off** in settings: the cobbled stove now vents like the gasifier.

## 5. Radiators and the accumulator

- [ ] A **radiator** on the network in an enclosed, cold room takes heat (*Heat taken: 150W of
  150W* for the cobbled one) and warms the room to its **target temperature**, then stops taking
  heat. It only gets what the turbines leave over.
- [ ] A **heat accumulator** in **buffer** mode (default) on a network whose turbine can take all the
  heat stays empty and says *Idle: the turbines are using all the heat*. With more heat than the
  turbines can take, it fills (orange bar, *Stored: x of 24 kWh (+W)*).
- [ ] Switch it to **reserve**: it charges straight away, taking heat before the turbine. Switch the
  stoves off: it discharges into the turbine, which keeps running until the tank is empty.
- [ ] In reserve mode a **Reserve: 100%** button steps 25/50/75/100%. At 50% the tank charges first up to half, then only from spare heat above that; with the stoves off it hands over everything above 50% first, then the reserve.
- [ ] Hazards on: the accumulator's readout shows **Explosion risk** (none below 20% full), days since it was last bled, and *Cobbled burner on the network: risk x4* with a cobbled stove piped in. The charge bar reddens as the risk climbs, and above 2% a day the **Heat accumulator at risk** alert fires.
- [ ] **Bleed to 25%**: a colonist walks over, works for a few seconds with steam puffs, the stored heat drops to 25%, the room warms, and the wear resets to x1. Right-click picks 0/25/50/75%.
- [ ] To see an explosion: dev-mode fill a tank next to a cobbled stove and wait, or set its explosion MTB low in the def. There's a blast, a steam cloud sized by how full it was, a letter, the tank left wrecked and empty.
- [ ] Selecting any of the mod's buildings shows **build buttons for all the others** (not itself),
  in chain order: hopper, stoves, pipes, valve, turbines, accumulator, radiators. Unresearched ones
  don't appear.
- [ ] With a radiator or accumulator soaking up the spare heat, pressure no longer climbs.

## 6. Steam leaks

Settings: slide **Steam leak frequency** to the maximum to see one sooner, or damage a pipe with a
debug action (take damage).

- [ ] A pipe below half its hit points while hot water flows **leaks**: a message, smoke puffs, and
  pawns walking past get small burns.
- [ ] A colonist repairs it and the leak stops.
- [ ] Nothing leaks while no burner is lit.

## 7. Simple play mode

Switch to simple mode in settings and **restart**.

- [ ] Turbines, pipes, valve, radiators and the accumulator are gone from the build menu.
- [ ] A cobbled stove makes **1000W** on 20 fuel a day; a gasifier **1200W** on 12.

## 8. With Dubs Bad Hygiene

- [ ] Sludge pellets: a biofuel refinery has **Make sludge pellets** (75 sludge → 100); both stoves
  and the fuel hopper take them.
- [ ] **Advanced:** a stove on DBH plumbing with a hot water tank shows a **Hot water: 0%** button
  (left-click steps 10%, right-click picks). No **Power Mode** stepper.
- [ ] At 0% with a turbine taking all its heat, the tank gets nothing. Raise it to 50%: the stove
  reads *Hot water share: 50% (125W offered first), drawn: xW*, the tank heats, and the turbine's
  heat drops by the amount drawn. Once the tank is hot and draws nothing, the turbine gets it all back.
- [ ] With no turbine, the built-in engine's power drops by what the hot water draws.
- [ ] **Simple:** the engine mode button cycles power / water-cooled / heat recovery. Water-cooled
  reads *(no water)* without plumbing water, and makes 120% with it.

- [ ] **Accumulator as a tank:** pipe a heat accumulator into DBH plumbing next to a shower. Its DBH readout shows a hot water level equal to its charge. Showers lower the charge; a burner's hot water share (or a DBH boiler) raises it. With no DBH hot water tank at all, showers still get hot water from it.

## 9. With VFE - Factory

- [ ] Our fuel hopper is gone from the build menu. A **factory hopper** on the intake feeds the stove.
- [ ] A conveyor into that factory hopper keeps the stove fed (belt → hopper → stove).

## What to send me

- Any red or yellow log lines, copied as-is. `Player.log` has everything: on Windows it's in
  `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`.
- Numbers that come out different from the ones above.
- Anything that looks wrong in a particular facing, with the facing named.
- How the balance *feels*: fuel use, burst timing, leak frequency and the rattle volume are all
  first guesses.
