//using MonoMod.RuntimeDetour.HookGen;
using MonoMod.RuntimeDetour;
using OptionalUI;
using Partiality.Modloader;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace BSH
{
    public class BetterShelters : PartialityMod
    {
        public BetterShelters()
        {
            instance = this;
            this.ModID = "BSH";
            this.Version = "0.4.5";
            this.author = "Thalber";
        }

        public static void Player_Update_Hook(On.Player.orig_Update orig, Player instance, bool eu)
        {
            orig.Invoke(instance, eu);
            if (CustomSleepCounter == null || !CustomSleepCounter.ContainsKey(instance)) return;
            CustomSleepCtrUpdate(instance);
        }
        public static void Player_ctor_hook(On.Player.orig_ctor orig, Player instance, AbstractCreature abstractCreature, World world)
        {
            orig(instance, abstractCreature, world);
            if (CustomSleepCounter == null) return;
            CustomSleepCounter.Add(instance, instance.touchedNoInputCounter);
            PrevRooms.Add(instance, instance.room);
        }
        public static void Creature_suckedIntoShortcut_hook(On.Creature.orig_SuckedIntoShortCut orig, Creature instance, RWCustom.IntVector2 entrancePos, bool CarriedByOther)
        {
            if (instance is Player && PrevRooms != null && PrevRooms.ContainsKey(instance as Player))
            {
                PrevRooms[instance as Player] = instance.room;
            }
            orig(instance, entrancePos, CarriedByOther);
        }
        public static void Player_SpitOutShortcut_hook(On.Player.orig_SpitOutOfShortCut orig, Player instance, RWCustom.IntVector2 pos, Room newroom, bool SpitOutAllSticks)
        {
            bool oldsss = instance.stillInStartShelter;
            orig(instance, pos, newroom, SpitOutAllSticks);
            if (PrevRooms != null && PrevRooms.ContainsKey(instance) && PrevRooms[instance] == instance.room) instance.stillInStartShelter = oldsss;
        }
        public static void Door_Close_Hook(On.ShelterDoor.orig_Close orig, ShelterDoor instance)
        {
            for (int i = 0; i < instance.room.game.Players.Count; i++)
            {
                if (CustomSleepCounter != null && CustomSleepCounter.ContainsKey((instance.room.game.Players[i].realizedCreature as Player)) && CustomSleepCounter[(instance.room.game.Players[i].realizedCreature as Player)] < BSHCMOI.BSHSettings.CustomTicksToSleep)
                {
                    return;
                }
            }
            orig.Invoke(instance);
            //rough consumable dup fix
            for (int i = 0; i < instance.room.abstractRoom.entities.Count; i++)
            {
                /*if (instance.room.abstractRoom.entities[i] is AbstractConsumable && !(instance.room.abstractRoom.entities[i] as AbstractConsumable).isConsumed && instance.room.ReallyTrulyRealizedInRoom(instance.room.abstractRoom.entities[i] as AbstractConsumable))
                {
                    instance.room.abstractRoom.RemoveEntity(instance.room.abstractRoom.entities[i]);
                }*/
                if (instance.room.abstractRoom.entities[i] is AbstractConsumable)
                {
                    if ((instance.room.abstractRoom.entities[i] as AbstractConsumable).type != AbstractPhysicalObject.AbstractObjectType.KarmaFlower && (instance.room.abstractRoom.entities[i] as AbstractConsumable).type != AbstractPhysicalObject.AbstractObjectType.SeedCob)
                    {
                        (instance.room.abstractRoom.entities[i] as AbstractConsumable).Consume();
                    }
                }
            }
        }
        public static void Room_Loaded_Hook(On.Room.orig_Loaded orig, Room instance)
        {
            orig.Invoke(instance);
            if (!instance.abstractRoom.shelter) return;
            if (instance.game == null || instance.world == null || instance.abstractRoom == null) return;
            for (int i = 0; i < instance.roomSettings.placedObjects.Count; i++)
            {
                if (instance.roomSettings.placedObjects[i].type == EnumExt_PCO.ShelterSpawnPoint)
                {
                    //this is spawnpoint, pretty straightforwards
                    if (instance.shelterDoor != null)
                    {
                        IntVector2 iv2 = IntVector2.FromVector2(instance.roomSettings.placedObjects[i].pos);
                        iv2.x /= 20;
                        iv2.y /= 20;
                        instance.shelterDoor.playerSpawnPos = iv2;
                    }
                    //else Debug.Log(instance.abstractRoom.name + ", SSP: No shelter door found!");
                }
                if (instance.roomSettings.placedObjects[i].type == EnumExt_PCO.ShelterDoorShift)
                {
                    //
                    if (instance.shelterDoor != null)
                    {
                        //Debug.Log(instance.abstractRoom.name + ": SDS PO Found.");
                        if ((instance.roomSettings.placedObjects[i].data as ShelderDoorShiftData).RemoveDoor)
                        {
                            instance.shelterDoor.pZero = new Vector2(-100f, -100f);
                            continue;
                            
                        }
                        IntVector2 cpos = (instance.roomSettings.placedObjects[i].data as ShelderDoorShiftData).tpos;
                        IntVector2 cdir = Custom.fourDirections[(instance.roomSettings.placedObjects[i].data as ShelderDoorShiftData).dir];
                        instance.shelterDoor.dir = cdir.ToVector2();
                        instance.shelterDoor.pZero = instance.MiddleOfTile(cpos);
                        instance.shelterDoor.pZero += instance.shelterDoor.dir * 60f;
                        for (int m = 0; m < 4; m++)
                        {
                            cpos += cdir;
                            if (instance.IsPositionInsideBoundries(cpos)) instance.shelterDoor.closeTiles[m] = cpos;
                            else instance.shelterDoor.closeTiles[m] = new IntVector2(0, 0);
                        }
                        instance.shelterDoor.perp = Custom.PerpendicularVector(instance.shelterDoor.dir);

                        
                    }
                    //else Debug.Log($"{instance.abstractRoom.name}, SDS: No shelter door found!");
                }
                if (instance.roomSettings.placedObjects[i].type == EnumExt_PCO.Fakedoor)
                {
                    FakeDoorData fdd = (instance.roomSettings.placedObjects[i].data as FakeDoorData);
                    instance.AddObject(new FakeDoor(instance, fdd.tpos, fdd.dir, instance.shelterDoor));
                    
                }
            }
            if (instance.shelterDoor != null) OldClosedFac.Add(instance.shelterDoor, instance.shelterDoor.closedFac);
            
            if (instance.game.globalRain != null && !instance.world.brokenShelters[instance.abstractRoom.shelterIndex])
            {
                if (instance.roomSettings.DangerType == RoomRain.DangerType.Rain)
                {
                    
                    int oldSI = instance.abstractRoom.shelterIndex;
                    if (instance.deathFallGraphic != null)
                    {
                        instance.RemoveObject(instance.deathFallGraphic);
                        instance.deathFallGraphic = null;
                    }
                    if (instance.roomRain != null)
                    {
                        instance.RemoveObject(instance.roomRain);
                        instance.roomRain = null;
                    }
                    
                    instance.abstractRoom.shelterIndex = -1;
                    instance.roomRain = new RoomRain(instance.game.globalRain, instance);
                    instance.AddObject(instance.roomRain);

                    instance.abstractRoom.shelterIndex = oldSI;
                    thesearespeshul.Add(instance.roomRain, false);
                    instance.roomRain.Update(true);
                    thesearespeshul[instance.roomRain] = true;
                }
                else if (instance.roomSettings.DangerType == RoomRain.DangerType.FloodAndRain)
                {
                    if (instance.roomRain != null) instance.RemoveObject(instance.roomRain);
                    //RoomRain.DangerType olddt = instance.roomSettings.DangerType;
                    instance.roomSettings.DangerType = RoomRain.DangerType.Rain;
                    instance.roomRain = new FlavourRoomRain(instance.game.globalRain, instance);
                    instance.roomSettings.DangerType = RoomRain.DangerType.FloodAndRain;
                    instance.AddObject(instance.roomRain);
                }
            }
            
        }
        public static void PO_GenerateEmptyData_Hook(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);
            if (instance.type == EnumExt_PCO.ShelterDoorShift)
            {
                instance.data = new ShelderDoorShiftData(instance);
            }
            if (instance.type == EnumExt_PCO.Fakedoor)
            {
                instance.data = new FakeDoorData(instance);
            }
        }
        public static void objectsPage_CreateObjectRep_Hook(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pobj)
        {
            orig(instance, tp, pobj);
            if (tp == EnumExt_PCO.ShelterDoorShift)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation nrep = new ShelterDoorShiftRep(instance.owner, tp.ToString() + "_rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(nrep);
                instance.subNodes.Add(nrep);
                
            }
            if (tp == EnumExt_PCO.Fakedoor)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation nrep = new FakeDoorRepresentation(instance.owner, tp.ToString() + "_rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(nrep);
                instance.subNodes.Add(nrep);
                
            }
        }
        public static void RWG_ctor_Hook(On.RainWorldGame.orig_ctor orig, RainWorldGame instance, ProcessManager manager)
        {
            OldClosedFac = new Dictionary<ShelterDoor, float>();
            CustomSleepCounter = new Dictionary<Player, int>();
            PrevRooms = new Dictionary<Player, Room>();
            if (thesearespeshul != null) thesearespeshul.Clear();
            thesearespeshul = new Dictionary<RoomRain, bool>();
            orig(instance, manager);
        }
        public static void RoomRain_ThrowAround_Hook(On.RoomRain.orig_ThrowAroundObjects orig, RoomRain instance)
        {
            if (instance.room.abstractRoom.shelter && !instance.room.world.brokenShelters[instance.room.abstractRoom.shelterIndex]) return;
            orig(instance);
        }
        public static void RoomRain_Ctor_Hook(On.RoomRain.orig_ctor orig, RoomRain instance, GlobalRain GR, Room RM)
        {
            orig(instance, GR, RM);
            if (RM.abstractRoom == null || instance.shelterTex == null) return;
            byte[] JPG = instance.shelterTex.EncodeToJPG();
            using (FileStream fs = File.OpenWrite(@"C:\Users\thalber\Documents\SHELTEX" + RM.abstractRoom.name + ".jpg"))
            {
                fs.Write(JPG, 0, JPG.Length);
            }
        }
        public static void RoomRain_CritSmash_Hook(On.RoomRain.orig_CreatureSmashedInGround orig, RoomRain instance, Creature crit, float speed)
        {
            if (instance.room.abstractRoom.shelter && !instance.room.world.brokenShelters[instance.room.abstractRoom.shelterIndex]) return;
            orig(instance, crit, speed);
        }
        public static void SoundLoop_Update_Hook(On.DynamicSoundLoop.orig_Update orig, DynamicSoundLoop instance)
        {
            if (instance.owner is RoomRain && thesearespeshul.ContainsKey(instance.owner as RoomRain))
            {
                instance.Volume /= 2.5f;  
                if (!thesearespeshul[instance.owner as RoomRain]) instance.Pitch *= 0.75f;
            }
            orig(instance);
        }
        public static void VirtualMicrophone_Update_Hook(On.VirtualMicrophone.orig_Update orig, VirtualMicrophone instance)
        {
            orig(instance);
            if (instance.room.roomRain != null && thesearespeshul.ContainsKey(instance.room.roomRain))
            {
                instance.volumeGroups[0] = 0.6f;
            }
        }
        public static IntVector2 Room_ShortcutEntDirection_Hook(On.Room.orig_ShorcutEntranceHoleDirection orig, Room instance, IntVector2 orpos)
        {
            IntVector2 origres = orig(instance, orpos);
            
            foreach (PlacedObject pobj in instance.roomSettings.placedObjects)
            {
                if (IntVector2.FromVector2(pobj.pos / 20) == orpos)
                {
                    if (pobj.type == EnumExt_PCO.ShelterDoorShift)
                    {
                        return RWCustom.Custom.fourDirections[(pobj.data as ShelderDoorShiftData).dir];

                    }
                    else if (pobj.type == EnumExt_PCO.Fakedoor)
                    {
                        return Custom.fourDirections[(pobj.data as FakeDoorData).dir];
                    }
                }
            }
            return origres;
            
        }
        public static void Water_Update_Hook(On.Water.orig_Update orig, Water instance)
        {
            bool sflag = instance.room.abstractRoom.shelter && !instance.room.world.brokenShelters[instance.room.abstractRoom.shelterIndex];
            float oldri = instance.room.roomSettings.RumbleIntensity;
            if (sflag)
            {
                instance.room.roomSettings.RumbleIntensity = 0f;
            }
            orig(instance);
            if (sflag)
            {
                instance.room.roomSettings.RumbleIntensity = oldri;
            }
        }
        
        public static BetterShelters instance;
        public static Dictionary<ShelterDoor, float> OldClosedFac;
        public static Dictionary<Player, int> CustomSleepCounter;
        public static Dictionary<Player, Room> PrevRooms;
        public static Dictionary<RoomRain, bool> thesearespeshul;

        public static void ShelterDoor_Update_Hook(On.ShelterDoor.orig_Update orig, ShelterDoor instance, bool eu)
        {
            if (OldClosedFac.ContainsKey(instance)) OldClosedFac[instance] = instance.closedFac;
            orig(instance, eu);
        }
        public static void CustomSleepCtrUpdate(Player player)
        {
            if (player.touchedNoInputCounter == 0) CustomSleepCounter[player] = 0;
            bool checkcrawling = (player.bodyMode == Player.BodyModeIndex.Crawl);
            bool checkfloating = (player.bodyMode == Player.BodyModeIndex.ZeroG);
            if (!BSHCMOI.BSHSettings.Use_AS_Alterations)
            {
                checkcrawling = true;
                checkfloating = true;
            }
            if (player.grabbedBy.Count > 0 || (!checkfloating && !checkcrawling))
            {
                CustomSleepCounter[player] = 0;
            }
            CustomSleepCounter[player]++;

        }

        public static float DETOUR_RR_RUC(RRDEL orig, RoomRain instance)
        {
            if (instance.room.abstractRoom != null && instance.room.abstractRoom.shelter) return 0f; 
            return orig.Invoke(instance);
        }
        public static float DETOUR_RR_GETINSIDEPUSHAROUND(RRDEL orig, RoomRain instance)
        {
            try
            {
                if (instance.room.abstractRoom != null && instance.room.abstractRoom.shelter) return 0f;
                return orig.Invoke(instance);
            }
            catch (System.NullReferenceException nrf)
            {
                Debug.Log(nrf);
                return 0f;
            }
            //return 0f;
        }
        public static float DETOUR_RR_GETOUTSIDEPUSHAROUND(RRDEL orig, RoomRain instance)
        {
            try
            {
                if (instance.room.abstractRoom != null && instance.room.abstractRoom.shelter) return 0f;
                return orig.Invoke(instance);
            }
            catch (System.NullReferenceException nrf)
            {
                Debug.Log(nrf);
                return 0f;
            }
        }

        public delegate float RRDEL(RoomRain instance);
        public static OptionInterface LoadOI()
        {
            return new BSHCMOI();
        }
        public override void OnEnable()
        {
            base.OnEnable();
            On.Player.Update += new On.Player.hook_Update(Player_Update_Hook);
            On.Player.ctor += new On.Player.hook_ctor(Player_ctor_hook);
            On.Player.SpitOutOfShortCut += new On.Player.hook_SpitOutOfShortCut(Player_SpitOutShortcut_hook);
            On.Creature.SuckedIntoShortCut += new On.Creature.hook_SuckedIntoShortCut(Creature_suckedIntoShortcut_hook);
            
            On.Room.Loaded += new On.Room.hook_Loaded(Room_Loaded_Hook);
            On.Room.ShorcutEntranceHoleDirection += new On.Room.hook_ShorcutEntranceHoleDirection(Room_ShortcutEntDirection_Hook);

            On.PlacedObject.GenerateEmptyData += new On.PlacedObject.hook_GenerateEmptyData(PO_GenerateEmptyData_Hook);
            
            On.DevInterface.ObjectsPage.CreateObjRep += new On.DevInterface.ObjectsPage.hook_CreateObjRep(objectsPage_CreateObjectRep_Hook);
            
            On.RainWorldGame.ctor += new On.RainWorldGame.hook_ctor(RWG_ctor_Hook);
            
            On.ShelterDoor.Update += new On.ShelterDoor.hook_Update(ShelterDoor_Update_Hook);
            On.ShelterDoor.Close += new On.ShelterDoor.hook_Close(Door_Close_Hook);

            //On.RoomRain.ctor += new On.RoomRain.hook_ctor(RoomRain_Ctor_Hook);
            On.RoomRain.ThrowAroundObjects += new On.RoomRain.hook_ThrowAroundObjects(RoomRain_ThrowAround_Hook);
            On.RoomRain.CreatureSmashedInGround += new On.RoomRain.hook_CreatureSmashedInGround(RoomRain_CritSmash_Hook);
            IDetour detIPA = new Hook(typeof(RoomRain).GetProperty("InsidePushAround").GetGetMethod(), typeof(BetterShelters).GetMethod(nameof(DETOUR_RR_GETINSIDEPUSHAROUND)));
            IDetour detOPA = new Hook(typeof(RoomRain).GetProperty("OutsidePushAround").GetGetMethod(), typeof(BetterShelters).GetMethod(nameof(DETOUR_RR_GETOUTSIDEPUSHAROUND)));
            IDetour HkRR_RUC = new Hook(typeof(RoomRain).GetProperty("RainUnderCeilings").GetGetMethod(), typeof(BetterShelters).GetMethod(nameof(DETOUR_RR_RUC)));

            On.DynamicSoundLoop.Update += new On.DynamicSoundLoop.hook_Update(SoundLoop_Update_Hook);
            On.VirtualMicrophone.Update += new On.VirtualMicrophone.hook_Update(VirtualMicrophone_Update_Hook);
            On.Water.Update += new On.Water.hook_Update(Water_Update_Hook);
        }
    }
}