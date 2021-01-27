using RWCustom;
using UnityEngine;

namespace WaspPile.BSH
{
    public class FakeDoor : UpdatableAndDeletable, IDrawable
    {
        public FakeDoor (Room room, IntVector2 tpos, int idir, ShelterDoor RealDoor)
        {
            this.room = room;
            this.ActualDoor = RealDoor;
            if (this.ActualDoor == null)
            {
                Debug.LogWarning($"{this.room.abstractRoom.name}: DOOR SYNC FAILED");
                this.Destroy();
                return;
            }
            this.dir = Custom.fourDirections[idir].ToVector2();
            this.pZero = this.room.MiddleOfTile(tpos);
            int seed = Random.seed;
            Random.seed = this.room.abstractRoom.index + (idir * tpos.x / tpos.y);
            this.brokenSegs = 0.25f + 0.5f * Random.value;
            this.brokenFlaps = 0.5f + 0.5f * Random.value;
            Random.seed = seed;

            this.closeTiles = new IntVector2[4];
            IntVector2 v2 = tpos;
            for (int m = 0; m < 4; m++)
            {
                v2 += Custom.fourDirections[idir];
                this.closeTiles[m] = (this.room.IsPositionInsideBoundries(v2) ? v2 : new IntVector2(0, 0));
            }
            this.AddMyCloseTilesToMainDoor();
            this.InitSegs();
            this.pZero += this.dir * 60f;
            
            this.workingLoop = new StaticSoundLoop(SoundID.Shelter_Working_Background_Loop, this.pZero, room, 0f, 1f);
            this.gasketMoverLoop = new StaticSoundLoop(SoundID.Shelter_Gasket_Mover_LOOP, this.pZero, room, 0f, 1f);

        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            float num2 = Mathf.InverseLerp(0.53f, 0.61f, this.closedFac);
            if (this.closeSpeed < 0f && Custom.Decimal(num2 * 4f) < Custom.Decimal(this.Cylinders * 4f))
            {
                this.room.PlaySound(SoundID.Shelter_Bolt_Open, this.pZero, 1f, 1f);
            }
            else if (this.closeSpeed > 0f && Custom.Decimal(num2 * 4f) > Custom.Decimal(this.Cylinders * 4f))
            {
                this.room.PlaySound(SoundID.Shelter_Bolt_Close, this.pZero, 1f, 1f);
            }
            this.workingLoop.Update();
            if (this.Closed > 0.1f && this.Closed < 1f)
            {
                this.workingLoop.volume = Mathf.Lerp(this.workingLoop.volume, 1f, 0.1f);
            }
            else
            {
                this.workingLoop.volume = Mathf.Max(0f, this.workingLoop.volume - 0.05f);
            }
            float num = BetterShelters.OldClosedFac[ActualDoor];
            if (num >= 0.04f && this.closedFac < 0.04f)
            {
                this.room.PlaySound(SoundID.Shelter_Little_Hatch_Open, this.pZero, 1f, 1f);
            }
            else if (num < 0.04f && this.closedFac >= 0.04f)
            {
                this.room.PlaySound(SoundID.Shelter_Little_Hatch_Open, this.pZero, 1f, 1f);
            }
            for (int l = 0; l < 5; l++)
            {
                this.segmentPairs[l, 1] = this.segmentPairs[l, 0];
                this.segmentPairs[l, 0] = Mathf.InverseLerp(this.segmentPairs[l, 2], this.segmentPairs[l, 2] + 0.5f, this.Segments);
                if (this.segmentPairs[l, 0] == 1f && this.segmentPairs[l, 1] < 1f)
                {
                    this.room.ScreenMovement(new Vector2?(this.pZero), this.dir * 0f, 0.1f);
                    this.room.PlaySound(SoundID.Shelter_Segment_Pair_Collide, this.pZero, 1f, 1f);
                }
                if (this.segmentPairs[l, 1] <= 0.1f && this.segmentPairs[l, 0] > 0.1f)
                {
                    this.room.PlaySound(SoundID.Shelter_Segment_Pair_Move_In, this.pZero, 1f, 1f);
                }
                else if (this.segmentPairs[l, 1] == 1f && this.segmentPairs[l, 0] < 1f)
                {
                    this.room.PlaySound(SoundID.Shelter_Segment_Pair_Move_Out, this.pZero, 1f, 1f);
                }
            }
            for (int m = 0; m < 2; m++)
            {
                this.pistons[m, 1] = this.pistons[m, 0];
                this.pistons[m, 0] = Mathf.InverseLerp(this.pistons[m, 2], 1f, Mathf.Max(this.Pistons, this.PistonsClosed));
                if (this.pistons[m, 0] >= 0.95f && this.pistons[m, 1] < 0.95f)
                {
                    if (this.PistonsClosed < 0.5f)
                    {
                        this.room.ScreenMovement(new Vector2?(this.pZero), this.dir * 3f, 0f);
                        this.room.PlaySound(SoundID.Shelter_Piston_In_Hard, this.pZero, 1f, 1f);
                    }
                    else
                    {
                        this.room.PlaySound(SoundID.Shelter_Piston_In_Soft, this.pZero, 1f, 1f);
                    }
                }
                else if (this.pistons[m, 0] < 1f && this.pistons[m, 1] == 1f)
                {
                    this.room.PlaySound(SoundID.Shelter_Piston_Out, this.pZero, 1f, 1f);
                }
            }
            for (int n = 0; n < 4; n++)
            {
                this.covers[n, 1] = this.covers[n, 0];
                this.covers[n, 0] = Mathf.InverseLerp(this.covers[n, 2], this.covers[n, 2] + 0.5f, this.Covers);
                if (this.covers[n, 1] == 1f && this.covers[n, 0] < 1f)
                {
                    this.room.PlaySound(SoundID.Shelter_Protective_Cover_Move_Out, this.pZero, 1f, 1f);
                }
                else if (this.covers[n, 1] == 0f && this.covers[n, 0] > 0f)
                {
                    this.room.PlaySound(SoundID.Shelter_Protective_Cover_Move_In, this.pZero, 1f, 1f);
                }
                else if (this.covers[n, 1] < 1f && this.covers[n, 0] == 1f)
                {
                    this.room.PlaySound(SoundID.Shelter_Protective_Cover_Click_Into_Place, this.pZero, 1f, 1f);
                }
            }
            for (int num3 = 0; num3 < 8; num3++)
            {
                this.pumps[num3, 1] = this.pumps[num3, 0];
                this.pumps[num3, 0] = Mathf.InverseLerp(this.pumps[num3, 2], 1f, this.PumpsEnter);
            }
            if (this.pumps[0, 0] == 1f && this.pumps[0, 1] < 1f)
            {
                this.room.PlaySound(SoundID.Shelter_Gaskets_Seal, this.pZero, 1f, 1f);
            }
            else if (this.pumps[0, 0] < 1f && this.pumps[0, 1] == 1f)
            {
                this.room.PlaySound(SoundID.Shelter_Gaskets_Unseal, this.pZero, 1f, 1f);
            }
            this.gasketMoverLoop.Update();
            if ((this.PumpsEnter > 0f && this.PumpsEnter < 1f) || (this.PumpsExit > 0f && this.PumpsExit < 1f))
            {
                this.gasketMoverLoop.volume = Mathf.Lerp(this.gasketMoverLoop.volume, 1f, 0.5f);
                if (this.PumpsEnter > 0f && this.PumpsEnter < 1f)
                {
                    this.gasketMoverLoop.pitch = Mathf.Lerp(this.gasketMoverLoop.pitch, 1.2f, 0.2f);
                }
                else
                {
                    this.gasketMoverLoop.pitch = Mathf.Lerp(this.gasketMoverLoop.pitch, 0.8f, 0.2f);
                }
            }
            else
            {
                this.gasketMoverLoop.volume = Mathf.Max(0f, this.gasketMoverLoop.volume - 0.05f);
            }
            if (this.PumpsEnter > 0f && this.PumpsExit < 1f)
            {
                this.room.ScreenMovement(new Vector2?(this.pZero), this.dir * 0f, 0.1f);
            }
            bool flag = this.Closed > 0f;
            if (flag != this.lastClosed)
            {
                this.Reset();
            }
            this.lastClosed = flag;
        }
        private void AddMyCloseTilesToMainDoor()
        {
            IntVector2[] oldct = ActualDoor.closeTiles;
            ActualDoor.closeTiles = null;
            ActualDoor.closeTiles = new IntVector2[oldct.Length + 4];
            for (int m = 0; m < oldct.Length; m++)
            {
                ActualDoor.closeTiles[m] = oldct[m];
            }
            for (int m = 0; m < 4; m++)
            {
                ActualDoor.closeTiles[m + oldct.Length] = this.closeTiles[m];
            }
        }
        private void InitSegs()
        {
            this.segmentPairs = new float[5, 3];
            for (int num = 0; num < 5; num++)
            {
                this.segmentPairs[num, 0] = this.closedFac;
                this.segmentPairs[num, 1] = this.closedFac;
            }
            this.pistons = new float[2, 3];
            for (int num = 0; num < 2; num++)
            {
                this.pistons[num, 0] = this.closedFac;
                this.pistons[num, 1] = this.closedFac;
            }
            this.covers = new float[4, 3];
            for (int num = 0; num < 4; num++)
            {
                this.covers[num, 0] = this.closedFac;
                this.covers[num, 1] = this.closedFac;
            }
            this.pumps = new float[8, 3];
            for (int num = 0; num < 8; num++)
            {
                this.pumps[num, 0] = this.closedFac;
                this.pumps[num, 1] = this.closedFac;
            }
        }
        private void Reset()
        {
            for (int i = 0; i < 5; i++)
            {
                this.segmentPairs[i, 2] = UnityEngine.Random.value * 0.5f;
            }
            for (int j = 0; j < 2; j++)
            {
                this.pistons[j, 2] = UnityEngine.Random.value * 0.8f;
            }
            for (int k = 0; k < 4; k++)
            {
                this.covers[k, 2] = UnityEngine.Random.value * 0.5f;
            }
            for (int l = 0; l < 8; l++)
            {
                this.pumps[l, 2] = UnityEngine.Random.value * 0.5f;
            }
        }

