using OptionalUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BSH
{
    public class BSHCMOI : OptionInterface
    {
        public BSHCMOI() : base(BetterShelters.instance)
        {

        }

        public override void Initialize()
        {
            base.Initialize();
            this.Tabs = new OpTab[1];
            this.Tabs[0] = new OpTab("BSH");
            OpCheckBox checkAutosleepAlt = new OpCheckBox(new UnityEngine.Vector2(40f, 500f), "BSH_UseAutoSleepAlt", true)
            {
                description = "Changes autosleep behaviour, requiring you to lie down in a shelter to hibernate. Zero-G shelters are unaffected."
            };
            OpLabel labelAutosleepAlt = new OpLabel(70f, 502f, "Alternative Autoleep");
            OpSlider draggerCustomSleepTicks = new OpSlider(new UnityEngine.Vector2(50f, 450f), "BSH_CustomSleepTicks", new RWCustom.IntVector2(20, 100))
            {
                description = "Sets the autosleep delay in ticks. Default value: 20 (0.5 seconds)."
            };
            OpLabel labelCustomSleepTicks = new OpLabel(150f, 455f, "Ticks to sleep");

            Tabs[0].AddItems(checkAutosleepAlt, draggerCustomSleepTicks, labelAutosleepAlt, labelCustomSleepTicks);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

        public override void ConfigOnChange()
        {
            base.ConfigOnChange();
            BSHSettings.Use_AS_Alterations = bool.Parse(config["BSH_UseAutoSleepAlt"]);
            BSHSettings.CustomTicksToSleep = int.Parse(config["BSH_CustomSleepTicks"]);
        }

        public static class BSHSettings
        {
            public static bool Use_AS_Alterations = true;
            public static int CustomTicksToSleep = 40;
        }
    }
}
