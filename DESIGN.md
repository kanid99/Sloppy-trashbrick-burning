# Rework design: Trash Gasification

**Status:** the baseline chain is built in 0.7.0, waiting on playtesting.

**Scope** (decided): get the baseline chain working well first: Cobbled / Trash Gasifier →
pipe → turbines, Overpressure Tank and radiators. The large and industrial tiers get rethought
after that. Until then they stay as they are.

Working notes for the rework. The baseline unit is defined first; everything else hangs off it.
**Decided** is from your notes. **Proposed** is my suggestion, waiting on you. **Open** needs an answer.

## Naming (decided: one family name, "Trash Gasification")

| Now | Proposed |
|---|---|
| Cobbled pellet stove | Cobbled Trash Gasifier |
| Trashbrick gasifier | Trash Gasifier (the baseline unit) |
| Large cobbled stove | Large Cobbled Trash Gasifier |
| Large trashbrick gasifier | Large Trash Gasifier |
| Industrial trashbrick gasifier | Industrial Trash Gasifier |
| Heat accumulator | Overpressure Tank |
| Cobbled / steam turbine | Cobbled Steam Turbine / Steam Turbine (unchanged) |

defNames stay as they are so existing saves keep their buildings; only labels change.

## Baseline: Trash Gasifier

Goal (decided): **eco** is the most fuel-efficient mode but makes much less heat. It's for
heating plus a little Stirling power, not for running turbines. **Normal** is the baseline:
1500W on 75 a day. **High** makes the most heat and is the least efficient.

Reconciled numbers (proposed, keeping 1500W on 75 a day as normal):

| | Eco | Normal | High |
|---|---|---|---|
| Fuel use | 30 / day (-60%) | **75 / day** | 112.5 / day (+50%) |
| Heat | 750W (-50%) | **1500W** | 2000W (+33%) |
| Heat per brick a day | **25W** (best) | 20W | 17.8W (worst) |
| Stirling power | 100W | 250W | 500W |
| Heat the Stirling uses (3x its output) | 300W | 750W | 1500W |
| Heat left over in Stirling mode | 450W | 750W | 500W |
| Water use with DBH (decided) | 20 / day | **40 / day** | 53 / day |

What changed from your notes: eco's fuel cut goes from -33% to -60%, so eco really is the
thrifty mode. The Stirling leftover becomes 450 / 750 / 500W, not 450 / 250 / 0W: that needed
high to be 1500W, which would have made normal 1000W. Every mode leaves heat over, which fits
"an Overpressure Tank is needed even in Stirling mode".

The numbers also line up with the turbine gears: one burner on eco (750W) runs low gear, one on
normal (1500W) runs medium gear, and two on normal (3000W) run high gear.

Effects (decided): smoke and steam effects, steam engine sound.

Decided: Stirling mode turns off when a turbine is on the network. An Overpressure Tank is still
needed in Stirling mode, to take the leftover heat.

## Cobbled Trash Gasifier (decided)

The early, cheap version of the baseline. It's worse at everything and riskier. Numbers are first
guesses, to be tuned in playtesting.

| | Cobbled Trash Gasifier | Trash Gasifier |
|---|---|---|
| Fuel, normal | 100 / day | 75 / day |
| Heat, normal | 1200W | 1500W |
| Heat per brick a day | 12W | 20W |
| Eco / Normal / High | same percentages: 600W on 40 / 1200W on 100 / 1600W on 150 | 750W on 30 / 1500W on 75 / 2000W on 112.5 |
| Stirling power | 75 / 150 / 300W (3x that in heat, as the baseline) | 100 / 250 / 500W |
| Room heat leak | about 25% of its heat | low, since it's insulated |
| Pressure safety | no safety valve: at 100% it bursts and breaks down | safety valve vents |
| Overpressure Tank risk | x4 while one is on the network | normal |
| Exhaust | heavy (0.06 cells, 60 gas per brick) | light (0.02 cells, 15 gas per brick) |
| Research | Electricity + VRE complex recycling | Microelectronics + VRE complex recycling |
| Cost | about 120 steel, 2 components | about 150 steel, 25 plasteel, 4 components |
| Look | current jerry-rigged art, loose parts rattle | clean industrial |