        private ShelterDoor ActualDoor;
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[42];
            for (int i = 0; i < 4; i++)
            {
                sLeaser.sprites[this.CogSprite(i)] = new FSprite("ShelterGate_cog", true);
                sLeaser.sprites[this.CogSprite(i)].alpha = 1f - ((i >= 2) ? 5f : 6f) / 30f;
            }
            for (int j = 0; j < 2; j++)
            {
                sLeaser.sprites[this.PistonSprite(j)] = new FSprite("ShelterGate_piston" + (j + 1), true);
                sLeaser.sprites[this.PistonSprite(j)].alpha = 0.9f;
            }
            for (int k = 0; k < 8; k++)
            {
                sLeaser.sprites[this.PlugSprite(k)] = new FSprite("ShelterGate_plug" + (k + 1), true);
            }
            for (int l = 0; l < 10; l++)
            {
                sLeaser.sprites[this.SegmentSprite(l)] = new FSprite("ShelterGate_segment" + (l + 1), true);
            }
            for (int m = 0; m < 4; m++)
            {
                sLeaser.sprites[this.CylinderSprite(m)] = new FSprite("ShelterGate_cylinder" + (m + 1), true);
            }
            for (int n = 0; n < 4; n++)
            {
                sLeaser.sprites[this.CoverSprite(n)] = new FSprite("ShelterGate_cover" + (n + 1), true);
            }
            for (int num = 0; num < 8; num++)
            {
                sLeaser.sprites[this.PumpSprite(num)] = new FSprite("ShelterGate_pump" + (num + 1), true);
            }
            for (int num2 = 0; num2 < 2; num2++)
            {
                sLeaser.sprites[this.FlapSprite(num2)] = new FSprite("ShelterGate_Hatch", true);
                sLeaser.sprites[this.FlapSprite(num2)].anchorX = 0.2f;
                sLeaser.sprites[this.FlapSprite(num2)].anchorY = 0.43f;
                if (num2 == 1)
                {
                    sLeaser.sprites[this.FlapSprite(num2)].scaleX = -1f;
                }
            }
            float rotation = Custom.AimFromOneVectorToAnother(this.dir, new Vector2(0f, 0f));
            for (int num3 = 0; num3 < sLeaser.sprites.Length; num3++)
            {
                sLeaser.sprites[num3].rotation = rotation;
                sLeaser.sprites[num3].shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
            }
            this.AddToContainer(sLeaser, rCam, null);
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            camPos.x += 0.25f;
            camPos.y += 0.25f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = this.perp * ((i % 2 != 0) ? 1f : -1f) * ((i < 2) ? 55f : 35f);
                a -= this.dir * ((i < 2) ? 15f : 50f);
                sLeaser.sprites[this.CogSprite(i)].x = this.pZero.x - camPos.x + a.x;
                sLeaser.sprites[this.CogSprite(i)].y = this.pZero.y - camPos.y + a.y;
                sLeaser.sprites[this.CogSprite(i)].rotation = this.Closed * ((i < 2) ? -150f : 400f) * ((i % 2 != 0) ? 1f : -1f);
            }
            for (int j = 0; j < 10; j++)
            {
                int num = j / 2;
                Vector2 vector = this.perp * Mathf.Pow(1f - Mathf.Lerp(this.segmentPairs[num, 1], this.segmentPairs[num, 0], timeStacker), 0.75f) * 55f * ((j % 2 != 0) ? 1f : -1f);
                sLeaser.sprites[this.SegmentSprite(j)].x = this.pZero.x - camPos.x + vector.x;
                sLeaser.sprites[this.SegmentSprite(j)].y = this.pZero.y - camPos.y + vector.y;
                sLeaser.sprites[this.SegmentSprite(j)].alpha = 1f - 4f * Mathf.InverseLerp(0.78f, 0.61f, this.Closed) / 30f;
            }
            for (int k = 0; k < 2; k++)
            {
                Vector2 a2 = this.dir * (1f - Mathf.Lerp(this.pistons[k, 1], this.pistons[k, 0], timeStacker)) * -120f;
                if (this.PistonsClosed > 0f)
                {
                    a2 += this.perp * 5f * ((k != 0) ? 1f : -1f);
                }
                sLeaser.sprites[this.PistonSprite(k)].x = this.pZero.x - camPos.x + a2.x;
                sLeaser.sprites[this.PistonSprite(k)].y = this.pZero.y - camPos.y + a2.y;
            }
            for (int l = 0; l < 4; l++)
            {
                Vector2 vector2 = this.perp * Mathf.Pow(1f - Mathf.Lerp(this.covers[l, 1], this.covers[l, 0], timeStacker), 2.5f) * 65f * ((l < 2) ? 1f : -1f);
                sLeaser.sprites[this.CoverSprite(l)].x = this.pZero.x - camPos.x + vector2.x;
                sLeaser.sprites[this.CoverSprite(l)].y = this.pZero.y - camPos.y + vector2.y;
            }
            for (int m = 0; m < 4; m++)
            {
                if (m / 4f < this.Cylinders)
                {
                    sLeaser.sprites[this.CylinderSprite(m)].x = this.pZero.x - camPos.x;
                    sLeaser.sprites[this.CylinderSprite(m)].y = this.pZero.y - camPos.y;
                    sLeaser.sprites[this.CylinderSprite(m)].isVisible = true;
                }
                else
                {
                    sLeaser.sprites[this.CylinderSprite(m)].isVisible = false;
                }
            }
            for (int n = 0; n < 8; n++)
            {
                Vector2 a3 = this.perp * Mathf.Lerp(this.pumps[n, 1], this.pumps[n, 0], timeStacker) * -42f * ((n % 2 != 0) ? 1f : -1f);
                a3 += -this.dir * this.PumpsExit * 80f;
                sLeaser.sprites[this.PumpSprite(n)].x = this.pZero.x - camPos.x + a3.x;
                sLeaser.sprites[this.PumpSprite(n)].y = this.pZero.y - camPos.y + a3.y;
                a3 = this.perp * Mathf.Clamp(1f - Mathf.Lerp(this.pumps[n, 1], this.pumps[n, 0], timeStacker) - 0.35f, 0f, 1f) * 60f * ((n % 2 != 0) ? 1f : -1f);
                sLeaser.sprites[this.PlugSprite(n)].x = this.pZero.x - camPos.x + a3.x;
                sLeaser.sprites[this.PlugSprite(n)].y = this.pZero.y - camPos.y + a3.y;
            }
            for (int num2 = 0; num2 < 2; num2++)
            {
                Vector2 vector3 = this.pZero + this.dir * 46f + this.perp * Mathf.Lerp(15f, 25f, this.FlapsOpen) * ((num2 != 0) ? -1f : 1f);
                sLeaser.sprites[this.FlapSprite(num2)].x = vector3.x - camPos.x;
                sLeaser.sprites[this.FlapSprite(num2)].y = vector3.y - camPos.y;
                sLeaser.sprites[this.FlapSprite(num2)].rotation = Custom.AimFromOneVectorToAnother(-this.dir, this.dir) - 90f * ((num2 != 0) ? 1f : -1f) * this.FlapsOpen;
            }
            }
        public void ApplyPalette(RoomCamera.SpriteLeaser leaser, RoomCamera rcam, RoomPalette palette)
        {

        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }
        public Vector2 pZero;
        public Vector2 dir;
        public Vector2 perp
        {
            get { return Custom.PerpendicularVector(this.dir); }
        }

        private IntVector2[] closeTiles;
        private float[,] segmentPairs;
        private float[,] pistons;
        private float[,] covers;
        private float[,] pumps;
        private bool lastClosed;
        //private float openUpTicks = 350f;
        //private float initialWait = 80f;
        public StaticSoundLoop workingLoop;
        public StaticSoundLoop gasketMoverLoop;
        private float brokenSegs;
        private float brokenFlaps;
        //private bool lastViewed;
        private bool Broken
        {
            get { return this.ActualDoor.Broken; }
        }
        private float PumpsExit
        {
            get
            {
                return Mathf.InverseLerp(0.75f, 1f, this.Closed);
            }
        }
        private float PistonsClosed
        {
            get
            {
                return Mathf.InverseLerp(0.2f, 0.1f, this.Closed);
            }
        }
        private float Cylinders
        {
            get
            {
                return Mathf.InverseLerp(0.53f, 0.61f, this.Closed);
            }
        }
        private float FlapsOpen
        {
            get
            {
                return (!this.Broken) ? Mathf.InverseLerp(0.04f, 0f, this.Closed) : this.brokenFlaps;
            }
        }
        private float Segments
        {
            get
            {
                return (!this.Broken) ? Mathf.InverseLerp(0.2f, 0.38f, this.Closed) : this.brokenSegs;
            }
        }
        private float Pistons
        {
            get
            {
                return Mathf.InverseLerp(0.38f, 0.41f, this.Closed);
            }
        }
        private float Covers
        {
            get
            {
                return Mathf.InverseLerp(0.41f, 0.51f, this.Closed);
            }
        }
        private float Closed
        {
            get
            {
                if (this.Broken)
                {
                    return 0f;
                }
                return Mathf.Clamp(this.closedFac, 0f, 1f);
            }
        }
        private float PumpsEnter
        {
            get
            {
                return Mathf.InverseLerp(0.59f, 0.7f, this.Closed);
            }
        }
        private float closeSpeed
        {
            get { return this.ActualDoor.closeSpeed; }
        }
        private float closedFac
        {
            get { return this.ActualDoor.closedFac; }
        }

        private RainCycle rainCycle
        {
            get { return this.room.game.world.rainCycle; }
        }
        private int CogSprite(int cog)
        {
            return cog;
        }
        private int PistonSprite(int piston)
        {
            return 4 + piston;
        }
        private int PlugSprite(int plug)
        {
            return 6 + plug;
        }
        private int SegmentSprite(int segment)
        {
            return 14 + segment;
        }
        private int CylinderSprite(int cylinder)
        {
            return 24 + cylinder;
        }
        private int CoverSprite(int cover)
        {
            return 28 + cover;
        }
        private int PumpSprite(int pump)
        {
            return 32 + pump;
        }
        private int FlapSprite(int flap)
        {
            return 40 + flap;
        }
    }
}
