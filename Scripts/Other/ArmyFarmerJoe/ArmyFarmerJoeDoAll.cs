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
using Skua.Core.Models.Quests;
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

    private int GetLocalPlayerNumber()
    {
        if (Army.ArmyPlayerNumber > 0)
            return Army.ArmyPlayerNumber;

        for (int playerNumber = 1; playerNumber <= 7; playerNumber++)
        {
            if (IsPlayer(playerNumber))
                return playerNumber;
        }

        return 1;
    }

    private int GetSubgroupRoomIndex(int maxPlayersPerRoom)
    {
        if (maxPlayersPerRoom <= 0)
            return 0;

        return (GetLocalPlayerNumber() - 1) / maxPlayersPerRoom;
    }

    private bool ApplyArmyRoomSettings(int roomOffset = 0)
    {
        const int MinRoom = 1001;
        const int MaxRoom = 99999;

        string? rawRoom = Bot.Config?.Get<string>("RoomNumber")?.Trim();
        int roomNumber = int.TryParse(rawRoom, out int parsedRoom) ? parsedRoom : 0;

        if (!Army.ValidatePrivateRoomNumber(roomNumber))
            return false;

        int span = MaxRoom - MinRoom + 1;
        int normalizedOffset = ((roomOffset % span) + span) % span;
        int effectiveRoom = MinRoom + (((roomNumber - MinRoom) + normalizedOffset) % span);

        CoreBots core = CoreBots.Instance;
        core.PrivateRooms = true;
        core.PrivateRoomNumber = effectiveRoom;
        core.PublicDifficult = false;
        return true;
    }

    private bool ApplySubgroupRoomSettings(int maxPlayersPerRoom) =>
        ApplyArmyRoomSettings(GetSubgroupRoomIndex(maxPlayersPerRoom));

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

                    SetClass(ClassType.Farm);
                    Farm.Experience(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "Bone Berserker",
                        compositionType: ClassType.Farm
                    );
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

                    SetClass(ClassType.Farm);
                    Farm.Experience(25);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        compositionType: ClassType.Farm
                    );
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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
                        SetClass(ClassType.Farm);
                        Daily.EldersBlood();
                    }
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "Gorillaphant",
                        compositionType: ClassType.Farm
                    );
                },
                maxWorkAttempts: 2
            )
        )
            return false;

        // 1b. Approved Bracket 30 Weeklies & Early Daily (Design Notes 1213, Wheel of Doom 3076/3075, Odd's 'n' Ends 6187)
        if (!RunBracket30ApprovedDailiesAndWeeklies())
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

                    SetClass(ClassType.Farm);
                    Farm.GetBoost("REP", 10, true);
                    Farm.GetBoost("XP", Bot.Player.Level >= 100 ? 0 : 10, true);
                    Boosts.GetBoostsSelect(10, 10, 0);
                    boostsDone = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        compositionType: ClassType.Farm
                    );
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
                    SetClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (
                        !enhancedBracket30
                        || Bot.Inventory.Items.Any(x =>
                            x != null && x.Equipped && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(currentClass ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(currentClass))
                            Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
                        enhancedBracket30 = true;
                    }

                    Farm.Experience(30);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        compositionType: ClassType.Farm
                    );
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
                    SetClass(ClassType.Farm);
                    Farm.SandseaREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

        // 5d. Imperial Chunin (Yokai Rep 10 in /dragonkoiz — room limit is 3 players, so split the
        // Army into <=3-player sub-rooms for farming Pockey Chew and buying from Shop 95, then
        // restore the shared private room before ranking Imperial Chunin in /icestormarena).
        const int DragonkoiMaxPlayers = 3;
        bool HasImperialChunin() => HasClassInInventory("Imperial Chunin");
        bool HasImperialChuninRank10() =>
            HasImperialChunin() && HasRank10Class("Imperial Chunin");
        void LeaveDragonkoizIfPresent()
        {
            if (string.Equals(Bot.Map?.Name, "dragonkoiz", StringComparison.OrdinalIgnoreCase))
                LeaveCombatRoomForEquipmentChange();
        }

        LeaveDragonkoizIfPresent();
        if (!ApplySubgroupRoomSettings(DragonkoiMaxPlayers))
            return false;

        core.Logger(
            $"Level 30: /dragonkoiz capped at {DragonkoiMaxPlayers} players; subgroup {GetSubgroupRoomIndex(DragonkoiMaxPlayers) + 1} using private room {core.PrivateRoomNumber}."
        );

        try
        {
            if (
                !RunCooperativeObjective(
                    "BRACKET30_CHUNIN_YOKAI_REP",
                    () => HasImperialChunin() || Farm.FactionRank("Yokai") >= 10,
                    () =>
                    {
                        if (HasImperialChunin() || Farm.FactionRank("Yokai") >= 10)
                            return;

                        core.Logger("Level 30: Farming Yokai Rep 10 for Imperial Chunin");
                        SetClass(ClassType.Farm);
                        Farm.YokaiREP(10);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        if (
                            targetPlayerNumber <= 0
                            || string.IsNullOrWhiteSpace(targetPlayerName)
                        )
                        {
                            LeaveDragonkoizIfPresent();
                            Bot.Sleep(1000);
                            return;
                        }

                        Army.CoordinateFollowAndCombat(
                            targetPlayerName,
                            "Pockey Chew",
                            compositionType: ClassType.Farm
                        );
                    },
                    maxPlayersPerSubgroup: DragonkoiMaxPlayers
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
                        LeaveDragonkoizIfPresent();
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                    {
                        LeaveDragonkoizIfPresent();
                        Bot.Sleep(1000);
                    },
                    maxWorkAttempts: 3,
                    maxPlayersPerSubgroup: DragonkoiMaxPlayers
                )
            )
                return false;

            LeaveDragonkoizIfPresent();
        }
        finally
        {
            ApplyArmyRoomSettings();
        }

        core.Logger(
            $"Level 30: Restored shared private room {core.PrivateRoomNumber} after /dragonkoiz."
        );

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
                    SetClass(ClassType.Farm);
                    Farm.ArcangroveREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(2935);
                    core.HuntMonster("castleundead", "Skeletal Viking", "Hilt Found!", isTemp: false);
                    core.EnsureComplete(2935);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Skeletal Viking", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(2936);
                    core.HuntMonster("hydra", "Hydra Head", "Blade Found!", isTemp: false);
                    core.EnsureComplete(2936);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Hydra Head", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
                    core.ResetQuest(2937);
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
                    SetClass(ClassType.Farm);
                    Farm.BladeofAweREP(10, farmBoA: true);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Skeletal Viking", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Farm.BlacksmithingREP(4, false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (
                        !enhancedBracket50
                        || Bot.Inventory.Items.Any(x =>
                            x != null && x.Equipped && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(currentClass ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(currentClass))
                            Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
                        enhancedBracket50 = true;
                    }

                    Farm.Experience(50);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        compositionType: ClassType.Farm
                    );
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
                    SetClass(ClassType.Solo);
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
                        if (
                            !string.Equals(
                                Bot.Player.CurrentClass?.Name,
                                "Blood Sorceress",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            LeaveCombatRoomForEquipmentChange();
                            Adv.SmartEnhance("Blood Sorceress");
                            core.Equip("Blood Sorceress");
                            Bot.Wait.ForTrue(() => Bot.Inventory.IsEquipped("Blood Sorceress"), 20);
                        }
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
                            {
                                LeaveCombatRoomForEquipmentChange();
                                core.Equip(fallbackClass);
                            }
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

                    SetClass(ClassType.Farm);
                    Farm.Gold(30000);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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
                    SetClass(ClassType.Farm);
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
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tempest Dracolich", compositionType: ClassType.Farm);
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
                    SetClass(ClassType.Solo);
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

        // 8. Approved Bracket 50 Dailies (Unlock "A Loyal Follower" 2084 -> Mine Crafting 2091 -> ShadowScythe General 3828/3827)
        if (!RunBracket50ApprovedDailies())
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

                    SetClass(ClassType.Farm);
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
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Knight's Reflection", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5299);
                    core.HuntMonster("hedgemaze", "Mirrored Shard", "Mirror Shard Slain", 4);
                    core.HuntMonsterMapID("hedgemaze", 48, "Hedge Goblin Slain", 4);
                    core.HuntMonsterMapID("hedgemaze", 20, "Minotaur Slain", 3);
                    Story.MapItemQuest(5299, "hedgemaze", 4679);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Story.KillQuest(5300, "hedgemaze", "Knight's Reflection");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Knight's Reflection", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Story.KillQuest(5302, "hedgemaze", "Hedge Goblin");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Hedge Goblin", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Story.KillQuest(5304, "hedgemaze", "Mirrored Shard");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Mirrored Shard", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5310);
                    core.KillMonster("hedgemaze", "r21", "Right", "*", "Maze Monster Killed", 8);
                    core.EnsureComplete(5310);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5314);
                    core.HuntMonster("towerofmirrors", "Glassgoyle", "Glass Powder", 8);
                    core.HuntMonster("towerofmirrors", "Glass Serpent", "Reflective Venom", 4);
                    core.EnsureComplete(5314);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5315);
                    core.GetMapItems(new[] { (4691, 1), (4692, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Mirror Key", 1);
                    core.EnsureComplete(5315);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5316);
                    core.HuntMonster("towerofmirrors", "Phans", "Mosh Past the Phans", 8);
                    core.HuntMonster("towerofmirrors", "Phans", "Backstage Pass", 1);
                    core.EnsureComplete(5316);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Phans", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5318);
                    core.GetMapItems(new[] { (4687, 1), (4693, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Andres' Mirror Key", 1);
                    core.EnsureComplete(5318);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
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
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Runway Wraith", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
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
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5322);
                    core.HuntMonster("towerofmirrors", "Pageant Mom", "Pageant Mom Defeated", 8);
                    core.HuntMonster("towerofmirrors", "Pageant Mom", "Pageant Sash", 1);
                    core.EnsureComplete(5322);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Pageant Mom", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
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
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5325);
                    core.HuntMonster("towerofmirrors", "Stage Tech", "Stage Tech Defeated", 9);
                    core.HuntMonster("towerofmirrors", "Stage Tech", "Rope Ladder");
                    core.EnsureComplete(5325);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Stage Tech", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5327);
                    core.GetMapItems(new[] { (4690, 1), (4696, 1) }, "towerofmirrors");
                    core.HuntMonster("towerofmirrors", "Silver Elemental", "Dora's Mirror Key", 1);
                    core.EnsureComplete(5327);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Silver Elemental", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5328);
                    core.HuntMonster("towerofmirrors", "Sasquatch", "Sasquatches Defeated", 10);
                    core.HuntMonster("towerofmirrors", "Sasquatch", "Tracking Tag", 5);
                    core.EnsureComplete(5328);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Sasquatch", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    Story.KillQuest(5330, "towerofmirrors", "Fervent Suitor");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Fervent Suitor", compositionType: ClassType.Farm);
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
                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (
                        !string.IsNullOrWhiteSpace(currentClass)
                        && Bot.Inventory.Items.Any(x =>
                            x != null && x.Equipped && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(currentClass);
                        Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
                    }

                    Farm.Experience(55);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "*",
                        compositionType: ClassType.Farm
                    );
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
                    SetClass(ClassType.Solo);
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

                    if (!Bot.Inventory.IsEquipped("Pyromancer"))
                    {
                        LeaveCombatRoomForEquipmentChange();
                        core.Equip("Pyromancer");
                        Bot.Wait.ForTrue(() => Bot.Inventory.IsEquipped("Pyromancer"), 20);
                    }
                    Bot.Skills.StartAdvanced("Pyromancer", false, ClassUseMode.Base);
                    core.KillMonster("noobshire", "Enter", "Spawn", "*", log: false);
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

                    SetClass(ClassType.Farm);
                    Farm.DoomWoodREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Farm.EmberseaREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm);
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
                        SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    Daily.Cryomancer();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm);
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

                    SetClass(ClassType.Farm);
                    Farm.RavenlossREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "ChaosWeaver Mage", compositionType: ClassType.Farm);
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

        // 5. Approved Bracket 55 Dailies (Seal of Light 3826 & Seal of Darkness 3825)
        if (!RunBracket55ApprovedDailies())
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

                    SetClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (
                        !string.IsNullOrWhiteSpace(currentClass)
                        && Bot.Inventory.Items.Any(x =>
                            x != null && x.Equipped && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(currentClass);
                        Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
                    }

                    Farm.Experience(60);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "*",
                        compositionType: ClassType.Farm
                    );
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
                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    Farm.BlacksmithingREP(4, false);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm);
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
                        SetClass(ClassType.Solo);
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
                        SetClass(ClassType.Solo);
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
                        SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    core.GetMapItem(54, 7, "greenguardeast");
                    core.HuntMonster("greenguardeast", "Spider", "Tiny Sword");
                    core.EnsureComplete(316);
                    Bot.Wait.ForPickup("Sword in the Stone");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Spider", compositionType: ClassType.Farm);
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
                    SetClass(ClassType.Farm);
                    core.GetMapItem(55, 4, "farm");
                    core.HuntMonster("farm", "Mosquito", "Mosquito Juice");
                    core.EnsureComplete(301);
                    Bot.Wait.ForPickup("Forest Axe");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Mosquito", compositionType: ClassType.Farm);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);

                    string? currentClass = Bot.Player.CurrentClass?.Name;
                    if (
                        !string.IsNullOrWhiteSpace(currentClass)
                        && Bot.Inventory.Items.Any(x =>
                            x != null && x.Equipped && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(currentClass);
                        Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
                    }

                    Farm.Experience(65);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "*",
                        compositionType: ClassType.Farm
                    );
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

                    SetClass(ClassType.Farm);
                    Farm.GlaceraREP(10);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm);
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

                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*");
                }
            )
        )
            return false;

        // 3. Approved Bracket 65 Dailies (Embrace Your Chaos 3596 & Lord of Order 7156-7165)
        if (!RunBracket65ApprovedDailies())
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3908, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3911);
                    core.HuntMonster("frozentower", "Frostwyrm", "Refugee Saved", 7);
                    core.EnsureComplete(3911);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3914, "frozentower", "Twisted Ice");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Twisted Ice", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3916, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3919);
                    core.GetMapItem(3008, 6, "frozentower");
                    core.HuntMonster("frozentower", "Polar Elemental", "Ice Sample", 6);
                    core.EnsureComplete(3919);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3920);
                    core.GetMapItem(3020, 1, "frozentower");
                    core.HuntMonster("frozentower", "Polar Elemental", "Polar Elemental Guard Defeated", 4);
                    core.HuntMonster("frozentower", "Ice Wolf", "Ice Wolf Guard Defeated", 5);
                    core.EnsureComplete(3920);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3922, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3923, "frozentower", "Frostwyrm");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3925);
                    core.GetMapItem(3012, 4, "frozentower");
                    core.GetMapItem(3011, 4, "frozentower");
                    core.HuntMonster("frozentower", "Frostwyrm", "Frozen Tail", 5);
                    core.HuntMonster("frozentower", "Arctic Eel", "Frozen Scale", 5);
                    core.EnsureComplete(3925);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3928, "frozentower", "Arctic Eel");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Arctic Eel", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3929, "frozentower", "Polar Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3930, "frozentower", "Twisted Ice");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Twisted Ice", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3931, "frozentower", "Frostwyrm");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frostwyrm", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3933, "frozentower", "Ice Wolf");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Ice Wolf", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3935);
                    core.GetMapItem(3018, 13, "frozentower");
                    core.HuntMonster("frozentower", "Ice Wolf", "Blood of the Ice Wolf", 1);
                    core.EnsureComplete(3935);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Ice Wolf", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3936);
                    core.HuntMonster("frozentower", "Frost Invader", "FrostSpawn Invader defeated", 10);
                    core.EnsureComplete(3936);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3937);
                    core.HuntMonster("frozentower", "Frost Fangbeast", "Fangbeasts defeated", 15);
                    core.EnsureComplete(3937);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast", compositionType: ClassType.Farm)
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

                        SetClass(ClassType.Farm);
                        core.EnsureAccept(wq.QuestId);
                        core.GetMapItem(wq.MapItemId, 1, "weaverwar");
                        core.KillMonster("weaverwar", wq.Cell, "Spawn", wq.Monster, wq.TempItem, wq.Quant);
                        core.EnsureComplete(wq.QuestId);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(targetPlayerName, wq.Monster, compositionType: ClassType.Farm)
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

                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6450);
                    core.HuntMonster("shogunwar", "Shadow Samurai", "Shadow Medallions", 5);
                    core.EnsureComplete(6450);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6451);
                    core.HuntMonsterMapID("shogunwar", 8, "Kijimuna Pollen", 4);
                    core.HuntMonsterMapID("shogunwar", 7, "Reishi Spores", 4);
                    core.EnsureComplete(6451);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6453, "shogunwar", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6454, "shogunwar", "Bamboo Treeant");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bamboo Treeant", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6455, "shogunwar", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6457, "shogunwar", "Reishi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Reishi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6458, "shogunwar", "Bamboo Treeant");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bamboo Treeant", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6463);
                    core.HuntMonster("shinringrove", "Reishi", "Reishi Caps", 6);
                    core.HuntMonster("shinringrove", "Tsurubebi", "Tsurubebi Flame", 3);
                    core.EnsureComplete(6463);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6465);
                    core.GetMapItem(5964, 5, "greenshell");
                    core.HuntMonster("greenshell", "Tsurubebi", "Tsurubebi Extinguished", 5);
                    core.EnsureComplete(6465);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tsurubebi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6466, "greenshell", "Tsukumogami");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Tsukumogami", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6467, "shinringrove", "Kodama");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kodama", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6468, "shinringrove", "Moglinberry Bush");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Moglinberry Bush", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6469);
                    core.GetMapItem(5965, 8, "greenshell");
                    core.HuntMonster("shinringrove", "Reishi", "Luminous Spores", 6);
                    core.EnsureComplete(6469);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Reishi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6470);
                    core.GetMapItem(5966, 1, "greenshell");
                    core.HuntMonster("greenshell", "Merdraconian", "Merdraconians Slain", 3);
                    core.EnsureComplete(6470);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Merdraconian", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6474, "heiwavalley", "Abumi Guchi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Abumi Guchi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6475);
                    core.HuntMonster("heiwavalley", "Shadow Samurai", "Pilfered Armor", 5);
                    core.HuntMonster("heiwavalley", "Shadow Samurai", "Stolen Sword", 5);
                    core.EnsureComplete(6475);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6476);
                    core.GetMapItem(5968, 6, "heiwavalley");
                    core.HuntMonster("heiwavalley", "Kosenjobi", "Kosenjobi Flame", 6);
                    core.EnsureComplete(6476);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kosenjobi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6477, "heiwavalley", "Goryo");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Goryo", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6478);
                    core.HuntMonster("heiwavalley", "Kosenjobi", "Spectral Heat", 5);
                    Story.BuyQuest(6478, "heiwavalley", 1608, "Wasabi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Kosenjobi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6479, "heiwavalley", "Abumi Guchi");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Abumi Guchi", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6480, "heiwavalley", "Inugami");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Inugami", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6481);
                    core.HuntMonster("heiwavalley", "Inugami", "Spirit Eyes", 4);
                    Story.MapItemQuest(6481, "heiwavalley", 5970, 8);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Inugami", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6485, "shadowfortress", "Restless Spirit");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Restless Spirit", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6486, "shadowfortress", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6488);
                    core.GetMapItem(5974, 1, "shadowfortress");
                    core.HuntMonster("shadowfortress", "Living Shadow", "Shadow Essence", 7);
                    core.EnsureComplete(6488);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Living Shadow", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(6489, "shadowfortress", "Shadow Samurai");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Shadow Samurai", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(6490);
                    core.GetMapItem(5975, 7, "shadowfortress");
                    core.HuntMonster("shadowfortress", "Creeping Shadow", "Shadows Slain", 7);
                    core.EnsureComplete(6490);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Creeping Shadow", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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

                        SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3941, "frozenruins", "Frost Fangbeast");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast", compositionType: ClassType.Farm)
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
                        SetClass(ClassType.Farm);
                        Story.KillQuest(currentQuest, "frozenruins", "Frost Reaper");
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3945);
                    core.GetMapItem(3050, 10, "frozenruins");
                    core.HuntMonster("frozenruins", "Frozen Moglinster", "Frostbreath Weed", 5);
                    core.EnsureComplete(3945);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frozen Moglinster", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3946);
                    if (!core.CheckInventory(27357))
                        core.HuntMonster("frozenruins", "Frost Reaper", "Mercury");
                    core.EnsureComplete(3946);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3948, "glacera", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(3951);
                    core.HuntMonster("frozenruins", "Frost Invader", "Ice Beast Destroyed", 10);
                    core.EnsureComplete(3951);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3952, "frozenruins", "Frost Fangbeast");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Fangbeast", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(3953, "frozenruins", "Frost Reaper");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Reaper", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(5588, "icewindpass", "Glacial Elemental");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Glacial Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    core.EnsureAccept(5589);
                    core.GetMapItem(5075, 5, "icewindpass");
                    core.HuntMonster("icewindpass", "Glacial Elemental", "Iced Mana", 9);
                    core.EnsureComplete(5589);
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Glacial Elemental", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(5590, "icewindpass", "Polar Golem");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Polar Golem", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(5591, "icewindpass", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
                    SetClass(ClassType.Farm);
                    Story.KillQuest(5593, "icewindpass", "Frost Invader");
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Frost Invader", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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

                    SetClass(ClassType.Farm);
                    core.EnsureAcceptmultiple(new[] { 5597, 5598, 5599, 5600 });
                    core.HuntMonster("icewindwar", "Glaceran Defender", "FrostSpawn Medal", 10);
                    core.HuntMonster("icewindwar", "Glaceran Defender", "Mega Frostspawn Medal", 5);
                    core.HuntMonster("icewindwar", "Frost Invader", "World Ender Medal", 10);
                    core.HuntMonster("icewindwar", "Frost Invader", "Mega World Ender Medal", 5);
                    core.EnsureComplete(new[] { 5597, 5598, 5599, 5600 });
                    warMedalsDone = true;
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(targetPlayerName, "*", compositionType: ClassType.Farm)
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
                    SetClass(ClassType.Solo);
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
    private bool _cboInitialized;
    private ClassType _syncedClassType = ClassType.None;

    public void SetClass(bool rankUpIfNeeded = false) =>
        SetClass(ClassType.Solo, rankUpIfNeeded);

    public void SetClass(ClassType targetType, bool rankUpIfNeeded = false)
    {
        if (System.Threading.Interlocked.Exchange(ref _setClassBusy, 1) == 1)
        {
            CoreBots.Instance.Logger("SetClass is already running, skipping the overlapping call.");
            return;
        }

        try
        {
            SetClassInner(targetType, rankUpIfNeeded);
        }
        finally
        {
            System.Threading.Volatile.Write(ref _setClassBusy, 0);
        }
    }

    private string ResolveDesiredClass(CoreBots core, ClassType targetType, out ClassType effectiveType)
    {
        if (
            targetType == ClassType.Farm
            && !CoreBots.IsGenericClass(core.FarmClass)
            && !string.IsNullOrWhiteSpace(core.FarmClass)
            && HasClassInInventory(core.FarmClass)
        )
        {
            effectiveType = ClassType.Farm;
            return core.FarmClass;
        }

        effectiveType = targetType == ClassType.Farm
            && !string.Equals(core.FarmClass, core.SoloClass, StringComparison.OrdinalIgnoreCase)
            ? ClassType.Solo
            : targetType;
        return core.SoloClass;
    }

    private void SetClassInner(ClassType targetType, bool rankUpIfNeeded)
    {
        CoreBots core = CoreBots.Instance;
        if (!_cboInitialized || rankUpIfNeeded)
        {
            core.ReadCBO();
            _cboInitialized = true;
        }
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
        string resolvedSolo = ResolveClass(bossPool);
        string resolvedFarm = ResolveClass(farmPool);

        core.SoloClass = resolvedSolo;
        core.DodgeClass = resolvedSolo;
        core.BossClass = resolvedSolo;
        core.FarmClass = resolvedFarm;

        if (
            !string.Equals(_lastSolo, resolvedSolo, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_lastFarm, resolvedFarm, StringComparison.OrdinalIgnoreCase)
        )
        {
            _syncedClassType = ClassType.None;
        }

        bool Settled(string? className) =>
            CoreBots.IsGenericClass(className)
            || (!string.IsNullOrWhiteSpace(className) && HasRank10Class(className));

        if (
            rankUpIfNeeded
            && (
                !string.Equals(_lastSolo, resolvedSolo, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_lastFarm, resolvedFarm, StringComparison.OrdinalIgnoreCase)
                || !Settled(resolvedSolo)
                || !Settled(resolvedFarm)
            )
        )
        {
            RankIfNeeded(resolvedSolo, "BossComposition");
            RankIfNeeded(resolvedFarm, "FarmComposition");
            Bot.Sleep(500);
            _lastSolo = resolvedSolo;
            _lastFarm = resolvedFarm;
        }
        else if (string.IsNullOrEmpty(_lastSolo) && string.IsNullOrEmpty(_lastFarm))
        {
            _lastSolo = resolvedSolo;
            _lastFarm = resolvedFarm;
        }

        string desiredClass = ResolveDesiredClass(core, targetType, out ClassType effectiveType);
        string currentClass = Bot.Player.CurrentClass?.Name ?? string.Empty;
        bool alreadyWearingDesired =
            !CoreBots.IsGenericClass(desiredClass)
            && string.Equals(currentClass, desiredClass, StringComparison.OrdinalIgnoreCase);

        if (alreadyWearingDesired)
        {
            if (!Bot.Skills.TimerRunning)
                Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
            _syncedClassType = effectiveType;
            return;
        }

        if (CoreBots.IsGenericClass(desiredClass))
        {
            if (_syncedClassType != effectiveType)
            {
                LeaveCombatRoomForEquipmentChange();
                core.EquipClass(effectiveType);
                _syncedClassType = effectiveType;
            }
            else if (!Bot.Skills.TimerRunning)
            {
                Bot.Skills.StartAdvanced(
                    string.IsNullOrWhiteSpace(currentClass) ? desiredClass : currentClass,
                    false,
                    ClassUseMode.Base
                );
            }
            return;
        }

        if (HasClassInInventory(desiredClass))
        {
            LeaveCombatRoomForEquipmentChange();
            core.EquipClass(effectiveType);
            _syncedClassType = effectiveType;
        }
        else if (!Bot.Skills.TimerRunning && !string.IsNullOrWhiteSpace(currentClass))
        {
            Bot.Skills.StartAdvanced(currentClass, false, ClassUseMode.Base);
        }
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

        _syncedClassType = ClassType.None;

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

    #region Approved Dailies, Weeklies & Future NSoD Daily
    private sealed class CoordinatedStoryStep
    {
        public CoordinatedStoryStep(
            int questId,
            string map,
            string helpMonster,
            Action<CoreBots> work,
            ClassType role = ClassType.Farm,
            bool isCombat = true,
            bool skipMapVerify = false
        )
        {
            QuestId = questId;
            Map = map;
            HelpMonster = helpMonster;
            Work = work;
            Role = role;
            IsCombat = isCombat;
            SkipMapVerify = skipMapVerify;
        }

        public int QuestId { get; }
        public string Map { get; }
        public string HelpMonster { get; }
        public Action<CoreBots> Work { get; }
        public ClassType Role { get; }
        public bool IsCombat { get; }
        public bool SkipMapVerify { get; }
    }

    private Quest? TryLoadQuest(int questId)
    {
        CoreBots core = CoreBots.Instance;
        return core.InitializeWithRetries(() => core.EnsureLoad(questId));
    }

    private bool CanRunPeriodicQuest(int questId, bool requireAvailable = false)
    {
        if (TryLoadQuest(questId) == null)
            return false;

        if (Bot.Quests.IsDailyComplete(questId))
            return false;

        return !requireAvailable || Bot.Quests.IsAvailable(questId);
    }

    private int GetInventoryAndBankQuantity(int itemId) =>
        (Bot.Inventory?.Items ?? Enumerable.Empty<InventoryItem>())
            .Concat(Bot.Bank?.Items ?? Enumerable.Empty<InventoryItem>())
            .Where(item => item != null && item.ID == itemId)
            .Sum(item => item.Quantity);

    private void BankIfPresent(params string[] itemNames)
    {
        if (itemNames == null || itemNames.Length == 0 || Bot.Inventory?.Items == null)
            return;

        string[] owned = itemNames
            .Where(name =>
                !string.IsNullOrWhiteSpace(name)
                && Bot.Inventory.Items.Any(item =>
                    item?.Name != null
                    && string.Equals(item.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)
                )
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (owned.Length == 0)
            return;

        LeaveCombatRoomForEquipmentChange();
        CoreBots.Instance.ToBank(owned);
    }

    private void BankQuestExtras(int questId, params string[] excludeNames)
    {
        string[] rewards = CoreBots.Instance.QuestRewards(questId);
        if (rewards == null || rewards.Length == 0)
            return;

        if (excludeNames != null && excludeNames.Length > 0)
        {
            rewards = rewards
                .Where(r => !excludeNames.Any(ex => string.Equals(r, ex, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        BankIfPresent(rewards);
    }

    private void UnbankIfOwned(params string[] itemNames)
    {
        if (itemNames == null || itemNames.Length == 0 || Bot.Bank?.Items == null)
            return;

        string[] inBank = itemNames
            .Where(name =>
                !string.IsNullOrWhiteSpace(name)
                && Bot.Bank.Items.Any(item =>
                    item?.Name != null
                    && string.Equals(item.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)
                )
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (inBank.Length > 0)
            CoreBots.Instance.Unbank(inBank);
    }

    private bool JoinMapVerified(string map, string? cell = null, string? pad = null)
    {
        CoreBots core = CoreBots.Instance;
        if (!string.IsNullOrEmpty(cell) && !string.IsNullOrEmpty(pad))
            core.Join(map, cell, pad);
        else
            core.Join(map);

        return string.Equals(Bot.Map?.Name, map, StringComparison.OrdinalIgnoreCase);
    }

    private bool RunCoordinatedTurnInStep(
        string objectiveName,
        Func<bool> initialSkipCheck,
        Func<bool> liveDoneCheck,
        Action work
    )
    {
        bool done = initialSkipCheck();
        return RunCooperativeObjective(
            objectiveName,
            () => done || liveDoneCheck(),
            () =>
            {
                if (done || liveDoneCheck())
                {
                    done = true;
                    return;
                }

                LeaveCombatRoomForEquipmentChange();
                work();
                done = true;
            },
            (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
            maxWorkAttempts: 2
        );
    }

    private bool RunCoordinatedCombatStep(
        string objectiveName,
        Func<bool> initialSkipCheck,
        Func<bool> liveDoneCheck,
        string helpMonster,
        ClassType role,
        Action work,
        int softRetryLimit = 2
    )
    {
        int attempts = 0;
        bool done = initialSkipCheck();

        return RunCooperativeObjective(
            objectiveName,
            () => done || liveDoneCheck(),
            () =>
            {
                if (done || liveDoneCheck())
                {
                    done = true;
                    return;
                }

                attempts++;
                SetClass(role);
                work();

                if (liveDoneCheck())
                {
                    done = true;
                    return;
                }

                if (softRetryLimit > 0 && attempts >= softRetryLimit)
                {
                    CoreBots.Instance.Logger(
                        $"Daily objective '{objectiveName}' could not finish after {attempts} attempts; skipping cleanly so the Army can continue."
                    );
                    done = true;
                }
            },
            (targetPlayerNumber, targetPlayerName) =>
                Army.CoordinateFollowAndCombat(targetPlayerName, helpMonster, compositionType: role),
            maxWorkAttempts: softRetryLimit > 0 ? softRetryLimit + 1 : 0
        );
    }

    private bool RunCoordinatedHuntStep(
        string objectiveName,
        int questId,
        string map,
        string monster,
        string itemName,
        int quant = 1,
        bool isTemp = false,
        ClassType role = ClassType.Solo,
        string? cell = null,
        string? pad = null,
        bool acceptQuest = true,
        bool checkBankAlso = true,
        int softRetryLimit = 2,
        Action<CoreBots>? beforeHunt = null,
        Func<bool>? isEnabled = null
    )
    {
        CoreBots core = CoreBots.Instance;

        bool IsStepDisabled() => isEnabled != null && !isEnabled();

        bool HasTargetItem() =>
            isTemp
                ? Bot.TempInv.Contains(itemName, quant)
                : core.CheckInventory(itemName, quant, toInv: !checkBankAlso);

        return RunCoordinatedCombatStep(
            objectiveName,
            () => IsStepDisabled() || HasTargetItem(),
            () => IsStepDisabled() || HasTargetItem(),
            monster,
            role,
            () =>
            {
                if (IsStepDisabled())
                    return;

                beforeHunt?.Invoke(core);
                if (!isTemp)
                    core.AddDrop(itemName);
                if (acceptQuest && questId > 0)
                    core.EnsureAccept(questId);

                if (!JoinMapVerified(map, cell, pad))
                {
                    core.Logger($"Map '/{map}' is currently unavailable for '{objectiveName}'.");
                    return;
                }

                if (!string.IsNullOrEmpty(cell) && !string.IsNullOrEmpty(pad))
                    core.KillMonster(map, cell, pad, monster, itemName, quant, isTemp);
                else
                    core.HuntMonster(map, monster, itemName, quant, isTemp);
            },
            softRetryLimit: softRetryLimit
        );
    }

    private bool RunCoordinatedStoryChain(
        string objectivePrefix,
        CoordinatedStoryStep[] steps,
        Func<bool>? isEnabled = null,
        params string[] dropsToKeep
    )
    {
        CoreBots core = CoreBots.Instance;
        bool IsDisabled() => isEnabled != null && !isEnabled();

        if (!IsDisabled() && dropsToKeep != null && dropsToKeep.Length > 0)
            core.AddDrop(dropsToKeep);

        foreach (CoordinatedStoryStep step in steps)
        {
            if (Bot.ShouldExit)
                return false;

            string objName = $"{objectivePrefix}_{step.QuestId}";
            if (!step.IsCombat)
            {
                if (
                    !RunCooperativeObjective(
                        objName,
                        () => IsDisabled() || core.isCompletedBefore(step.QuestId),
                        () =>
                        {
                            if (IsDisabled() || core.isCompletedBefore(step.QuestId))
                                return;

                            LeaveCombatRoomForEquipmentChange();
                            step.Work(core);
                        },
                        (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                        maxWorkAttempts: 3
                    )
                )
                    return false;

                continue;
            }

            if (
                !RunCooperativeObjective(
                    objName,
                    () => IsDisabled() || core.isCompletedBefore(step.QuestId),
                    () =>
                    {
                        if (IsDisabled() || core.isCompletedBefore(step.QuestId))
                            return;

                        SetClass(step.Role);
                        if (!step.SkipMapVerify && !string.IsNullOrEmpty(step.Map))
                            core.Join(step.Map);
                        step.Work(core);
                    },
                    (targetPlayerNumber, targetPlayerName) =>
                        Army.CoordinateFollowAndCombat(
                            targetPlayerName,
                            step.HelpMonster,
                            compositionType: step.Role
                        )
                )
            )
                return false;
        }

        return !Bot.ShouldExit;
    }

    private bool RunBracket30ApprovedDailiesAndWeeklies()
    {
        CoreBots core = CoreBots.Instance;

        // 1b-1. Weekly: Read the Design Notes! [1213] (+150 Loremaster Rep)
        bool skipDesignNotes =
            Farm.FactionRank("Loremaster") >= 10
            || !CanRunPeriodicQuest(1213);
        if (
            !RunCoordinatedTurnInStep(
                "BRACKET30_DESIGN_NOTES_WEEKLY",
                () => skipDesignNotes,
                () => Bot.Quests.IsDailyComplete(1213) || Farm.FactionRank("Loremaster") >= 10,
                () =>
                {
                    core.Logger("Weekly: Read the Design Notes! [1213]");
                    core.ChainComplete(1213);
                }
            )
        )
            return false;

        // 1b-2. Weekly: Wheel of Doom [3076 F2P / 3075 Mem]
        bool HasGearForSpin() => core.CheckInventory("Gear of Doom", 3, toInv: false);
        bool f2pWheelReady = HasGearForSpin() && CanRunPeriodicQuest(3076);
        bool memWheelReady = core.IsMember && CanRunPeriodicQuest(3075);
        bool skipWheel = !f2pWheelReady && !memWheelReady;
        if (
            !RunCoordinatedTurnInStep(
                "BRACKET30_WHEEL_OF_DOOM",
                () => skipWheel,
                () =>
                    (!HasGearForSpin() || Bot.Quests.IsDailyComplete(3076))
                    && (!core.IsMember || Bot.Quests.IsDailyComplete(3075)),
                () =>
                {
                    core.Logger("Weekly: Wheel of Doom [3076/3075]");
                    if (HasGearForSpin())
                        core.Unbank("Gear of Doom");
                    core.AddDrop("Treasure Potion");
                    Daily.WheelofDoom(log: true);
                    BankIfPresent("Gear of Doom", "Treasure Potion");
                }
            )
        )
            return false;

        // 1b-3. Daily: Odd's 'n' Ends [6187] (Crypto Token, cap 300)
        bool cryptoCapped = core.CheckInventory("Crypto Token", 300, toInv: false);
        bool oddsAvailable = !cryptoCapped && CanRunPeriodicQuest(6187);
        if (
            !RunCoordinatedCombatStep(
                "BRACKET30_ODDS_N_ENDS_DAILY",
                () => !oddsAvailable,
                () => Bot.Quests.IsDailyComplete(6187) || core.CheckInventory("Crypto Token", 300, toInv: false),
                "Sneevil",
                ClassType.Farm,
                () =>
                {
                    core.Logger("Daily: Odd's 'n' Ends [6187] (/boxes Sneevil -> Crypto Token)");
                    UnbankIfOwned("Crypto Token");
                    core.AddDrop("Crypto Token");
                    core.EnsureAccept(6187);
                    core.KillMonster("boxes", "Closet", "Center", "Sneevil", "Metal Ore", 1, isTemp: true);
                    LeaveCombatRoomForEquipmentChange();
                    core.EnsureComplete(6187);
                    Bot.Wait.ForPickup("Crypto Token");
                    BankIfPresent("Crypto Token");
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    private bool EnsureLoyalFollowerUnlocked(string objectivePrefix)
    {
        CoreBots core = CoreBots.Instance;
        TryLoadQuest(2084);

        bool IsUnlocked() => core.isCompletedBefore(2084);
        if (IsUnlocked())
        {
            return RunCooperativeObjective(
                $"{objectivePrefix}_LOYAL_FOLLOWER_SYNC",
                IsUnlocked,
                () => { },
                (n, name) => Bot.Sleep(500),
                maxWorkAttempts: 1
            );
        }

        // Step 1: 2066 (Handle) & 2067 (Bonegrinder Medal, 15,000 Gold) in /doomwood Shop 276
        if (
            !RunCooperativeObjective(
                $"{objectivePrefix}_LOYAL_FOLLOWER_SETUP",
                () => IsUnlocked() || (core.isCompletedBefore(2066) && core.isCompletedBefore(2067)),
                () =>
                {
                    if (IsUnlocked() || (core.isCompletedBefore(2066) && core.isCompletedBefore(2067)))
                        return;

                    if (!core.isCompletedBefore(2067) && !core.CheckInventory("Bonegrinder Medal") && Bot.Player.Gold < 16000)
                        Farm.Gold(20000);

                    LeaveCombatRoomForEquipmentChange();
                    Story.BuyQuest(2066, "doomwood", 276, "Blinding Light of Destiny Handle");
                    Story.BuyQuest(2067, "doomwood", 276, "Bonegrinder Medal");
                },
                (targetPlayerNumber, targetPlayerName) => Bot.Sleep(1000),
                maxWorkAttempts: 3
            )
        )
            return false;

        // Step 2: 2082, 2083 & 100 Spirit Orbs in /battleunderb (Skeleton Warrior)
        bool AreSpiritOrbsReady() =>
            IsUnlocked()
            || (
                core.isCompletedBefore(2082)
                && core.isCompletedBefore(2083)
                && core.CheckInventory("Spirit Orb", 100, toInv: false)
            );

        if (
            !RunCooperativeObjective(
                $"{objectivePrefix}_LOYAL_FOLLOWER_SPIRIT_ORBS",
                AreSpiritOrbsReady,
                () =>
                {
                    if (AreSpiritOrbsReady())
                        return;

                    core.Logger("Unlocking Mine Crafting: 2082, 2083 & 100 Spirit Orbs in /battleunderb");
                    UnbankIfOwned("Spirit Orb", "Undead Essence", "Bone Dust");
                    core.AddDrop("Spirit Orb", "Undead Energy", "Undead Essence", "Bone Dust");
                    SetClass(ClassType.Farm);

                    if (!core.isCompletedBefore(2082))
                    {
                        core.EnsureAccept(2082);
                        core.HuntMonster("battleunderb", "Skeleton Warrior", "Undead Essence", 25, isTemp: false);
                        core.EnsureComplete(2082);
                    }

                    if (!core.isCompletedBefore(2083))
                    {
                        core.EnsureAccept(2083);
                        core.HuntMonster("battleunderb", "Skeleton Warrior", "Bone Dust", 40, isTemp: false);
                        core.EnsureComplete(2083);
                    }

                    if (!core.CheckInventory("Spirit Orb", 100))
                    {
                        try
                        {
                            core.RegisterQuests(2082, 2083);
                            while (!Bot.ShouldExit && !core.CheckInventory("Spirit Orb", 100))
                                core.KillMonster("battleunderb", "Enter", "Spawn", "Skeleton Warrior", log: false);
                        }
                        finally
                        {
                            core.CancelRegisteredQuests();
                        }
                    }
                },
                (targetPlayerNumber, targetPlayerName) =>
                    Army.CoordinateFollowAndCombat(
                        targetPlayerName,
                        "Skeleton Warrior",
                        compositionType: ClassType.Farm
                    )
            )
        )
            return false;

        // Step 3: 2084 ("A Loyal Follower" — Ephemerite in /timevoid)
        return RunCooperativeObjective(
            $"{objectivePrefix}_LOYAL_FOLLOWER_COMPLETE",
            IsUnlocked,
            () =>
            {
                if (IsUnlocked())
                    return;

                SetClass(ClassType.Solo);
                core.EnsureAccept(2084);
                core.HuntMonster("timevoid", "Ephemerite", "Celestial Compass");
                LeaveCombatRoomForEquipmentChange();
                core.EnsureComplete(2084);
                BankIfPresent("Spirit Orb", "Loyal Spirit Orb", "Undead Energy", "Bone Dust", "Undead Essence");
            },
            (targetPlayerNumber, targetPlayerName) =>
                Army.CoordinateFollowAndCombat(targetPlayerName, "Ephemerite")
        );
    }

    private (string Name, int ItemId) PickMineCraftingMetal(CoreBots core)
    {
        (string Name, int ItemId, int TargetQuant)[] priority =
        {
            ("Barium", (int)MineCraftingMetalsEnum.Barium, 1),
            ("Copper", (int)MineCraftingMetalsEnum.Copper, 1),
            ("Silver", (int)MineCraftingMetalsEnum.Silver, 1),
            ("Barium", (int)MineCraftingMetalsEnum.Barium, 4),
            ("Aluminum", (int)MineCraftingMetalsEnum.Aluminum, 1),
            ("Gold", (int)MineCraftingMetalsEnum.Gold, 1),
            ("Iron", (int)MineCraftingMetalsEnum.Iron, 1),
            ("Platinum", (int)MineCraftingMetalsEnum.Platinum, 1),
        };

        foreach ((string name, int itemId, int targetQuant) in priority)
        {
            if (!core.CheckInventory(itemId, targetQuant, toInv: false))
                return (name, itemId);
        }

        if (Farm.FactionRank("DoomWood") < 10)
        {
            return priority
                .DistinctBy(m => m.ItemId)
                .Select(m => (m.Name, m.ItemId, Owned: GetInventoryAndBankQuantity(m.ItemId)))
                .Where(m => m.Owned < 999)
                .OrderBy(m => m.Owned)
                .Select(m => (m.Name, m.ItemId))
                .FirstOrDefault();
        }

        return (string.Empty, 0);
    }

    private bool RunBracket50ApprovedDailies()
    {
        CoreBots core = CoreBots.Instance;

        // 8a. Unlock "A Loyal Follower" [2084] (prereq for Mine Crafting, Bright Knight Seals & Embrace Your Chaos)
        if (!EnsureLoyalFollowerUnlocked("BRACKET50"))
            return false;

        // 8b. Daily: Mine Crafting [2091] (Metals for BLoD/NSoD + 2,000 DoomWood Rep)
        (string targetMetalName, int targetMetalId) = PickMineCraftingMetal(core);
        bool mineCraftingAvailable =
            core.isCompletedBefore(2084)
            && targetMetalId > 0
            && CanRunPeriodicQuest(2091);

        if (
            !RunCoordinatedCombatStep(
                "BRACKET50_MINE_CRAFTING_DAILY",
                () => !mineCraftingAvailable,
                () => Bot.Quests.IsDailyComplete(2091),
                "Balboa",
                ClassType.Farm,
                () =>
                {
                    core.Logger($"Daily: Mine Crafting [2091] -> Target Metal: {targetMetalName} (+2,000 DoomWood Rep)");
                    UnbankIfOwned("Axe of the Prospector");
                    core.AddDrop("Axe of the Prospector", targetMetalName);
                    core.EnsureAccept(2091);
                    if (!core.CheckInventory("Axe of the Prospector"))
                        core.HuntMonster("stalagbite", "Balboa", "Axe of the Prospector", 1, isTemp: false);
                    core.HuntMonster("stalagbite", "Balboa", "Raw Ore", 30, isTemp: true);
                    LeaveCombatRoomForEquipmentChange();
                    core.EnsureComplete(2091, targetMetalId);
                    Bot.Wait.ForPickup(targetMetalName);
                    BankIfPresent(targetMetalName, "Axe of the Prospector");
                }
            )
        )
            return false;

        // 8c. Daily: ShadowScythe General [3828 F2P / 3827 Mem] (Lvl 50+, fixed F2P handling)
        bool HasSSG() => HasClassInInventory("ShadowScythe General");
        bool ssgF2PReady = Bot.Player.Level >= 50 && !HasSSG() && CanRunPeriodicQuest(3828);
        bool ssgMemReady = Bot.Player.Level >= 50 && !HasSSG() && core.IsMember && CanRunPeriodicQuest(3827);
        bool canBuySSGNow = !HasSSG() && core.CheckInventory("Shadow Shield", 100, toInv: false);

        if (
            !RunCoordinatedCombatStep(
                "BRACKET50_SHADOWSCYTHE_DAILY",
                () => HasSSG() || (!ssgF2PReady && !ssgMemReady && !canBuySSGNow),
                () =>
                    HasSSG()
                    || (
                        Bot.Quests.IsDailyComplete(3828)
                        && (!core.IsMember || Bot.Quests.IsDailyComplete(3827))
                        && !core.CheckInventory("Shadow Shield", 100, toInv: false)
                    ),
                "Citadel Crusader",
                ClassType.Farm,
                () =>
                {
                    core.Logger("Daily: ShadowScythe General [3828/3827]");
                    UnbankIfOwned("Shadow Shield");
                    core.AddDrop("Shadow Shield");

                    if (ssgF2PReady && !Bot.Quests.IsDailyComplete(3828))
                    {
                        core.EnsureAccept(3828);
                        core.HuntMonster("lightguardwar", "Citadel Crusader", "Broken Blade", 1, isTemp: true);
                        LeaveCombatRoomForEquipmentChange();
                        core.EnsureComplete(3828);
                        Bot.Wait.ForPickup("Shadow Shield");
                    }

                    if (ssgMemReady && !Bot.Quests.IsDailyComplete(3827))
                    {
                        core.EnsureAccept(3827);
                        core.HuntMonster("lightguardwar", "Citadel Crusader", "Broken Blade", 1, isTemp: true);
                        LeaveCombatRoomForEquipmentChange();
                        core.EnsureComplete(3827);
                        Bot.Wait.ForPickup("Shadow Shield");
                    }

                    if (!HasSSG() && core.CheckInventory("Shadow Shield", 100))
                    {
                        LeaveCombatRoomForEquipmentChange();
                        core.BuyItem("shadowfall", 1644, "ShadowScythe General");
                        Bot.Wait.ForItemBuy();
                    }

                    BankIfPresent("Shadow Shield");
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    private bool RunBracket55ApprovedDailies()
    {
        CoreBots core = CoreBots.Instance;

        if (!EnsureLoyalFollowerUnlocked("BRACKET55"))
            return false;

        bool HasBrightKnight() => core.CheckInventory("Bright Knight", toInv: false);
        bool unlocked = core.isCompletedBefore(2084);

        // 5a. Daily: Seal of Light [3826] (ULTRA Alteon)
        bool sealLightReady =
            unlocked
            && !HasBrightKnight()
            && !core.CheckInventory("Seal of Light", 50, toInv: false)
            && CanRunPeriodicQuest(3826);

        if (
            !RunCoordinatedCombatStep(
                "BRACKET55_SEAL_OF_LIGHT_DAILY",
                () => !sealLightReady,
                () => Bot.Quests.IsDailyComplete(3826) || HasBrightKnight(),
                "ULTRA Alteon",
                ClassType.Solo,
                () =>
                {
                    core.Logger("Daily: Seal of Light [3826] (/alteonbattle ULTRA Alteon)");
                    UnbankIfOwned("Seal of Light");
                    core.AddDrop("Seal of Light");
                    core.EnsureAccept(3826);
                    core.HuntMonster("alteonbattle", "ULTRA Alteon", "Alteon Defeated", 1, isTemp: true);
                    LeaveCombatRoomForEquipmentChange();
                    core.EnsureComplete(3826);
                    Bot.Wait.ForPickup("Seal of Light");
                }
            )
        )
            return false;

        // 5b. Daily: Seal of Darkness [3825] (ULTRA Sepulchure)
        bool sealDarknessReady =
            unlocked
            && !HasBrightKnight()
            && !core.CheckInventory("Seal of Darkness", 50, toInv: false)
            && CanRunPeriodicQuest(3825);

        if (
            !RunCoordinatedCombatStep(
                "BRACKET55_SEAL_OF_DARKNESS_DAILY",
                () => !sealDarknessReady,
                () => Bot.Quests.IsDailyComplete(3825) || HasBrightKnight(),
                "ULTRA Sepulchure",
                ClassType.Solo,
                () =>
                {
                    core.Logger("Daily: Seal of Darkness [3825] (/sepulchurebattle ULTRA Sepulchure)");
                    UnbankIfOwned("Seal of Darkness");
                    core.AddDrop("Seal of Darkness");
                    core.EnsureAccept(3825);
                    core.HuntMonster("sepulchurebattle", "ULTRA Sepulchure", "Sepulchure Defeated", 1, isTemp: true);
                    LeaveCombatRoomForEquipmentChange();
                    core.EnsureComplete(3825);
                    Bot.Wait.ForPickup("Seal of Darkness");
                }
            )
        )
            return false;

        // 5c. Turn in Seals for Bright Knight if 50+50 seals & 1,000,000 Gold are available, else bank Seals
        bool CanBuyBrightKnightNow() =>
            !HasBrightKnight()
            && core.CheckInventory("Seal of Light", 50, toInv: false)
            && core.CheckInventory("Seal of Darkness", 50, toInv: false)
            && Bot.Player.Gold >= 1000000;

        bool hasLooseSealsInInv =
            Bot.Inventory?.Items.Any(i =>
                i?.Name != null
                && (
                    string.Equals(i.Name.Trim(), "Seal of Light", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(i.Name.Trim(), "Seal of Darkness", StringComparison.OrdinalIgnoreCase)
                )
            ) == true;

        bool buyHandled = !CanBuyBrightKnightNow() && !hasLooseSealsInInv;
        if (
            !RunCoordinatedTurnInStep(
                "BRACKET55_BRIGHT_KNIGHT_CLEANUP",
                () => buyHandled,
                () => buyHandled,
                () =>
                {
                    if (CanBuyBrightKnightNow())
                    {
                        UnbankIfOwned("Seal of Light", "Seal of Darkness");
                        core.BuyItem("alteonbattle", 574, "Bright Knight");
                        Bot.Wait.ForItemBuy();
                    }

                    BankIfPresent("Bright Knight", "Seal of Light", "Seal of Darkness");
                    buyHandled = true;
                }
            )
        )
            return false;

        return !Bot.ShouldExit;
    }

    private bool RunBracket65ApprovedDailies()
    {
        CoreBots core = CoreBots.Instance;

        if (!EnsureLoyalFollowerUnlocked("BRACKET65"))
            return false;

        // 3a. Daily: Embrace Your Chaos [3596] (/mountdoomskull b1 -> +5,000 Chaos Rep + Dage's Scroll Fragment)
        bool embraceReady =
            core.isCompletedBefore(2084)
            && !core.CheckInventory("Dage's Scroll Fragment", 13, toInv: false)
            && CanRunPeriodicQuest(3596, requireAvailable: true);

        if (
            !RunCoordinatedCombatStep(
                "BRACKET65_EMBRACE_YOUR_CHAOS_DAILY",
                () => !embraceReady,
                () =>
                    Bot.Quests.IsDailyComplete(3596)
                    || core.CheckInventory("Dage's Scroll Fragment", 13, toInv: false),
                "*",
                ClassType.Farm,
                () =>
                {
                    core.Logger("Daily: Embrace Your Chaos [3596] (/mountdoomskull)");
                    if (!JoinMapVerified("mountdoomskull", "b1", "Left"))
                    {
                        core.Logger("Map '/mountdoomskull' is not unlocked yet; skipping Embrace Your Chaos safely.");
                        return;
                    }

                    UnbankIfOwned("Dage's Scroll Fragment");
                    core.AddDrop("Dage's Scroll Fragment");
                    core.EnsureAccept(3596);
                    core.KillMonster("mountdoomskull", "b1", "Left", "*", "Chaos Power Increased", 6, isTemp: true);
                    LeaveCombatRoomForEquipmentChange();
                    core.EnsureComplete(3596);
                    Bot.Wait.ForPickup("Dage's Scroll Fragment");
                    BankIfPresent("Dage's Scroll Fragment");
                }
            )
        )
            return false;

        // 3b. Daily: Lord of Order Progression Chain [7156-7165] (Level 50+, Player 4 Endgame Support Class)
        return RunLordOfOrderProgression();
    }

    private int GetNextAvailableLoOQuest(CoreBots core)
    {
        for (int questId = 7156; questId <= 7165; questId++)
        {
            TryLoadQuest(questId);
            if (core.isCompletedBefore(questId))
                continue;

            if (Bot.Quests.IsAvailable(questId) && !Bot.Quests.IsDailyComplete(questId))
                return questId;

            // Chain is sequential: stop at the first uncompleted day.
            return 0;
        }

        return 0;
    }

    private bool RunLoODay1Stories(CoreBots core, Func<bool> activeCondition)
    {
        // Murry's Quests (/citadel 144-182)
        if (
            !RunCoordinatedStoryChain(
                "BRACKET65_LOO_MURRY",
                new[]
                {
                    new CoordinatedStoryStep(144, "citadel", "Inquisitor Guard", c => Story.KillQuest(144, "Citadel", "Inquisitor Guard")),
                    new CoordinatedStoryStep(145, "citadel", "Crusader", c => Story.KillQuest(145, "Citadel", "Crusader")),
                    new CoordinatedStoryStep(146, "citadel", "Inquisitor Captain", c => Story.KillQuest(146, "Citadel", "Inquisitor Captain")),
                    new CoordinatedStoryStep(147, "citadel", "Burning Witch", c => Story.KillQuest(147, "Citadel", "Burning Witch")),
                    new CoordinatedStoryStep(148, "citadel", "Inquisitor Guard", c => Story.KillQuest(148, "Citadel", "Inquisitor Guard")),
                    new CoordinatedStoryStep(149, "citadel", "Inquisitor Guard", c => Story.KillQuest(149, "Citadel", "Inquisitor Guard", false)),
                    new CoordinatedStoryStep(181, "citadel", "Belrot The Fiend", c => Story.KillQuest(181, "Citadel", "Belrot The Fiend"), ClassType.Solo),
                    new CoordinatedStoryStep(
                        182,
                        "citadel",
                        "Grand Inquisitor",
                        c =>
                        {
                            c.EnsureAccept(182);
                            c.HuntMonster("Citadel", "Grand Inquisitor", "Ring of Evil Intent");
                            c.EnsureComplete(182);
                        },
                        ClassType.Solo
                    ),
                },
                activeCondition
            )
        )
            return false;

        // Polish's Quests (/citadelruins 6172-6182)
        if (
            !RunCoordinatedStoryChain(
                "BRACKET65_LOO_CITADELRUINS",
                new[]
                {
                    new CoordinatedStoryStep(
                        6172,
                        "citadelruins",
                        "Mana Sprites",
                        c =>
                        {
                            c.EnsureAccept(6172);
                            c.GetMapItem(5592, 1, "citadelruins");
                            c.HuntMonster("citadelruins", "Mana Sprites", "Siphoned Mana", 8);
                            c.EnsureComplete(6172);
                        }
                    ),
                    new CoordinatedStoryStep(6173, "citadelruins", "*", c => Story.MapItemQuest(6173, "citadelruins", 5602), isCombat: false),
                    new CoordinatedStoryStep(
                        6174,
                        "citadelruins",
                        "Inquisitor Hobo",
                        c =>
                        {
                            c.EnsureAccept(6174);
                            c.HuntMonster("citadelruins", "Inquisitor Hobo", "Inquisitor Hobos Defeated", 8);
                            c.HuntMonster("citadelruins", "Inquisitor Hobo", "Inquisitors' Manifesto", 5);
                            c.EnsureComplete(6174);
                        }
                    ),
                    new CoordinatedStoryStep(
                        6175,
                        "citadelruins",
                        "Inquisitor Hobo",
                        c =>
                        {
                            c.EnsureAccept(6175);
                            c.GetMapItem(5593, 5, "citadelruins");
                            c.HuntMonster("citadelruins", "Inquisitor Hobo", "Clue Found", 1);
                            c.EnsureComplete(6175);
                            c.Sleep(2500);
                        }
                    ),
                    new CoordinatedStoryStep(
                        6176,
                        "citadelruins",
                        "Inquisitor Heavy",
                        c =>
                        {
                            c.EnsureAccept(6176);
                            c.HuntMonster("citadelruins", "Inquisitor Heavy", "Heavies Beat Down", 2);
                            c.HuntMonster("citadelruins", "Inquisitor Heavy", "Citadel Key");
                            Story.MapItemQuest(6176, "citadelruins", 5603);
                        }
                    ),
                    new CoordinatedStoryStep(
                        6177,
                        "citadelruins",
                        "Inquisitor Hobo",
                        c =>
                        {
                            c.EnsureAccept(6177);
                            c.GetMapItems(new[] { (5594, 1), (5595, 1), (5596, 1) }, "citadelruins");
                            c.HuntMonster("citadelruins", "Inquisitor Hobo", "Inquisitors Defeated", 6);
                            c.EnsureComplete(6177);
                        }
                    ),
                    new CoordinatedStoryStep(
                        6178,
                        "citadelruins",
                        "Mana Sprites",
                        c =>
                        {
                            c.EnsureAccept(6178);
                            c.GetMapItem(5597, 1, "citadelruins");
                            c.HuntMonster("citadelruins", "Mana Sprites", "Siphoned Mana", 8);
                            c.EnsureComplete(6178);
                        }
                    ),
                    new CoordinatedStoryStep(6179, "citadelruins", "*", c => Story.MapItemQuest(6179, "citadelruins", 5598), isCombat: false),
                    new CoordinatedStoryStep(
                        6180,
                        "citadelruins",
                        "Inquisitor Hobo",
                        c =>
                        {
                            c.EnsureAccept(6180);
                            c.GetMapItems(new[] { (5599, 1), (5600, 1), (5601, 1) }, "citadelruins");
                            c.HuntMonster("citadelruins", "Inquisitor Hobo", "Missing Parts", 6);
                            c.EnsureComplete(6180);
                        }
                    ),
                    new CoordinatedStoryStep(6181, "citadelruins", "Grand Inquisitor Murry", c => Story.KillQuest(6181, "citadelruins", "Grand Inquisitor Murry"), ClassType.Solo),
                    new CoordinatedStoryStep(6182, "citadelruins", "Enn'tröpy", c => Story.KillQuest(6182, "citadelruins", "Enn'tröpy"), ClassType.Solo),
                },
                activeCondition,
                "Mage's Gratitude"
            )
        )
            return false;

        // Ravenscar Storyline (/ravenscar 2901-2907)
        return RunCoordinatedStoryChain(
            "BRACKET65_LOO_RAVENSCAR",
            new[]
            {
                new CoordinatedStoryStep(2901, "ravenscar", "*", c => Story.MapItemQuest(2901, "ravenscar", 1767), isCombat: false),
                new CoordinatedStoryStep(
                    2902,
                    "ravenscar",
                    "Restless Undead",
                    c =>
                    {
                        c.EnsureAccept(2902);
                        c.HuntMonster("ravenscar", "Restless Undead", "Crumpled Book Page", 1);
                        c.HuntMonster("ravenscar", "Restless Undead", "Undead Slain", 6);
                        c.EnsureComplete(2902);
                    }
                ),
                new CoordinatedStoryStep(2903, "ravenscar", "*", c => Story.MapItemQuest(2903, "ravenscar", 1768), isCombat: false),
                new CoordinatedStoryStep(2904, "ravenscar", "Restless Undead", c => Story.KillQuest(2904, "ravenscar", "Restless Undead")),
                new CoordinatedStoryStep(2905, "ravenscar", "Undead Soldier", c => Story.KillQuest(2905, "ravenscar", "Undead Soldier")),
                new CoordinatedStoryStep(2906, "ravenscar", "Shadowman", c => Story.KillQuest(2906, "ravenscar", "Shadowman"), ClassType.Solo),
                new CoordinatedStoryStep(2907, "ravenscar", "*", c => Story.MapItemQuest(2907, "ravenscar", 1769), isCombat: false),
            },
            activeCondition
        );
    }

    private bool RunLoODay5DFOStories(Func<bool> activeCondition)
    {
        return RunCoordinatedStoryChain(
            "BRACKET65_LOO_DFO",
            new[]
            {
                new CoordinatedStoryStep(6294, "firewar", "Fire Dragon", c => Story.KillQuest(6294, "firewar", "Fire Dragon")),
                new CoordinatedStoryStep(6295, "firewar", "Fire Dragon", c => Story.KillQuest(6295, "firewar", "Fire Dragon")),
                new CoordinatedStoryStep(
                    6296,
                    "firewar",
                    "Inferno Dragon",
                    c =>
                    {
                        c.EnsureAccept(6296);
                        c.KillMonster("firewar", "r8", "Right", "Inferno Dragon", "Pyritium Shards", 5);
                        c.EnsureComplete(6296);
                    }
                ),
                new CoordinatedStoryStep(
                    6297,
                    "firewar",
                    "Inferno Dragon",
                    c =>
                    {
                        c.EnsureAccept(6297);
                        c.KillMonster("firewar", "r8", "Right", "Inferno Dragon", "Perfect Pyritium");
                        c.EnsureComplete(6297);
                    }
                ),
                new CoordinatedStoryStep(6298, "firewar", "Uriax", c => Story.KillQuest(6298, "firewar", "Uriax"), ClassType.Solo),
                new CoordinatedStoryStep(
                    6299,
                    "firewar",
                    "*",
                    c =>
                    {
                        c.EnsureAccept(6299);
                        c.Join("firewar", "r10", "Left");
                        if (Bot.Player.Cell != "r10")
                            c.Jump("r10", "Left");
                        while (!Bot.ShouldExit && !Bot.TempInv.Contains(43572))
                        {
                            Bot.Combat.Attack("*");
                            Bot.Sleep(500);
                        }
                        c.EnsureComplete(6299);
                        c.JumpWait();
                    },
                    ClassType.Solo
                ),
                new CoordinatedStoryStep(
                    6301,
                    "northmountain",
                    "Ice Elemental",
                    c =>
                    {
                        c.EnsureAccept(6301);
                        c.HuntMonster("northmountain", "Ice Elemental", "Monsters Defeated", 8);
                        c.GetMapItem(5812, 1, "northmountain");
                        c.EnsureComplete(6301);
                    }
                ),
                new CoordinatedStoryStep(6302, "northmountain", "Ice Elemental", c => Story.KillQuest(6302, "northmountain", "Ice Elemental")),
                new CoordinatedStoryStep(6303, "northmountain", "Ursice Savage", c => Story.KillQuest(6303, "northmountain", "Ursice Savage")),
                new CoordinatedStoryStep(6304, "northmountain", "Ice Spitter", c => Story.KillQuest(6304, "northmountain", "Ice Spitter")),
                new CoordinatedStoryStep(6305, "northmountain", "Ice Elemental", c => Story.KillQuest(6305, "northmountain", "Ice Elemental")),
                new CoordinatedStoryStep(
                    6306,
                    "northmountain",
                    "*",
                    c =>
                    {
                        Story.MapItemQuest(6306, "northmountain", 5813, 4);
                        Story.MapItemQuest(6306, "northmountain", 5814);
                    },
                    isCombat: false
                ),
                new CoordinatedStoryStep(6307, "northmountain", "Izotz", c => Story.KillQuest(6307, "northmountain", "Izotz"), ClassType.Solo),
                new CoordinatedStoryStep(6308, "charredplains", "Fire Dragon", c => Story.KillQuest(6308, "charredplains", "Fire Dragon")),
                new CoordinatedStoryStep(6309, "charredplains", "*", c => Story.MapItemQuest(6309, "charredplains", 5815), isCombat: false),
                new CoordinatedStoryStep(6310, "charredplains", "Akriloth", c => Story.KillQuest(6310, "charredplains", "Akriloth"), ClassType.Solo),
                new CoordinatedStoryStep(6311, "northmountain", "Izotz", c => Story.KillQuest(6311, "northmountain", "Izotz"), ClassType.Solo),
                new CoordinatedStoryStep(6312, "drakonnan", "*", c => Story.MapItemQuest(6312, "drakonnan", 5827, 5), isCombat: false),
                new CoordinatedStoryStep(6313, "drakonnan", "Fire Dragon", c => Story.KillQuest(6313, "drakonnan", "Fire Dragon")),
                new CoordinatedStoryStep(6314, "drakonnan", "Living Lava", c => Story.KillQuest(6314, "drakonnan", "Living Lava")),
                new CoordinatedStoryStep(6315, "drakonnan", "Fire Dragon", c => Story.KillQuest(6315, "drakonnan", "Fire Dragon")),
                new CoordinatedStoryStep(6316, "northmountain", "Ice Elemental", c => Story.KillQuest(6316, "northmountain", "Ice Elemental")),
                new CoordinatedStoryStep(6317, "northmountain", "*", c => Story.MapItemQuest(6317, "northmountain", 5826, 6), isCombat: false),
                new CoordinatedStoryStep(6318, "drakonnan", "Fire Elemental", c => Story.KillQuest(6318, "drakonnan", "Fire Elemental")),
                new CoordinatedStoryStep(6319, "drakonnan", "Living Fire", c => Story.KillQuest(6319, "drakonnan", "Living Fire")),
            },
            activeCondition,
            "Ice Katana"
        );
    }

    private bool CompleteLoODayIfReady(CoreBots core, int questId, int nextLoOQuest, string[] requiredItems, int chooseItemId = -1)
    {
        bool completeAttempted = false;
        return RunCoordinatedTurnInStep(
            $"BRACKET65_LOO_{questId}_COMPLETE",
            () => completeAttempted || nextLoOQuest != questId || core.isCompletedBefore(questId) || !Bot.Quests.IsAvailable(questId),
            () => completeAttempted || nextLoOQuest != questId || core.isCompletedBefore(questId),
            () =>
            {
                completeAttempted = true;
                UnbankIfOwned(requiredItems);
                if (!requiredItems.All(item => core.CheckInventory(item)))
                {
                    core.Logger($"LoO [{questId}] missing one or more required items; will retry on a future daily run.");
                    return;
                }

                core.EnsureAccept(questId);
                core.AddDrop(core.QuestRewards(questId));
                if (chooseItemId > 0)
                {
                    Bot.Drops.Add(chooseItemId);
                    core.EnsureComplete(questId, chooseItemId);
                    Bot.Wait.ForPickup(chooseItemId);
                    core.EnsureCompleteChoose(questId);
                    BankQuestExtras(questId, "Lord of Order");
                }
                else
                {
                    core.EnsureComplete(questId);
                    BankQuestExtras(questId);
                }
            }
        );
    }

    private bool RunLoOQuestMultiHunt(
        CoreBots core,
        int questId,
        int nextLoOQuest,
        (string Map, string Monster, string Item, int Quant, ClassType Role, string? Cell, string? Pad, Action<CoreBots>? BeforeHunt)[] targets,
        int softRetryLimit = 2
    )
    {
        bool IsActiveOnThisQuest() => nextLoOQuest == questId && !core.isCompletedBefore(questId);

        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (
                !RunCoordinatedHuntStep(
                    $"BRACKET65_LOO_{questId}_STEP_{i + 1}",
                    questId,
                    t.Map,
                    t.Monster,
                    t.Item,
                    t.Quant,
                    isTemp: false,
                    role: t.Role,
                    cell: t.Cell,
                    pad: t.Pad,
                    acceptQuest: true,
                    checkBankAlso: true,
                    softRetryLimit: softRetryLimit,
                    beforeHunt: t.BeforeHunt,
                    isEnabled: IsActiveOnThisQuest
                )
            )
            {
                return false;
            }
        }

        return CompleteLoODayIfReady(core, questId, nextLoOQuest, targets.Select(t => t.Item).ToArray());
    }

    private bool RunLordOfOrderProgression()
    {
        CoreBots core = CoreBots.Instance;
        bool HasLoOClass() => HasClassInInventory(50741) || HasClassInInventory("Lord of Order");

        if (Bot.Player.Level < 50)
            return true;

        int nextQuestId = HasLoOClass() ? 0 : GetNextAvailableLoOQuest(core);
        if (nextQuestId > 0)
            core.Logger($"Daily: Lord of Order [{nextQuestId}/7165] active for this account.");

        // Always keep barriers aligned across all accounts, skipping work instantly when nextQuestId != day.
        // Day 1 [7156]: Heart of Servitude
        if (nextQuestId == 7156)
            core.AddDrop(core.QuestRewards(7156));

        if (
            !RunCoordinatedTurnInStep(
                "BRACKET65_LOO_7156_SHOPS",
                () => nextQuestId != 7156 || core.CheckInventory("Dreadrock Donation Receipt", 1, toInv: false),
                () => nextQuestId != 7156 || core.CheckInventory("Dreadrock Donation Receipt", 1, toInv: false),
                () =>
                {
                    if (!core.CheckInventory("Dreadrock Donation Receipt", 1, toInv: false))
                    {
                        Farm.Gold(1000000);
                        LeaveCombatRoomForEquipmentChange();
                        core.BuyItem("dreadrock", 1221, "Dreadrock Donation Receipt");
                    }
                }
            )
            || !RunLoODay1Stories(core, () => nextQuestId == 7156)
            || !RunCoordinatedCombatStep(
                "BRACKET65_LOO_7156_MAGES_GRATITUDE",
                () => nextQuestId != 7156 || core.CheckInventory("Mage's Gratitude", 1, toInv: false),
                () => nextQuestId != 7156 || core.CheckInventory("Mage's Gratitude", 1, toInv: false),
                "Enn'tröpy",
                ClassType.Solo,
                () =>
                {
                    UnbankIfOwned("Mage's Gratitude");
                    if (!core.CheckInventory("Mage's Gratitude"))
                    {
                        core.AddDrop("Mage's Gratitude");
                        core.EnsureAccept(6182);
                        core.HuntMonster("citadelruins", "Enn'tröpy", "Enn'tröpy Defeated", isTemp: true);
                        LeaveCombatRoomForEquipmentChange();
                        core.EnsureComplete(6182);
                        Bot.Wait.ForPickup("Mage's Gratitude");
                    }
                }
            )
            || !RunCoordinatedTurnInStep(
                "BRACKET65_LOO_7156_RAVENSCAR_TRUTH",
                () => nextQuestId != 7156 || core.CheckInventory("Ravenscar's Truth", 1, toInv: false),
                () => nextQuestId != 7156 || core.CheckInventory("Ravenscar's Truth", 1, toInv: false),
                () =>
                {
                    UnbankIfOwned("Ravenscar's Truth");
                    if (!core.CheckInventory("Ravenscar's Truth"))
                        core.BuyItem("ravenscar", 614, "Ravenscar's Truth");
                }
            )
            || !RunCoordinatedHuntStep(
                "BRACKET65_LOO_7156_WATCHTOWER",
                7156,
                "watchtower",
                "Chaorrupted Knight",
                "Pristine Blades of Order",
                1,
                role: ClassType.Solo,
                acceptQuest: true,
                isEnabled: () => nextQuestId == 7156
            )
            || !RunCoordinatedHuntStep(
                "BRACKET65_LOO_7156_DEADMOOR",
                7156,
                "deadmoor",
                "Banshee Mallora",
                "Deadmoor Spirits Helped",
                1,
                role: ClassType.Solo,
                acceptQuest: true,
                isEnabled: () => nextQuestId == 7156
            )
            || !CompleteLoODayIfReady(
                core,
                7156,
                nextQuestId,
                new[]
                {
                    "Pristine Blades of Order",
                    "Dreadrock Donation Receipt",
                    "Deadmoor Spirits Helped",
                    "Mage's Gratitude",
                    "Ravenscar's Truth",
                }
            )
        )
            return false;

        // Day 2 [7157]: Spirit of Justice
        if (
            !RunLoOQuestMultiHunt(
                core,
                7157,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("dwarfprison", "Warden Elfis", "Warden Elfis Detained", 1, ClassType.Solo, null, null, null),
                    ("prison", "Piggy Drake", "Piggy Drake Punished", 1, ClassType.Solo, null, null, null),
                    ("mysteriousdungeon", "Mysterious Stranger", "Mysterious Stranger Foiled", 1, ClassType.Solo, null, null, null),
                    ("dreammaster", "Calico Cobby", "Calico Cobby Crushed", 1, ClassType.Solo, null, null, null),
                }
            )
        )
            return false;

        // Day 3 [7158]: Purification of Chaos
        if (
            !RunCoordinatedCombatStep(
                "BRACKET65_LOO_7158_CHAOROOT",
                () => nextQuestId != 7158 || core.CheckInventory("Chaoroot", 15, toInv: false),
                () => nextQuestId != 7158 || core.CheckInventory("Chaoroot", 15, toInv: false),
                "*",
                ClassType.Solo,
                () =>
                {
                    UnbankIfOwned("Chaoroot");
                    if (!core.CheckInventory("Chaoroot", 15))
                    {
                        Farm.Gold(500000);
                        Adv.BuyItem("tercessuinotlim", 1951, "Chaoroot", 15);
                    }
                }
            )
            || !RunLoOQuestMultiHunt(
                core,
                7158,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("chaosboss", "Ultra Chaos Warlord", "Chaotic War Essence", 15, ClassType.Solo, null, null, null),
                    ("shadowgates", "Chaorruption", "Chaorrupting Particles", 15, ClassType.Solo, null, null, null),
                    ("stormtemple", "Chaos Lord Lionfang", "Purified Raindrop", 45, ClassType.Solo, null, null, _ => Bot.Quests.UpdateQuest(2814)),
                }
            )
        )
            return false;

        // Day 4 [7159]: Steadfast Will
        if (
            !RunLoOQuestMultiHunt(
                core,
                7159,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("gaiazor", "Gaiazor", "Gaiazor's Cornerstone", 1, ClassType.Solo, null, null, null),
                    ("treetitanbattle", "Dakka the Dire Dragon", "Dakka's Crystal", 1, ClassType.Solo, null, null, _ => { Bot.Quests.UpdateQuest(4361); Bot.Sleep(1500); }),
                    ("andre", "Giant Necklace", "Andre's Necklace Fragment", 1, ClassType.Solo, null, null, null),
                    ("desolich", "Desolich", "Desolich's Skull", 1, ClassType.Solo, null, null, _ => LeaveCombatRoomForEquipmentChange()),
                }
            )
        )
            return false;

        // Day 5 [7160]: Strike of Order
        if (
            !RunCoordinatedCombatStep(
                "BRACKET65_LOO_7160_KITSUNE",
                () => nextQuestId != 7160 || core.CheckInventory("Hanzamune Dragon Koi Blade", 1, toInv: false),
                () => nextQuestId != 7160 || core.CheckInventory("Hanzamune Dragon Koi Blade", 1, toInv: false),
                "*",
                ClassType.Solo,
                () =>
                {
                    core.AddDrop("Hanzamune Dragon Koi Blade");
                    core.EnsureAccept(7160);
                    core.KillKitsune("Hanzamune Dragon Koi Blade");
                }
            )
            || !RunCoordinatedHuntStep("BRACKET65_LOO_7160_LEDGERMAYNE", 7160, "ledgermayne", "Ledgermayne", "The Supreme Arcane Staff", 1, role: ClassType.Solo, isEnabled: () => nextQuestId == 7160)
            || !RunCoordinatedHuntStep("BRACKET65_LOO_7160_DRAGONOID", 7160, "mqlesson", "Dragonoid", "Dragonoid of Hours", 1, role: ClassType.Solo, isEnabled: () => nextQuestId == 7160)
            || !RunCoordinatedTurnInStep(
                "BRACKET65_LOO_7160_SAFIRIA",
                () => nextQuestId != 7160 || core.CheckInventory("Safiria's Spirit Orb", 1, toInv: false),
                () => nextQuestId != 7160 || core.CheckInventory("Safiria's Spirit Orb", 1, toInv: false),
                () =>
                {
                    UnbankIfOwned("Safiria's Spirit Orb");
                    if (!core.CheckInventory("Safiria's Spirit Orb"))
                    {
                        core.AddDrop("Safiria's Spirit Orb");
                        core.EnsureAccept(7160);
                        core.GetMapItem(5470, 1, "maxius");
                        Bot.Wait.ForPickup("Safiria's Spirit Orb");
                    }
                }
            )
            || !RunLoODay5DFOStories(() => nextQuestId == 7160)
            || !RunCoordinatedCombatStep(
                "BRACKET65_LOO_7160_ICE_KATANA",
                () => nextQuestId != 7160 || core.CheckInventory("Ice Katana", 1, toInv: false),
                () => nextQuestId != 7160 || core.CheckInventory("Ice Katana", 1, toInv: false),
                "Living Fire",
                ClassType.Farm,
                () =>
                {
                    UnbankIfOwned("Ice Katana");
                    if (!core.CheckInventory("Ice Katana"))
                    {
                        core.AddDrop("Ice Katana");
                        core.EnsureAccept(6319);
                        core.HuntMonster("drakonnan", "Living Fire", "Inferno Heart");
                        LeaveCombatRoomForEquipmentChange();
                        core.EnsureComplete(6319);
                        Bot.Wait.ForPickup("Ice Katana");
                    }
                }
            )
            || !CompleteLoODayIfReady(
                core,
                7160,
                nextQuestId,
                new[]
                {
                    "Hanzamune Dragon Koi Blade",
                    "The Supreme Arcane Staff",
                    "Dragonoid of Hours",
                    "Safiria's Spirit Orb",
                    "Ice Katana",
                }
            )
        )
            return false;

        // Day 6 [7161]: Harmony
        if (
            !RunLoOQuestMultiHunt(
                core,
                7161,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("elemental", "Tree of Destiny", "Unity of Life", 1, ClassType.Solo, null, null, null),
                    ("orchestra", "Faust", "Harmony of Solace", 1, ClassType.Solo, null, null, null),
                    ("cathedral", "Pactagonal Knight", "Teamwork Observed", 100, ClassType.Farm, null, null, null),
                    ("goose", "Queen's ArchSage", "Scroll of Enchantment", 1, ClassType.Solo, null, null, null),
                }
            )
        )
            return false;

        // Day 7 [7162]: Ordinance
        if (
            !RunLoOQuestMultiHunt(
                core,
                7162,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("newfinale", "Alliance Healer", "Acolyte's Braille", 1, ClassType.Farm, null, null, null),
                    ("wardwarf", "Drow Assassin", "Suppressed Drows", 50, ClassType.Farm, null, null, null),
                    ("warundead", "Skeletal Fire Mage", "Suppressed Undead", 50, ClassType.Farm, null, null, null),
                    ("warhorc", "Horc Warrior", "Suppressed Horcs", 50, ClassType.Farm, null, null, null),
                    ("weaverwar", "Weaver Queen's Hound", "Suppressed Weavers", 50, ClassType.Farm, null, null, null),
                    ("extriki", "Extriki", "Strength of Resilience", 1, ClassType.Solo, null, null, null),
                }
            )
        )
            return false;

        // Day 8 [7163]: Axiom
        if (
            !RunLoOQuestMultiHunt(
                core,
                7163,
                nextQuestId,
                new (string, string, string, int, ClassType, string?, string?, Action<CoreBots>?)[]
                {
                    ("elfhame", "Guardian Spirit", "Law of Nature", 1, ClassType.Solo, null, null, null),
                    ("deepchaos", "Kathool", "Law of Time", 1, ClassType.Solo, null, null, null),
                    ("necrocavern", "ShadowStone Support", "Law of Gravity", 1, ClassType.Solo, null, null, null),
                    ("blackholesun", "Reflecteract", "Law of Relativity", 1, ClassType.Solo, null, null, null),
                    ("thunderfang", "Tonitru", "Law of Conservation of Energy", 1, ClassType.Solo, null, null, null),
                    ("lair", "Red Dragon", "Law of Low Drop Rates", 100, ClassType.Solo, null, null, null),
                }
            )
        )
            return false;

        // Day 9 [7164]: Blessing of Order
        if (
            !RunCoordinatedHuntStep(
                "BRACKET65_LOO_7164_RAXGORE",
                7164,
                "doomvaultb",
                "Undead Raxgore",
                "Weapon Imprint",
                15,
                role: ClassType.Solo,
                cell: "r26",
                pad: "Left",
                isEnabled: () => nextQuestId == 7164
            )
            || !RunCoordinatedTurnInStep(
                "BRACKET65_LOO_7164_LURE",
                () => nextQuestId != 7164 || core.CheckInventory("Lure of Order", 1, toInv: false),
                () => nextQuestId != 7164 || core.CheckInventory("Lure of Order", 1, toInv: false),
                () =>
                {
                    UnbankIfOwned("Lure of Order");
                    if (!core.CheckInventory("Lure of Order"))
                    {
                        Farm.FishingREP(7);
                        LeaveCombatRoomForEquipmentChange();
                        core.BuyItem("greenguardwest", 363, "Lure of Order");
                    }
                }
            )
            || !RunCoordinatedCombatStep(
                "BRACKET65_LOO_7164_XIANG",
                () => nextQuestId != 7164 || core.CheckInventory("Quixotic Mana Essence", 10, toInv: false),
                () => nextQuestId != 7164 || core.CheckInventory("Quixotic Mana Essence", 10, toInv: false),
                "*",
                ClassType.Solo,
                () =>
                {
                    UnbankIfOwned("Quixotic Mana Essence");
                    core.AddDrop("Quixotic Mana Essence");
                    core.EnsureAccept(7164);
                    Adv.GearStore(EnhAfter: true);
                    core.KillXiang("Quixotic Mana Essence", 10, true);
                    Adv.GearStore(true, EnhAfter: true);
                }
            )
            || !RunCoordinatedHuntStep("BRACKET65_LOO_7164_SEREPTHYS", 7164, "yasaris", "Serepthys", "Inversion Infusion", 5, role: ClassType.Solo, isEnabled: () => nextQuestId == 7164)
            || !CompleteLoODayIfReady(
                core,
                7164,
                nextQuestId,
                new[] { "Weapon Imprint", "Lure of Order", "Quixotic Mana Essence", "Inversion Infusion" }
            )
        )
            return false;

        // Day 10 [7165]: The Final Challenge -> Unlock Lord of Order + Rank 10 for Player 4
        if (
            !RunCoordinatedHuntStep(
                "BRACKET65_LOO_7165_DRAKATH",
                7165,
                "ultradrakath",
                "Champion of Chaos",
                "Champion of Chaos Confronted",
                1,
                role: ClassType.Solo,
                isEnabled: () => nextQuestId == 7165
            )
            || !CompleteLoODayIfReady(
                core,
                7165,
                nextQuestId,
                new[] { "Champion of Chaos Confronted" },
                chooseItemId: 50741
            )
        )
            return false;

        return RunCooperativeObjective(
            "BRACKET65_LOO_RANK_10",
            () => !HasLoOClass() || HasRank10Class("Lord of Order"),
            () =>
            {
                if (!HasLoOClass())
                    return;

                if (!HasRank10Class("Lord of Order"))
                    RankUpClassForArmy("Lord of Order");

                SetClass();
            },
            (targetPlayerNumber, targetPlayerName) =>
                Army.CoordinateFollowAndCombat(targetPlayerName, "*")
        );
    }

    /// <summary>
    /// Future Bracket (Level 75+): Daily "The Encroaching Shadows" [8653] (Void Aura x50 + (Necro) Scroll of Dark Arts).
    /// Intentionally NOT invoked during Level 1-65 brackets; ready for future Level 75+ integration.
    /// </summary>
    public bool RunTheEncroachingShadowsDaily()
    {
        if (Bot.Player.Level < 75)
            return true;

        CoreBots core = CoreBots.Instance;
        bool voidAuraCapped = core.CheckInventory("Void Aura", 7500, toInv: false);
        bool dailyAvailable = !voidAuraCapped && CanRunPeriodicQuest(8653);

        if (
            !RunCoordinatedHuntStep(
                "FUTURE75_ENCROACHING_SHADOWS_ICEWING",
                8653,
                "icewing",
                "Warlord Icewing",
                "Glacial Pinion",
                1,
                role: ClassType.Solo,
                acceptQuest: true,
                checkBankAlso: true,
                beforeHunt: c =>
                {
                    if (!dailyAvailable)
                        return;
                    c.AddDrop("Void Aura", "(Necro) Scroll of Dark Arts", "Glacial Pinion", "Hydra Eyeball", "Flibbitigiblets");
                },
                isEnabled: () => dailyAvailable
            )
            || !RunCoordinatedHuntStep(
                "FUTURE75_ENCROACHING_SHADOWS_HYDRA90",
                8653,
                "hydrachallenge",
                "Hydra Head 90",
                "Hydra Eyeball",
                3,
                role: ClassType.Solo,
                cell: "h90",
                pad: "Left",
                isEnabled: () => dailyAvailable
            )
            || !RunCoordinatedHuntStep(
                "FUTURE75_ENCROACHING_SHADOWS_FLIBBI",
                8653,
                "voidflibbi",
                "Flibbitiestgibbet",
                "Flibbitigiblets",
                1,
                role: ClassType.Solo,
                isEnabled: () => dailyAvailable
            )
        )
            return false;

        bool turnInDone = false;
        return RunCoordinatedTurnInStep(
            "FUTURE75_ENCROACHING_SHADOWS_COMPLETE",
            () => !dailyAvailable || turnInDone,
            () => !dailyAvailable || Bot.Quests.IsDailyComplete(8653) || turnInDone,
            () =>
            {
                turnInDone = true;
                UnbankIfOwned("Glacial Pinion", "Hydra Eyeball", "Flibbitigiblets");
                if (
                    !core.CheckInventory("Glacial Pinion", 1)
                    || !core.CheckInventory("Hydra Eyeball", 3)
                    || !core.CheckInventory("Flibbitigiblets", 1)
                )
                    return;

                core.AddDrop("Void Aura", "(Necro) Scroll of Dark Arts");
                core.EnsureAccept(8653);
                core.EnsureComplete(8653);
                Bot.Wait.ForPickup("Void Aura");
                BankIfPresent("Void Aura", "(Necro) Scroll of Dark Arts");
            }
        );
    }
    #endregion Approved Dailies, Weeklies & Future NSoD Daily

    public bool RunCooperativeObjective(
        string objectiveName,
        Func<bool> isComplete,
        Action doWork,
        Action help,
        int maxWorkAttempts = 0,
        int maxPlayersPerSubgroup = 0
    ) => Army.RunCooperativeObjective(
        objectiveName,
        isComplete,
        doWork,
        (targetPlayerNumber, targetPlayerName) => help(),
        maxWorkAttempts,
        maxPlayersPerSubgroup
    );

    public bool RunCooperativeObjective(
        string objectiveName,
        Func<bool> isComplete,
        Action doWork,
        Action<int, string> help,
        int maxWorkAttempts = 0,
        int maxPlayersPerSubgroup = 0
    ) => Army.RunCooperativeObjective(
        objectiveName,
        isComplete,
        doWork,
        help,
        maxWorkAttempts,
        maxPlayersPerSubgroup
    );

    public bool IsPlayer(int playerNumber) => Army.IsArmyPlayer(playerNumber);

    public bool StopArmySync(string reason) => Army.StopArmySync(reason);

    public bool WaitForArmyStop() => Army.SyncArmy("STOP_CHECK");
}
