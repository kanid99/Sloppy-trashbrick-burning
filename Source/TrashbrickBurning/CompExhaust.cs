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

        /// <summary>Toxic gas into a cell next to the source that gas can occupy.</summary>
        public static void ReleaseToxGas(Thing source, int amount)
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
            GasUtility.AddGas(cells.RandomElement(), map, GasType.ToxGas, amount);
        }
    }

    public class CompProperties_Exhaust : CompProperties
    {
        /// <summary>Ground cells polluted per unit of fuel burnt (Biotech pollution).</summary>
        public float pollutionPerFuel = 0.05f;

        /// <summary>Toxic gas released per unit of fuel burnt, wherever the exhaust ends up in a room.</summary>
        public float toxGasPerFuel = 40f;

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
            if (open.Count > 0)
            {
                foreach (CompExhaustPort port in open)
                {
                    port.Receive(pollution / open.Count, gas / open.Count);
                }
                return;
            }
            ventingLocally = true;
            pollutionBuffer += pollution;
            gasBuffer += gas;
            Emit(parent, null, ref pollutionBuffer, ref gasBuffer, true);
        }

        /// <summary>Lets out whole units: ground pollution anywhere, toxic gas only under a roof or in a room.</summary>
        /// <summary>
        /// Lets out whole units. With an outlet cell (a wall-mounted port), everything comes out
        /// into that cell; otherwise around the thing itself. Ground pollution always; toxic gas
        /// only indoors, unless alwaysGas (a burner gassing its own surroundings).
        /// </summary>
        public static void Emit(Thing at, IntVec3? outlet, ref float pollution, ref float gas, bool alwaysGas)
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
            if (gas >= 1f)
            {
                int amount = (int)gas;
                gas -= amount;
                Room room = outlet.HasValue ? cell.GetRoom(map) : at.GetRoom();
                bool indoors = room != null && !room.PsychologicallyOutdoors;
                if (alwaysGas || indoors)
                {
                    if (outlet.HasValue && !cell.Impassable(map))
                    {
                        GasUtility.AddGas(cell, map, GasType.ToxGas, amount);
                    }
                    else
                    {
                        ExhaustNetwork.ReleaseToxGas(at, amount);
                    }
                }
            }
            if (Rand.Chance(0.7f))
            {
                Vector3 smoke = outlet.HasValue ? cell.ToVector3Shifted() : at.TrueCenter();
                FleckMaker.ThrowSmoke(smoke, map, Rand.Range(0.8f, 1.4f));
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
        /// <summary>Wall-mounted: built into a wall, it lets the exhaust out into the cell it faces.</summary>
        public bool outletInFront;

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
        private float receivedToday;
        private int lastReceiveTick = -1;

        public bool Open => parent.Spawned && FlickUtility.WantsToBeOn(parent);

        private CompProperties_ExhaustPort Props => (CompProperties_ExhaustPort)props;

        /// <summary>Where the exhaust comes out: the cell in front of a wall port, or null for around the stack.</summary>
        public IntVec3? Outlet => Props.outletInFront ? parent.Position + parent.Rotation.FacingCell : (IntVec3?)null;

        public bool Active => lastReceiveTick >= 0 && Find.TickManager.TicksGame - lastReceiveTick < GenTicks.TickRareInterval * 2;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pollutionBuffer, "exhaustPollution", 0f);
            Scribe_Values.Look(ref gasBuffer, "exhaustGas", 0f);
        }

        public void Receive(float pollution, float gas)
        {
            pollutionBuffer += pollution;
            gasBuffer += gas;
            receivedToday = pollution * GenDate.TicksPerDay / GenTicks.TickRareInterval;
            lastReceiveTick = Find.TickManager.TicksGame;
            CompExhaust.Emit(parent, Outlet, ref pollutionBuffer, ref gasBuffer, false);
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
}
