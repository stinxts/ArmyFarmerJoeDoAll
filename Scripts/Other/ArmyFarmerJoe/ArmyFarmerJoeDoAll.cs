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
using System;
using System.Collections.Generic;
using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using Skua.Core.Options;

public class ArmyFarmerJoeDoAll
{
    private static readonly CoreArmyFJ Army = new();
    private static readonly CoreAdvanced Adv = new();
    private static readonly CoreFarms Farm = new();
    private static readonly CoreStory Story = new();
    private static readonly CoreDailies Daily = new();
    private static readonly Tutorial Tutorial = new();
    private static readonly Mazumi Mazumi = new();
    private static readonly Lair Lair = new();
    private static readonly FreeBoosts Boosts = new();

    private const int Rank10ClassPoints = 302500;

    public enum PetChoice
    {
        None,
        HotMama,
        Akriloth,
    }

    private static IScriptInterface Bot => IScriptInterface.Instance;

    public string OptionsStorage = "ArmyFarmerJoeStep1";
    public bool DontPreconfigure = true;
    public List<IOption> Options = new()
    {
        Army.player1,
        Army.player2,
        Army.player3,
        Army.player4,
        Army.player5,
        Army.player6,
        Army.player7,
        new Option<string>(
            "RoomNumber",
            "Army Private Room Number",
            "Required. Shared private room number (1001-99999) so all Army accounts join the exact same map instance. Do not leave as 00000.",
            "00000"
        ),
        new Option<bool>(
            "OutFit",
            "Get a Pre-Made Outfit, Curtious of the Community",
            "We are farmers, bum ba dum bum bum bum bum",
            false
        ),
        new Option<bool>(
            "EquipOutfit",
            "Equip our the FarmerJoe outfit at the end?",
            "Yay or Nay",
            false
        ),
        new Option<bool>(
            "SellStarterClasses",
            "SellStarterClasses",
            "Yay or Nay",
            false
        ),
        new Option<bool>(
            "GetBoosts",
            "GetBoosts",
            "Do the \"Free Boosts\" quests to get some 10minute boosts (the droprate is < 1% so itll take awhile)",
            false
        ),
        new Option<PetChoice>(
            "PetChoice",
            "Choose Your Pet",
            "Extra stuff to choose",
            PetChoice.None
        ),
        CoreBots.Instance.SkipOptions,
    };

    public bool StartArmySync(string syncFileName)
    {
        if (Bot.Config == null)
        {
            CoreBots.Instance.Logger(
                "Army player configuration is unavailable.",
                "ArmyFarmerJoeDoAll"
            );
            return false;
        }

        int playerCount = 0;
        bool foundEmptySlot = false;
        for (int playerNumber = 1; playerNumber <= 7; playerNumber++)
        {
            string playerName = Bot.Config.Get<string>($"player{playerNumber}");
            if (string.IsNullOrWhiteSpace(playerName))
            {
                foundEmptySlot = true;
                continue;
            }

            if (foundEmptySlot)
            {
                CoreBots.Instance.Logger(
                    "Army player slots must be filled consecutively starting at Player 1.",
                    "ArmyFarmerJoeDoAll"
                );
                return false;
            }

            playerCount++;
        }

        if (playerCount < 4)
        {
            CoreBots.Instance.Logger(
                "Army FarmerJoe requires at least 4 players. Players 5-7 are optional.",
                "ArmyFarmerJoeDoAll"
            );
            return false;
        }

        if (!ApplyArmyRoomSettings())
            return false;

        return Army.StartArmySync(syncFileName, playerCount);
    }

    private bool ApplyArmyRoomSettings()
    {
        string? rawRoom = Bot.Config?.Get<string>("RoomNumber")?.Trim();
        int roomNumber = int.TryParse(rawRoom, out int parsedRoom) ? parsedRoom : 0;

        if (!Army.ValidatePrivateRoomNumber(roomNumber))
            return false;

        CoreBots core = CoreBots.Instance;
        core.PrivateRooms = true;
        core.PrivateRoomNumber = roomNumber;
        core.PublicDifficult = false;
        return true;
    }

    public bool RunLevel1To30()
    {
        if (!Army.SyncArmy("STEP1_START"))
            return false;

        ApplyArmyRoomSettings();
        CoreBots.Instance.Logger("Army FarmerJoe: starting level 1-30 on all accounts.");
        if (!RunBeginnerItems())
            return false;

        if (IScriptInterface.Instance.ShouldExit)
            return false;

        CoreBots.Instance.Logger(
            "Class points may be desynced at Rank 9, if you are stuck at rank 9, please stop the bot & relog if this happens"
        );
        return Army.SyncArmy("STEP1_COMPLETE");
    }

    private bool RunBeginnerItems()
    {
        if (
            !RunCooperativeObjective(
                "BEGINNER_INITIAL_CHECK",
                () => true,
                () => { },
                (targetPlayerNumber, targetPlayerName) => { }
            )
        )
            return false;

        CoreBots.Instance.Logger(
            "Running Step 1 (Level 1-30) with strict per-step verification on all accounts."
        );

        // 1. Tutorial badges
        bool tutorialComplete = false;
        if (
            !RunCooperativeObjective(
                "BEGINNER_TUTORIAL",
                () => tutorialComplete,
                () =>
                {
                    Tutorial.Badges();
                    tutorialComplete = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    // Tutorial badges are non-combat; helpers just wait.
                    Bot.Sleep(1000);
                }
            )
        )
            return false;

        // 2. Basic equipment
        bool basicEquipmentComplete = false;
        Action equipBasicItems = () =>
        {
            LeaveCombatRoomForEquipmentChange();
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                InventoryItem? equippedItem = Bot.Inventory.Items.Find(item =>
                    item.Equipped && item.Category == category
                );

                switch (category)
                {
                    case ItemCategory.Helm:
                        if (equippedItem == null)
                        {
                            CoreBots.Instance.BuyItem("classhalla", 299, "Battle Oracle Hood");
                            Bot.Wait.ForItemBuy();
                            CoreBots.Instance.Equip("Battle Oracle Hood");
                        }
                        break;

                    case ItemCategory.Cape:
                        if (equippedItem == null)
                        {
                            CoreBots.Instance.BuyItem("classhalla", 299, "Battle Oracle Wings");
                            Bot.Wait.ForItemBuy();
                            CoreBots.Instance.Equip("Battle Oracle Wings");
                        }
                        break;

                    case ItemCategory.Staff:
                    case ItemCategory.Axe:
                    case ItemCategory.Bow:
                    case ItemCategory.HandGun:
                    case ItemCategory.Gauntlet:
                    case ItemCategory.Dagger:
                    case ItemCategory.Rifle:
                    case ItemCategory.Sword:
                    case ItemCategory.Whip:
                    case ItemCategory.Wand:
                    case ItemCategory.Mace:
                    case ItemCategory.Polearm:
                        ItemBase? defaultWeapon = Bot.Inventory.Items.Find(item =>
                            item.Name.StartsWith("Default")
                        );

                        if (
                            defaultWeapon != null
                            && CoreBots.Instance.CheckInventory(defaultWeapon.ID)
                        )
                        {
                            CoreBots.Instance.BuyItem(
                                "classhalla",
                                299,
                                "Battle Oracle Battlestaff"
                            );
                            Bot.Wait.ForItemBuy();
                            CoreBots.Instance.Equip("Battle Oracle Battlestaff");
                            Bot.Shops.SellItem(defaultWeapon.ID);
                            Bot.Wait.ForTrue(
                                () => !Bot.Inventory.Contains(defaultWeapon.ID),
                                20
                            );
                        }
                        break;
                }
            }

            basicEquipmentComplete = !Bot.ShouldExit;
        };