**All cobbled gear breaks down more often** (decided): the cobbled gasifier, cobbled turbine and
cobbled radiator. Proposed: twice vanilla's breakdown rate, via a per-def multiplier on
CompBreakdownable's mean time between breakdowns.

## Exhaust (decided)

Ash is gone. Burning trash makes exhaust: piped through exhaust pipe to an exhaust port, it
pollutes the ground around the port; not piped, it comes out of the burner as toxic gas in its
room. Like DBH's sewage pipes.

## Overpressure Tank (the heat accumulator, renamed)

One mode, no reserve or buffer setting (decided). In order of purpose:

1. **Relieve pressure.** It takes whatever heat the turbines don't use, so the burners don't
   build pressure.
2. **Heat radiators.** Our radiators, and DBH radiators when DBH is installed, draw from what it
   holds, which also draws its pressure down.
3. **Reserve steam.** When the burners fall short (one breaks down, runs out of fuel or is
   switched off), it feeds the turbines from what it holds.

Hazards (decided): the fuller it is, the higher the risk of a burst or explosion. Pawns have to
bleed it now and then (the current bleed job). Wear since the last bleed and a cobbled burner on
the network raise the risk (as now).

**Auto-release** (decided; threshold proposed): above a fill level you set (25/50/75/100%), it
vents steam on its own, so it stays safe without pawns. The vented heat goes into its room, which
gets hot fast indoors.

## Turbines

Decided: sound effects and a spinning animation. Three gears; a higher gear is more efficient
but needs more heat just to run:

| Gear | Minimum heat | Efficiency | Output at the minimum |
|---|---|---|---|
| Low | 500W | 60% | 300W |
| Medium | 1500W | 70% | 1050W |
| High | 3000W | 80% | 2400W |

So one Trash Gasifier on Normal (1500W) runs a turbine in medium gear, and high gear takes two.

**Maximum heat** (decided): a turbine takes up to the heat of 3 burners on high. The steam turbine
takes up to **6000W** (3 x 2000W) and the cobbled turbine up to **4800W** (3 x 1600W cobbled).
Heat over the maximum goes on to radiators and the Overpressure Tank.

**Below the gear's minimum** (decided): the turbine stalls and makes nothing, and the heat is
wasted - it takes it but doesn't turn. That makes the gear a real choice: high gear is best, but only if
you can keep 3000W flowing. Its readout says when it has stalled and which gear would run.

**Cobbled Steam Turbine** (decided): one step worse in every gear, with the same minimum heat:

| Gear | Minimum heat | Steam turbine | Cobbled turbine |
|---|---|---|---|
| Low | 500W | 60% | 50% |
| Medium | 1500W | 70% | 60% |
| High | 3000W | 80% | 70% |

It also breaks down more often, like all cobbled gear.

## DBH units to our watts (researched)

DBH's own electric boiler draws **250W** and supplies **250 heating units**, so **1 U = 1 W**.
That's what the mod uses now. For scale: a DBH hot water tank holds 100 U, a radiator 100 U,
a large radiator 300 U, and a log boiler 2000 U from 10 logs a day.

## The DBH radiator bug from your screenshots

Your DBH radiator's line "Connected demand/capacity: 0 / 0 U" is DBH's own view of that
plumbing: 0 U of tanks and radiators and 0 U of boilers. The radiator itself should count 300 U
there, so DBH isn't counting it: either its valve is closed, or it isn't on the same plumbing
network as the gasifier. The gasifier's readout didn't show a plumbing line either, which it
only showed when DBH counted boilers. Build 0.6.3 always shows what DBH sees from the burner:
*Not on DBH plumbing*, or the tank and boiler totals, with a warning when there are no working
tanks or radiators.
