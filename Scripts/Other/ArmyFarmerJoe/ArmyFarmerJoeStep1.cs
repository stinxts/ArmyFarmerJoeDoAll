/*
name: Army FarmerJoe Step 1
description: Test runner for FarmerJoe level 1-30, Bracket 30, Bracket 50, and Step 2C (Brackets 55, 60, and 65) with Army synchronization.
tags: army, farmerjoe, test
*/
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/CoreStory.cs
//cs_include Scripts/CoreDailies.cs
//cs_include Scripts/CoreArmyFJ.cs
//cs_include Scripts/Story/Tutorial.cs
//cs_include Scripts/Story/Mazumi.cs
//cs_include Scripts/Story/Lair.cs
//cs_include Scripts/Other/FreeBoosts/FreeBoostsQuest(10mns)[Rng].cs
//cs_include Scripts/Other/ArmyFarmerJoe/ArmyFarmerJoeDoAll.cs
using System.Collections.Generic;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class ArmyFarmerJoeStep1
{
    private static readonly ArmyFarmerJoeDoAll Harness = new();
    private CoreBots Core => CoreBots.Instance;

    public string OptionsStorage = "ArmyFarmerJoeStep1";
    public bool DontPreconfigure = true;
    public List<IOption> Options = Harness.Options;

    private const string SyncFileName = "ArmyFarmerJoeStep1.sync";

    public void ScriptMain(IScriptInterface Bot)
    {
        Core.SetOptions();
        try
        {
            if (!Harness.StartArmySync(SyncFileName))
                return;

            if (!Harness.RunLevel1To30())
                return;

            if (!Harness.RunBracket30())
                return;

            if (!Harness.RunBracket50())
                return;

            if (!Harness.RunBracket55())
                return;

            if (!Harness.RunBracket60())
                return;

            if (!Harness.RunBracket65())
                return;

            if (!Harness.RunSellStarterClasses())
                return;

            if (!Harness.RunFarmerJoeOutfit())
                return;

            if (Harness.IsPlayer(1))
            {
                if (Harness.StopArmySync("STEP2C_COMPLETE"))
                    Core.Logger("Army FarmerJoe: player 1 published STEP2C_COMPLETE.");
                return;
            }

            Harness.WaitForArmyStop();
        }
        finally
        {
            Core.SetOptions(false);
        }
    }
}
