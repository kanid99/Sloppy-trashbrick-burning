using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TrashbrickBurning
{
    /// <summary>A point on the machine, in the art's own coordinates: a across its front, f from front to back, z up.</summary>
    public class MachinePoint
    {
        public float a;
        public float f;
        public float z;
    }

    public class CompProperties_MachineEffects : CompProperties
    {
        /// <summary>Flue: smoke puffs while running.</summary>
        public MachinePoint smoke;

        /// <summary>Steam wisps while running, and where a safety valve vents.</summary>
        public MachinePoint steam;

        /// <summary>A flickering glow, drawn over the fire window.</summary>
        public MachinePoint glow;
        public float glowSize = 1.2f;
        public string glowTexPath = "Things/Building/Power/STB_FireGlow";

        /// <summary>Looped while running.</summary>
        public SoundDef sound;

        /// <summary>A part that spins while running - a turbine's cooling fan - drawn over the sprite.</summary>
        public MachinePoint rotor;
        public float rotorSize = 0.6f;
        public string rotorTexPath = "Things/Building/Power/STB_TurbineRotor";

        /// <summary>Degrees a tick at full speed.</summary>
        public float rotorSpeed = 24f;

        public CompProperties_MachineEffects()
        {
            compClass = typeof(CompMachineEffects);
        }
    }

    /// <summary>
    /// Smoke from the flue, steam wisps, a flickering firebox glow and a running sound. Positions
    /// are given in the same machine coordinates the art is drawn in (Source/Art/stb_draw.py's
    /// View), and turned into world offsets here for whichever way the building faces. Everything
    /// is driven by game ticks, so it all stops when the game is paused.
    /// </summary>
    public class CompMachineEffects : ThingComp
    {
        /// <summary>Must match stb_draw.LIFT: how far up the screen one cell of height is drawn.</summary>
        private const float Lift = 0.55f;

        private const int GlowLevels = 8;

        private Sustainer sustainer;
        private static readonly Dictionary<string, Material[]> GlowMats = new Dictionary<string, Material[]>();

        private CompProperties_MachineEffects Props => (CompProperties_MachineEffects)props;

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

        /// <summary>
        /// Machine coordinates to a world position. The front (f = 0) is the side the building faces;
        /// a runs left to right across that front as you look at it. Height draws up the screen.
        /// </summary>
        public Vector3 WorldPos(MachinePoint p)
        {
            IntVec2 size = parent.def.size;
            IntVec3 facing = parent.Rotation.FacingCell;
            Vector3 forward = new Vector3(facing.x, 0f, facing.z);
            Vector3 right = new Vector3(-facing.z, 0f, facing.x);
            Vector3 pos = parent.TrueCenter() + forward * (size.z / 2f - p.f) + right * (p.a - size.x / 2f);
            pos.z += p.z * Lift;
            return pos;
        }

        public override void CompTick()
        {
            base.CompTick();
            bool running = Running && parent.Spawned;
            UpdateSound(running);
            if (!running)
            {
                return;
            }
            Map map = parent.Map;
            if (Props.smoke != null && parent.IsHashIntervalTick(40) && Rand.Chance(0.75f))
            {
                FleckMaker.ThrowSmoke(WorldPos(Props.smoke), map, Rand.Range(0.5f, 0.9f));
            }
            if (Props.steam != null)
            {
                CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
                if (engine != null && engine.Venting)
                {
                    if (parent.IsHashIntervalTick(15))
                    {
                        SteamBurst.Vent(parent, WorldPos(Props.steam));
                    }
                }
                else if (parent.IsHashIntervalTick(70) && Rand.Chance(0.6f))
                {
                    FleckMaker.ThrowSmoke(WorldPos(Props.steam), map, Rand.Range(0.3f, 0.55f));
                }
            }
        }

        // A PerTick sustainer ends by itself once nothing maintains it, so a despawned or destroyed
        // machine goes quiet without a PostDeSpawn override - whose signature differs between 1.5 and 1.6.
        private void UpdateSound(bool running)
        {
            if (Props.sound == null)
            {
                return;
            }
            if (running)
            {
                if (sustainer == null || sustainer.Ended)
                {
                    sustainer = Props.sound.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
                }
                sustainer?.Maintain();
            }
            else if (sustainer != null)
            {
                sustainer.End();
                sustainer = null;
            }
        }

        private float rotorAngle;
        private float rotorSpeed;
        private int lastRotorTick = -1;

        /// <summary>
        /// The fan spins up while running and coasts down when it stops. Driven by game ticks, so it
        /// freezes when paused; drawn every frame, so it needs drawerType MapMeshAndRealTime.
        /// </summary>
        private void DrawRotor()
        {
            if (Props.rotor == null)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame;
            if (lastRotorTick >= 0 && tick > lastRotorTick)
            {
                int dt = System.Math.Min(tick - lastRotorTick, 60);
                float target = Running ? 1f : 0f;
                rotorSpeed = UnityEngine.Mathf.MoveTowards(rotorSpeed, target, dt / 90f);
                rotorAngle = (rotorAngle + rotorSpeed * Props.rotorSpeed * dt) % 360f;
            }
            lastRotorTick = tick;
            Material mat = MaterialPool.MatFrom(Props.rotorTexPath, ShaderDatabase.Cutout);
            Vector3 pos = WorldPos(Props.rotor);
            pos.y = parent.def.Altitude + 0.06f;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.AngleAxis(rotorAngle, Vector3.up),
                new Vector3(Props.rotorSize, 1f, Props.rotorSize));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            DrawRotor();
            if (Props.glow == null || !Running)
            {
                return;
            }
            Material[] mats = GlowMaterials(Props.glowTexPath);
            if (mats == null)
            {
                return;
            }
            // Two slow waves and a fast one: a fire's flicker, not a strobe. Off game ticks, so it
            // freezes with the game.
            float t = Find.TickManager.TicksGame + parent.thingIDNumber * 17;
            float f = 0.55f + 0.2f * Mathf.Sin(t * 0.11f) + 0.15f * Mathf.Sin(t * 0.047f + 1.3f) + 0.1f * Mathf.Sin(t * 0.61f);
            int level = Mathf.Clamp(Mathf.RoundToInt(f * (GlowLevels - 1)), 0, GlowLevels - 1);
            Vector3 pos = WorldPos(Props.glow);
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(Props.glowSize, 1f, Props.glowSize));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mats[level], 0);
        }

        private static Material[] GlowMaterials(string path)
        {
            if (GlowMats.TryGetValue(path, out Material[] mats))
            {
                return mats;
            }
            Texture2D tex = ContentFinder<Texture2D>.Get(path, false);
            if (tex != null)
            {
                mats = new Material[GlowLevels];
                for (int i = 0; i < GlowLevels; i++)
                {
                    float a = 0.35f + 0.65f * i / (GlowLevels - 1);
                    mats[i] = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, a));
                }
            }
            GlowMats[path] = mats;
            return mats;
        }
    }
}
