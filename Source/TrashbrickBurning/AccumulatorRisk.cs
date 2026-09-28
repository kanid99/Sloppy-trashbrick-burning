using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TrashbrickBurning
{
    /// <summary>
    /// A heat accumulator letting go: a blast and a 1000C steam cloud, both sized by how full it
    /// was, and the tank itself wrecked. See CompHeatAccumulator.ExplosionChancePerDay for the odds.
    /// </summary>
    public static class AccumulatorExplosion
    {
        public static void Explode(CompHeatAccumulator acc)
        {
            ThingWithComps tank = acc.parent;
            Map map = tank.Map;
            if (map == null)
            {
                return;
            }
            IntVec3 center = tank.Position;
            float fill = acc.Fraction;
            float radius = acc.BlastRadius;
            acc.Emptied();

            SteamBurst.Cloud(center, map, radius, tank);
            GenExplosion.DoExplosion(center, map, radius * 0.6f, DamageDefOf.Bomb, tank, Mathf.RoundToInt(Mathf.Lerp(20f, 60f, fill)));
            if (!tank.Destroyed)
            {
                tank.TakeDamage(new DamageInfo(DamageDefOf.Blunt, tank.MaxHitPoints * 0.6f, 0f, -1f, tank));
            }
            Find.LetterStack.ReceiveLetter("STB_AccExplodedLabel".Translate(),
                "STB_AccExplodedText".Translate(fill.ToStringPercent(), radius.ToString("0")),
                LetterDefOf.ThreatSmall, new TargetInfo(center, map));
        }
    }

    /// <summary>
    /// Asks a colonist to open the blow-off valve. Left-click requests (or cancels) it; right-click
    /// picks how far down to bleed.
    /// </summary>
    public class Command_BleedAccumulator : Command_Action
    {
        private static readonly float[] Targets = { 0f, 0.25f, 0.5f, 0.75f };

        private readonly CompHeatAccumulator acc;

        public Command_BleedAccumulator(CompHeatAccumulator acc)
        {
            this.acc = acc;
            defaultLabel = acc.bleedRequested
                ? "STB_AccBleedCancel".Translate()
                : "STB_AccBleed".Translate(acc.bleedTo.ToStringPercent());
            defaultDesc = "STB_AccBleedDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", false) ?? TexCommand.ForbidOff;
            action = () => acc.bleedRequested = !acc.bleedRequested;
            if (!acc.bleedRequested && acc.Fraction <= acc.bleedTo + 0.01f)
            {
                Disable("STB_AccBleedNothing".Translate(acc.bleedTo.ToStringPercent()));
            }
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (float t in Targets)
                {
                    float to = t;
                    yield return new FloatMenuOption("STB_AccBleedTo".Translate(to.ToStringPercent()), () =>
                    {
                        acc.bleedTo = to;
                        acc.bleedRequested = acc.Fraction > to + 0.01f;
                    });
                }
            }
        }
    }

    public class WorkGiver_BleedAccumulator : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Building> buildings = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                CompHeatAccumulator acc = buildings[i].GetComp<CompHeatAccumulator>();
                if (acc != null && acc.NeedsBleed)
                {
                    yield return buildings[i];
                }
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            foreach (Thing _ in PotentialWorkThingsGlobal(pawn))
            {
                return false;
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompHeatAccumulator acc = t.TryGetComp<CompHeatAccumulator>();
            return acc != null && acc.NeedsBleed && !t.IsForbidden(pawn) && !t.IsBurning()
                && pawn.CanReserve(t, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("STB_BleedAccumulator"), t);
        }
    }

    public class JobDriver_BleedAccumulator : JobDriver
    {
        private const int WorkTicks = 300;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !(job.targetA.Thing.TryGetComp<CompHeatAccumulator>()?.NeedsBleed ?? false));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = Toils_General.Wait(WorkTicks, TargetIndex.A);
            work.WithProgressBarToilDelay(TargetIndex.A);
            work.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(40))
                {
                    FleckMaker.ThrowSmoke(job.targetA.Thing.TrueCenter(), pawn.Map, Rand.Range(0.6f, 1.1f));
                }
            };
            yield return work;
            yield return Toils_General.Do(() => job.targetA.Thing.TryGetComp<CompHeatAccumulator>()?.Bleed());
        }
    }

    /// <summary>A heat accumulator at real risk of letting go.</summary>
    public class Alert_STBAccumulatorRisk : Alert
    {
        public const float WarnAt = 0.02f;

        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_STBAccumulatorRisk()
        {
            defaultLabel = "STB_AlertAccRisk".Translate();
            defaultExplanation = "STB_AlertAccRiskDesc".Translate();
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            if (!TrashbrickBurningMod.Advanced || !TrashbrickBurningMod.Hazards)
            {
                return false;
            }
            foreach (ThingWithComps b in AlertUtility.PlayerBuildingsWith<CompHeatAccumulator>())
            {
                CompHeatAccumulator acc = b.GetComp<CompHeatAccumulator>();
                if (acc.ExplosionChancePerDay >= WarnAt && !acc.bleedRequested)
                {
                    culprits.Add(b);
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }
}
