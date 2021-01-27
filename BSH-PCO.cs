using DevInterface;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WaspPile.BSH
{
    public static class EnumExt_PCO
    {
        public static PlacedObject.Type ShelterSpawnPoint;
        public static PlacedObject.Type ShelterDoorShift;
        public static PlacedObject.Type Fakedoor;
    }

    public static class BSH_Custom
    {
        public static string RoateTheBananas(int dir)
        {
            switch (dir)
            {
                case 3:
                    return "up";
                case 0:
                    return "left";
                case 1:
                    return "down";
                case 2:
                    return "right";
            }
            return "fuck";
        }

    }

    public class ShelderDoorShiftData : PlacedObject.Data
    {
        public ShelderDoorShiftData(PlacedObject owner) : base(owner)
        {
            this.tpos.x = (int)owner.pos.x / 20;
            this.tpos.y = (int)owner.pos.y / 20;
        }

        public int dir;
        public RWCustom.IntVector2 tpos;
        public Vector2 panelpos;
        
        public override string ToString()
        {
            return string.Concat(new object[]{
                this.dir.ToString(),
                "~",
                this.panelpos.x,
                "~",
                this.panelpos.y
            });
        }

        public bool RemoveDoor;

        public override void FromString(string s)
        {
            this.dir = 0;
            panelpos = new Vector2(0,0);
            try
            {
                string[] strarr = Regex.Split(s, "~");
                this.dir = int.Parse(strarr[0]);
                this.panelpos.x = float.Parse(strarr[1]);
                this.panelpos.y = float.Parse(strarr[2]);
            }
            catch
            {
                Debug.Log("Failed to load ShelterDoorShiftData from string; some parameters are reset to default.");
            }
            

        }
    }

    public class ShelterDoorShiftRep : PlacedObjectRepresentation
    {
        public ShelterDoorShiftRep(DevUI owner, string IDstring, DevUINode parentnode, PlacedObject pobj, string name) : base(owner, IDstring, parentnode, pobj, name)
        {
            this.pObj = pobj;
            this.fLabels.Add(new FLabel("font", name));
            this.subNodes.Add(new SDSControlPanel(owner, this.IDstring + "_pl", this, new Vector2(0f, 100f), BSH_Custom.RoateTheBananas((this.pObj.data as ShelderDoorShiftData).dir)));
            for (int i = 0; i < this.subNodes.Count; i++)
            {
                if (this.subNodes[i] is SDSControlPanel) this.theonlypanel = (this.subNodes[i] as SDSControlPanel);
            }
            theonlypanel.pos = (this.pObj.data as ShelderDoorShiftData).panelpos;
            for (int i = 0; i < theonlypanel.subNodes.Count; i++)
            {
                if (this.theonlypanel.subNodes[i] is SDSButton && (this.theonlypanel.subNodes[i] as SDSButton).IDstring == "DirCycleBtn")
                {
                    this.theonlybutton = (this.theonlypanel.subNodes[i] as SDSButton);
                }
            }
        }

        private SDSControlPanel theonlypanel;
        private SDSButton theonlybutton;
        public override void Update()
        {
            base.Update();
            if (theonlybutton.down && !theonlybutton.wasDown)
            {
                (this.pObj.data as ShelderDoorShiftData).dir++;
                if (((this.pObj.data as ShelderDoorShiftData).dir > 3) || (this.pObj.data as ShelderDoorShiftData).dir < 0) (this.pObj.data as ShelderDoorShiftData).dir = 0;
                theonlybutton.Text = BSH_Custom.RoateTheBananas((this.pObj.data as ShelderDoorShiftData).dir);
            }
            
        }
        public class SDSControlPanel : Panel
        {
            public SDSControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string initdir) : base(owner, IDstring, parentNode, pos, new Vector2(150f, 30f), "Shelter Door Shift")
            {
                this.subNodes.Add(new SDSButton(owner, "DirCycleBtn", this, new Vector2(2f, 2f), 50f, initdir));
            }
            
        }

        public class SDSButton : DevInterface.Button
        {
            public SDSButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text) : base(owner, IDstring, parentNode, pos, width, text)
            {
                wasDown = false;
                this.owner = owner;
            }
            public override void Update()
            {
                wasDown = this.down;
                base.Update();

            }
            public bool wasDown;
        }
    }

    public class FakeDoorData : PlacedObject.Data
    {
        public FakeDoorData (PlacedObject owner) : base (owner)
        {
            
        }

        public override void FromString(string s)
        {
            string[] strarr = Regex.Split(s, "~");
            this.dir = int.Parse(strarr[0]);
            this.panelpos.x = float.Parse(strarr[1]);
            this.panelpos.y = float.Parse(strarr[2]);
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                this.dir,
                "~",
                this.panelpos.x.ToString(),
                "~",
                this.panelpos.y.ToString()
            });
        }

        public int dir;
        public Vector2 panelpos = new Vector2();

        public RWCustom.IntVector2 tpos
        {
            get { return RWCustom.IntVector2.FromVector2(this.owner.pos / 20); }
        }
        
    }

    public class FakeDoorRepresentation : PlacedObjectRepresentation
    {
        public FakeDoorRepresentation(DevUI owner, string IDstring, DevUINode parentnode, PlacedObject pobj, string name) : base(owner, IDstring, parentnode, pobj, name)
        {
            this.pObj = pobj;
            this.subNodes.Add(new ShelterDoorShiftRep.SDSControlPanel(owner, this.IDstring + "_pl", this, new Vector2(0f, 100f), BSH_Custom.RoateTheBananas((this.pObj.data as FakeDoorData).dir)));
            for (int i = 0; i < this.subNodes.Count; i++)
            {
                if (this.subNodes[i] is ShelterDoorShiftRep.SDSControlPanel) this.theonlypanel = (this.subNodes[i] as ShelterDoorShiftRep.SDSControlPanel);
            }
            theonlypanel.pos = (this.pObj.data as FakeDoorData).panelpos;
            theonlypanel.fLabels[0].text = "Fake Door";
            for (int i = 0; i < theonlypanel.subNodes.Count; i++)
            {
                if (this.theonlypanel.subNodes[i] is ShelterDoorShiftRep.SDSButton && (this.theonlypanel.subNodes[i] as ShelterDoorShiftRep.SDSButton).IDstring == "DirCycleBtn")
                {
                    this.theonlybutton = (this.theonlypanel.subNodes[i] as ShelterDoorShiftRep.SDSButton);
                }
            }
        }

        private ShelterDoorShiftRep.SDSControlPanel theonlypanel;
        private ShelterDoorShiftRep.SDSButton theonlybutton;
        //FakeDoor fakeDoor;

        public override void Update()
        {
            base.Update();
            if (theonlybutton.down && !theonlybutton.wasDown)
            {
                (this.pObj.data as FakeDoorData).dir++;
                if ((this.pObj.data as FakeDoorData).dir > 3 || (this.pObj.data as FakeDoorData).dir < 0)
                {
                    (this.pObj.data as FakeDoorData).dir = 0;
                }
                theonlybutton.Text = BSH_Custom.RoateTheBananas((this.pObj.data as FakeDoorData).dir);
            }
        }

    }
}

