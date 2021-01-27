using UnityEngine;

namespace WaspPile.BSH
{
    internal class FlavourRoomRain : RoomRain, IDrawable
    {
        public FlavourRoomRain(GlobalRain GR, Room RM) : base (GR, RM)
        {
            this.room = RM;
			
        }
        public override void Update(bool eu)
        {
            this.evenUpdate = eu;
			if (this.dangerType != RoomRain.DangerType.Flood)
			{
				this.normalRainSound.Volume = ((this.intensity <= 0f) ? 0f : (0.1f + 0.9f * Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.InverseLerp(0.001f, 0.7f, this.intensity) * 3.14159274f)), 1.5f)));
				this.normalRainSound.Update();
				this.heavyRainSound.Volume = Mathf.Pow(Mathf.InverseLerp(0.12f, 0.5f, this.intensity), 0.85f) * Mathf.Pow(1f - this.deathRainSound.Volume, 0.3f);
				this.heavyRainSound.Update();
			}
			this.deathRainSound.Volume = Mathf.Pow(Mathf.InverseLerp(0.35f, 0.75f, this.intensity), 0.8f);
			this.deathRainSound.Update();
			this.rumbleSound.Volume = this.globalRain.RumbleSound * this.room.roomSettings.RumbleIntensity;
			this.rumbleSound.Update();
			this.distantDeathRainSound.Volume = Mathf.InverseLerp(1400f, 0f, room.world.rainCycle.TimeUntilRain) * this.room.roomSettings.RainIntensity;
			this.distantDeathRainSound.Update();
			/*if (this.dangerType != RoomRain.DangerType.Rain)
			{
				this.floodingSound.Volume = Mathf.InverseLerp(0.01f, 0.5f, this.globalRain.floodSpeed);
				this.floodingSound.Update();
			}*/
			if (this.room.game.cameras[0].room == this.room)
			{
				this.SCREENSHAKESOUND.Volume = this.room.game.cameras[0].ScreenShake * (1f - this.rumbleSound.Volume);
			}
			else
			{
				this.SCREENSHAKESOUND.Volume = 0f;
			}
			this.SCREENSHAKESOUND.Update();
		}

		/*new public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{

        }

		new public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

        }

		new public void ApplyPalette(RoomCamera.SpriteLeaser leaser, RoomCamera rcam, RoomPalette palette)
        {

        }

		new public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {

        }*/
	}
}
