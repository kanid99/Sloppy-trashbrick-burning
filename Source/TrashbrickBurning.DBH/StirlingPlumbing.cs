using DubsBadHygiene;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning.DBH
{
    // Only built into Mods/DubsBadHygiene/<version>/Assemblies, which LoadFolders.xml loads only
    // when Dubs Bad Hygiene is active, so the main assembly never references BadHygiene.dll. The
    // burner and its modes live in the main assembly's CompStirlingEngine; this does the plumbing.

    public class CompProperties_StirlingWater : CompProperties
    {
        /// <summary>Simple play mode: plumbing water drawn per day while water-cooled.</summary>
        public float waterPerDay = 50f;

        public CompProperties_StirlingWater()
        {
            compClass = typeof(CompStirlingWater);
        }
    }

    /// <summary>
    /// Simple play mode: while water-cooled, draws plumbing water and tells the engine whether it
    /// got any. Advanced: works out how much of the burner's leftover heat DBH's hot water actually
    /// draws - our boiler's capacity times the network's demand over all boiler capacity, capped at
    /// all of it - so the burner's own small engine can lose that share.
    /// </summary>
    public class CompStirlingWater : ThingComp
    {
        private const int Interval = GenTicks.TickRareInterval;

        private CompProperties_StirlingWater Props => (CompProperties_StirlingWater)props;

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Interval))
            {
                Update();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Update();
        }

        private void Update()
        {
            CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
            if (engine == null)
            {
                return;
            }
            PlumbingNet net = parent.GetComp<CompPipe>()?.pipeNet;
            if (CompStirlingEngine.Advanced)
            {
                engine.waterFlowing = false;
                float draw = 0f;
                CompStirlingBoiler boiler = parent.GetComp<CompStirlingBoiler>();
                if (net != null && boiler != null && boiler.WorkingNow && net.BoilerCapacitySum > 0f)
                {
                    draw = boiler.Capacity * Mathf.Clamp01(net.HeatStoreCapacitySum / net.BoilerCapacitySum);
                }
                engine.hotWaterDrawWatts = draw;
            }
            else
            {
                engine.hotWaterDrawWatts = 0f;
                bool flowing = false;
                if (engine.mode == StirlingMode.WaterCooled && engine.Burning && net != null)
                {
                    flowing = net.PullWater(Props.waterPerDay * Interval / GenDate.TicksPerDay, out _);
                }
                engine.waterFlowing = flowing;
            }
            engine.Apply();
        }
    }

    /// <summary>
    /// DBH's boiler, feeding the plumbing's hot water and central heating. Advanced play mode: it
    /// offers whatever heat the turbines left, as boiler units one to one with watts. Simple: only
    /// in heat recovery mode, at its def capacity. WorkingNow is replaced rather than extended: the
    /// base version checks the power comp's PowerOn, which means nothing on a generator.
    /// </summary>
    public class CompStirlingBoiler : CompBoiler
    {
        private CompStirlingEngine Engine => parent.GetComp<CompStirlingEngine>();

        public override bool WorkingNow
        {
            get
            {
                CompStirlingEngine engine = Engine;
                if (engine == null)
                {
                    return false;
                }
                return CompStirlingEngine.Advanced ? engine.Burning && engine.surplusWatts > 0.5f : engine.HeatRecoveryActive;
            }
        }

        public override float Capacity
        {
            get
            {
                CompStirlingEngine engine = Engine;
                if (engine != null && CompStirlingEngine.Advanced)
                {
                    return WorkingNow ? engine.surplusWatts : 0f;
                }
                return base.Capacity;
            }
        }

        // CompBoiler's own gizmos are the electric boiler's power-level stepper ("Power Mode 1/8",
        // reduce/increase power) and its thermostat override. On a burner they do nothing - its
        // heat is set by the burn rate - so they're not shown. ThingComp's base gizmos are empty.
        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        // CompBoiler's readout prints its def capacity against the power comp's rating, which on a
        // generator reads as "1600 U / -1000 W" whatever the burner is doing. This says what is
        // actually going to the plumbing, and how loaded the plumbing's heating is.
        public override string CompInspectStringExtra()
        {
            PlumbingNet net = parent.GetComp<CompPipe>()?.pipeNet;
            string line = WorkingNow
                ? "STB_BoilerOffering".Translate(Capacity.ToString("0")).ToString()
                : "STB_BoilerIdle".Translate().ToString();
            if (net != null && net.BoilerCapacitySum > 0f)
            {
                line += "\n" + "STB_BoilerNetwork".Translate(net.HeatStoreCapacitySum.ToString("0"),
                    net.BoilerCapacitySum.ToString("0"));
            }
            return line;
        }
    }
}
