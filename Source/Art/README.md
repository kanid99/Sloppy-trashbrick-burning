# Trashbrick Burning artwork

Every sprite is drawn from primitives by the scripts here. Nothing is generated and there are no
source PSDs, so the art rebuilds from a clean checkout with nothing but Pillow (and numpy for
`measure.py`).

```sh
pip install pillow numpy
python3 Source/Art/draw_sprites.py      # every texture, all four views
python3 Source/Art/verify_art.py        # checks the textures against the defs and the C#
python3 Source/Art/make_about_art.py    # About/Preview.png and About/ModIcon.png
python3 Source/Art/contact_sheet.py /tmp/sheet.png   # every view on one sheet, for review
python3 Source/Art/measure.py Textures/Things/Building/Power/STB_TrashbrickGasifier_south.png
```

All run from the repo root. The rules are the ones the SloppyMods mending and Riimba art
settled on; see their `Source/Art/README.md` for how each was arrived at.

## The rules

* **Drawn, not generated.** Generated machines come back rendered, with photoreal metal and a
  heavy outline of their own. RimWorld's and VFE's are abstract: plain blocks, flat tonal ramps,
  detail only as rows of identical marks.
* **Black is the silhouette and nothing else.** Every division inside a sprite is a thin seam a
  tone or two darker, or a plain tone step. Only solid pixels get the silhouette ring; cast
  shadows stay soft.
* **Height is a wall and a shadow, not an outline.** A raised part is a lit top face, a darker
  side wall under it, and a soft shadow down and right. `LIFT` (0.55) sets how far one cell of
  height rises up the screen. At 1.0, anything tall behind a port on the far edge hid it in the
  north view.
* **Rotations are views, not rotations.** Each machine is laid out once in its own coordinates
  (`a` across its front, `f` from front to back) and `View` places the layout for each facing.
  Pixels are never rotated, so the light stays at the top of the screen in every view. All four
  views are drawn, `_west` included, because these layouts aren't mirror-symmetric.
* **One accent, and it means something.** On the cobbled stove the only saturated colour is the
  fire. On the gasifier, teal marks what's powered: the chamber seal, the status panel and the
  pilot lamp. The intake green is VFE Factory's input colour.
* **Straight pipe runs only.** `Canvas.pipe` asserts it. Bends read as debris at play zoom.
  **Cables sag.** Pipes are rigid and cables aren't: `Canvas.cable` hangs between its two ends,
  and that sag is what tells a cable from a pipe at play zoom.
* **Round forms.** Each machine has at least one cylinder (flue, chamber, filters, exhaust), and
  the skids are chamfered, not square.
* **Supersampled** at 4x, reduced with LANCZOS.

## One input, no output

A generator takes fuel in and puts power out, so each machine has a single intake, centred on
the edge it faces, with a flat chevron pointing IN. There is no output port.

That's a contract with the C#. `CompHopperFeed.IntakeCells` reads fuel only from the cells just
outside that edge, and `PlaceWorker_ShowIntake` outlines them in the same green. `verify_art.py`
reimplements the cells, finds the green in each of the four textures, and checks it sits on the
facing edge, centred along it, with none of it on the far half. It allows for the port's own lift
(taken from `INTAKE_Z`, `INTAKE_H` and `LIFT`, not hard-coded). It also checks the C#'s outline
colour matches the art's green.

Map z runs up and screen y runs down, so **north's front is the TOP of the texture**. That sign
is what the mending mod once shipped backwards. Swapping the east and west textures makes
`verify_art.py` fail on both, which is how the check itself was tested.

## The steam turbine

Laid out like a real turbine set, front to back: steam chest and stop valve, the rotor casing in
three stages stepping WIDER from high to low pressure with a bolted flange ring at each step, the
exhaust hood under the widest stage, bearing pedestals, coupling, generator and exciter. The
casing and generator are `drum`s: cylinders lying down, shaded across their axis so they read as
round. The detail is all repeated marks: the casing's split-line bolts, the flange bolts, the
generator's ribs and the walkway grating. The hot-water orange is only where the pipe network comes
in, on the inlet line and the steam chest's ring, so it carries 1.6% saturated pixels against a
VFE mean of 12.5%. It's the quietest sprite in the set, which suits a machine that sits at the end
of a pipe.

