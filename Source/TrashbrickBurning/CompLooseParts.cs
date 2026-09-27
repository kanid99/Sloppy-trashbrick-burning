using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>One layer of loose parts: a Graphic_Multi the same size as the building, drawn over it.</summary>
    public class LoosePartLayer
    {
        public string texPath;

        /// <summary>How far the layer shakes while running, in cells. A couple of pixels at 192px a cell.</summary>
        public float amplitude = 0.012f;

        /// <summary>Radians per tick of the shake. Layers at different rates rattle out of step.</summary>
        public float frequency = 1.9f;

        public float phase;
    }

    public class CompProperties_LooseParts : CompProperties
    {
        public List<LoosePartLayer> layers = new List<LoosePartLayer>();

        public CompProperties_LooseParts()
        {
            compClass = typeof(CompLooseParts);
        }
    }

    /// <summary>
    /// Draws the jerry-rigged machines' loose parts - dangling cables, a pipe hanging off one clamp,
    /// a cover barely held on - as separate layers over the building, and shakes them while it runs.
    /// A building's own texture is baked into the map mesh and can't move, so the parts that rattle
    /// live in their own textures (see Source/Art/draw_sprites.py) and are drawn here every frame;
    /// the building needs drawerType MapMeshAndRealTime for PostDraw to be called. The shake is
    /// driven by game ticks, so it stops when the game is paused and speeds up with the game.
    /// </summary>
    public class CompLooseParts : ThingComp
    {
        private List<Graphic> graphics;

        private CompProperties_LooseParts Props => (CompProperties_LooseParts)props;

        private bool Running
        {
            get
            {
                CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
                if (engine != null)
                {
                    return engine.Burning;
                }
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOutput > 0f;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (graphics == null)
            {
                graphics = new List<Graphic>();
                foreach (LoosePartLayer layer in Props.layers)
                {
                    graphics.Add(GraphicDatabase.Get<Graphic_Multi>(layer.texPath, ShaderDatabase.Cutout,
                        parent.def.graphicData.drawSize, Color.white));
                }
            }
            bool running = Running;
            float t = Find.TickManager.TicksGame;
            for (int i = 0; i < graphics.Count; i++)
            {
                LoosePartLayer layer = Props.layers[i];
                Vector3 pos = parent.DrawPos;
                // Above the building, below pawns; each layer a hair above the last.
                pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + i * 0.001f;
                if (running)
                {
                    // Two sines at unrelated rates per axis: a rattle, not a sway.
                    pos.x += layer.amplitude * Mathf.Sin(t * layer.frequency + layer.phase)
                             * (0.6f + 0.4f * Mathf.Sin(t * 0.37f + layer.phase * 2f));
                    pos.z += layer.amplitude * Mathf.Sin(t * layer.frequency * 1.31f + layer.phase * 1.7f)
                             * (0.6f + 0.4f * Mathf.Sin(t * 0.29f + layer.phase));
                }
                graphics[i].Draw(pos, parent.Rotation, parent);
            }
        }
    }
}