        if (
            !RunCooperativeObjective(
                "BEGINNER_BASIC_EQUIPMENT",
                () => basicEquipmentComplete,
                equipBasicItems,
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 2b. Oracle (free 0 AC Healer/Support class in /classhalla shop 299 — bought early so Player 2 can heal from Level 1!)
        bool HasOracle() => HasClassInInventory("Oracle");
        bool HasOracleRank10() => HasOracle() && HasRank10Class("Oracle");

        if (
            !RunCooperativeObjective(
                "BEGINNER_ORACLE_BUY",
                HasOracle,
                () =>
                {
                    if (HasOracle())
                        return;

                    CoreBots.Instance.Logger("Getting Starter Support/Healer class (Oracle - 0 AC)");
                    LeaveCombatRoomForEquipmentChange();
                    CoreBots.Instance.BuyItem("classhalla", 299, "Oracle");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        SetClass();

        // 3. Level 10
        CoreBots.Instance.Logger("Starting out acc:\n\tGoals: Oracle, Ninja & Mage Classes");
        if (
            !RunCooperativeObjective(
                "BEGINNER_LEVEL_10",
                () => Bot.Player.Level >= 10,
                () =>
                {
                    if (Bot.Player.Level >= 10)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    CoreBots.Instance.EquipClass(ClassType.Farm);
                    Farm.Experience(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bone Berserker");
                }
            )
        )
            return false;

        // 3b. Rank Oracle to 10 right after Level 10 so our Healer is full power for Rogue/Ninja/Mazumi
        if (
            !RunCooperativeObjective(
                "BEGINNER_ORACLE_RANK_10",
                HasOracleRank10,
                () =>
                {
                    if (HasOracleRank10())
                        return;

                    RankUpClassForArmy("Oracle");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        SetClass();

        // 4. Rogue
        CoreBots.Instance.Logger("Getting Started: Beginner Levels/Equipment");
        bool HasRogue() => HasAnyClassInInventory("Rogue (Rare)", "Rogue");

        if (
            !RunCooperativeObjective(
                "BEGINNER_ROGUE",
                HasRogue,
                () =>
                {
                    if (HasRogue())
                        return;

                    CoreBots.Instance.Logger(
                        "Getting rogue.. so we can get ninja (there's a 10k hp \"boss\" to kill during the \"Hit Job\" quest.)"
                    );
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("classhalla", 172, "Rogue");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 5. Ninja (split into granular cooperative barriers)
        bool HasNinja() => HasAnyClassInInventory("Assassin", "Ninja Warrior", "Ninja");

        bool HasRank10Rogue() =>
            HasRank10Class("Rogue") || HasRank10Class("Rogue (Rare)");

        // 5a. Rank up Rogue to 10
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_RANK_ROGUE",
                () => HasNinja() || HasRank10Rogue(),
                () =>
                {
                    if (HasNinja() || HasRank10Rogue())
                        return;

                    CoreBots.Instance.Logger("Getting starter Dodge class (Ninja) - Ranking Rogue");
                    string rogueClass = HasClassInInventory("Rogue (Rare)")
                        ? "Rogue (Rare)"
                        : "Rogue";
                    RankUpClassForArmy(rogueClass);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 5b. Level to 25 + SetClass
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_LEVEL_25",
                () => Bot.Player.Level >= 25,
                () =>
                {
                    if (Bot.Player.Level >= 25)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    CoreBots.Instance.EquipClass(ClassType.Farm);
                    Farm.Experience(25);
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 5c. Mazumi quests (coordinated quest by quest and monster by monster)
        if (!RunCoordinatedMazumiQuests(HasNinja))
            return false;

        // 5d. Buy Ninja
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_BUY",
                HasNinja,
                () =>
                {
                    if (HasNinja())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    CoreBots.Instance.BuyItem("classhalla", 178, "Ninja");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 5e. Rank Ninja to 10 (all accounts rank up Ninja; 1st to finish becomes Support Helper)
        bool HasRank10Ninja() =>
            HasRank10Class("Ninja") || HasRank10Class("Assassin") || HasRank10Class("Ninja Warrior");

        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_RANK_10",
                HasRank10Ninja,
                () =>
                {
                    if (HasRank10Ninja())
                        return;

                    string ninjaClass =
                        HasClassInInventory("Assassin") ? "Assassin"
                        : HasClassInInventory("Ninja Warrior") ? "Ninja Warrior"
                        : "Ninja";
                    RankUpClassForArmy(ninjaClass);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        SetClass();

        // 6. Mage
        bool HasMage() => HasAnyClassInInventory("Mage (Rare)", "Mage");

        bool HasRank10Mage() =>
            HasRank10Class("Mage") || HasRank10Class("Mage (Rare)");

        if (
            !RunCooperativeObjective(
                "BEGINNER_MAGE",
                HasMage,
                () =>
                {
                    if (HasMage())
                        return;

                    CoreBots.Instance.Logger("Getting Starter Farm class (Mage)");
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("classhalla", 174, 15653, shopItemID: 9845);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6b. Rank Mage to 10 (all accounts rank up Mage; 1st to finish becomes Support Helper)
        if (
            !RunCooperativeObjective(
                "BEGINNER_MAGE_RANK_10",
                HasRank10Mage,
                () =>
                {
                    if (HasRank10Mage())
                        return;

                    string mageClass = HasClassInInventory("Mage (Rare)")
                        ? "Mage (Rare)"
                        : "Mage";
                    RankUpClassForArmy(mageClass);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 8. Final SetClass (resolves party role Solo/Support/Debuff + Farm + Dodge and ranks to 10)
        bool finalClassSetComplete = false;
        if (
            !RunCooperativeObjective(
                "BEGINNER_FINAL_SET_CLASS",
                () => finalClassSetComplete,
                () =>
                {
                    SetClass(rankUpIfNeeded: true);
                    finalClassSetComplete = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    private bool RunCoordinatedMazumiQuests(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;
        Story.PreLoad(Mazumi);

        // 5c-1. Quest 90: Ninja Grudge (Fishman Soldier in /pirates)
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_MAZUMI_90",
                () => skipCondition() || core.isCompletedBefore(92) || core.isCompletedBefore(90),
                () =>
                {
                    if (skipCondition() || core.isCompletedBefore(92) || core.isCompletedBefore(90))
                        return;

                    SetClass();
                    Story.KillQuest(90, "pirates", "Fishman Soldier");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Fishman Soldier");
                }
            )
        )
            return false;

        // 5c-2. Quest 91: Without a Trace (5 targets in /greenguardwest)
        bool IsQuest91Done() =>
            skipCondition() || core.isCompletedBefore(92) || core.isCompletedBefore(91);

        (string Suffix, string Monster, string Item, int Quant, string? Cell, string? Pad)[] q91Targets =
        {
            ("KITTARIAN", "Kittarian", "Kittarian's Wallet", 2, null, null),
            ("FISHMAN", "River Fishman", "River Fishman's Wallet", 2, null, null),
            ("SLIME", "Slime", "Slime-Soaked Wallet", 2, null, null),
            ("FROGZARD", "Frogzard", "Frogzard's Lint Hoard", 2, "West3", "Up"),
            ("BOAR", "Big Bad Boar", "Big Bad Boar's Wallet", 1, "West12", "Up"),
        };

        foreach (var target in q91Targets)
        {
            if (
                !RunCooperativeObjective(
                    $"BEGINNER_NINJA_MAZUMI_91_{target.Suffix}",
                    () => IsQuest91Done() || core.CheckInventory(target.Item, target.Quant),
                    () =>
                    {
                        if (IsQuest91Done() || core.CheckInventory(target.Item, target.Quant))
                            return;

                        SetClass();
                        core.EnsureAccept(91);
                        if (target.Cell != null && target.Pad != null)
                            core.KillMonster("greenguardwest", target.Cell, target.Pad, target.Monster, target.Item, target.Quant);
                        else
                            core.HuntMonster("greenguardwest", target.Monster, target.Item, target.Quant);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        Army.CoordinateFollowAndCombat(targetPlayerName, target.Monster);
                    }
                )
            )
                return false;
        }

        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_MAZUMI_91_COMPLETE",
                IsQuest91Done,
                () =>
                {
                    if (IsQuest91Done())
                        return;

                    core.EnsureComplete(91);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 5c-3. Quest 92: Hit Job (2 10k HP bosses in /greenguardwest)
        bool IsQuest92Done() => skipCondition() || core.isCompletedBefore(92);

        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_MAZUMI_92_BREKEN",
                () => IsQuest92Done() || core.CheckInventory("Breken the Vile's Sword", 1),
                () =>
                {
                    if (IsQuest92Done() || core.CheckInventory("Breken the Vile's Sword", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(92);
                    core.HuntMonster("greenguardwest", "Breken the Vile", "Breken the Vile's Sword", 1);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Breken the Vile");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_MAZUMI_92_OGUG",
                () => IsQuest92Done() || core.CheckInventory("Ogug Stoneaxe's Helmet", 1),
                () =>
                {
                    if (IsQuest92Done() || core.CheckInventory("Ogug Stoneaxe's Helmet", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(92);
                    core.HuntMonster("greenguardwest", "Ogug Stoneaxe", "Ogug Stoneaxe's Helmet", 1);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Ogug Stoneaxe");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_MAZUMI_92_COMPLETE",
                IsQuest92Done,
                () =>
                {
                    if (IsQuest92Done())
                        return;

                    core.EnsureComplete(92);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    #region Step 2A: Bracket 30
    public bool RunBracket30()
    {
        if (!Army.SyncArmy("STEP2A_START"))
            return false;

        ApplyArmyRoomSettings();
        CoreBots core = CoreBots.Instance;
        core.Logger("Army FarmerJoe: starting Step 2A (Bracket 30) on all accounts.");

        // 0. Initial SetClass setup
        bool initialSetupDone = false;
        if (
            !RunCooperativeObjective(
                "BRACKET30_INITIAL_SETUP",
                () => initialSetupDone,
                () =>
                {
                    SetClass();
                    initialSetupDone = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 1. Elders' Blood Daily (if Level >= 30 and daily available)
        if (
            !RunCooperativeObjective(
                "BRACKET30_ELDERS_BLOOD",
                () =>
                    Bot.Player.Level < 30
                    || core.CheckInventory("Elders' Blood", 20)
                    || !Daily.CheckDailyv2(802, true, true, "Elders' Blood"),
                () =>
                {
                    if (
                        Bot.Player.Level >= 30
                        && !core.CheckInventory("Elders' Blood", 20)
                        && Daily.CheckDailyv2(802, true, true, "Elders' Blood")
                    )
                    {
                        Daily.EldersBlood();
                    }
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Gorillaphant");
                },
                maxWorkAttempts: 2
            )
        )
            return false;

        // 2. GetBoosts (only if enabled in options)
        bool getBoostsEnabled = Bot.Config?.Get<bool>("GetBoosts") == true;
        bool boostsDone = false;
        if (
            !RunCooperativeObjective(
                "BRACKET30_GET_BOOSTS",
                () => !getBoostsEnabled || boostsDone,
                () =>
                {
                    if (!getBoostsEnabled)
                    {
                        boostsDone = true;
                        return;
                    }

                    Farm.GetBoost("REP", 10, true);
                    Farm.GetBoost("XP", Bot.Player.Level >= 100 ? 0 : 10, true);
                    Boosts.GetBoostsSelect(10, 10, 0);
                    boostsDone = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 3. Level 30 + SmartEnhance
        bool enhancedBracket30 = Bot.Player.Level > 75;
        if (
            !RunCooperativeObjective(
                "BRACKET30_LEVEL_30",
                () => Bot.Player.Level >= 30 && enhancedBracket30,
                () =>
                {
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);

                    if (
                        !enhancedBracket30
                        || Bot.Inventory.Items.Any(x =>
                            x != null && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        Adv.SmartEnhance(Bot.Player.CurrentClass?.Name ?? string.Empty);
                        enhancedBracket30 = true;
                    }

                    Farm.Experience(30);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 4. Master Ranger (Sandsea Rep 10 -> Buy & Rank 10)
        bool HasMasterRanger() => HasClassInInventory("Master Ranger");
        bool HasMasterRangerRank10() => HasMasterRanger() && HasRank10Class("Master Ranger");

        // 4a. Sandsea Rep 10
        if (
            !RunCooperativeObjective(
                "BRACKET30_MR_SANDSEA_REP",
                () => HasMasterRanger() || Farm.FactionRank("Sandsea") >= 10,
                () =>
                {
                    if (HasMasterRanger() || Farm.FactionRank("Sandsea") >= 10)
                        return;

                    if (core.CheckInventory("Venom Head"))
                        core.SellItem("Venom Head");

                    core.Logger("Level 30: Farming Sandsea Rep 10 for Master Ranger");
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.SandseaREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 4b. Buy Master Ranger
        if (
            !RunCooperativeObjective(
                "BRACKET30_MR_BUY",
                HasMasterRanger,
                () =>
                {
                    if (HasMasterRanger())
                        return;

                    if (core.CheckInventory("Venom Head"))
                        core.SellItem("Venom Head");

                    core.Logger("Level 30: Buying Master Ranger");
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("sandsea", 242, 7260, shopItemID: 5682);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 4c. Rank Master Ranger to 10 & SetClass
        if (
            !RunCooperativeObjective(
                "BRACKET30_MR_RANK_10",
                HasMasterRangerRank10,
                () =>
                {
                    if (HasMasterRangerRank10())
                        return;

                    RankUpClassForArmy("Master Ranger");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 5. Dragonslayer (Galanoth 4 quests coordinated 1 by 1 -> Buy -> Rank 10)
        bool HasDragonslayer() => HasClassInInventory("Dragonslayer");
        bool HasDragonslayerRank10() => HasDragonslayer() && HasRank10Class("Dragonslayer");

        Story.PreLoad(Lair);
        (int QuestId, string Monster)[] galanothQuests =
        {
            (165, "Wyvern"),
            (166, "Bronze Draconian"),
            (167, "Dark Draconian"),
            (168, "Red Dragon"),
        };

        // 5a. Galanoth quests 165, 166, 167, 168 coordinated quest by quest
        foreach (var gQuest in galanothQuests)
        {
            if (
                !RunCooperativeObjective(
                    $"BRACKET30_DS_QUEST_{gQuest.QuestId}",
                    () =>
                        HasDragonslayer()
                        || core.isCompletedBefore(169)
                        || core.isCompletedBefore(gQuest.QuestId),
                    () =>
                    {
                        if (
                            HasDragonslayer()
                            || core.isCompletedBefore(169)
                            || core.isCompletedBefore(gQuest.QuestId)
                        )
                            return;

                        SetClass();
                        Story.KillQuest(gQuest.QuestId, "lair", gQuest.Monster);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        Army.CoordinateFollowAndCombat(targetPlayerName, gQuest.Monster);
                    }
                )
            )
                return false;
        }

        // 5b. Buy Dragonslayer
        if (
            !RunCooperativeObjective(
                "BRACKET30_DS_BUY",
                HasDragonslayer,
                () =>
                {
                    if (HasDragonslayer())
                        return;

                    core.Logger("Level 30: Buying Dragonslayer");
                    LeaveCombatRoomForEquipmentChange();
                    core.BuyItem("lair", 38, "Dragonslayer");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 5c. Rank Dragonslayer to 10 & SetClass
        if (
            !RunCooperativeObjective(
                "BRACKET30_DS_RANK_10",
                HasDragonslayerRank10,
                () =>
                {
                    if (HasDragonslayerRank10())
                        return;

                    RankUpClassForArmy("Dragonslayer");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 5d. Imperial Chunin (Yokai Rep 10 in /dragonkoiz — No boss, Pockey Chew only ~1k HP, gives Boss Hit-Chance Debuffer for Player 3)
        bool HasImperialChunin() => HasClassInInventory("Imperial Chunin");
        bool HasImperialChuninRank10() =>
            HasImperialChunin() && HasRank10Class("Imperial Chunin");

        if (
            !RunCooperativeObjective(
                "BRACKET30_CHUNIN_YOKAI_REP",
                () => HasImperialChunin() || Farm.FactionRank("Yokai") >= 10,
                () =>
                {
                    if (HasImperialChunin() || Farm.FactionRank("Yokai") >= 10)
                        return;

                    core.Logger("Level 30: Farming Yokai Rep 10 for Imperial Chunin");
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.YokaiREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Pockey Chew");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET30_CHUNIN_BUY",
                HasImperialChunin,
                () =>
                {
                    if (HasImperialChunin())
                        return;

                    core.Logger("Level 30: Buying Imperial Chunin");
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("dragonkoiz", 95, "Imperial Chunin");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET30_CHUNIN_RANK_10",
                HasImperialChuninRank10,
                () =>
                {
                    if (HasImperialChuninRank10())
                        return;

                    RankUpClassForArmy("Imperial Chunin");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 5e. Shaman & Evolved Shaman (Arcangrove Rep 10 in /arcangrove — No boss, Gorillaphant & Seed Spitter ~2k HP, unlocks 2 unique classes for Player 4 & Player 5 from the same Rep!)
        bool HasShaman() => HasClassInInventory("Shaman");
        bool HasShamanRank10() => HasShaman() && HasRank10Class("Shaman");
        bool HasEvolvedShaman() => HasClassInInventory("Evolved Shaman");
        bool HasEvolvedShamanRank10() =>
            HasEvolvedShaman() && HasRank10Class("Evolved Shaman");

        if (
            !RunCooperativeObjective(
                "BRACKET30_SHAMAN_ARCANGROVE_REP",
                () =>
                    (HasShaman() && HasEvolvedShaman())
                    || Farm.FactionRank("Arcangrove") >= 10,
                () =>
                {
                    if (
                        (HasShaman() && HasEvolvedShaman())
                        || Farm.FactionRank("Arcangrove") >= 10
                    )
                        return;

                    core.Logger("Level 30: Farming Arcangrove Rep 10 for Shaman & Evolved Shaman");
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.ArcangroveREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET30_SHAMAN_BUY",
                HasShaman,
                () =>
                {
                    if (HasShaman())
                        return;

                    core.Logger("Level 30: Buying Shaman");
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("arcangrove", 214, 5765, shopItemID: 4799);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET30_SHAMAN_RANK_10",
                HasShamanRank10,
                () =>
                {
                    if (HasShamanRank10())
                        return;

                    RankUpClassForArmy("Shaman");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        if (
            !RunCooperativeObjective(
                "BRACKET30_EVOLVED_SHAMAN_BUY",
                HasEvolvedShaman,
                () =>
                {
                    if (HasEvolvedShaman())
                        return;

                    core.Logger("Level 30: Buying Evolved Shaman (same Arcangrove Rep 10)");
                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("arcangrove", 214, "Evolved Shaman", shopItemID: 6396);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET30_EVOLVED_SHAMAN_RANK_10",
                HasEvolvedShamanRank10,
                () =>
                {
                    if (HasEvolvedShamanRank10())
                        return;

                    RankUpClassForArmy("Evolved Shaman");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 6. Blade of Awe Progression (5 UnlockBoA quests coordinated 1 by 1 -> BoA Rep 10 -> Awethur's Accoutrements)
        core.Logger("Level 30: Farming Blade of Awe Rep for enhancements & sword");

        // 6a-1. Quest 2933: Find the Stonewrit! (Sketchy Dragon in /j6)
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_2933_STONEWRIT",
                () => core.isCompletedBefore(2937) || core.isCompletedBefore(2933),
                () =>
                {
                    if (core.isCompletedBefore(2937) || core.isCompletedBefore(2933))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(2933);
                    core.HuntMonster("j6", "Sketchy Dragon", "Stonewrit Found!", isTemp: false);
                    core.EnsureComplete(2933);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Sketchy Dragon");
                }
            )
        )
            return false;

        // 6a-2. Quest 2934: Find the Handle! (Fire Elemental in /gilead)
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_2934_HANDLE",
                () => core.isCompletedBefore(2937) || core.isCompletedBefore(2934),
                () =>
                {
                    if (core.isCompletedBefore(2937) || core.isCompletedBefore(2934))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(2934);
                    core.HuntMonster("gilead", "Fire Elemental", "Handle Found!", isTemp: false);
                    core.EnsureComplete(2934);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Fire Elemental");
                }
            )
        )
            return false;

        // 6a-3. Quest 2935: Find the Hilt! (Skeletal Viking in /castleundead)
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_2935_HILT",
                () => core.isCompletedBefore(2937) || core.isCompletedBefore(2935),
                () =>
                {
                    if (core.isCompletedBefore(2937) || core.isCompletedBefore(2935))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(2935);
                    core.HuntMonster("castleundead", "Skeletal Viking", "Hilt Found!", isTemp: false);
                    core.EnsureComplete(2935);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Skeletal Viking");
                }
            )
        )
            return false;

        // 6a-4. Quest 2936: Find the Blade! (Hydra Head in /hydra)
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_2936_BLADE",
                () => core.isCompletedBefore(2937) || core.isCompletedBefore(2936),
                () =>
                {
                    if (core.isCompletedBefore(2937) || core.isCompletedBefore(2936))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(2936);
                    core.HuntMonster("hydra", "Hydra Head", "Blade Found!", isTemp: false);
                    core.EnsureComplete(2936);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Hydra Head");
                }
            )
        )
            return false;

        // 6a-5. Quest 2937: Find the Runes! (Escherion in /escherion)
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_2937_RUNES",
                () => core.isCompletedBefore(2937),
                () =>
                {
                    if (core.isCompletedBefore(2937))
                        return;

                    SetClass();
                    core.ResetQuest(2937);
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(2937);
                    core.KillEscherion("Runes Found!");
                    core.EnsureComplete(2937);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 6b. Blade of Awe Rep 10 & Blade of Awe purchase
        if (
            !RunCooperativeObjective(
                "BRACKET30_BOA_REP_10",
                () => Farm.FactionRank("Blade of Awe") >= 10 && core.CheckInventory(17585, toInv: false),
                () =>
                {
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.BladeofAweREP(10, farmBoA: true);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Skeletal Viking");
                }
            )
        )
            return false;

        // 6c. Buy Awethur's Accoutrements
        if (
            !RunCooperativeObjective(
                "BRACKET30_AWETHUR_BUY",
                () => core.CheckInventory("Awethur's Accoutrements", toInv: false),
                () =>
                {
                    if (core.CheckInventory("Awethur's Accoutrements", toInv: false))
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("museum", 631, "Awethur's Accoutrements");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 7. Unlock Forge Helm Enhancement (Quest 8828 coordinated boss by boss)
        bool IsForgeHelmDone() => core.isCompletedBefore(8828);

        // 7a. Blacksmithing Rep 4
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_BLACKSMITH_REP_4",
                () => IsForgeHelmDone() || Farm.FactionRank("Blacksmithing") >= 4,
                () =>
                {
                    if (IsForgeHelmDone() || Farm.FactionRank("Blacksmithing") >= 4)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.BlacksmithingREP(4, false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 7b-1. Quest 8828 Boss 1: Escherion (1st Lord Of Chaos Staff)
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_HELM_ESCHERION",
                () => IsForgeHelmDone() || core.CheckInventory("1st Lord Of Chaos Staff", 1),
                () =>
                {
                    if (IsForgeHelmDone() || core.CheckInventory("1st Lord Of Chaos Staff", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(8828);
                    core.KillEscherion("1st Lord Of Chaos Staff");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 7b-2. Quest 8828 Boss 2: Vath (Chaos Dragonlord Axe)
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_HELM_VATH",
                () => IsForgeHelmDone() || core.CheckInventory("Chaos Dragonlord Axe", 1),
                () =>
                {
                    if (IsForgeHelmDone() || core.CheckInventory("Chaos Dragonlord Axe", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(8828);
                    core.KillVath("Chaos Dragonlord Axe");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 7b-3. Quest 8828 Boss 3: Kitsune (Hanzamune Dragon Koi Blade)
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_HELM_KITSUNE",
                () => IsForgeHelmDone() || core.CheckInventory("Hanzamune Dragon Koi Blade", 1),
                () =>
                {
                    if (IsForgeHelmDone() || core.CheckInventory("Hanzamune Dragon Koi Blade", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(8828);
                    core.KillKitsune("Hanzamune Dragon Koi Blade");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 7b-4. Quest 8828 Boss 4: Wolfwing (Wrath of the Werepyre)
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_HELM_WOLFWING",
                () => IsForgeHelmDone() || core.CheckInventory("Wrath of the Werepyre", 1),
                () =>
                {
                    if (IsForgeHelmDone() || core.CheckInventory("Wrath of the Werepyre", 1))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(8828);
                    core.HuntMonster("wolfwing", "Wolfwing", "Wrath of the Werepyre", isTemp: false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Wolfwing");
                }
            )
        )
            return false;

        // 7b-5. Complete Quest 8828
        if (
            !RunCooperativeObjective(
                "BRACKET30_FORGE_HELM_COMPLETE",
                IsForgeHelmDone,
                () =>
                {
                    if (IsForgeHelmDone())
                        return;

                    core.EnsureComplete(8828);
                    core.Logger("Enhancement Unlocked: Forge (Helm)");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        return Army.SyncArmy("STEP2A_COMPLETE");
    }
    #endregion Step 2A: Bracket 30

    #region Step 2B: Bracket 50
    public bool RunBracket50()
    {
        if (!Army.SyncArmy("STEP2B_START"))
            return false;

        ApplyArmyRoomSettings();
        CoreBots core = CoreBots.Instance;
        core.Logger("Army FarmerJoe: starting Step 2B (Bracket 50) on all accounts.");

        // 1. Level 50 + SmartEnhance
        bool enhancedBracket50 = Bot.Player.Level > 75;
        if (
            !RunCooperativeObjective(
                "BRACKET50_LEVEL_50",
                () => Bot.Player.Level >= 50 && enhancedBracket50,
                () =>
                {
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);

                    if (
                        !enhancedBracket50
                        || Bot.Inventory.Items.Any(x =>
                            x != null && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        Adv.SmartEnhance(Bot.Player.CurrentClass?.Name ?? string.Empty);
                        enhancedBracket50 = true;
                    }

                    Farm.Experience(50);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 2. Scarlet Sorceress (ShatterGlassMaze -> TowerOfMirrors -> Blood Sorceress drop -> 1 CP + unequip -> Quest 6236 -> Rank 10)
        bool HasScarletSorceress() => HasClassInInventory("Scarlet Sorceress");
        bool HasScarletSorceressRank10() =>
            HasScarletSorceress() && HasRank10Class("Scarlet Sorceress");

        // 2a. Coordinated ShatterGlassMaze (/hedgemaze, Quests 5298-5313)
        if (!RunCoordinatedShatterGlassMaze(HasScarletSorceress))
            return false;

        // 2b. Coordinated TowerOfMirrors (/towerofmirrors, Quests 5314-5332)
        if (!RunCoordinatedTowerOfMirrors(HasScarletSorceress))
            return false;

        // 2c. Drop Blood Sorceress (ItemID 36298) from Scarletta in /towerofmirrors
        if (
            !RunCooperativeObjective(
                "BRACKET50_SS_BLOOD_DROP",
                () => HasScarletSorceress() || HasClassInInventory(36298),
                () =>
                {
                    if (HasScarletSorceress() || HasClassInInventory(36298))
                        return;

                    core.Logger("Level 50: Farming Blood Sorceress drop from Scarletta");
                    core.AddDrop(36298, 42992);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.KillMonster(
                        "towerofmirrors",
                        "r16",
                        "Top",
                        "Scarletta",
                        "Blood Sorceress",
                        isTemp: false
                    );
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Scarletta");
                }
            )
        )
            return false;

        // 2d. Upgrade Blood Sorceress (36298) -> Scarlet Sorceress (42992) via Quest 6236
        // Wiki requirements:
        // 1) Blood Sorceress must have >= 1 Class Point (iQty >= 1) in inventory.
        // 2) Blood Sorceress MUST be unequipped when turning in Quest 6236 (the quest consumes it).
        if (
            !RunCooperativeObjective(
                "BRACKET50_SS_UPGRADE_6236",
                HasScarletSorceress,
                () =>
                {
                    if (HasScarletSorceress())
                        return;

                    if (!HasClassInInventory(36298))
                        return;

                    core.AddDrop(42992);

                    InventoryItem? bloodSorc = Bot.Inventory.Items.FirstOrDefault(x =>
                        x != null && x.ID == 36298
                    );
                    if (bloodSorc != null && bloodSorc.Quantity < 1)
                    {
                        core.Logger(
                            "Level 50: Getting 1 Class Point on Blood Sorceress for Quest 6236"
                        );
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance("Blood Sorceress");
                        core.Equip("Blood Sorceress");
                        Bot.Wait.ForTrue(() => Bot.Inventory.IsEquipped("Blood Sorceress"), 20);
                        Bot.Skills.StartAdvanced("Blood Sorceress", false, ClassUseMode.Base);
                        core.KillMonster("towerofmirrors", "r2", "Left", "*", log: false);
                    }

                    // Ensure Blood Sorceress is UNEQUIPPED before turning in Quest 6236
                    if (
                        Bot.Inventory.IsEquipped("Blood Sorceress")
                        || string.Equals(
                            Bot.Player.CurrentClass?.Name,
                            "Blood Sorceress",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        SetClass();
                        if (
                            string.Equals(
                                Bot.Player.CurrentClass?.Name,
                                "Blood Sorceress",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            string fallbackClass =
                                new[] { "Oracle", "Mage", "Rogue", "Healer", "Warrior" }
                                    .FirstOrDefault(HasClassInInventory)
                                ?? string.Empty;
                            if (!string.IsNullOrEmpty(fallbackClass))
                                core.Equip(fallbackClass);
                        }
                    }

                    core.Logger("Level 50: Turning in Quest 6236 for Scarlet Sorceress");
                    core.EnsureAccept(6236);
                    core.EnsureComplete(6236);
                    Bot.Wait.ForPickup(42992);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                },
                maxWorkAttempts: 4
            )
        )
            return false;

        // 2e. Rank Scarlet Sorceress to 10 & SetClass
        if (
            !RunCooperativeObjective(
                "BRACKET50_SS_RANK_10",
                HasScarletSorceressRank10,
                () =>
                {
                    if (HasScarletSorceressRank10())
                        return;

                    RankUpClassForArmy("Scarlet Sorceress");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 3. Dragonslayer General (30k Gold + 75 Enchanted Scale via Quest 5294 + 100 Dragon Claw in /dragontown -> Buy -> Rank 10)
        bool HasDSG() => HasClassInInventory(35996);
        bool HasDSGRank10() => HasDSG() && HasRank10Class("Dragonslayer General");

        // 3a. 30,000 Gold for Dragonslayer General
        if (
            !RunCooperativeObjective(
                "BRACKET50_DSG_GOLD",
                () => HasDSG() || Bot.Player.Gold >= 30000,
                () =>
                {
                    if (HasDSG() || Bot.Player.Gold >= 30000)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.Gold(30000);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 3b. Farm 75 Enchanted Scale (Quest 5294) & 100 Dragon Claw from Tempest Dracolich in /dragontown
        if (
            !RunCooperativeObjective(
                "BRACKET50_DSG_MATS",
                () =>
                    HasDSG()
                    || (
                        core.CheckInventory("Enchanted Scale", 75)
                        && core.CheckInventory("Dragon Claw", 100)
                    ),
                () =>
                {
                    if (
                        HasDSG()
                        || (
                            core.CheckInventory("Enchanted Scale", 75)
                            && core.CheckInventory("Dragon Claw", 100)
                        )
                    )
                        return;

                    // Quest 5294 requires Dragonslayer Rank 10 in inventory
                    core.Logger(
                        "Level 50: Farming Enchanted Scale (75) & Dragon Claw (100) for Dragonslayer General"
                    );
                    core.AddDrop("Enchanted Scale", "Dragon Claw");
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.RegisterQuests(5294);
                    while (
                        !Bot.ShouldExit
                        && (
                            !core.CheckInventory("Enchanted Scale", 75)
                            || !core.CheckInventory("Dragon Claw", 100)
                        )
                    )
                    {
                        core.KillMonster(
                            "dragontown",
                            "r4",
                            "Right",
                            "Tempest Dracolich",
                            log: false
                        );
                    }
                    core.CancelRegisteredQuests();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tempest Dracolich");
                }
            )
        )
            return false;

        // 3c. Buy Dragonslayer General
        if (
            !RunCooperativeObjective(
                "BRACKET50_DSG_BUY",
                HasDSG,
                () =>
                {
                    if (HasDSG())
                        return;

                    core.Logger("Level 50: Buying Dragonslayer General");
                    LeaveCombatRoomForEquipmentChange();
                    core.BuyItem("dragontown", 1286, 35996, shopItemID: 3937);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3d. Rank Dragonslayer General to 10 & SetClass
        if (
            !RunCooperativeObjective(
                "BRACKET50_DSG_RANK_10",
                HasDSGRank10,
                () =>
                {
                    if (HasDSGRank10())
                        return;

                    RankUpClassForArmy("Dragonslayer General");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;
        SetClass();

        // 4. Burning Blade (+15% All Damage weapon from Diabolical Warlord in /lostruinswar)
        bool HasBurningBlade() =>
            core.CheckInventory("Burning Blade", toInv: false)
            || core.CheckInventory("Burning Blade of Abezeth", toInv: false);

        if (
            !RunCooperativeObjective(
                "BRACKET50_BURNING_BLADE",
                HasBurningBlade,
                () =>
                {
                    if (HasBurningBlade())
                        return;

                    core.Logger("Level 50: Farming Burning Blade (+15% All Damage) from Diabolical Warlord");
                    core.AddDrop("Burning Blade");
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.KillMonster(
                        "lostruinswar",
                        "r7",
                        "Left",
                        "Diabolical Warlord",
                        "Burning Blade",
                        isTemp: false
                    );
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Diabolical Warlord");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET50_BURNING_BLADE_EQUIP",
                () =>
                    core.CheckInventory("Burning Blade of Abezeth", toInv: false)
                    || !core.CheckInventory("Burning Blade", toInv: true)
                    || Bot.Inventory.IsEquipped("Burning Blade"),
                () =>
                {
                    if (
                        core.CheckInventory("Burning Blade of Abezeth", toInv: false)
                        || !core.CheckInventory("Burning Blade", toInv: true)
                        || Bot.Inventory.IsEquipped("Burning Blade")
                    )
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    core.Equip("Burning Blade");
                    Adv.SmartEnhance(Bot.Player.CurrentClass?.Name ?? string.Empty);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 2
            )
        )
            return false;

        return Army.SyncArmy("STEP2B_COMPLETE");
    }

    private bool RunCoordinatedShatterGlassMaze(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;
        core.EnsureLoad(Enumerable.Range(5298, 16).ToArray());

        bool IsHedgeQuestDone(int questId) =>
            skipCondition()
            || core.isCompletedBefore(5332)
            || core.isCompletedBefore(5313)
            || core.isCompletedBefore(questId);

        // Quest 5298: Find the Hedge Maze
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5298",
                () => IsHedgeQuestDone(5298),
                () =>
                {
                    if (IsHedgeQuestDone(5298))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5298);
                    core.GetMapItem(4678, 1, "hedgemaze");
                    core.HuntMonster(
                        "hedgemaze",
                        "Knight's Reflection",
                        "Knight's Reflection Dispersed",
                        1
                    );
                    core.EnsureComplete(5298);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Knight's Reflection");
                }
            )
        )
            return false;

        // Quest 5299: Clear the Hedges
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5299",
                () => IsHedgeQuestDone(5299),
                () =>
                {
                    if (IsHedgeQuestDone(5299))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5299);
                    core.HuntMonster("hedgemaze", "Mirrored Shard", "Mirror Shard Slain", 4);
                    core.HuntMonsterMapID("hedgemaze", 48, "Hedge Goblin Slain", 4);
                    core.HuntMonsterMapID("hedgemaze", 20, "Minotaur Slain", 3);
                    Story.MapItemQuest(5299, "hedgemaze", 4679);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // Quest 5300: Un-Mirror the Knights
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5300",
                () => IsHedgeQuestDone(5300),
                () =>
                {
                    if (IsHedgeQuestDone(5300))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5300, "hedgemaze", "Knight's Reflection");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Knight's Reflection");
                }
            )
        )
            return false;

        // Quest 5301: Find the Goblin Camp
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5301",
                () => IsHedgeQuestDone(5301),
                () =>
                {
                    if (IsHedgeQuestDone(5301))
                        return;

                    Story.MapItemQuest(5301, "hedgemaze", 4680);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5302:Wait, Those Aren't Redcaps!
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5302",
                () => IsHedgeQuestDone(5302),
                () =>
                {
                    if (IsHedgeQuestDone(5302))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5302, "hedgemaze", "Hedge Goblin");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Hedge Goblin");
                }
            )
        )
            return false;

        // Quest 5303: Pick the Glass Flowers
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5303",
                () => IsHedgeQuestDone(5303),
                () =>
                {
                    if (IsHedgeQuestDone(5303))
                        return;

                    Story.MapItemQuest(5303, "hedgemaze", 4681, 12);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5304: Polish the Mirror Shards
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5304",
                () => IsHedgeQuestDone(5304),
                () =>
                {
                    if (IsHedgeQuestDone(5304))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5304, "hedgemaze", "Mirrored Shard");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Mirrored Shard");
                }
            )
        )
            return false;

        // Quest 5305: Hone the Guerilla Tactic
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5305",
                () => IsHedgeQuestDone(5305),
                () =>
                {
                    if (IsHedgeQuestDone(5305))
                        return;

                    Story.MapItemQuest(5305, "hedgemaze", 4682);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5306: Slay the Minotaur Prime (Boss)
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5306_MINOTAUR_PRIME",
                () => IsHedgeQuestDone(5306),
                () =>
                {
                    if (IsHedgeQuestDone(5306))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5306, "hedgemaze", "Minotaur Prime");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Minotaur Prime");
                }
            )
        )
            return false;

        // Quests 5307, 5308, 5309: MapItems 4683, 4684, 4685 x5
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5307_5309",
                () => IsHedgeQuestDone(5309),
                () =>
                {
                    if (IsHedgeQuestDone(5309))
                        return;

                    if (!IsHedgeQuestDone(5307))
                        Story.MapItemQuest(5307, "hedgemaze", 4683);
                    if (!IsHedgeQuestDone(5308))
                        Story.MapItemQuest(5308, "hedgemaze", 4684);
                    if (!IsHedgeQuestDone(5309))
                        Story.MapItemQuest(5309, "hedgemaze", 4685, 5);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5310: Defend the Maze
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5310",
                () => IsHedgeQuestDone(5310),
                () =>
                {
                    if (IsHedgeQuestDone(5310))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5310);
                    core.KillMonster("hedgemaze", "r21", "Right", "*", "Maze Monster Killed", 8);
                    core.EnsureComplete(5310);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // Quest 5311: Find the Center
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5311",
                () => IsHedgeQuestDone(5311),
                () =>
                {
                    if (IsHedgeQuestDone(5311))
                        return;

                    Story.MapItemQuest(5311, "hedgemaze", 4686);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5312: Shattered Knight (54.3k HP Boss)
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5312_SHATTERED_KNIGHT",
                () => IsHedgeQuestDone(5312),
                () =>
                {
                    if (IsHedgeQuestDone(5312))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5312, "hedgemaze", "Shattered Knight");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shattered Knight");
                }
            )
        )
            return false;

        // Quest 5313: Resurrected Minotaur (79.1k HP Boss)
        if (
            !RunCooperativeObjective(
                "BRACKET50_HEDGEMAZE_5313_RESURRECTED_MINOTAUR",
                () => IsHedgeQuestDone(5313),
                () =>
                {
                    if (IsHedgeQuestDone(5313))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5313, "hedgemaze", "Resurrected Minotaur");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Resurrected Minotaur");
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    private bool RunCoordinatedTowerOfMirrors(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;
        core.EnsureLoad(Enumerable.Range(5314, 19).ToArray());

        bool IsTowerQuestDone(int questId) =>
            skipCondition()
            || core.isCompletedBefore(5332)
            || core.isCompletedBefore(questId);

        // Quest 5314: Drink Me
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5314_DRINK_ME",
                () => IsTowerQuestDone(5314),
                () =>
                {
                    if (IsTowerQuestDone(5314))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5314);
                    core.HuntMonster("towerofmirrors", "Glassgoyle", "Glass Powder", 8);
                    core.HuntMonster("towerofmirrors", "Glass Serpent", "Reflective Venom", 4);
                    core.EnsureComplete(5314);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // Quest 5315: The Key To Success
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5315_KEY_SUCCESS",
                () => IsTowerQuestDone(5315),
                () =>
                {
                    if (IsTowerQuestDone(5315))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5315);
                    core.GetMapItems(new[] { (4691, 1), (4692, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Mirror Key", 1);
                    core.EnsureComplete(5315);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental");
                }
            )
        )
            return false;

        // Quest 5316: Phanatics
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5316_PHANATICS",
                () => IsTowerQuestDone(5316),
                () =>
                {
                    if (IsTowerQuestDone(5316))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5316);
                    core.HuntMonster("towerofmirrors", "Phans", "Mosh Past the Phans", 8);
                    core.HuntMonster("towerofmirrors", "Phans", "Backstage Pass", 1);
                    core.EnsureComplete(5316);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Phans");
                }
            )
        )
            return false;

        // Quest 5317: But I Have A Backstage Pass! (Boss: Lathannos the Reversed, MapID 36)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5317_LATHANNOS",
                () => IsTowerQuestDone(5317),
                () =>
                {
                    if (IsTowerQuestDone(5317))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5317);
                    core.HuntMonsterMapID(
                        "towerofmirrors",
                        36,
                        "Lathannos the Reversed Defeated"
                    );
                    core.EnsureComplete(5317);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "36");
                }
            )
        )
            return false;

        // Quest 5318: True Love
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5318_TRUE_LOVE",
                () => IsTowerQuestDone(5318),
                () =>
                {
                    if (IsTowerQuestDone(5318))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5318);
                    core.GetMapItems(new[] { (4687, 1), (4693, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Andres' Mirror Key", 1);
                    core.EnsureComplete(5318);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental");
                }
            )
        )
            return false;

        // Quest 5319: Turn to the Left
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5319_TURN_LEFT",
                () => IsTowerQuestDone(5319),
                () =>
                {
                    if (IsTowerQuestDone(5319))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5319);
                    core.HuntMonster("towerofmirrors", "Runway Wraith", "Runway Wraith Defeated", 6);
                    core.HuntMonster("towerofmirrors", "Runway Wraith", "Avant-Garde Footwear", 1);
                    core.HuntMonster("towerofmirrors", "Runway Wraith", "Retro Wig", 1);
                    core.HuntMonster(
                        "towerofmirrors",
                        "Runway Wraith",
                        "Someone's Wearable Art Project",
                        1
                    );
                    core.EnsureComplete(5319);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Runway Wraith");
                }
            )
        )
            return false;

        // Quest 5320: Now Turn to the Right (Boss: Lukrisio the Buffed, MapID 40)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5320_LUKRISIO",
                () => IsTowerQuestDone(5320),
                () =>
                {
                    if (IsTowerQuestDone(5320))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5320);
                    core.HuntMonsterMapID("towerofmirrors", 40, "Lukrisio the Buffed Defeated");
                    core.EnsureComplete(5320);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "40");
                }
            )
        )
            return false;

        // Quest 5321: Or Maybe THIS Is True Love
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5321_MAYBE_TRUE_LOVE",
                () => IsTowerQuestDone(5321),
                () =>
                {
                    if (IsTowerQuestDone(5321))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5321);
                    core.GetMapItems(new[] { (4688, 1), (4694, 1) }, "towerofmirrors");
                    core.HuntMonster(
                        "towerofmirrors",
                        "Silver Elemental",
                        "Gisele's Mirror Key",
                        1
                    );
                    core.EnsureComplete(5321);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental");
                }
            )
        )
            return false;

        // Quest 5322: Those Harpies!
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5322_HARPIES",
                () => IsTowerQuestDone(5322),
                () =>
                {
                    if (IsTowerQuestDone(5322))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5322);
                    core.HuntMonster("towerofmirrors", "Pageant Mom", "Pageant Mom Defeated", 8);
                    core.HuntMonster("towerofmirrors", "Pageant Mom", "Pageant Sash", 1);
                    core.EnsureComplete(5322);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Pageant Mom");
                }
            )
        )
            return false;

        // Quest 5323: Drink your Go-Go Juice (Boss: Mendeskar the Smudged, MapID 45)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5323_MENDESKAR",
                () => IsTowerQuestDone(5323),
                () =>
                {
                    if (IsTowerQuestDone(5323))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5323);
                    core.HuntMonsterMapID("towerofmirrors", 45, "Mendeskar the Smudged Defeated");
                    core.EnsureComplete(5323);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "45");
                }
            )
        )
            return false;

        // Quest 5324: Oh Sure, Why Not
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5324_SURE_WHY_NOT",
                () => IsTowerQuestDone(5324),
                () =>
                {
                    if (IsTowerQuestDone(5324))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5324);
                    core.GetMapItems(new[] { (4689, 1), (4695, 1) }, "towerofmirrors");
                    core.HuntMonster(
                        "towerofmirrors",
                        "Silver Elemental",
                        "Emilia's Mirror Key",
                        1
                    );
                    core.EnsureComplete(5324);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental");
                }
            )
        )
            return false;

        // Quest 5325: Behind the Scenes
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5325_BEHIND_SCENES",
                () => IsTowerQuestDone(5325),
                () =>
                {
                    if (IsTowerQuestDone(5325))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5325);
                    core.HuntMonster("towerofmirrors", "Stage Tech", "Stage Tech Defeated", 9);
                    core.HuntMonster("towerofmirrors", "Stage Tech", "Rope Ladder");
                    core.EnsureComplete(5325);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Stage Tech");
                }
            )
        )
            return false;

        // Quest 5326: In the Spotlight (Boss: Atticus the Warped, MapID 49)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5326_ATTICUS",
                () => IsTowerQuestDone(5326),
                () =>
                {
                    if (IsTowerQuestDone(5326))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5326);
                    core.HuntMonsterMapID("towerofmirrors", 49, "Atticus the Warped Defeated");
                    core.EnsureComplete(5326);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "49");
                }
            )
        )
            return false;

        // Quest 5327: Oh, I Give Up
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5327_I_GIVE_UP",
                () => IsTowerQuestDone(5327),
                () =>
                {
                    if (IsTowerQuestDone(5327))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5327);
                    core.GetMapItems(new[] { (4690, 1), (4696, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Dora's Mirror Key", 1);
                    core.EnsureComplete(5327);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental");
                }
            )
        )
            return false;

        // Quest 5328: We Gotta Wendi-GO
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5328_WENDIGO",
                () => IsTowerQuestDone(5328),
                () =>
                {
                    if (IsTowerQuestDone(5328))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5328);
                    core.HuntMonster("towerofmirrors", "Sasquatch", "Sasquatches Defeated", 10);
                    core.HuntMonster("towerofmirrors", "Sasquatch", "Tracking Tag", 5);
                    core.EnsureComplete(5328);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Sasquatch");
                }
            )
        )
            return false;

        // Quest 5329: Kick His- Wait, Did We Already Use That Pun? (Boss: Leofire the Shattered, MapID 52)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5329_LEOFIRE",
                () => IsTowerQuestDone(5329),
                () =>
                {
                    if (IsTowerQuestDone(5329))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5329);
                    core.HuntMonsterMapID("towerofmirrors", 52, "Leofire the Shattered Defeated");
                    core.EnsureComplete(5329);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "52");
                }
            )
        )
            return false;

        // Quest 5330: Fiend Zoned
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5330_FIEND_ZONED",
                () => IsTowerQuestDone(5330),
                () =>
                {
                    if (IsTowerQuestDone(5330))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5330, "towerofmirrors", "Fervent Suitor");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Fervent Suitor");
                }
            )
        )
            return false;

        // Quest 5331: Find Scarletta (Cutscene Cut4 -> Jump back to Enter)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5331_FIND_SCARLETTA",
                () => IsTowerQuestDone(5331),
                () =>
                {
                    if (IsTowerQuestDone(5331))
                        return;

                    Story.MapItemQuest(5331, "towerofmirrors", 4697);
                    Bot.Wait.ForCellChange("Cut4");
                    while (!Bot.ShouldExit && Bot.Player.Cell != "Enter")
                    {
                        core.Jump();
                        core.Sleep();
                    }
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // Quest 5332: Defeat ... Wait. What? (Final Boss: Scarletta / Groglurk, MapID 32)
        if (
            !RunCooperativeObjective(
                "BRACKET50_TOWER_5332_GROGLURK",
                () => IsTowerQuestDone(5332),
                () =>
                {
                    if (IsTowerQuestDone(5332))
                        return;

                    core.AddDrop("Blood Sorceress");
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(5332);
                    core.HuntMonsterMapID("towerofmirrors", 32, "Defeat the Groglurk");
                    core.EnsureComplete(5332);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "32");
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }
    #endregion Step 2B: Bracket 50

    #region Step 2C: Brackets 55, 60, and 65
    /// <summary>
    /// Step 2C (Part 1 - Bracket 55):
    /// 1. Level 55 + SmartEnhance
    /// 2. Blaze Binder (Pyromancer -> 1 CP -> DoomWood REP 10 -> Embersea REP 10 -> Blaze Binder Rank 10)
    /// 3. Cryomancer (Coordinated /frozentower 3907-3937 -> Daily 3966/3965 -> Buy & Rank 10 if 84x tokens reached)
    /// 4. Arachnomancer (Coordinated /twilightedge 3428 + /weaverwar 3429-3439 -> Ravenloss REP 10 -> Buy & Rank 10)
    /// </summary>
    public bool RunBracket55()
    {
        CoreBots core = CoreBots.Instance;
        ApplyArmyRoomSettings();

        // Entry barrier for Bracket 55
        if (!Army.SyncArmy("STEP2C_55_START"))
            return false;

        SetClass(rankUpIfNeeded: true);

        // 1. Level 55 + SmartEnhance
        if (
            !RunCooperativeObjective(
                "BRACKET55_LEVEL_55",
                () => Bot.Player.Level >= 55,
                () =>
                {
                    if (Bot.Player.Level >= 55)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (!string.IsNullOrWhiteSpace(currentClass))
                        Adv.SmartEnhance(currentClass);

                    Farm.Experience(55);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2. Blaze Binder (Pyromancer -> 1 CP -> DoomWood REP 10 -> Embersea REP 10 -> Blaze Binder Rank 10)
        bool HasBB() => HasClassInInventory("Blaze Binder");
        bool HasBBRank10() => HasBB() && HasRank10Class("Blaze Binder");
        bool HasPyro() => HasBB() || HasAnyClassInInventory(12811, 12812) || HasClassInInventory("Pyromancer");
        bool HasPyro1CP() =>
            HasBB()
            || Bot.Inventory.Items.Any(item =>
                item != null
                && (
                    item.ID == 12811
                    || item.ID == 12812
                    || string.Equals(item.Name?.Trim(), "Pyromancer", StringComparison.OrdinalIgnoreCase)
                )
                && item.Category == ItemCategory.Class
                && item.Quantity >= 1
            );

        // 2a. Farm 84x Shurpu Blaze Token from Shurpu Ring Guardian in /xancave (cell r9, Down)
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_PYRO_TOKENS",
                () => HasPyro() || core.CheckInventory("Shurpu Blaze Token", 84),
                () =>
                {
                    if (HasPyro() || core.CheckInventory("Shurpu Blaze Token", 84))
                        return;

                    core.AddDrop("Shurpu Blaze Token");
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.KillMonster(
                        "xancave",
                        "r9",
                        "Down",
                        "Shurpu Ring Guardian",
                        "Shurpu Blaze Token",
                        84,
                        isTemp: false
                    );
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shurpu Ring Guardian");
                }
            )
        )
            return false;

        // 2b. Buy Pyromancer (ItemID 12812) in /xancave Shop 447 and sell leftover tokens
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_PYRO_BUY",
                HasPyro,
                () =>
                {
                    if (HasPyro())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    core.BuyItem("xancave", 447, 12812, shopItemID: 1278);
                    Bot.Wait.ForItemBuy();
                    if (core.CheckInventory("Shurpu Blaze Token"))
                        core.SellItem("Shurpu Blaze Token", all: true);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 2c. Ensure Pyromancer has >= 1 Class Point (1 CP) so Firestorm Merge Shop (1140) accepts it
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_PYRO_1CP",
                HasPyro1CP,
                () =>
                {
                    if (HasPyro1CP())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    core.Equip("Pyromancer");
                    Bot.Wait.ForTrue(() => Bot.Inventory.IsEquipped("Pyromancer"), 20);
                    Bot.Skills.StartAdvanced("Pyromancer", false, ClassUseMode.Base);
                    core.KillMonster("noobshire", "Enter", "Spawn", "*", log: false);
                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2d. DoomWood REP Rank 10 in /shadowfallwar (Garden1, Left, Quests 1151, 1152, 1153)
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_DOOMWOOD_REP",
                () => HasBB() || Farm.FactionRank("DoomWood") >= 10,
                () =>
                {
                    if (HasBB() || Farm.FactionRank("DoomWood") >= 10)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.DoomWoodREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2e. Embersea REP Rank 10 in /fireforge (Quest 4228, Blazebinder MapID 4)
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_EMBERSEA_REP",
                () => HasBB() || Farm.FactionRank("Embersea") >= 10,
                () =>
                {
                    if (HasBB() || Farm.FactionRank("Embersea") >= 10)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.EmberseaREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2f. Buy Darkness Sigil, Flame Sigil, and Blaze Binder in /fireforge
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_BUY",
                HasBB,
                () =>
                {
                    if (HasBB())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    if (Bot.Inventory.IsEquipped("Pyromancer"))
                    {
                        SetClass();
                        core.EquipClass(ClassType.Solo);
                    }

                    Adv.BuyItem("fireforge", 1142, "Darkness Sigil");
                    Adv.BuyItem("fireforge", 1142, "Flame Sigil");
                    Adv.BuyItem("fireforge", 1140, "Blaze Binder");
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 2g. Rank Blaze Binder to Rank 10 and update SetClass()
        if (
            !RunCooperativeObjective(
                "BRACKET55_BB_RANK_10",
                HasBBRank10,
                () =>
                {
                    if (!HasBB())
                        return;

                    if (!HasBBRank10())
                        RankUpClassForArmy("Blaze Binder");

                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 3. Cryomancer (Coordinated /frozentower 3907-3937 -> Daily 3966/3965 -> Buy & Rank 10 if acquired)
        bool HasCryo() => HasClassInInventory("Cryomancer");
        bool HasCryoRank10() => HasCryo() && HasRank10Class("Cryomancer");
        bool IsCryoDailyDone() =>
            HasCryo()
            || (
                !Daily.CheckDailyv2(3966, true, true, "Glacera Ice Token")
                && (!core.IsMember || !Daily.CheckDailyv2(3965, true, true, "Glacera Ice Token"))
                && !core.CheckInventory("Glacera Ice Token", 84)
            );

        if (!RunCoordinatedFrozenTower(HasCryo))
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET55_CRYO_DAILY",
                IsCryoDailyDone,
                () =>
                {
                    if (IsCryoDailyDone())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Daily.Cryomancer();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET55_CRYO_RANK_10",
                () => !HasCryo() || HasCryoRank10(),
                () =>
                {
                    if (!HasCryo())
                        return;

                    if (!HasCryoRank10())
                        RankUpClassForArmy("Cryomancer");

                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 4. Arachnomancer (Coordinated /twilightedge 3428 + /weaverwar 3429-3439 -> Ravenloss REP 10 -> Buy & Rank 10)
        bool HasArach() => HasClassInInventory("Arachnomancer");
        bool HasArachRank10() => HasArach() && HasRank10Class("Arachnomancer");
        bool IsWeaverWarDone() =>
            HasArach() || Farm.FactionRank("Ravenloss") >= 10 || core.isCompletedBefore(3439);

        if (!RunCoordinatedWeaverWar(IsWeaverWarDone))
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET55_ARACH_RAVENLOSS_REP",
                () => HasArach() || Farm.FactionRank("Ravenloss") >= 10,
                () =>
                {
                    if (HasArach() || Farm.FactionRank("Ravenloss") >= 10)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.RavenlossREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "ChaosWeaver Mage");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET55_ARACH_BUY",
                HasArach,
                () =>
                {
                    if (HasArach())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    core.BuyItem("ravenloss", 850, "Arachnomancer", shopItemID: 14837);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET55_ARACH_RANK_10",
                HasArachRank10,
                () =>
                {
                    if (!HasArach())
                        return;

                    if (!HasArachRank10())
                        RankUpClassForArmy("Arachnomancer");

                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        SetClass(rankUpIfNeeded: true);
        return Army.SyncArmy("STEP2C_55_COMPLETE") && !Bot.ShouldExit;
    }

    /// <summary>
    /// Step 2C (Part 2 - Bracket 60):
    /// 1. Level 60 + SmartEnhance
    /// 2. DragonSoul Shinobi (Coordinated Yokai Story: /shogunwar, /shinringrove, /greenshell, /heiwavalley, /shadowfortress -> 300x Dragon Shinobi Token -> Buy & Rank 10)
    /// 3. Unlock Forge Weapon Enhancement: Lacerate (Quest 8739)
    /// </summary>
    public bool RunBracket60()
    {
        CoreBots core = CoreBots.Instance;
        ApplyArmyRoomSettings();

        // Entry barrier for Bracket 60
        if (!Army.SyncArmy("STEP2C_60_START"))
            return false;

        SetClass(rankUpIfNeeded: true);

        // 1. Level 60 + SmartEnhance
        if (
            !RunCooperativeObjective(
                "BRACKET60_LEVEL_60",
                () => Bot.Player.Level >= 60,
                () =>
                {
                    if (Bot.Player.Level >= 60)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (!string.IsNullOrWhiteSpace(currentClass))
                        Adv.SmartEnhance(currentClass);

                    Farm.Experience(60);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2. DragonSoul Shinobi (ItemID 59476)
        bool HasDSS() =>
            HasClassInInventory(59476) || HasClassInInventory("DragonSoul Shinobi");
        bool HasDSSRank10() => HasRank10Class("DragonSoul Shinobi");

        if (!RunCoordinatedYokaiStory(HasDSS))
            return false;

        // 2a. Farm 300x Dragon Shinobi Token (ItemID 20561) via Quest 7924 from 1st Head of Orochi in /shadowfortress
        if (
            !RunCooperativeObjective(
                "BRACKET60_DSS_TOKENS",
                () => HasDSS() || core.CheckInventory(20561, 300),
                () =>
                {
                    if (HasDSS() || core.CheckInventory(20561, 300))
                        return;

                    core.AddDrop(20561);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.FarmingLogger("Dragon Shinobi Token", 300);
                    core.RegisterQuests(7924);
                    while (!Bot.ShouldExit && !core.CheckInventory(20561, 300))
                        core.HuntMonster("shadowfortress", "1st Head of Orochi", log: false);
                    core.CancelRegisteredQuests();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "1st Head of Orochi");
                }
            )
        )
            return false;

        // 2b. Buy DragonSoul Shinobi (ItemID 59476) in /shadowfortress Shop 1968
        if (
            !RunCooperativeObjective(
                "BRACKET60_DSS_BUY",
                HasDSS,
                () =>
                {
                    if (HasDSS())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("shadowfortress", 1968, 59476, shopItemID: 8078);
                    Bot.Wait.ForItemBuy();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 2c. Rank DragonSoul Shinobi to Rank 10
        if (
            !RunCooperativeObjective(
                "BRACKET60_DSS_RANK_10",
                HasDSSRank10,
                () =>
                {
                    if (!HasDSS())
                        return;

                    if (!HasDSSRank10())
                        RankUpClassForArmy("DragonSoul Shinobi");

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 3. Unlock Forge Weapon Enhancement: Lacerate (Quest 8739)
        bool IsLacerateDone() => core.isCompletedBefore(8739);
        bool HasBlacksmithRank5() => IsLacerateDone() || Farm.FactionRank("Blacksmithing") >= 5;

        // 3a. Ensure Prerequisite Quest 92 (Mazumi) is completed
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_PREREQ_92",
                () => IsLacerateDone() || Bot.Quests.IsUnlocked(8739) || core.isCompletedBefore(92),
                () =>
                {
                    if (IsLacerateDone() || Bot.Quests.IsUnlocked(8739) || core.isCompletedBefore(92))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    if (!Bot.Quests.IsUnlocked(92))
                    {
                        if (!Bot.Quests.IsUnlocked(91))
                        {
                            core.EnsureAccept(90);
                            core.HuntMonster("pirates", "Shark Bait", "Pirate Medallion", 12);
                            core.EnsureComplete(90);
                        }
                        core.EnsureAccept(91);
                        core.KillMonster("greenguardwest", "West1", "Left", "Kittarian", "Kittarian's Wallet", 2);
                        core.KillMonster("greenguardwest", "West9", "Left", "River Fishman", "River Fishman's Wallet", 2);
                        core.KillMonster("greenguardwest", "West10", "Left", "Slime", "Slime-Soaked Wallet", 2);
                        core.KillMonster("greenguardwest", "West3", "Left", "Frogzard", "Frogzard's Lint Hoard", 2);
                        core.KillMonster("greenguardwest", "West12", "Up", "Big Bad Boar", "Big Bad Boar's Wallet");
                        core.EnsureComplete(91);
                    }
                    if (!core.isCompletedBefore(92))
                    {
                        core.EnsureAccept(92);
                        core.HuntMonster("greenguardwest", "Breken the Vile", "Breken the Vile's Sword", 1);
                        core.HuntMonster("greenguardwest", "Ogug Stoneaxe", "Ogug Stoneaxe's Helmet", 1);
                        core.EnsureComplete(92);
                    }
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 3b. Blacksmithing Rank 5 (Traditional Rank 4 via Quest 2777 + 6 shared rounds of Quest 8736)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BLACKSMITH_REP_4",
                () => HasBlacksmithRank5() || Farm.FactionRank("Blacksmithing") >= 4,
                () =>
                {
                    if (HasBlacksmithRank5() || Farm.FactionRank("Blacksmithing") >= 4)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.BlacksmithingREP(4, false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // Coordinate Quest 8736 (Intensive Metallic Mastery: 2,500 Rep per turn-in, 12,500 Rep from Rank 4 to Rank 5)
        // Split across its 3 maps per round so the entire army fights Hydra Head 25, Creature Creation, and Dread Klunk together!
        for (int round = 1; round <= 6; round++)
        {
            int currentRound = round;
            bool roundCompleted = false;

            if (
                !RunCooperativeObjective(
                    $"BRACKET60_LACERATE_BS8736_R{currentRound}_HYDRA",
                    () => HasBlacksmithRank5() || core.CheckInventory("Hydra Scale Piece", 75),
                    () =>
                    {
                        if (HasBlacksmithRank5() || core.CheckInventory("Hydra Scale Piece", 75))
                            return;

                        core.AddDrop("Hydra Scale Piece", "Creature Shard", "Monster Trophy");
                        core.EnsureAccept(8736);
                        SetClass();
                        core.EquipClass(ClassType.Solo);
                        core.HuntMonster(
                            "hydrachallenge",
                            "Hydra Head 25",
                            "Hydra Scale Piece",
                            75,
                            isTemp: false,
                            log: false
                        );
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        Army.CoordinateFollowAndCombat(targetPlayerName, "Hydra Head 25");
                    }
                )
            )
                return false;

            if (
                !RunCooperativeObjective(
                    $"BRACKET60_LACERATE_BS8736_R{currentRound}_MAUL",
                    () => HasBlacksmithRank5() || core.CheckInventory("Creature Shard", 1),
                    () =>
                    {
                        if (HasBlacksmithRank5() || core.CheckInventory("Creature Shard", 1))
                            return;

                        core.AddDrop("Hydra Scale Piece", "Creature Shard", "Monster Trophy");
                        core.EnsureAccept(8736);
                        SetClass();
                        core.EquipClass(ClassType.Solo);
                        core.HuntMonster(
                            "maul",
                            "Creature Creation",
                            "Creature Shard",
                            1,
                            isTemp: false,
                            log: false
                        );
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        Army.CoordinateFollowAndCombat(targetPlayerName, "Creature Creation");
                    }
                )
            )
                return false;

            if (
                !RunCooperativeObjective(
                    $"BRACKET60_LACERATE_BS8736_R{currentRound}_KLUNK",
                    () => HasBlacksmithRank5() || roundCompleted,
                    () =>
                    {
                        if (HasBlacksmithRank5() || roundCompleted)
                            return;

                        core.AddDrop("Hydra Scale Piece", "Creature Shard", "Monster Trophy");
                        core.EnsureAccept(8736);
                        SetClass();
                        core.EquipClass(ClassType.Solo);
                        core.HuntMonster(
                            "towerofdoom",
                            "Dread Klunk",
                            "Monster Trophy",
                            15,
                            isTemp: false,
                            log: false
                        );
                        core.EnsureComplete(8736);
                        roundCompleted = true;
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        Army.CoordinateFollowAndCombat(targetPlayerName, "Dread Klunk");
                    }
                )
            )
                return false;
        }

        // 3c. Coordinate the 6 required items for Quest 8739 (Lacerate)
        // Item 1: Undead Plague Spear (Big Jack Sprat in /graveyard)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_SPEAR",
                () => IsLacerateDone() || core.CheckInventory("Undead Plague Spear"),
                () =>
                {
                    if (IsLacerateDone() || core.CheckInventory("Undead Plague Spear"))
                        return;

                    core.AddDrop("Undead Plague Spear");
                    core.EnsureAccept(8739);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("graveyard", "Big Jack Sprat", "Undead Plague Spear", isTemp: false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Big Jack Sprat");
                }
            )
        )
            return false;

        // Item 2: Kuro's Wrath (Kuro in /river)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_KURO",
                () => IsLacerateDone() || core.CheckInventory("Kuro's Wrath"),
                () =>
                {
                    if (IsLacerateDone() || core.CheckInventory("Kuro's Wrath"))
                        return;

                    core.AddDrop("Kuro's Wrath");
                    core.EnsureAccept(8739);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("river", "Kuro", "Kuro's Wrath", isTemp: false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kuro");
                }
            )
        )
            return false;

        // Item 3: Massive Horc Cleaver (Quest 279 -> Horc Master in /warhorc)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_CLEAVER",
                () => IsLacerateDone() || core.CheckInventory("Massive Horc Cleaver"),
                () =>
                {
                    if (IsLacerateDone() || core.CheckInventory("Massive Horc Cleaver"))
                        return;

                    core.AddDrop("Massive Horc Cleaver");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(279);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("warhorc", "Horc Master", "Boss Prize");
                    core.EnsureComplete(279);
                    Bot.Wait.ForPickup("Massive Horc Cleaver");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Horc Master");
                }
            )
        )
            return false;

        // Item 4: Sword in the Stone (Quest 316 -> 7x MapItem 54 + Spider in /greenguardeast)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_SWORD_STONE",
                () => IsLacerateDone() || core.CheckInventory("Sword in the Stone"),
                () =>
                {
                    if (IsLacerateDone() || core.CheckInventory("Sword in the Stone"))
                        return;

                    core.AddDrop("Sword in the Stone");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(316);
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.GetMapItem(54, 7, "greenguardeast");
                    core.HuntMonster("greenguardeast", "Spider", "Tiny Sword");
                    core.EnsureComplete(316);
                    Bot.Wait.ForPickup("Sword in the Stone");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Spider");
                }
            )
        )
            return false;

        // Item 5: Forest Axe (Quest 301 -> 4x MapItem 55 + Mosquito in /farm)
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_FOREST_AXE",
                () => IsLacerateDone() || core.CheckInventory("Forest Axe"),
                () =>
                {
                    if (IsLacerateDone() || core.CheckInventory("Forest Axe"))
                        return;

                    core.AddDrop("Forest Axe");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(301);
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.GetMapItem(55, 4, "farm");
                    core.HuntMonster("farm", "Mosquito", "Mosquito Juice");
                    core.EnsureComplete(301);
                    Bot.Wait.ForPickup("Forest Axe");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Mosquito");
                }
            )
        )
            return false;

        // Item 6: Black Knight Orb (Quest 318 across 4 bosses)
        bool HasBKO() => IsLacerateDone() || core.CheckInventory("Black Knight Orb");

        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BKO_WELL",
                () => HasBKO() || Bot.TempInv.Contains("Black Knight Leg Piece"),
                () =>
                {
                    if (HasBKO() || Bot.TempInv.Contains("Black Knight Leg Piece"))
                        return;

                    core.AddDrop("Black Knight Orb");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(318);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("well", "Gell Oh No", "Black Knight Leg Piece");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Gell Oh No");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BKO_DRAGON",
                () => HasBKO() || Bot.TempInv.Contains("Black Knight Chest Piece"),
                () =>
                {
                    if (HasBKO() || Bot.TempInv.Contains("Black Knight Chest Piece"))
                        return;

                    core.AddDrop("Black Knight Orb");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(318);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("greendragon", "Greenguard Dragon", "Black Knight Chest Piece");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Greenguard Dragon");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BKO_DEATHGAZER",
                () => HasBKO() || Bot.TempInv.Contains("Black Knight Shoulder Piece"),
                () =>
                {
                    if (HasBKO() || Bot.TempInv.Contains("Black Knight Shoulder Piece"))
                        return;

                    core.AddDrop("Black Knight Orb");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(318);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("deathgazer", "Deathgazer", "Black Knight Shoulder Piece");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Deathgazer");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BKO_TRUNK",
                () => HasBKO() || Bot.TempInv.Contains("Black Knight Arm Piece"),
                () =>
                {
                    if (HasBKO() || Bot.TempInv.Contains("Black Knight Arm Piece"))
                        return;

                    core.AddDrop("Black Knight Orb");
                    core.EnsureAccept(8739);
                    core.EnsureAccept(318);
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.HuntMonster("trunk", "GreenGuard Basilisk", "Black Knight Arm Piece");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "GreenGuard Basilisk");
                }
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_BKO_COMPLETE",
                HasBKO,
                () =>
                {
                    if (HasBKO())
                        return;

                    core.AddDrop("Black Knight Orb");
                    core.EnsureComplete(318);
                    Bot.Wait.ForPickup("Black Knight Orb");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3d. Turn in Quest 8739 to unlock Lacerate
        if (
            !RunCooperativeObjective(
                "BRACKET60_LACERATE_COMPLETE",
                IsLacerateDone,
                () =>
                {
                    if (IsLacerateDone())
                        return;

                    core.EnsureAccept(8739);
                    core.EnsureComplete(8739);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        SetClass(rankUpIfNeeded: true);
        return Army.SyncArmy("STEP2C_60_COMPLETE") && !Bot.ShouldExit;
    }

    /// <summary>
    /// Step 2C (Part 3 - Bracket 65):
    /// 1. Level 65 + SmartEnhance
    /// 2. Glacial Berserker (Coordinated Glacera Story up to Quest 5601 -> Glacera REP 10 -> Buy & Rank 10 if Shop 1339 is active)
    /// </summary>
    public bool RunBracket65()
    {
        CoreBots core = CoreBots.Instance;
        ApplyArmyRoomSettings();

        // Entry barrier for Bracket 65
        if (!Army.SyncArmy("STEP2C_65_START"))
            return false;

        SetClass(rankUpIfNeeded: true);

        // 1. Level 65 + SmartEnhance
        if (
            !RunCooperativeObjective(
                "BRACKET65_LEVEL_65",
                () => Bot.Player.Level >= 65,
                () =>
                {
                    if (Bot.Player.Level >= 65)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (!string.IsNullOrWhiteSpace(currentClass))
                        Adv.SmartEnhance(currentClass);

                    Farm.Experience(65);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2. Glacial Berserker (ItemID 38084 or 38049)
        bool HasGB() =>
            HasAnyClassInInventory(38084, 38049) || HasClassInInventory("Glacial Berserker");
        bool HasGBRank10() => HasRank10Class("Glacial Berserker");
        bool IsGlaceraRepDone() => HasGB() || Farm.FactionRank("Glacera") >= 10;
        bool IsGlaceraStoryDone() => IsGlaceraRepDone() || core.isCompletedBefore(5601);

        if (!RunCoordinatedFrozenTower(IsGlaceraStoryDone))
            return false;

        if (!RunCoordinatedGlaceraForGB(IsGlaceraStoryDone))
            return false;

        // 2a. Glacera REP Rank 10 in /icewindwar (cell r5, Left, Quests 5597, 5598, 5599, 5600)
        if (
            !RunCooperativeObjective(
                "BRACKET65_GB_GLACERA_REP",
                IsGlaceraRepDone,
                () =>
                {
                    if (IsGlaceraRepDone())
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Farm.GlaceraREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 2b. Buy Glacial Berserker in /icewindpass Shop 1339 (Seasonal Frostval shop; skips cleanly if out-of-season)
        bool gbBuyAttempted = false;
        if (
            !RunCooperativeObjective(
                "BRACKET65_GB_BUY",
                () => HasGB() || gbBuyAttempted,
                () =>
                {
                    if (HasGB() || gbBuyAttempted)
                        return;

                    LeaveCombatRoomForEquipmentChange();
                    Adv.BuyItem("icewindpass", 1339, "Glacial Berserker", shopItemID: 22266);
                    Bot.Wait.ForItemBuy();
                    gbBuyAttempted = true;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 2c. Rank Glacial Berserker to Rank 10 if acquired
        if (
            !RunCooperativeObjective(
                "BRACKET65_GB_RANK_10",
                () => !HasGB() || HasGBRank10(),
                () =>
                {
                    if (!HasGB())
                        return;

                    if (!HasGBRank10())
                        RankUpClassForArmy("Glacial Berserker");

                    LeaveCombatRoomForEquipmentChange();
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        SetClass(rankUpIfNeeded: true);
        return Army.SyncArmy("STEP2C_COMPLETE") && !Bot.ShouldExit;
    }

    /// <summary>
    /// Coordinates /frozentower Quests 3907-3937 (Slot 185, Value 1..31) for Cryomancer and Glacera Story.
    /// </summary>
    private bool RunCoordinatedFrozenTower(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;

        bool IsFTQuestDone(int questId) =>
            skipCondition() || core.isCompletedBefore(3937) || core.isCompletedBefore(questId);

        Story.PreLoad(this);

        // 3907: Seek the Tower
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3907",
                () => IsFTQuestDone(3907),
                () =>
                {
                    if (IsFTQuestDone(3907))
                        return;
                    Story.MapItemQuest(3907, "frozentower", 3022);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3908: A n-Ice Beginning
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3908",
                () => IsFTQuestDone(3908),
                () =>
                {
                    if (IsFTQuestDone(3908))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3908, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental")
            )
        )
            return false;

        // 3909: Search for Syrrus
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3909",
                () => IsFTQuestDone(3909),
                () =>
                {
                    if (IsFTQuestDone(3909))
                        return;
                    Story.MapItemQuest(3909, "frozentower", 3019);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3910: Building the Base
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3910",
                () => IsFTQuestDone(3910),
                () =>
                {
                    if (IsFTQuestDone(3910))
                        return;
                    Story.MapItemQuest(3910, "frozentower", 3004, 13);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3911: Refugee Roundup
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3911",
                () => IsFTQuestDone(3911),
                () =>
                {
                    if (IsFTQuestDone(3911))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3911);
                    core.HuntMonster("frozentower", "Frostwyrm", "Refugee Saved", 7);
                    core.EnsureComplete(3911);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm")
            )
        )
            return false;

        // 3912: Retrieve the Water Starstone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3912",
                () => IsFTQuestDone(3912),
                () =>
                {
                    if (IsFTQuestDone(3912))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(3912, "frozentower", "FrostDeep Dweller");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "FrostDeep Dweller")
            )
        )
            return false;

        // 3913: Magical Attraction
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3913",
                () => IsFTQuestDone(3913),
                () =>
                {
                    if (IsFTQuestDone(3913))
                        return;
                    Story.MapItemQuest(3913, "frozentower", 3005, 13);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3914: Frozen Blood
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3914",
                () => IsFTQuestDone(3914),
                () =>
                {
                    if (IsFTQuestDone(3914))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3914, "frozentower", "Twisted Ice");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Twisted Ice")
            )
        )
            return false;

        // 3915: Retrieve the Fire Starstone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3915",
                () => IsFTQuestDone(3915),
                () =>
                {
                    if (IsFTQuestDone(3915))
                        return;
                    Story.MapItemQuest(3915, "frozentower", 3006);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3916: Defend the Tower!
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3916",
                () => IsFTQuestDone(3916),
                () =>
                {
                    if (IsFTQuestDone(3916))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3916, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental")
            )
        )
            return false;

        // 3917: Refugee Rescue Run
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3917",
                () => IsFTQuestDone(3917),
                () =>
                {
                    if (IsFTQuestDone(3917))
                        return;
                    Story.MapItemQuest(3917, "frozentower", 3007, 6);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3918: Retrieve the Earth Starstone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3918",
                () => IsFTQuestDone(3918),
                () =>
                {
                    if (IsFTQuestDone(3918))
                        return;
                    Story.MapItemQuest(3918, "frozentower", 3013);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3919: Polar Penetration and Progress
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3919",
                () => IsFTQuestDone(3919),
                () =>
                {
                    if (IsFTQuestDone(3919))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3919);
                    core.GetMapItem(3008, 6, "frozentower");
                    core.HuntMonster("frozentower", "Polar Elemental", "Ice Sample", 6);
                    core.EnsureComplete(3919);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental")
            )
        )
            return false;

        // 3920: Save the Astronomer Apprentice
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3920",
                () => IsFTQuestDone(3920),
                () =>
                {
                    if (IsFTQuestDone(3920))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3920);
                    core.GetMapItem(3020, 1, "frozentower");
                    core.HuntMonster("frozentower", "Polar Elemental", "Polar Elemental Guard Defeated", 4);
                    core.HuntMonster("frozentower", "Ice Wolf", "Ice Wolf Guard Defeated", 5);
                    core.EnsureComplete(3920);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*")
            )
        )
            return false;

        // 3921: Glacial Elixir
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3921",
                () => IsFTQuestDone(3921),
                () =>
                {
                    if (IsFTQuestDone(3921))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(3921);
                    core.GetMapItem(3017, 6, "frozentower");
                    core.HuntMonster("frozentower", "FrostDeep Dweller", "FrostDeep Venom", 1);
                    core.EnsureComplete(3921);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "FrostDeep Dweller")
            )
        )
            return false;

        // 3922: Retrieve the Energy Starstone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3922",
                () => IsFTQuestDone(3922),
                () =>
                {
                    if (IsFTQuestDone(3922))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3922, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental")
            )
        )
            return false;

        // 3923: Marking the Future
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3923",
                () => IsFTQuestDone(3923),
                () =>
                {
                    if (IsFTQuestDone(3923))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3923, "frozentower", "Frostwyrm");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm")
            )
        )
            return false;

        // 3924: Glacial Shift
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3924",
                () => IsFTQuestDone(3924),
                () =>
                {
                    if (IsFTQuestDone(3924))
                        return;
                    Story.MapItemQuest(3924, "frozentower", 3009, 6);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3925: Divination Draft
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3925",
                () => IsFTQuestDone(3925),
                () =>
                {
                    if (IsFTQuestDone(3925))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3925);
                    core.GetMapItem(3012, 4, "frozentower");
                    core.GetMapItem(3011, 4, "frozentower");
                    core.HuntMonster("frozentower", "Frostwyrm", "Frozen Tail", 5);
                    core.HuntMonster("frozentower", "Arctic Eel", "Frozen Scale", 5);
                    core.EnsureComplete(3925);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*")
            )
        )
            return false;

        // 3926: Retrieve the Light StarStone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3926",
                () => IsFTQuestDone(3926),
                () =>
                {
                    if (IsFTQuestDone(3926))
                        return;
                    Story.MapItemQuest(3926, "frozentower", 3021);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3927: The Future is Bright
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3927",
                () => IsFTQuestDone(3927),
                () =>
                {
                    if (IsFTQuestDone(3927))
                        return;
                    Story.MapItemQuest(3927, "frozentower", 3014, 7);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3928: Bled Bone Dry
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3928",
                () => IsFTQuestDone(3928),
                () =>
                {
                    if (IsFTQuestDone(3928))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3928, "frozentower", "Arctic Eel");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Arctic Eel")
            )
        )
            return false;

        // 3929: Chill of Fear
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3929",
                () => IsFTQuestDone(3929),
                () =>
                {
                    if (IsFTQuestDone(3929))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3929, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental")
            )
        )
            return false;

        // 3930: Retrieve the Darkness Starstone
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3930",
                () => IsFTQuestDone(3930),
                () =>
                {
                    if (IsFTQuestDone(3930))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3930, "frozentower", "Twisted Ice");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Twisted Ice")
            )
        )
            return false;

        // 3931: Web of Fear
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3931",
                () => IsFTQuestDone(3931),
                () =>
                {
                    if (IsFTQuestDone(3931))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3931, "frozentower", "Frostwyrm");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm")
            )
        )
            return false;

        // 3932: Frozen in Time
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3932",
                () => IsFTQuestDone(3932),
                () =>
                {
                    if (IsFTQuestDone(3932))
                        return;
                    Story.MapItemQuest(3932, "frozentower", 3016, 13);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3933: Heart of the Matter
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3933",
                () => IsFTQuestDone(3933),
                () =>
                {
                    if (IsFTQuestDone(3933))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3933, "frozentower", "Ice Wolf");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Ice Wolf")
            )
        )
            return false;

        // 3934: Retrieve the Wind Starstone (Boss: Rotten Ice, 29,280 HP)
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3934",
                () => IsFTQuestDone(3934),
                () =>
                {
                    if (IsFTQuestDone(3934))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(3934, "frozentower", "Rotten Ice");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Rotten Ice")
            )
        )
            return false;

        // 3935: Create the Gate
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3935",
                () => IsFTQuestDone(3935),
                () =>
                {
                    if (IsFTQuestDone(3935))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3935);
                    core.GetMapItem(3018, 13, "frozentower");
                    core.HuntMonster("frozentower", "Ice Wolf", "Blood of the Ice Wolf", 1);
                    core.EnsureComplete(3935);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Ice Wolf")
            )
        )
            return false;

        // 3936: Drive Back the Invaders
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3936",
                () => IsFTQuestDone(3936),
                () =>
                {
                    if (IsFTQuestDone(3936))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3936);
                    core.HuntMonster("frozentower", "Frost Invader", "FrostSpawn Invader defeated", 10);
                    core.EnsureComplete(3936);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader")
            )
        )
            return false;

        // 3937: Defeat the FrostSpawn Invaders
        if (
            !RunCooperativeObjective(
                "BRACKET55_FT_3937",
                () => IsFTQuestDone(3937),
                () =>
                {
                    if (IsFTQuestDone(3937))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3937);
                    core.HuntMonster("frozentower", "Frost Fangbeast", "Fangbeasts defeated", 15);
                    core.EnsureComplete(3937);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast")
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    /// <summary>
    /// Coordinates /twilightedge Quest 3428 and /weaverwar Quests 3429-3439 to unlock Quest 3445 (Ravenloss REP).
    /// </summary>
    private bool RunCoordinatedWeaverWar(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;

        bool IsWWQuestDone(int questId) =>
            skipCondition() || core.isCompletedBefore(3439) || core.isCompletedBefore(questId);

        Story.PreLoad(this);

        // 3428: Clear Twilight's Edge
        if (
            !RunCooperativeObjective(
                "BRACKET55_WW_3428",
                () => IsWWQuestDone(3428),
                () =>
                {
                    if (IsWWQuestDone(3428))
                        return;
                    Story.MapItemQuest(3428, "twilightedge", 2577);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        (int QuestId, int MapItemId, string Cell, string Monster, string TempItem, int Quant)[] weaverQuests =
        {
            (3429, 2578, "s1", "Weaver Queen's Hound", "Weaver Hounds Slain", 6),
            (3430, 2579, "f1", "ChaosWeaver Mage", "Spider Slain", 6),
            (3431, 2580, "d1", "Dread Arysa", "Arysa Slain", 6),
            (3432, 2581, "do1", "Weaver Queen's Hound", "Weaver Hound Slain", 6),
            (3433, 2582, "fr1", "ChaosWeaver Mage", "Weaver Slain", 6),
            (3434, 2583, "n1", "Evolved Dreadspider", "Spider Slain", 6),
            (3435, 2584, "b1", "Dread Arysa", "Arysa Slain", 6),
            (3436, 2585, "g1", "ChaosWeaver Mage", "Weaver Slain", 6),
            (3437, 2586, "sl1", "ChaosWeaver Mage", "Weaver Slain", 6),
            (3438, 2587, "ba1", "Evolved Dreadspider", "Spider Slain", 6),
        };

        foreach (var wq in weaverQuests)
        {
            if (
                !RunCooperativeObjective(
                    $"BRACKET55_WW_{wq.QuestId}",
                    () => IsWWQuestDone(wq.QuestId),
                    () =>
                    {
                        if (IsWWQuestDone(wq.QuestId))
                            return;

                        SetClass();
                        core.EquipClass(ClassType.Farm);
                        core.EnsureAccept(wq.QuestId);
                        core.GetMapItem(wq.MapItemId, 1, "weaverwar");
                        core.KillMonster("weaverwar", wq.Cell, "Spawn", wq.Monster, wq.TempItem, wq.Quant);
                        core.EnsureComplete(wq.QuestId);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(targetPlayerName, wq.Monster)
                )
            )
                return false;
        }

        // 3439: Save the SoulWeaver (Boss: ChaosWeaver Warrior, 132,840 HP in /twilightedge)
        if (
            !RunCooperativeObjective(
                "BRACKET55_WW_3439_BOSS",
                () => IsWWQuestDone(3439),
                () =>
                {
                    if (IsWWQuestDone(3439))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(3439, "twilightedge", "ChaosWeaver Warrior");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "ChaosWeaver Warrior")
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    /// <summary>
    /// Coordinates the Yokai Storyline (/shogunwar, /shinringrove, /greenshell, /heiwavalley, /shadowfortress)
    /// up to Quest 6494 (Defeat Jaaku!) to unlock Quest 7924 for DragonSoul Shinobi.
    /// Skips optional side-quests 6460, 6461, and 6473, and fixes the Quest 6494 completion bug.
    /// </summary>
    private bool RunCoordinatedYokaiStory(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;

        bool IsShogunDone(int questId) =>
            skipCondition()
            || core.isCompletedBefore(6494)
            || core.isCompletedBefore(6459)
            || core.isCompletedBefore(questId);

        bool IsShinrinDone(int questId) =>
            skipCondition()
            || core.isCompletedBefore(6494)
            || core.isCompletedBefore(6472)
            || core.isCompletedBefore(questId);

        bool IsShadowFortressDone(int questId) =>
            skipCondition() || core.isCompletedBefore(6494) || core.isCompletedBefore(questId);

        Story.PreLoad(this);

        // --- Part 1: ShogunWar (Quests 6450-6459) ---
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6450",
                () => IsShogunDone(6450),
                () =>
                {
                    if (IsShogunDone(6450))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6450);
                    core.HuntMonster("shogunwar", "Shadow Samurai", "Shadow Medallions", 5);
                    core.EnsureComplete(6450);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6451",
                () => IsShogunDone(6451),
                () =>
                {
                    if (IsShogunDone(6451))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6451);
                    core.HuntMonsterMapID("shogunwar", 8, "Kijimuna Pollen", 4);
                    core.HuntMonsterMapID("shogunwar", 7, "Reishi Spores", 4);
                    core.EnsureComplete(6451);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6452",
                () => IsShogunDone(6452),
                () =>
                {
                    if (IsShogunDone(6452))
                        return;
                    Story.MapItemQuest(6452, "shogunwar", 5956, 5);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6453",
                () => IsShogunDone(6453),
                () =>
                {
                    if (IsShogunDone(6453))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6453, "shogunwar", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6454",
                () => IsShogunDone(6454),
                () =>
                {
                    if (IsShogunDone(6454))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6454, "shogunwar", "Bamboo Treeant");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bamboo Treeant")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6455",
                () => IsShogunDone(6455),
                () =>
                {
                    if (IsShogunDone(6455))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6455, "shogunwar", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6456",
                () => IsShogunDone(6456),
                () =>
                {
                    if (IsShogunDone(6456))
                        return;
                    Story.MapItemQuest(6456, "shogunwar", new[] { 5957, 5958, 5959, 5960 });
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6457",
                () => IsShogunDone(6457),
                () =>
                {
                    if (IsShogunDone(6457))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6457, "shogunwar", "Reishi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Reishi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6458",
                () => IsShogunDone(6458),
                () =>
                {
                    if (IsShogunDone(6458))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6458, "shogunwar", "Bamboo Treeant");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bamboo Treeant")
            )
        )
            return false;

        // 6459: Defeat the Beast (Boss: Orochi, MapID 25)
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6459_OROCHI",
                () => IsShogunDone(6459),
                () =>
                {
                    if (IsShogunDone(6459))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6459);
                    core.HuntMonsterMapID("shogunwar", 25, "Orochi Defeated");
                    core.EnsureComplete(6459);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "25")
            )
        )
            return false;

        // --- Part 2: ShinrinGrove & GreenShell (Quests 6462-6472) ---
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6462",
                () => IsShinrinDone(6462),
                () =>
                {
                    if (IsShinrinDone(6462))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6462);
                    core.HuntMonster("shinringrove", "Kame", "Kame Defeated");
                    core.EnsureComplete(6462);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kame")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6463",
                () => IsShinrinDone(6463),
                () =>
                {
                    if (IsShinrinDone(6463))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6463);
                    core.HuntMonster("shinringrove", "Reishi", "Reishi Caps", 6);
                    core.HuntMonster("shinringrove", "Tsurubebi", "Tsurubebi Flame", 3);
                    core.EnsureComplete(6463);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*")
            )
        )
            return false;

        // 6464: Summon Otoroshi (Boss: Otoroshi, 84,920 HP in /shinringrove r4a, Left)
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6464_OTOROSHI",
                () => IsShinrinDone(6464),
                () =>
                {
                    if (IsShinrinDone(6464))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6464);
                    core.GetMapItem(5962, 1, "shinringrove");
                    core.Sleep();
                    core.KillMonster("shinringrove", "r4a", "Left", "Otoroshi", "Otoroshi Defeated");
                    core.EnsureComplete(6464);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Otoroshi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6465",
                () => IsShinrinDone(6465),
                () =>
                {
                    if (IsShinrinDone(6465))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6465);
                    core.GetMapItem(5964, 5, "greenshell");
                    core.HuntMonster("greenshell", "Tsurubebi", "Tsurubebi Extinguished", 5);
                    core.EnsureComplete(6465);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tsurubebi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6466",
                () => IsShinrinDone(6466),
                () =>
                {
                    if (IsShinrinDone(6466))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6466, "greenshell", "Tsukumogami");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tsukumogami")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6467",
                () => IsShinrinDone(6467),
                () =>
                {
                    if (IsShinrinDone(6467))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6467, "shinringrove", "Kodama");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kodama")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6468",
                () => IsShinrinDone(6468),
                () =>
                {
                    if (IsShinrinDone(6468))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6468, "shinringrove", "Moglinberry Bush");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Moglinberry Bush")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6469",
                () => IsShinrinDone(6469),
                () =>
                {
                    if (IsShinrinDone(6469))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6469);
                    core.GetMapItem(5965, 8, "greenshell");
                    core.HuntMonster("shinringrove", "Reishi", "Luminous Spores", 6);
                    core.EnsureComplete(6469);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Reishi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6470",
                () => IsShinrinDone(6470),
                () =>
                {
                    if (IsShinrinDone(6470))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6470);
                    core.GetMapItem(5966, 1, "greenshell");
                    core.HuntMonster("greenshell", "Merdraconian", "Merdraconians Slain", 3);
                    core.EnsureComplete(6470);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Merdraconian")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6471",
                () => IsShinrinDone(6471),
                () =>
                {
                    if (IsShinrinDone(6471))
                        return;
                    Story.MapItemQuest(6471, "greenshell", 5967);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6472: Battle for the Shinrin Do (Boss: Nagami, 129,050 HP in /greenshell)
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6472_NAGAMI",
                () => IsShinrinDone(6472),
                () =>
                {
                    if (IsShinrinDone(6472))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6472);
                    core.HuntMonster("greenshell", "Nagami", "Nagami Defeated");
                    core.EnsureComplete(6472);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Nagami")
            )
        )
            return false;

        // --- Part 3: HeiwaValley & ShadowFortress (Quests 6474-6483 & 6485-6494) ---
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6474",
                () => IsShadowFortressDone(6474),
                () =>
                {
                    if (IsShadowFortressDone(6474))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6474, "heiwavalley", "Abumi Guchi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Abumi Guchi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6475",
                () => IsShadowFortressDone(6475),
                () =>
                {
                    if (IsShadowFortressDone(6475))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6475);
                    core.HuntMonster("heiwavalley", "Shadow Samurai", "Pilfered Armor", 5);
                    core.HuntMonster("heiwavalley", "Shadow Samurai", "Stolen Sword", 5);
                    core.EnsureComplete(6475);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6476",
                () => IsShadowFortressDone(6476),
                () =>
                {
                    if (IsShadowFortressDone(6476))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6476);
                    core.GetMapItem(5968, 6, "heiwavalley");
                    core.HuntMonster("heiwavalley", "Kosenjobi", "Kosenjobi Flame", 6);
                    core.EnsureComplete(6476);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kosenjobi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6477",
                () => IsShadowFortressDone(6477),
                () =>
                {
                    if (IsShadowFortressDone(6477))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6477, "heiwavalley", "Goryo");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Goryo")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6478",
                () => IsShadowFortressDone(6478),
                () =>
                {
                    if (IsShadowFortressDone(6478))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6478);
                    core.HuntMonster("heiwavalley", "Kosenjobi", "Spectral Heat", 5);
                    Story.BuyQuest(6478, "heiwavalley", 1608, "Wasabi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kosenjobi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6479",
                () => IsShadowFortressDone(6479),
                () =>
                {
                    if (IsShadowFortressDone(6479))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6479, "heiwavalley", "Abumi Guchi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Abumi Guchi")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6480",
                () => IsShadowFortressDone(6480),
                () =>
                {
                    if (IsShadowFortressDone(6480))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6480, "heiwavalley", "Inugami");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Inugami")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6481",
                () => IsShadowFortressDone(6481),
                () =>
                {
                    if (IsShadowFortressDone(6481))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6481);
                    core.HuntMonster("heiwavalley", "Inugami", "Spirit Eyes", 4);
                    Story.MapItemQuest(6481, "heiwavalley", 5970, 8);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Inugami")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6482",
                () => IsShadowFortressDone(6482),
                () =>
                {
                    if (IsShadowFortressDone(6482))
                        return;
                    Story.MapItemQuest(6482, "heiwavalley", 5971);
                    Story.MapItemQuest(6482, "heiwavalley", 5972, 8);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6483: Defeat the Onryo (Boss: Onryo, MapID 15 in /heiwavalley)
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6483_ONRYO",
                () => IsShadowFortressDone(6483),
                () =>
                {
                    if (IsShadowFortressDone(6483))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6483);
                    core.HuntMonsterMapID("heiwavalley", 15, "Onryo Slain");
                    core.EnsureComplete(6483);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "15")
            )
        )
            return false;

        // 6485: Calm the Spirits
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6485",
                () => IsShadowFortressDone(6485),
                () =>
                {
                    if (IsShadowFortressDone(6485))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6485, "shadowfortress", "Restless Spirit");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Restless Spirit")
            )
        )
            return false;

        // 6486: Disarm the Samurai
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6486",
                () => IsShadowFortressDone(6486),
                () =>
                {
                    if (IsShadowFortressDone(6486))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6486, "shadowfortress", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        // 6487: Find the Throne Room
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6487",
                () => IsShadowFortressDone(6487),
                () =>
                {
                    if (IsShadowFortressDone(6487))
                        return;
                    Story.MapItemQuest(6487, "shadowfortress", 5973);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6488: Shadow Essences
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6488",
                () => IsShadowFortressDone(6488),
                () =>
                {
                    if (IsShadowFortressDone(6488))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6488);
                    core.GetMapItem(5974, 1, "shadowfortress");
                    core.HuntMonster("shadowfortress", "Living Shadow", "Shadow Essence", 7);
                    core.EnsureComplete(6488);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Living Shadow")
            )
        )
            return false;

        // 6489: New Recruits
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6489",
                () => IsShadowFortressDone(6489),
                () =>
                {
                    if (IsShadowFortressDone(6489))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(6489, "shadowfortress", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai")
            )
        )
            return false;

        // 6490: Shadow Heads
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6490",
                () => IsShadowFortressDone(6490),
                () =>
                {
                    if (IsShadowFortressDone(6490))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(6490);
                    core.GetMapItem(5975, 7, "shadowfortress");
                    core.HuntMonster("shadowfortress", "Creeping Shadow", "Shadows Slain", 7);
                    core.EnsureComplete(6490);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Creeping Shadow")
            )
        )
            return false;

        // 6491: A Head has Woken (Boss: 7th Head of Orochi)
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6491",
                () => IsShadowFortressDone(6491),
                () =>
                {
                    if (IsShadowFortressDone(6491))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(6491, "shadowfortress", "7th Head of Orochi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "7th Head of Orochi")
            )
        )
            return false;

        // 6492: Get to the Roof
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6492",
                () => IsShadowFortressDone(6492),
                () =>
                {
                    if (IsShadowFortressDone(6492))
                        return;
                    Story.MapItemQuest(6492, "shadowfortress", new[] { 5976, 5979 });
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6493: Find Jaaku! (Coordinate each of the 6 Heads of Orochi + MapItem 5977)
        (string Label, string Monster, string TempDrop)[] orochiHeads =
        {
            ("6TH", "6th Head of Orochi", "6th Head Defeated"),
            ("5TH", "5th Head of Orochi", "5th Head Defeated"),
            ("4TH", "4th Head of Orochi", "4th Head Defeated"),
            ("3RD", "3rd Head of Orochi", "3rd Head Defeated"),
            ("2ND", "2nd Head of Orochi", "2nd Head Defeated"),
            ("1ST", "1st Head of Orochi", "1st Head Defeated"),
        };

        foreach (var head in orochiHeads)
        {
            if (
                !RunCooperativeObjective(
                    $"BRACKET60_YOKAI_6493_HEAD_{head.Label}",
                    () => IsShadowFortressDone(6493) || Bot.TempInv.Contains(head.TempDrop),
                    () =>
                    {
                        if (IsShadowFortressDone(6493) || Bot.TempInv.Contains(head.TempDrop))
                            return;

                        SetClass();
                        core.EquipClass(ClassType.Solo);
                        core.EnsureAccept(6493);
                        core.HuntMonster("shadowfortress", head.Monster, head.TempDrop);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(targetPlayerName, head.Monster)
                )
            )
                return false;
        }

        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6493_COMPLETE",
                () => IsShadowFortressDone(6493),
                () =>
                {
                    if (IsShadowFortressDone(6493))
                        return;

                    core.EnsureAccept(6493);
                    Story.MapItemQuest(6493, "shadowfortress", 5977);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 6494: Defeat Jaaku! (Boss: Jaaku, MapID 31, 175,880 HP — fixes EnsureComplete(6494))
        if (
            !RunCooperativeObjective(
                "BRACKET60_YOKAI_6494_JAAKU",
                () => IsShadowFortressDone(6494),
                () =>
                {
                    if (IsShadowFortressDone(6494))
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    core.EnsureAccept(6494);
                    core.HuntMonsterMapID("shadowfortress", 31, "Jaaku Defeated");
                    core.EnsureComplete(6494);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "31")
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    /// <summary>
    /// Coordinates /frozenruins, /glacera, /icewindpass, and /icewindwar Quests (3941-3954 & 5587-5601)
    /// to unlock Glacera REP farming (Quests 5597-5600) for Glacial Berserker.
    /// Skips Northstar (3958-3971), optional quests 5595-5596, and IceDungeon (7832-7841).
    /// </summary>
    private bool RunCoordinatedGlaceraForGB(Func<bool> skipCondition)
    {
        CoreBots core = CoreBots.Instance;

        bool IsRuinsQuestDone(int questId) =>
            skipCondition()
            || core.isCompletedBefore(5601)
            || core.isCompletedBefore(3954)
            || core.isCompletedBefore(questId);

        bool IsIceWindQuestDone(int questId) =>
            skipCondition() || core.isCompletedBefore(5601) || core.isCompletedBefore(questId);

        Story.PreLoad(this);

        // --- Part 1: FrozenRuins & Glacera (Quests 3941-3954) ---
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3941",
                () => IsRuinsQuestDone(3941),
                () =>
                {
                    if (IsRuinsQuestDone(3941))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3941, "frozenruins", "Frost Fangbeast");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast")
            )
        )
            return false;

        for (int qId = 3942; qId <= 3944; qId++)
        {
            int currentQuest = qId;
            if (
                !RunCooperativeObjective(
                    $"BRACKET65_GLACERA_{currentQuest}",
                    () => IsRuinsQuestDone(currentQuest),
                    () =>
                    {
                        if (IsRuinsQuestDone(currentQuest))
                            return;
                        SetClass();
                        core.EquipClass(ClassType.Farm);
                        Story.KillQuest(currentQuest, "frozenruins", "Frost Reaper");
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper")
                )
            )
                return false;
        }

        // 3945: Form the Lock
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3945",
                () => IsRuinsQuestDone(3945),
                () =>
                {
                    if (IsRuinsQuestDone(3945))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3945);
                    core.GetMapItem(3050, 10, "frozenruins");
                    core.HuntMonster("frozenruins", "Frozen Moglinster", "Frostbreath Weed", 5);
                    core.EnsureComplete(3945);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frozen Moglinster")
            )
        )
            return false;

        // 3946: Glacera
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3946",
                () => IsRuinsQuestDone(3946),
                () =>
                {
                    if (IsRuinsQuestDone(3946))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3946);
                    if (!core.CheckInventory(27357))
                        core.HuntMonster("frozenruins", "Frost Reaper", "Mercury");
                    core.EnsureComplete(3946);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper")
            )
        )
            return false;

        // 3947: A Frosty Welcome
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3947",
                () => IsRuinsQuestDone(3947),
                () =>
                {
                    if (IsRuinsQuestDone(3947))
                        return;
                    core.EnsureAccept(3947);
                    core.Join("glacera");
                    Bot.Wait.ForMapLoad("glacera");
                    core.GetMapItem(3048, 1, "glacera");
                    core.EnsureComplete(3947);
                    core.Jump("Enter", "Spawn");
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3948: Key to the Fortress
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3948",
                () => IsRuinsQuestDone(3948),
                () =>
                {
                    if (IsRuinsQuestDone(3948))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3948, "glacera", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader")
            )
        )
            return false;

        // 3949: Breaking Boulders
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3949",
                () => IsRuinsQuestDone(3949),
                () =>
                {
                    if (IsRuinsQuestDone(3949))
                        return;
                    Story.MapItemQuest(3949, "glacera", 3049, 6);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3950: The Scythe of Vengeance
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3950",
                () => IsRuinsQuestDone(3950),
                () =>
                {
                    if (IsRuinsQuestDone(3950))
                        return;
                    Story.MapItemQuest(3950, "glacera", 3047);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3951: Rescue the Refugees
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3951",
                () => IsRuinsQuestDone(3951),
                () =>
                {
                    if (IsRuinsQuestDone(3951))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(3951);
                    core.HuntMonster("frozenruins", "Frost Invader", "Ice Beast Destroyed", 10);
                    core.EnsureComplete(3951);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader")
            )
        )
            return false;

        // 3952: Defeat the Fangbeasts
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3952",
                () => IsRuinsQuestDone(3952),
                () =>
                {
                    if (IsRuinsQuestDone(3952))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3952, "frozenruins", "Frost Fangbeast");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast")
            )
        )
            return false;

        // 3953: Destroy the Frost Reapers
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3953",
                () => IsRuinsQuestDone(3953),
                () =>
                {
                    if (IsRuinsQuestDone(3953))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(3953, "frozenruins", "Frost Reaper");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper")
            )
        )
            return false;

        // 3954: FrostSpawn General Takedown (Boss: Frost General, 65,620 HP)
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_3954_BOSS",
                () => IsRuinsQuestDone(3954),
                () =>
                {
                    if (IsRuinsQuestDone(3954))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(3954, "frozenruins", "Frost General");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost General")
            )
        )
            return false;

        // --- Part 2: IceWindPass & IceWindWar (Quests 5587-5594, 5597-5601) ---
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5587",
                () => IsIceWindQuestDone(5587),
                () =>
                {
                    if (IsIceWindQuestDone(5587))
                        return;
                    Story.MapItemQuest(5587, "icewindpass", 5074, 5);
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5588",
                () => IsIceWindQuestDone(5588),
                () =>
                {
                    if (IsIceWindQuestDone(5588))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5588, "icewindpass", "Glacial Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Glacial Elemental")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5589",
                () => IsIceWindQuestDone(5589),
                () =>
                {
                    if (IsIceWindQuestDone(5589))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAccept(5589);
                    core.GetMapItem(5075, 5, "icewindpass");
                    core.HuntMonster("icewindpass", "Glacial Elemental", "Iced Mana", 9);
                    core.EnsureComplete(5589);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Glacial Elemental")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5590",
                () => IsIceWindQuestDone(5590),
                () =>
                {
                    if (IsIceWindQuestDone(5590))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5590, "icewindpass", "Polar Golem");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Golem")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5591",
                () => IsIceWindQuestDone(5591),
                () =>
                {
                    if (IsIceWindQuestDone(5591))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5591, "icewindpass", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader")
            )
        )
            return false;

        // 5592: Blast the Frostspawn Symbiote (Boss: Frostspawn Symbiote)
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5592_SYMBIOTE",
                () => IsIceWindQuestDone(5592),
                () =>
                {
                    if (IsIceWindQuestDone(5592))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5592, "icewindpass", "Frostspawn Symbiote");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostspawn Symbiote")
            )
        )
            return false;

        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5593",
                () => IsIceWindQuestDone(5593),
                () =>
                {
                    if (IsIceWindQuestDone(5593))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    Story.KillQuest(5593, "icewindpass", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader")
            )
        )
            return false;

        // 5594: Take it Down (Boss: Frostspawn Horror, 153,590 HP)
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5594_HORROR",
                () => IsIceWindQuestDone(5594),
                () =>
                {
                    if (IsIceWindQuestDone(5594))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5594, "icewindpass", "Frostspawn Horror");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostspawn Horror")
            )
        )
            return false;

        // 5597, 5598, 5599, 5600: IceWindWar Medals (Slot 225, Value 9)
        bool warMedalsDone = false;
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5597_5600_WAR",
                () => IsIceWindQuestDone(5597) || warMedalsDone,
                () =>
                {
                    if (IsIceWindQuestDone(5597) || warMedalsDone)
                        return;

                    SetClass();
                    core.EquipClass(ClassType.Farm);
                    core.EnsureAcceptmultiple(new[] { 5597, 5598, 5599, 5600 });
                    core.HuntMonster("icewindwar", "Glaceran Defender", "FrostSpawn Medal", 10);
                    core.HuntMonster("icewindwar", "Glaceran Defender", "Mega Frostspawn Medal", 5);
                    core.HuntMonster("icewindwar", "Frost Invader", "World Ender Medal", 10);
                    core.HuntMonster("icewindwar", "Frost Invader", "Mega World Ender Medal", 5);
                    core.EnsureComplete(new[] { 5597, 5598, 5599, 5600 });
                    warMedalsDone = true;
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*")
            )
        )
            return false;

        // 5601: What is THAT? (Boss: Soricomorpha in /icewindwar, Slot 225, Value 10)
        if (
            !RunCooperativeObjective(
                "BRACKET65_GLACERA_5601_SORICOMORPHA",
                () => IsIceWindQuestDone(5601),
                () =>
                {
                    if (IsIceWindQuestDone(5601))
                        return;
                    SetClass();
                    core.EquipClass(ClassType.Solo);
                    Story.KillQuest(5601, "icewindwar", "Soricomorpha");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Soricomorpha")
            )
        )
            return false;

        return !Bot.ShouldExit;
    }
    #endregion Step 2C: Brackets 55, 60, and 65

    #region SetClass (Party Role Composition & CBO Resolution — 100% Free-to-Play Classes Only)
    // Required Players 1-4 form the Endgame Meta Core (Main DPS + StoneCrusher + ArchPaladin + Lord of Order).
    // Optional Players 5-7 add non-overlapping debuffs, amplifiers, dodge DPS, and backup support.

    // Player 1 (Required): Main Solo DPS (never overlaps with Players 2-7 at the same milestone)
    private static readonly string[] Player1SoloClasses =
    {
        "King's Echo",
        "Arcana Invoker",
        "Chaos Avenger",
        "Void Highlord",
        "Verus DoomKnight",
        "Dragon of Time",
        "Cryomancer",
        "Dragonslayer General",
        "Glacial Berserker",
        "Dragonslayer",
        "DragonSoul Shinobi",
        "Ninja",
        "Rogue",
    };

    // Player 2 (Required): Primary Support / Haste & HoT Buffer -> Endgame preference: StoneCrusher
    private static readonly string[] Player2SupportClasses =
    {
        "StoneCrusher",
        "Oracle",
        "Healer (Rare)",
        "Healer",
    };

    // Player 3 (Required): Hit-Chance / Damage Debuffer & Tank -> Endgame preference: ArchPaladin
    private static readonly string[] Player3DebuffClasses =
    {
        "ArchPaladin",
        "Imperial Chunin",
        "Rogue (Rare)",
        "Rogue",
        "Ninja",
        "Oracle",
    };

    // Player 4 (Required): Damage Amplifier & Stat Buffer -> Endgame preference: Lord of Order
    private static readonly string[] Player4AmpClasses =
    {
        "Lord of Order",
        "Arachnomancer",
        "Shaman",
        "Mage (Rare)",
        "Mage",
        "Oracle",
    };

    // Player 5 (Optional): Defense Shred / Decay Debuffer & Sub-DPS (Evolved Shaman -> Glacial Berserker -> Verus DoomKnight)
    private static readonly string[] Player5FlexClasses =
    {
        "Verus DoomKnight",
        "Glacial Berserker",
        "Evolved Shaman",
        "Master Ranger",
        "Oracle",
        "Healer (Rare)",
        "Healer",
    };

    // Player 6 (Optional): Crit / Damage Amplifier & Flex DPS (Master Ranger -> Scarlet Sorceress -> Archfiend)
    private static readonly string[] Player6FlexClasses =
    {
        "Archfiend",
        "Scarlet Sorceress",
        "Master Ranger",
        "Oracle",
        "Healer (Rare)",
        "Healer",
    };

    // Player 7 (Optional): Stealth / Dodge DPS & Execute (Ninja -> DragonSoul Shinobi -> Yami No Ronin)
    private static readonly string[] Player7FlexClasses =
    {
        "Yami No Ronin",
        "DragonSoul Shinobi",
        "Ninja",
        "Oracle",
        "Healer (Rare)",
        "Healer",
    };

    // Secondary Support pool for Player 5 during Farm mode in larger armies (5-7 players)
    private static readonly string[] Player5FarmSupportClasses =
    {
        "Lord of Order",
        "StoneCrusher",
        "ArchPaladin",
        "Oracle",
        "Evolved Shaman",
        "Healer (Rare)",
        "Healer",
    };

    private static readonly string[] FarmClasses =
    {
        "ArchMage",
        "Arcana Invoker",
        "Dragon of Time",
        "Archfiend",
        "Blaze Binder",
        "Scarlet Sorceress",
        "Pyromancer",
        "Shaman",
        "Imperial Chunin",
        "Master Ranger",
        "Mage",
    };

    private string[] GetRoleBossPool()
    {
        if (IsPlayer(2))
            return Player2SupportClasses;
        if (IsPlayer(3))
            return Player3DebuffClasses;
        if (IsPlayer(4))
            return Player4AmpClasses;
        if (IsPlayer(5))
            return Player5FlexClasses;
        if (IsPlayer(6))
            return Player6FlexClasses;
        if (IsPlayer(7))
            return Player7FlexClasses;

        return Player1SoloClasses;
    }

    private string[] GetRoleFarmPool()
    {
        // Farm Composition: AoE Farm classes combined with Support/Healer classes
        // - Player 2 stays on Primary Healer/Support (4-7 player armies)
        // - Player 5 adds Secondary Buffer/Support in larger armies (5-7 player armies)
        if (IsPlayer(2))
            return Player2SupportClasses;
        if (IsPlayer(5))
            return Player5FarmSupportClasses;

        return FarmClasses;
    }

    private string GetPartyRoleLabel()
    {
        if (IsPlayer(2))
            return "P2-Support(SC)";
        if (IsPlayer(3))
            return "P3-Debuff(AP)";
        if (IsPlayer(4))
            return "P4-BuffAmp(LoO)";
        if (IsPlayer(5))
            return "P5-FlexDebuff";
        if (IsPlayer(6))
            return "P6-FlexAmpDPS";
        if (IsPlayer(7))
            return "P7-FlexDodgeDPS";
        return "P1-MainDPS";
    }

    private string? _lastSolo;
    private string? _lastFarm;
    private int _setClassBusy;

    public void SetClass(bool rankUpIfNeeded = false)
    {
        if (System.Threading.Interlocked.Exchange(ref _setClassBusy, 1) == 1)
        {
            CoreBots.Instance.Logger("SetClass is already running, skipping the overlapping call.");
            return;
        }

        try
        {
            SetClassInner(rankUpIfNeeded);
        }
        finally
        {
            System.Threading.Volatile.Write(ref _setClassBusy, 0);
        }
    }

    private void SetClassInner(bool rankUpIfNeeded)
    {
        CoreBots core = CoreBots.Instance;
        core.ReadCBO();
        ApplyArmyRoomSettings();

        // Always use the standard Base mode from Skua's Advanced Skills Set
        core.SoloUseMode = ClassUseMode.Base;
        core.FarmUseMode = ClassUseMode.Base;
        core.DodgeUseMode = ClassUseMode.Base;
        core.BossUseMode = ClassUseMode.Base;

        string[] bossPool = GetRoleBossPool();
        string[] farmPool = GetRoleFarmPool();

        // Two unified compositions:
        // 1. Boss Composition (used for SoloClass, DodgeClass, and BossClass)
        // 2. Farm Composition (AoE farmers + dedicated Supports on P2 and P5)
        core.SoloClass = ResolveClass(bossPool);
        core.DodgeClass = core.SoloClass;
        core.BossClass = core.SoloClass;
        core.FarmClass = ResolveClass(farmPool);

        bool Settled(string? className) =>
            CoreBots.IsGenericClass(className)
            || (!string.IsNullOrWhiteSpace(className) && HasRank10Class(className));

        if (
            !rankUpIfNeeded
            || (
                string.Equals(_lastSolo, core.SoloClass, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_lastFarm, core.FarmClass, StringComparison.OrdinalIgnoreCase)
                && Settled(core.SoloClass)
                && Settled(core.FarmClass)
            )
        )
        {
            if (
                !CoreBots.IsGenericClass(core.SoloClass)
                && !string.Equals(Bot.Player.CurrentClass?.Name, core.SoloClass, StringComparison.OrdinalIgnoreCase)
            )
            {
                LeaveCombatRoomForEquipmentChange();
                core.EquipClass(ClassType.Solo);
            }
            else if (!Bot.Skills.TimerRunning)
            {
                Bot.Skills.StartAdvanced(Bot.Player.CurrentClass?.Name ?? core.SoloClass, false, ClassUseMode.Base);
            }
            return;
        }

        RankIfNeeded(core.SoloClass, "BossComposition");
        RankIfNeeded(core.FarmClass, "FarmComposition");

        Bot.Sleep(500);
        if (
            !CoreBots.IsGenericClass(core.SoloClass)
            && !string.Equals(Bot.Player.CurrentClass?.Name, core.SoloClass, StringComparison.OrdinalIgnoreCase)
        )
        {
            LeaveCombatRoomForEquipmentChange();
        }
        core.EquipClass(ClassType.Solo);

        _lastSolo = core.SoloClass;
        _lastFarm = core.FarmClass;
    }

    private void RankUpClassForArmy(string className)
    {
        if (string.IsNullOrWhiteSpace(className) || Bot.ShouldExit)
            return;

        if (!HasClassInInventory(className))
        {
            CoreBots.Instance.Logger($"WARNING: Cannot rank up class '{className}' because it is not in inventory.");
            return;
        }

        // If the class is not yet equipped, step out to /whitemap first so SmartEnhance and Equip
        // cannot be blocked by monster aggro, kill-credit, or party buffs in the room.
        if (!string.Equals(Bot.Player.CurrentClass?.Name, className, StringComparison.OrdinalIgnoreCase))
        {
            LeaveCombatRoomForEquipmentChange();
            Adv.SmartEnhance(className);
            CoreBots.Instance.Equip(className);
            Bot.Wait.ForTrue(() => Bot.Inventory.IsEquipped(className), 20);
        }

        // Start Skua's Advanced Skills Set rotation for the class being ranked up!
        // (Adv.RankUpClass only calls Core.Equip, which does not start Bot.Skills.StartAdvanced!)
        Bot.Skills.StartAdvanced(className, false, ClassUseMode.Base);

        // Pass gearRestore: false so that when this account hits Rank 10 in /icestormarena while other
        // accounts are still fighting, it doesn't try to restore/re-enhance old gear inside the active combat room!
        Adv.RankUpClass(className, gearRestore: false);
    }

    private void LeaveCombatRoomForEquipmentChange()
    {
        try
        {
            Bot.Skills.Stop();
        }
        catch
        {
            // Ignore if skill timer is not active
        }

        if (Bot.Player.HasTarget)
            Bot.Combat.CancelTarget();

        Bot.Options.AttackWithoutTarget = false;
        Bot.Options.AggroAllMonsters = false;
        Bot.Options.AggroMonsters = false;

        string currentMap = Bot.Map?.Name ?? string.Empty;
        if (
            !string.Equals(currentMap, "whitemap", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(currentMap, "battleon", StringComparison.OrdinalIgnoreCase)
            && (Bot.Player.InCombat || Bot.Monsters.MapMonsters.Count > 0)
        )
        {
            CoreBots.Instance.Join("whitemap");
            Bot.Wait.ForMapLoad("whitemap");
        }

        CoreBots.Instance.JumpWait();
        Bot.Wait.ForCombatExit(20);
    }

    private string ResolveClass(string[] pool)
    {
        // Prefer the highest-priority class in the pool that is already Rank 10 in Inventory,
        // falling back to the highest-priority owned class in Inventory.
        string? bestRank10 = pool.FirstOrDefault(className =>
            HasClassInInventory(className) && HasRank10Class(className)
        );
        if (!string.IsNullOrEmpty(bestRank10))
            return bestRank10;

        return pool.FirstOrDefault(HasClassInInventory) ?? "Generic";
    }

    private void RankIfNeeded(string? className, string label)
    {
        CoreBots core = CoreBots.Instance;
        if (string.IsNullOrEmpty(className) || CoreBots.IsGenericClass(className))
            return;

        if (!HasClassInInventory(className))
        {
            core.Logger($"WARNING: {label} class '{className}' not in inventory!");
            return;
        }

        if (HasRank10Class(className))
            return;

        core.Logger($"{label} resolved -> {className}");
        RankUpClassForArmy(className);
    }
    #endregion SetClass

    private bool HasClassInInventory(string? className) =>
        !string.IsNullOrWhiteSpace(className)
        && Bot.Inventory?.Items.Any(item =>
            item.Category == ItemCategory.Class
            && item.Name?.Trim().Equals(className.Trim(), StringComparison.OrdinalIgnoreCase) == true
        ) == true;

    private bool HasClassInInventory(int itemId) =>
        itemId > 0
        && Bot.Inventory?.Items.Any(item =>
            item.Category == ItemCategory.Class && item.ID == itemId
        ) == true;

    private bool HasAnyClassInInventory(params string[] classNames) =>
        classNames.Any(HasClassInInventory);

    private bool HasAnyClassInInventory(params int[] itemIds) =>
        itemIds.Any(HasClassInInventory);

    private bool HasRank10Class(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return false;

        InventoryItem? item = Bot.Inventory?.Items.FirstOrDefault(item =>
            item.Category == ItemCategory.Class
            && item.Name?.Trim().Equals(className.Trim(), StringComparison.OrdinalIgnoreCase) == true
        );

        return item != null && item.Quantity >= Rank10ClassPoints;
    }

    public bool RunCooperativeObjective(
        string objectiveName,
        Func<bool> isComplete,
        Action doWork,
        Action help,
        int maxWorkAttempts = 0
    ) => Army.RunCooperativeObjective(
        objectiveName,
        isComplete,
        doWork,
        (targetPlayerNumber, targetPlayerName) => help(),
        maxWorkAttempts
    );

    public bool RunCooperativeObjective(
        string objectiveName,
        Func<bool> isComplete,
        Action doWork,
        Action<int, string> help,
        int maxWorkAttempts = 0
    ) => Army.RunCooperativeObjective(objectiveName, isComplete, doWork, help, maxWorkAttempts);

    public bool IsPlayer(int playerNumber) => Army.IsArmyPlayer(playerNumber);

    public bool StopArmySync(string reason) => Army.StopArmySync(reason);

    public bool WaitForArmyStop() => Army.SyncArmy("STOP_CHECK");
}
