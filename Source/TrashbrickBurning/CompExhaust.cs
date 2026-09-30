using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>
    /// The exhaust network: a second Vanilla Expanded Framework pipe network, like Dubs Bad
    /// Hygiene's sewage pipes, carrying burner exhaust to exhaust ports.
    /// </summary>
    public static class ExhaustNetwork
    {
        public const string NetDefName = "STB_ExhaustNet";

        public static PipeNet NetOf(ThingWithComps thing) => NetOf(thing, NetDefName);

        /// <summary>The thing's network of the given PipeNetDef - a building can sit on more than one.</summary>
        public static PipeNet NetOf(ThingWithComps thing, string netDefName)
        {
            List<ThingComp> comps = thing.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompResource res && res.PipeNet != null && res.Props.pipeNet?.defName == netDefName)
                {
                    return res.PipeNet;
                }
            }
            return null;
        }

        public static List<CompExhaustPort> Ports(PipeNet net)
        {
            List<CompExhaustPort> ports = new List<CompExhaustPort>();
            if (net == null)
            {
                return ports;
            }
            foreach (ThingWithComps thing in HeatNetwork.Members(net))
            {
                CompExhaustPort port = thing.GetComp<CompExhaustPort>();
                if (port != null && port.Open)
                {
                    ports.Add(port);
                }
            }
            return ports;
        }

        /// <summary>
        /// Toxic gas straight into the gas grid, overflowing: a cell holds at most 255, and without
        /// overflow everything past that is thrown away, so a steady stream into one cell never built
        /// up. With it, the excess floods out into the cells around, as a real leak would.
        /// </summary>
        public static void AddToxGas(IntVec3 cell, Map map, int amount) => AddGas(cell, map, GasType.ToxGas, amount);

        public static void AddGas(IntVec3 cell, Map map, GasType type, int amount)
        {
            if (amount > 0 && cell.InBounds(map))
            {
                map.gasGrid.AddGas(cell, type, amount, true);
            }
        }

        public static void ReleaseToxGas(Thing source, int amount) => ReleaseGas(source, GasType.ToxGas, amount);

        /// <summary>Gas into a cell next to the source that gas can occupy.</summary>
        public static void ReleaseGas(Thing source, GasType type, int amount)
        {
            Map map = source.Map;
            if (map == null || amount <= 0)
            {
                return;
            }
            List<IntVec3> cells = new List<IntVec3>();
            foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(source))
            {
                if (c.InBounds(map) && !c.Impassable(map))
                {
                    cells.Add(c);
                }
            }
            if (cells.Count == 0)
            {
                return;
            }
            AddGas(cells.RandomElement(), map, type, amount);
        }
    }

    public class CompProperties_Exhaust : CompProperties
    {
        /// <summary>Ground cells polluted per unit of fuel burnt (Biotech pollution).</summary>
        public float pollutionPerFuel = 0.05f;

        /// <summary>Toxic gas released per unit of fuel burnt, wherever the exhaust comes out.</summary>
        public float toxGasPerFuel = 100f;

        /// <summary>Heat the exhaust carries out of a port, per unit of fuel burnt: a mild warmth where it comes out.</summary>
        public float heatPerFuel = 20f;

        public CompProperties_Exhaust()
        {
            compClass = typeof(CompExhaust);
        }
    }

    /// <summary>
    /// Burning trash makes exhaust. Piped to an exhaust port, it comes out there - polluting the
    /// ground around the port, and gassing the room if the port is indoors. Not piped anywhere, it
    /// comes out of the burner itself: toxic gas in the room around it, and pollution on the ground.
    /// </summary>
    public class CompExhaust : ThingComp
    {
        private const int Interval = GenTicks.TickRareInterval;

        private float pollutionBuffer;
        private float gasBuffer;
        private float rotBuffer;
        private int ports;
        private bool ventingLocally;

        public CompProperties_Exhaust Props => (CompProperties_Exhaust)props;

        /// <summary>Burning with nowhere for the exhaust to go but its own room.</summary>
        public bool VentingLocally => ventingLocally;

        private float FuelPerDay => parent.GetComp<CompStirlingEngine>()?.FuelPerDay ?? 0f;

        public float PollutionPerDay => FuelPerDay * Props.pollutionPerFuel * TrashbrickBurningMod.S.pollutionMultiplier
            * (parent.GetComp<CompStirlingEngine>()?.mixPollution ?? 1f);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pollutionBuffer, "exhaustPollution", 0f);
            Scribe_Values.Look(ref gasBuffer, "exhaustGas", 0f);
            Scribe_Values.Look(ref rotBuffer, "exhaustRot", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Interval))
            {
                Step(Interval);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Step(Interval);
        }

        private void Step(int ticks)
        {
            CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
            List<CompExhaustPort> open = ExhaustNetwork.Ports(ExhaustNetwork.NetOf(parent));
            ports = open.Count;
            ventingLocally = false;
            if (engine == null || !engine.Burning || !parent.Spawned)
            {
                return;
            }
            float mult = TrashbrickBurningMod.S.pollutionMultiplier;
            float fuel = engine.FuelPerDay * ticks / GenDate.TicksPerDay;
            // Dirtier fuel mixes (wastepacks, loose trash) make more gas and less ground pollution.
            float pollution = fuel * Props.pollutionPerFuel * mult * engine.mixPollution;
            float gas = fuel * Props.toxGasPerFuel * mult * engine.mixToxGas;
            // Burnt corpses: rot stink, scaled off the same toxic gas figure.
            float rot = fuel * Props.toxGasPerFuel * mult * engine.mixRotStink;
            if (open.Count > 0)
            {
                float heat = fuel * Props.heatPerFuel;
                foreach (CompExhaustPort port in open)
                {
                    port.Receive(pollution / open.Count, gas / open.Count, heat / open.Count, rot / open.Count);
                }
                return;
            }
            ventingLocally = true;
            pollutionBuffer += pollution;
            gasBuffer += gas;
            rotBuffer += rot;
            Emit(parent, null, ref pollutionBuffer, ref gasBuffer, ref rotBuffer, true);
        }

        /// <summary>
        /// Lets out whole units. With an outlet cell (a wall-mounted port's own cell), everything
        /// comes out there; otherwise into the cells around the thing. Ground pollution, toxic gas and rot stink
        /// wherever it is - outdoors the gas drifts off on its own, in a room it builds up.
        /// </summary>
        public static void Emit(Thing at, IntVec3? outlet, ref float pollution, ref float gas, ref float rot, bool alwaysGas = true)
        {
            Map map = at.Map;
            if (map == null)
            {
                return;
            }
            IntVec3 cell = outlet ?? at.Position;
            if (!cell.InBounds(map))
            {
                return;
            }
            if (pollution >= 1f && ModsConfig.BiotechActive)
            {
                int cells = (int)pollution;
                pollution -= cells;
                PollutionUtility.GrowPollutionAt(cell, map, cells);
            }
            EmitGas(at, outlet, cell, map, GasType.ToxGas, ref gas, alwaysGas);
            EmitGas(at, outlet, cell, map, GasType.RotStink, ref rot, alwaysGas);
            if (Rand.Chance(0.7f))
            {
                Vector3 smoke = outlet.HasValue ? cell.ToVector3Shifted() : at.TrueCenter();
                FleckMaker.ThrowSmoke(smoke, map, Rand.Range(0.8f, 1.4f));
            }
        }

        private static void EmitGas(Thing at, IntVec3? outlet, IntVec3 cell, Map map, GasType type, ref float gas, bool alwaysGas)
        {
            if (gas < 1f)
            {
                return;
            }
            int amount = (int)gas;
            gas -= amount;
            Room room = outlet.HasValue ? cell.GetRoom(map) : at.GetRoom();
            bool indoors = room != null && !room.PsychologicallyOutdoors;
            if (!alwaysGas && !indoors)
            {
                return;
            }
            if (outlet.HasValue && !cell.Impassable(map))
            {
                ExhaustNetwork.AddGas(cell, map, type, amount);
            }
            else
            {
                ExhaustNetwork.ReleaseGas(at, type, amount);
            }
        }

        public override string CompInspectStringExtra()
        {
            CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
            if (engine == null || !engine.Burning)
            {
                return ports > 0 ? "STB_ExhaustPiped".Translate(ports).Resolve() : null;
            }
            if (ventingLocally)
            {
                return "STB_ExhaustLocal".Translate(PollutionPerDay.ToString("0.#")).Resolve();
            }
            return "STB_ExhaustToPorts".Translate(ports, PollutionPerDay.ToString("0.#")).Resolve();
        }
    }

    public class CompProperties_ExhaustPort : CompProperties
    {
        /// <summary>
        /// Wall-mounted: hung on a wall, it lets the exhaust out into its own cell, on the side of the
        /// wall it's hung on. Otherwise (the stack) into the cells around it.
        /// </summary>
        public bool outletAtSelf;

        public CompProperties_ExhaustPort()
        {
            compClass = typeof(CompExhaustPort);
        }
    }

    /// <summary>
    /// Where the exhaust network comes out: it pollutes the ground around the port and, if the
    /// port is indoors, gasses the room. Build it outside, somewhere you don't mind polluting.
    /// </summary>
    public class CompExhaustPort : ThingComp
    {
        private float pollutionBuffer;
        private float gasBuffer;
        private float rotBuffer;
        private float receivedToday;
        private int lastReceiveTick = -1;

        /// <summary>
        /// Its fan has to run to draw the exhaust: switched on and powered. With no open port, the
        /// exhaust backs up and comes out of the burners (and compactors) that make it.
        /// </summary>
        public bool Open
        {
            get
            {
                if (!parent.Spawned || !FlickUtility.WantsToBeOn(parent))
                {
                    return false;
                }
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        private CompProperties_ExhaustPort Props => (CompProperties_ExhaustPort)props;

        /// <summary>Where the exhaust comes out: the cell in front of a wall port, or null for around the stack.</summary>
        public IntVec3? Outlet => Props.outletAtSelf ? parent.Position : (IntVec3?)null;

        public bool Active => lastReceiveTick >= 0 && Find.TickManager.TicksGame - lastReceiveTick < GenTicks.TickRareInterval * 2;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pollutionBuffer, "exhaustPollution", 0f);
            Scribe_Values.Look(ref gasBuffer, "exhaustGas", 0f);
            Scribe_Values.Look(ref rotBuffer, "exhaustRot", 0f);
        }

        /// <summary>Exhaust from the burners: pollution, gas and rot stink out where it comes out, and a little heat.</summary>
        public void Receive(float pollution, float gas, float heat, float rot = 0f)
        {
            pollutionBuffer += pollution;
            gasBuffer += gas;
            rotBuffer += rot;
            receivedToday = pollution * GenDate.TicksPerDay / GenTicks.TickRareInterval;
            lastReceiveTick = Find.TickManager.TicksGame;
            IntVec3 cell = Outlet ?? parent.Position;
            if (heat > 0f && cell.InBounds(parent.Map))
            {
                GenTemperature.PushHeat(cell, parent.Map, heat);
            }
            CompExhaust.Emit(parent, Outlet, ref pollutionBuffer, ref gasBuffer, ref rotBuffer);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Outlet.HasValue)
            {
                GenDraw.DrawFieldEdges(new List<IntVec3> { Outlet.Value }, PlaceWorker_SteamVent.PlumeColor);
            }
        }

        public override string CompInspectStringExtra()
        {
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn && FlickUtility.WantsToBeOn(parent))
            {
                return "STB_PortNoPower".Translate();
            }
            if (!Active)
            {
                return "STB_PortIdle".Translate();
            }
            Room room = Outlet.HasValue ? Outlet.Value.GetRoom(parent.Map) : parent.GetRoom();
            string s = "STB_PortActive".Translate(receivedToday.ToString("0.#"));
            if (room != null && !room.PsychologicallyOutdoors)
            {
                s += "\n" + "STB_PortIndoors".Translate();
            }
            return s;
        }
    }

    public class CompProperties_ExhaustSource : CompProperties
    {
        public CompProperties_ExhaustSource()
        {
            compClass = typeof(CompExhaustSource);
        }
    }

    /// <summary>
    /// On something else that makes toxic gas - Vanilla Recycling Expanded's garbage compactor. Piped
    /// to a powered exhaust port, the gas it would let out goes down the exhaust instead
    /// (Patch_CompactorExhaust); otherwise it comes out as usual.
    /// </summary>
    public class CompExhaustSource : ThingComp
    {
        /// <summary>Takes the gas if it can: true if it went down the exhaust.</summary>
        public bool TryRoute(int amount)
        {
            List<CompExhaustPort> open = ExhaustNetwork.Ports(ExhaustNetwork.NetOf(parent));
            if (open.Count == 0)
            {
                return false;
            }
            foreach (CompExhaustPort port in open)
            {
                port.Receive(0f, (float)amount / open.Count, 0f);
            }
            return true;
        }

        public override string CompInspectStringExtra()
        {
            int ports = ExhaustNetwork.Ports(ExhaustNetwork.NetOf(parent)).Count;
            if (ports > 0)
            {
                return "STB_SourceToPorts".Translate(ports);
            }
            return ExhaustNetwork.NetOf(parent) != null ? "STB_SourceNoPort".Translate().Resolve() : null;
        }
    }
}