## Jerry-rigged: the low-tech look

The cobbled stove and cobbled turbine share one vocabulary, so the low-tech tier reads as one
set. Nothing on them was made to go together:

* **Patchwork deck.** Mismatched scrap plates over a dark base, weld beads down one edge of each,
  bolts in their corners. They sort just below the deck's own height, so nothing standing on the
  deck is ever painted over. When they didn't, `verify_art.py` caught a plate covering half the
  stove's intake in its east view.
* **Mismatched stages.** Each casing stage (or firebox plate) is a different scrap: rust, grey,
  olive. On the turbine each one sits a little off the line of the last.
* **Straps and welds, not flanges.** Joints are a strap with a weld bead at each end, and seams are
  rows of weld beads. The high-tech turbine uses neat rows of bolts instead.
* **Timber.** Chocks shim the drums up: the one thing on them that isn't metal.
* **Bolted-on salvage.** A radiator, a spare tank, a junction box, a bypass pipe lifted over the
  top, pressure gauges stuck on wherever there was room, and a taped pipe joint.
* **Wear.** Bare-metal scrapes, a couple of tones lighter. This is tone, not outline, and it's what
  lifted the turbine's contrast into VFE's range.

* **Home-brew.** A car battery wired in, duct tape round the pipe joints, a galvanised bucket
  catching drips, cables sagging across the deck.

The only saturated colour is still the one that means something: the fire on the stove, and the
hot-water inlet on the turbine.

## Effects, placed in the art's coordinates

`CompMachineEffects` puts smoke, steam and the firebox glow at points given in the same machine
coordinates the art is drawn in (`a` across the front, `f` front to back, `z` up), and turns them
into world offsets for whichever way the building faces, including `LIFT` for height. So the flue
smoke comes out of the flue in all four views, with no per-rotation offsets to keep in step.

## Loose parts that rattle

A building's texture is baked into the map mesh and can't move. So on the two jerry-rigged
machines, the parts that should rattle are drawn into two extra textures per view,
`<name>_LooseA_<rot>` and `<name>_LooseB_<rot>`: the same canvas as the building, holding only
those parts.

* **Layer A:** the dangling cables, a pipe hanging off one clamp, a cover or patch held by one bolt.
* **Layer B:** the gauges on their stalks, a second cable, and loose nuts walking across the deck.

`CompLooseParts` draws both layers over the building every frame (the def needs `drawerType
MapMeshAndRealTime`). While the machine runs it shakes each one by about two pixels, as two sines at
unrelated rates per axis, so it's a rattle rather than a sway. The two layers run at different
rates, so they rattle out of step. The shake is driven by game ticks: it stops when paused, speeds
up with the game, and sits still when the machine is off.

`verify_art.py` checks every layer exists in all four views at the building's size, that the
building is drawn in real time, and that no layer covers the intake. Painting a block over the
stove's intake in one layer makes it fail. `compose.py` puts building and layers together the way
the game does, for the contact sheet and store art; given offsets, it renders a shaken frame.

## The hot water pipe atlas

