using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>Opens the burner's fuel bill. The label counts the fuels it allows.</summary>
    public class Command_FuelBill : Command_Action
    {
        public Command_FuelBill(CompStirlingEngine engine)
        {
            int allowed = engine.AllowedCount(out int total);
            defaultLabel = "STB_FuelBill".Translate(allowed, total);
            defaultDesc = "STB_FuelBillDesc".Translate();
            icon = DefDatabase<ThingDef>.GetNamedSilentFail(CompProperties_StirlingEngine.TrashBrickDefName)?.uiIcon
                   ?? TexCommand.ForbidOff;
            action = () => Find.WindowStack.Add(new Dialog_FuelBill(engine));
        }
    }

    /// <summary>
    /// A burner's fuel bill, laid out like a vanilla bill: the ingredient search radius, and the
    /// fuel filter tree with its search box and special filters (rotten corpses and so on). Down the
    /// right, what each fuel is worth and how dirty it burns.
    /// </summary>
    public class Dialog_FuelBill : Window
    {
        private readonly CompStirlingEngine engine;
        private readonly ThingFilterUI.UIState filterState = new ThingFilterUI.UIState();
        private Vector2 valuesScroll;

        public override Vector2 InitialSize => new Vector2(860f, 640f);

        public Dialog_FuelBill(CompStirlingEngine engine)
        {
            this.engine = engine;
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "STB_FuelBillTitle".Translate(engine.parent.LabelCap));
            Text.Font = GameFont.Small;

            Rect left = new Rect(0f, 40f, 360f, inRect.height - 40f - CloseButSize.y - 10f);
            Rect right = new Rect(left.xMax + 17f, 40f, inRect.width - left.xMax - 17f, left.height);

            // Search radius, as a bill's ingredient search radius: 999 is anywhere.
            Listing_Standard list = new Listing_Standard();
            list.Begin(new Rect(left.x, left.y, left.width, 90f));
            string radius = engine.searchRadius >= CompStirlingEngine.AnyDistance
                ? "Unlimited".TranslateSimple()
                : engine.searchRadius.ToString("F0");
            list.Label("STB_FuelBillRadius".Translate(radius));
            engine.searchRadius = list.Slider(engine.searchRadius, 3f, 100f);
            if (engine.searchRadius >= 100f)
            {
                engine.searchRadius = CompStirlingEngine.AnyDistance;
            }
            list.End();

            Rect buttons = new Rect(left.x, left.y + 62f, left.width, 28f);
            if (Widgets.ButtonText(buttons.LeftHalf().ContractedBy(2f, 0f), "STB_FuelAll".Translate()))
            {
                engine.AllowAll(true);
            }
            if (Widgets.ButtonText(buttons.RightHalf().ContractedBy(2f, 0f), "STB_FuelBricksOnly".Translate()))
            {
                engine.AllowAll(false);
            }

            Rect filterRect = new Rect(left.x, buttons.yMax + 6f, left.width, left.yMax - buttons.yMax - 6f);
            ThingFilter parentFilter = engine.parent.GetComp<CompRefuelable>()?.Props.fuelFilter;
            ThingFilterUI.DoThingFilterConfigWindow(filterRect, filterState, engine.fuelFilter, parentFilter, 4,
                null, null, true, true, false, null, engine.parent.Map);

            DoValues(right);
        }

        /// <summary>Each fuel's worth in trashbricks and its exhaust against a trashbrick's.</summary>
        private void DoValues(Rect rect)
        {
            CompProperties_StirlingEngine props = engine.Props;
            ThingFilter parentFilter = engine.parent.GetComp<CompRefuelable>()?.Props.fuelFilter;
            List<string> lines = new List<string>();
            bool corpses = false;
            if (parentFilter != null)
            {
                foreach (ThingDef def in parentFilter.AllowedThingDefs)
                {
                    if (def.IsCorpse)
                    {
                        corpses = true;
                        continue;
                    }
                    FuelValue fv = props.FuelOf(def);
                    lines.Add("STB_FuelLine".Translate(def.LabelCap, props.FuelValueOf(def).ToString("0.##"),
                        (fv?.toxGasFactor ?? 1f).ToString("0.##"), (fv?.pollutionFactor ?? 1f).ToString("0.##")));
                }
            }
            if (corpses && props.corpseFuel != null)
            {
                FuelValue fv = props.corpseFuel;
                float human = CompProperties_StirlingEngine.CorpseMass(ThingDefOf.Human.race.corpseDef) / props.CorpseKgPerBrick;
                lines.Add("STB_FuelLineCorpse".Translate(props.CorpseKgPerBrick.ToString("0.#"), human.ToString("0.#"),
                    fv.toxGasFactor.ToString("0.##"), fv.rotStinkFactor.ToString("0.##"), fv.pollutionFactor.ToString("0.##")));
            }
            string text = "STB_FuelValuesHeader".Translate() + "\n\n" + string.Join("\n", lines);
            float height = Text.CalcHeight(text, rect.width - 16f);
            Rect view = new Rect(0f, 0f, rect.width - 16f, height);
            Widgets.BeginScrollView(rect, ref valuesScroll, view);
            Widgets.Label(view, text);
            Widgets.EndScrollView();
        }
    }
}
