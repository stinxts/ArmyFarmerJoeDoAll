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

    private static readonly string[] HighTierSoloClasses =
    {
        "King's Echo",
        "Chaos Avenger",
        "Void Highlord",
        "Void Highlord (IoDA)",
        "Legion Revenant",
        "Dragon of Time",
        "ArchPaladin",
        "Lord of Order",
        "Cryomancer",
        "Dragonslayer General",
        "Glacial Berserker",
        "Dragonslayer",
        "DragonSoul Shinobi",
        "Classic Ninja",
        "Ninja (Rare)",
        "Rogue (Rare)",
        "Rogue",
        "Healer (Rare)",
        "Healer",
    };

    private static readonly string[] HighTierFarmClasses =
    {
        "Legion Revenant",
        "ArchMage",
        "Dragon of Time",
        "Archfiend",
        "Blaze Binder",
        "Scarlet Sorceress",
        "Pyromancer",
        "Shaman",
        "Imperial Chunin",
        "Master Ranger",
    };

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
        new Option<int>(
            "RoomNumber",
            "Army Private Room Number",
            "Shared private room number (1001-99999) so all Army accounts join the exact same map instance.",
            91827
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

        if (playerCount < 2)
        {
            CoreBots.Instance.Logger(
                "Army FarmerJoe requires at least 2 players. 4 players are recommended; players 5-7 are optional.",
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
        int roomNumber = Bot.Config?.Get<int>("RoomNumber") ?? 91827;
        if (roomNumber <= 0)
            roomNumber = 91827;

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
        bool hasHighTier =
            CoreBots.Instance.CheckInventory(HighTierSoloClasses, any: true, toInv: false)
            && CoreBots.Instance.CheckInventory(HighTierFarmClasses, any: true, toInv: false);

        bool hasBothRank10Beginners =
            CoreBots.Instance.CheckInventory(
                new[] { "Assassin", "Ninja Warrior", "Ninja" },
                any: true,
                toInv: false
            )
            && HasRank10Class("Ninja")
            && CoreBots.Instance.CheckInventory(
                new[] { "Mage (Rare)", "Mage" },
                any: true,
                toInv: false
            )
            && HasRank10Class("Mage")
            && Bot.Player.Level >= 30;

        bool skipBeginnerItems = hasHighTier || hasBothRank10Beginners;

        if (
            !RunCooperativeObjective(
                "BEGINNER_INITIAL_CHECK",
                () => true,
                () => { },
                (targetPlayerNumber, targetPlayerName) => { }
            )
        )
            return false;

        if (skipBeginnerItems)
        {
            CoreBots.Instance.Logger(
                "Account is level 30+ with beginner classes or has high-tier classes. Acting as helper for accounts still progressing."
            );
        }
        else
        {
            CoreBots.Instance.Logger(
                "Account is progressing beginner items; running with Army synchronization."
            );
        }

        // 1. Tutorial badges
        bool tutorialComplete = false;
        if (
            !RunCooperativeObjective(
                "BEGINNER_TUTORIAL",
                () => skipBeginnerItems || tutorialComplete,
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
                            CoreBots.Instance.Equip("Battle Oracle Hood");
                        }
                        break;

                    case ItemCategory.Cape:
                        if (equippedItem == null)
                        {
                            CoreBots.Instance.BuyItem("classhalla", 299, "Battle Oracle Wings");
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
                () => skipBeginnerItems || basicEquipmentComplete,
                equipBasicItems,
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 3. Level 10
        CoreBots.Instance.Logger("Starting out acc:\n\tGoals: Ninja class & Mage Class");
        if (
            !RunCooperativeObjective(
                "BEGINNER_LEVEL_10",
                () => skipBeginnerItems || Bot.Player.Level >= 10,
                () => Farm.Experience(10),
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName, "Bone Berserker");
                }
            )
        )
            return false;

        // 4. Rogue
        CoreBots.Instance.Logger("Getting Started: Beginner Levels/Equipment");
        bool HasRogue() =>
            CoreBots.Instance.CheckInventory(
                new[] { "Rogue (Rare)", "Rogue" },
                any: true,
                toInv: false
            );

        if (
            !RunCooperativeObjective(
                "BEGINNER_ROGUE",
                () => skipBeginnerItems || HasRogue(),
                () =>
                {
                    if (HasRogue())
                        return;

                    CoreBots.Instance.Logger(
                        "Getting rogue.. so we can get ninja (there's a 10k hp \"boss\" to kill during the \"Hit Job\" quest.)"
                    );
                    Adv.BuyItem("classhalla", 172, "Rogue");
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
        bool HasNinja() =>
            CoreBots.Instance.CheckInventory(
                new[] { "Assassin", "Ninja Warrior", "Ninja" },
                any: true,
                toInv: false
            );

        bool HasRank10Rogue() =>
            HasRank10Class("Rogue") || HasRank10Class("Rogue (Rare)");

        // 5a. Rank up Rogue to 10
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_RANK_ROGUE",
                () => skipBeginnerItems || HasNinja() || HasRank10Rogue(),
                () =>
                {
                    if (HasNinja() || HasRank10Rogue())
                        return;

                    CoreBots.Instance.Logger("Getting starter Dodge class (Ninja) - Ranking Rogue");
                    string rogueClass = CoreBots.Instance.CheckInventory("Rogue (Rare)", toInv: false)
                        ? "Rogue (Rare)"
                        : "Rogue";
                    Adv.RankUpClass(rogueClass);
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
                () => skipBeginnerItems || HasNinja() || Bot.Player.Level >= 25,
                () =>
                {
                    if (HasNinja())
                        return;

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
        if (!RunCoordinatedMazumiQuests(() => skipBeginnerItems || HasNinja()))
            return false;

        // 5d. Buy Ninja
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_BUY",
                () => skipBeginnerItems || HasNinja(),
                () =>
                {
                    if (HasNinja())
                        return;

                    CoreBots.Instance.BuyItem("classhalla", 178, "Ninja");
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 5e. SetClass after Ninja (resolves Solo/Farm/Dodge and ranks Ninja to 10)
        bool ninjaClassSetComplete = false;
        if (
            !RunCooperativeObjective(
                "BEGINNER_NINJA_SET_CLASS",
                () => skipBeginnerItems || ninjaClassSetComplete,
                () =>
                {
                    SetClass();
                    ninjaClassSetComplete = !Bot.ShouldExit;
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 6. Mage
        bool HasMage() =>
            CoreBots.Instance.CheckInventory(
                new[] { "Mage (Rare)", "Mage" },
                any: true,
                toInv: false
            );

        if (
            !RunCooperativeObjective(
                "BEGINNER_MAGE",
                () => skipBeginnerItems || HasMage(),
                () =>
                {
                    if (HasMage())
                        return;

                    CoreBots.Instance.Logger("Getting Starter Farm class (Mage)");
                    Adv.BuyItem("classhalla", 174, 15653, shopItemID: 9845);
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Bot.Sleep(1000);
                },
                maxWorkAttempts: 3
            )
        )
            return false;

        // 7. Oracle (free 0 AC Healer/Support class for early Army composition)
        bool HasOracle() => CoreBots.Instance.CheckInventory("Oracle", toInv: false);
        bool HasOracleRank10() => HasOracle() && HasRank10Class("Oracle");

        if (
            !RunCooperativeObjective(
                "BEGINNER_ORACLE_BUY",
                () => skipBeginnerItems || HasOracle(),
                () =>
                {
                    if (HasOracle())
                        return;

                    CoreBots.Instance.Logger("Getting Starter Support/Healer class (Oracle - 0 AC)");
                    CoreBots.Instance.BuyItem("classhalla", 299, "Oracle");
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
                "BEGINNER_ORACLE_RANK_10",
                () => skipBeginnerItems || HasOracleRank10(),
                () =>
                {
                    if (HasOracleRank10())
                        return;

                    Adv.RankUpClass("Oracle");
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
                () => skipBeginnerItems || finalClassSetComplete,
                () =>
                {
                    SetClass();
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
                () => skipCondition() || core.isCompletedBefore(92) || Story.QuestProgression(90),
                () =>
                {
                    if (skipCondition() || core.isCompletedBefore(92) || Story.QuestProgression(90))
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
        bool HasMasterRanger() => core.CheckInventory("Master Ranger", toInv: false);
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
                    SetClass();
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
                    Adv.BuyItem("sandsea", 242, 7260, shopItemID: 5682);
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

                    Adv.RankUpClass("Master Ranger");
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 5. Dragonslayer (Galanoth 4 quests coordinated 1 by 1 -> Buy -> Rank 10)
        bool HasDragonslayer() => core.CheckInventory("Dragonslayer", toInv: false);
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
                        || Story.QuestProgression(gQuest.QuestId),
                    () =>
                    {
                        if (
                            HasDragonslayer()
                            || core.isCompletedBefore(169)
                            || Story.QuestProgression(gQuest.QuestId)
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
                    core.BuyItem("lair", 38, "Dragonslayer");
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

                    Adv.RankUpClass("Dragonslayer");
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 5d. Imperial Chunin (Yokai Rep 10 in /dragonkoiz — No boss, Pockey Chew only ~1k HP, gives Boss Hit-Chance Debuffer for Player 3)
        bool HasImperialChunin() => core.CheckInventory("Imperial Chunin", toInv: false);
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
                    SetClass();
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
                    Adv.BuyItem("dragonkoiz", 95, "Imperial Chunin");
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

                    Adv.RankUpClass("Imperial Chunin");
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

        // 5e. Shaman & Evolved Shaman (Arcangrove Rep 10 in /arcangrove — No boss, Gorillaphant & Seed Spitter ~2k HP, unlocks 2 unique classes for Player 4 & Player 5 from the same Rep!)
        bool HasShaman() => core.CheckInventory("Shaman", toInv: false);
        bool HasShamanRank10() => HasShaman() && HasRank10Class("Shaman");
        bool HasEvolvedShaman() => core.CheckInventory("Evolved Shaman", toInv: false);
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
                    SetClass();
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
                    Adv.BuyItem("arcangrove", 214, 5765, shopItemID: 4799);
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

                    Adv.RankUpClass("Shaman");
                    SetClass();
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
                "BRACKET30_EVOLVED_SHAMAN_BUY",
                HasEvolvedShaman,
                () =>
                {
                    if (HasEvolvedShaman())
                        return;

                    core.Logger("Level 30: Buying Evolved Shaman (same Arcangrove Rep 10)");
                    Adv.BuyItem("arcangrove", 214, "Evolved Shaman", shopItemID: 6396);
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

                    Adv.RankUpClass("Evolved Shaman");
                    SetClass();
                },
                (targetPlayerNumber, targetPlayerName) =>
                {
                    Army.CoordinateFollowAndCombat(targetPlayerName);
                }
            )
        )
            return false;

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
                    SetClass();
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

                    Adv.BuyItem("museum", 631, "Awethur's Accoutrements");
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

                    SetClass();
                    Farm.BlacksmithingREP(4, Bot.Config?.Get<bool>("UseGold") == true);
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

    #region SetClass (Party Role Composition & CBO Resolution)
    // Player 1: Main Solo DPS (never overlaps with Players 2-7 at the same milestone)
    private static readonly string[] Player1SoloClasses =
    {
        "King's Echo",
        "Chaos Avenger",
        "Void Highlord",
        "Void Highlord (IoDA)",
        "Legion Revenant",
        "Dragon of Time",
        "Cryomancer",
        "Dragonslayer General",
        "Glacial Berserker",
        "Dragonslayer",
        "DragonSoul Shinobi",
        "Assassin",
        "Ninja Warrior",
        "Classic Ninja",
        "Ninja",
        "Ninja (Rare)",
        "Rogue (Rare)",
        "Rogue",
    };

    // Player 2 (and Player 7 fallback in 7-player armies): Dedicated Healer / Support Buffer
    private static readonly string[] Player2SupportClasses =
    {
        "Lord of Order",
        "ArchPaladin",
        "StoneCrusher",
        "Oracle",
        "Healer (Rare)",
        "Healer",
    };

    // Player 3: Boss Hit-Chance Debuffer / Defense Shredder (Rogue at Step 1 -> Imperial Chunin at Lv 30)
    private static readonly string[] Player3DebuffClasses =
    {
        "ArchPaladin",
        "StoneCrusher",
        "Legion Revenant",
        "Imperial Chunin",
        "Rogue (Rare)",
        "Rogue",
        "Ninja",
    };

    // Player 4: Magic Damage Amplifier & Evasion Suppressor (Mage at Step 1 -> Shaman at Lv 30)
    private static readonly string[] Player4AmpClasses =
    {
        "Archfiend",
        "StoneCrusher",
        "Arachnomancer",
        "Shaman",
        "Mage (Rare)",
        "Mage",
        "Oracle",
    };

    // Player 5: Ranged / Secondary Debuff & Heal (Oracle at Step 1 -> Evolved Shaman at Lv 30)
    private static readonly string[] Player5FlexClasses =
    {
        "Dragon of Time",
        "Glacial Berserker",
        "Evolved Shaman",
        "Master Ranger",
        "Oracle",
        "Healer",
    };

    // Player 6: Ranged Physical DPS / Secondary Support (Master Ranger at Lv 30, or Support fallback)
    private static readonly string[] Player6FlexClasses =
    {
        "Void Highlord",
        "ArchPaladin",
        "StoneCrusher",
        "Master Ranger",
        "Oracle",
        "Healer",
    };

    // Player 7: Stealth / Dodge DPS or Secondary Healer
    private static readonly string[] Player7FlexClasses =
    {
        "Yami No Ronin",
        "DragonSoul Shinobi",
        "Ninja",
        "Oracle",
        "Healer",
    };

    private static readonly string[] FarmClasses =
    {
        "Legion Revenant",
        "ArchMage",
        "Dragon of Time",
        "Archfiend",
        "Blaze Binder",
        "Scarlet Sorceress",
        "Pyromancer",
        "Shaman",
        "Imperial Chunin",
        "Master Ranger",
        "Mage (Rare)",
        "Mage",
    };

    private static readonly string[] DodgeClasses =
    {
        "Great Thief",
        "Yami No Ronin",
        "Chrono Assassin",
        "Horc Evader",
        "Imperial Chunin",
        "Assassin",
        "Ninja Warrior",
        "Classic Ninja",
        "Ninja",
        "Ninja (Rare)",
        "Oracle",
        "Rogue (Rare)",
        "Rogue",
    };

    private string[] GetRoleSoloPool()
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

    private string GetPartyRoleLabel()
    {
        if (IsPlayer(2))
            return "P2-Healer/Support";
        if (IsPlayer(3))
            return "P3-Debuffer";
        if (IsPlayer(4))
            return "P4-MagicAmp";
        if (IsPlayer(5))
            return "P5-SubHealer/Debuff";
        if (IsPlayer(6))
            return "P6-RangedDPS/Support";
        if (IsPlayer(7))
            return "P7-DodgeDPS/Support";
        return "P1-MainDPS";
    }

    private static readonly HashSet<string> UpgradeHintsShown =
        new(StringComparer.OrdinalIgnoreCase);

    private string? _lastSolo;
    private string? _lastFarm;
    private string? _lastDodge;
    private int _setClassBusy;

    public void SetClass()
    {
        if (System.Threading.Interlocked.Exchange(ref _setClassBusy, 1) == 1)
        {
            CoreBots.Instance.Logger("SetClass is already running, skipping the overlapping call.");
            return;
        }

        try
        {
            SetClassInner();
        }
        finally
        {
            System.Threading.Volatile.Write(ref _setClassBusy, 0);
        }
    }

    private void SetClassInner()
    {
        CoreBots core = CoreBots.Instance;
        core.SoloClass = core.FarmClass = core.DodgeClass = "Generic";
        core.ReadCBO();
        ApplyArmyRoomSettings();

        string[] soloPool = GetRoleSoloPool();
        string roleLabel = GetPartyRoleLabel();

        core.SoloClass = ResolveClass(core.SoloClass, soloPool, $"Solo ({roleLabel})") ?? core.SoloClass;
        core.FarmClass = ResolveClass(core.FarmClass, FarmClasses, "Farm") ?? core.FarmClass;
        core.DodgeClass = ResolveClass(core.DodgeClass, DodgeClasses, "Dodge") ?? core.DodgeClass;

        static bool Settled(string? className) =>
            CoreBots.IsGenericClass(className)
            || Bot.Inventory.Items.Concat(Bot.Bank.Items).Any(item =>
                item.Category == ItemCategory.Class
                && item.Name.Equals(className, StringComparison.OrdinalIgnoreCase)
                && item.Quantity >= Rank10ClassPoints
            );

        if (
            string.Equals(_lastSolo, core.SoloClass, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_lastFarm, core.FarmClass, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_lastDodge, core.DodgeClass, StringComparison.OrdinalIgnoreCase)
            && Settled(core.SoloClass)
            && Settled(core.FarmClass)
            && Settled(core.DodgeClass)
        )
            return;

        RankIfNeeded(core.SoloClass, "SoloClass");
        RankIfNeeded(core.FarmClass, "FarmClass");
        RankIfNeeded(core.DodgeClass, "DodgeClass");

        Bot.Sleep(500);
        core.EquipClass(ClassType.Solo);

        _lastSolo = core.SoloClass;
        _lastFarm = core.FarmClass;
        _lastDodge = core.DodgeClass;
    }

    private static string? ResolveClass(string? current, string[] pool, string slot)
    {
        CoreBots core = CoreBots.Instance;
        bool userChoice =
            !CoreBots.IsGenericClass(current) && core.CheckInventory(current, toInv: false);

        if (!userChoice)
            return pool.FirstOrDefault(className => core.CheckInventory(className)) ?? current;

        int currentRank = Array.FindIndex(
            pool,
            className => className.Equals(current, StringComparison.OrdinalIgnoreCase)
        );
        if (currentRank > 0)
        {
            string? better = pool.Take(currentRank)
                .FirstOrDefault(className => core.CheckInventory(className, toInv: false));
            if (better != null && UpgradeHintsShown.Add($"{slot}:{current}>{better}"))
                core.Logger(
                    $"{slot} class is set to \"{current}\" in CBO, but you own \"{better}\". "
                        + $"Set the {slot} class to Generic in CBO to let FarmerJoe pick the best class automatically."
                );
        }
        return current;
    }

    private static void RankIfNeeded(string? className, string label)
    {
        CoreBots core = CoreBots.Instance;
        if (string.IsNullOrEmpty(className))
            return;

        if (!core.CheckInventory(className))
        {
            core.Logger($"WARNING: {label} class '{className}' not in inventory!");
            return;
        }

        if (core.CheckClassRank(false, className) >= 10)
            return;

        core.Logger($"{label} resolved -> {className}");
        Adv.RankUpClass(className);
    }
    #endregion SetClass

    private bool HasRank10Class(string className)
    {
        InventoryItem? item = Bot.Inventory.Items.Concat(Bot.Bank.Items).FirstOrDefault(
            item =>
                item.Name?.Trim().Equals(className, StringComparison.OrdinalIgnoreCase) == true
                && item.Category == ItemCategory.Class
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
