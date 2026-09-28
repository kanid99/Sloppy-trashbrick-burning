# Rework design: Trash Gasification

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

| | Eco | Normal | High |
|---|---|---|---|
| Fuel use (decided: -33% / 0 / +50% on 75 a day) | 50 / day | **75 / day** | 112.5 / day |
| Heat (decided: -50% / 0 / +33% on 1500W) | 750W | **1500W** | 2000W |
| Heat per fuel a day | 15W | 20W | 17.8W |
| Stirling power (decided: 100 / 250 / 500W) | 100W | 250W | 500W |
| Heat the Stirling uses (decided: 3x its output) | 300W | 750W | 1500W |
| Heat left over in Stirling mode | 450W | 750W | 500W |
| Water use with DBH | scales with heat (decided); baseline **open** | | |

Effects (decided): smoke and steam effects, steam engine sound.

**Open 1: eco is the least efficient mode.** With these percentages, eco gets 15W of heat per
brick a day, normal 20W and high 17.8W. So "eco" burns the most fuel per watt, not the least.
Two fixes that keep normal at 75 a day and 1500W:
- **(a)** Eco: -50% fuel, -33% heat → 37.5 a day for 1000W (26.7W per brick). High stays
  +50% / +33%.
- **(b)** Keep the fuel change at -33% but make eco's heat -20% → 50 a day for 1200W (24W per brick).

**Open 2: the Stirling leftover numbers don't match the heat numbers.** Your notes say 450W,
250W and 0W left over. Those only work out if the heat per mode is **750 / 1000 / 1500W**
(750-300, 1000-750, 1500-1500). With 750 / 1500 / 2000W the leftover is 450 / 750 / 500W.
Which set is right? If it's 750 / 1000 / 1500W, the modes are -50% / -33% / 0 on 1500W, and
"normal" is really the middle mode, not the 1500W one.

Decided: Stirling mode turns off when a turbine is on the network. An Overpressure Tank is still
needed in Stirling mode, to take the leftover heat.

## Overpressure Tank (the heat accumulator, renamed)

- Decided: takes heat the turbines don't need. The more it holds, the higher the risk of a burst
  or explosion. Pawns have to release steam now and then. This is how the accumulator works now:
  the risk grows with fill and wear, and a pawn bleeds it with a job.
- Decided: an **auto-release** setting keeps it safe, but vents a lot of heat into the room if
  it's indoors. Proposed: it vents above a threshold you set (like the 25/50/75/100% reserve
  levels), pushing the vented heat into its room.
- Decided: our radiators, and DBH radiators when DBH is installed, can also draw pressure down.
- Open: keep the reserve and buffer modes, or does the tank simply take what the turbines don't
  use?

## Turbines

Decided: sound effects and a spinning animation. Three gears; a higher gear is more efficient
but needs more heat just to run:

| Gear | Minimum heat | Efficiency | Output at the minimum |
|---|---|---|---|
| Low | 500W | 60% | 300W |
| Medium | 1500W | 70% | 1050W |
| High | 3000W | 80% | 2400W |

So one Trash Gasifier on Normal (1500W) runs a turbine in medium gear, and high gear takes two.

**Open 3:** is there still a maximum heat per turbine, like today's 1500W? For example 3000W for
low and medium and 6000W for high, or no cap? And below the gear's minimum, does the turbine
stall at 0W (heat then goes on to the tank), or run at reduced efficiency?

**Open 4:** do the cobbled turbine's gears differ (lower efficiency, for example)?

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