`STB_HotWaterPipe_Atlas` is a `Graphic_Linked` atlas: 4x4 tiles of 128px, tile *i* for the link
bits N=1, E=2, S=4, W=8, at column `i % 4` and row `3 - i // 4` from the top (the UV origin is
bottom-left). That order was read off a working VE atlas (Vanilla Chemfuel Expanded's), not
remembered. `verify_art.py` checks that tile 5 is the vertical straight, tile 10 the horizontal one
and tile 0 an isolated stub, in both the pipe and its blueprint atlas. Flipping the atlas makes it
fail.

The pipe is styled after, not copied from, the Dubs pipes: lagged warm grey with steel straps
every quarter tile, a thin hot-orange line down its crown as the network's one accent, and a round
flange boss at every joint and dead end. The hidden pipe uses VEF's own hidden-conduit texture, as
VE's hidden pipes do.

## Sizes

| texture | size | why |
| --- | --- | --- |
| `STB_CobbledPelletStove_*`, `STB_TrashbrickGasifier_*` | 576x576 | 2x2 footprint drawn at `drawSize (3,3)`: half a cell of margin all round so raised parts aren't clipped, as VFE draws its machines a cell larger than they stand |
| `STB_SteamTurbine_*`, `STB_CobbledTurbine_*` | 576x768 / 768x576 | 2x3 at `drawSize (3,4)`, axes swapped for east and west |
| `*_LooseA_*`, `*_LooseB_*` | as their building | loose-part layers for the cobbled stove and turbine |
| `STB_FuelHopper_*` | 288x288 | 1x1 at `drawSize (1.5,1.5)` |
| `STB_HotWaterPipe_Atlas`, `_Blueprint_Atlas` | 512x512 | 4x4 linked atlas, 128px tiles |
| `STB_HotWaterValve` | 288x288 | 1x1 at `drawSize (1.5,1.5)` |
| `STB_HotWaterRadiator` | 288x288 | 1x1 at `drawSize (1.5,1.5)`: a row of identical cast columns |
| `STB_CobbledRadiator` | 288x288 | 1x1 at `drawSize (1.5,1.5)`: a serpentine of three salvaged pipes, straight runs joined by elbow blocks, on timber |
| `STB_HeatAccumulator` | 576x576 | 2x2 at `drawSize (3,3)`: a lagged tank with a bolted manway |
| `STB_FireGlow` | 128x128 | the firebox flicker's soft blob, drawn by CompMachineEffects with the MoteGlow shader |
| `STB_Ash/*`, `STB_AshcreteBlocks/*` | 128x128 | `Graphic_StackCount`, small to large |
| `STB_SludgePellets/*_a,_b,_c` | 128x128 | `Graphic_StackCount`, small to large |
| `About/Preview.png` | 640x360 | composited from the shipped textures |
| `About/ModIcon.png` | 256x256 | the cobbled stove alone, which still reads at 32px |

## Measured against VFE Factory

`measure.py` is the mending mod's, unchanged apart from where it finds VFE's textures. All nine
metrics fall inside VFE's observed range for every machine (south views):

| | cobbled stove | gasifier | cobbled turbine | steam turbine | VFE mean | VFE range |
| --- | --- | --- | --- | --- | --- | --- |
| contrast (std) | 0.137 | 0.153 | 0.118 | 0.126 | 0.151 | 0.11-0.23 |
| p99 highlight | 185 | 192 | 150 | 164 | 166 | 133-255 |
| median luminance | 92 | 99 | 90 | 89 | 93 | 48-116 |
| near-black | 3.6% | 2.6% | 2.0% | 2.0% | 6.6% | 0-19.7% |
| saturated pixels | 24.0% | 7.3% | 20.5% | 1.6% | 12.5% | 1.0-33.3% |
| hard-edge density | 8.0 | 6.9 | 6.6 | 6.1 | 6.7 | 3.3-9.7 |
| warm-hued | 23.3% | 0.0% | 20.4% | 1.6% | 8.8% | 0-33.3% |
| cool-hued | 0.0% | 6.6% | 0.0% | 0.0% | 2.9% | 0-24.3% |
| diagonal silhouette | 0.114 | 0.139 | 0.059 | 0.094 | 0.054 | 0-0.22 |

The low-tech pair runs warm (rust, olive and timber) and the high-tech pair runs cool or neutral,
which is the tier difference, and all four are inside VFE's range. The cobbled turbine first came
in at 0.097 contrast, below VFE's floor: tidy scrap is still tidy. The jerry-rigging pass (below)
is what brought it into range.

## Scripts

| script | does |
| --- | --- |
| `stb_draw.py` | primitives: `View` (per-rotation placement), `Canvas` (slab, cylinder, pipe, seams, glow, painter's order), supersampling and the silhouette |
| `draw_sprites.py` | the jerry-rig helpers and the layouts: cobbled stove, gasifier, cobbled and steam turbines, fuel hopper, hot water pipe atlas and valve, sludge pellets |
| `verify_art.py` | textures vs defs (files, sizes, blueprints, icons, atlas tile order, loose layers) and vs the C# (intake edge, outline colour) |
| `measure.py` | nine style metrics against VFE Factory's machine sprites |
| `make_about_art.py` | the store preview and mod icon |
| `contact_sheet.py` | every building view on one sheet, loose parts included |
| `compose.py` | a building plus its loose-part layers, optionally shaken |
