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
        bool HasOracle() => CoreBots.Instance.CheckInventory("Oracle", toInv: false);
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
        bool HasRogue() =>
            CoreBots.Instance.CheckInventory(
                new[] { "Rogue (Rare)", "Rogue" },
                any: true,
                toInv: false
            );

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
                () => HasNinja() || HasRank10Rogue(),
                () =>
                {
                    if (HasNinja() || HasRank10Rogue())
                        return;

                    CoreBots.Instance.Logger("Getting starter Dodge class (Ninja) - Ranking Rogue");
                    string rogueClass = CoreBots.Instance.CheckInventory("Rogue (Rare)", toInv: false)
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
                        CoreBots.Instance.CheckInventory("Assassin", toInv: false) ? "Assassin"
                        : CoreBots.Instance.CheckInventory("Ninja Warrior", toInv: false) ? "Ninja Warrior"
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
        bool HasMage() =>
            CoreBots.Instance.CheckInventory(
                new[] { "Mage (Rare)", "Mage" },
                any: true,
                toInv: false
            );

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

        // 6b. Rank Mage to 10 (all accounts rank up Mage; 1st to finish becomes Support Helper)
        if (
            !RunCooperativeObjective(
                "BEGINNER_MAGE_RANK_10",
                HasRank10Mage,
                () =>
                {
                    if (HasRank10Mage())
                        return;

                    string mageClass = CoreBots.Instance.CheckInventory("Mage (Rare)", toInv: false)
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
                    if (
                        !enhancedBracket50
                        || Bot.Inventory.Items.Any(x =>
                            x != null && x.EnhancementLevel < Bot.Player.Level
                        )
                    )
                    {
                        LeaveCombatRoomForEquipmentChange();
                        Adv.SmartEnhance(Bot.Player.CurrentClass?.Name ?? string.Empty);
                        enhancedBracket50 = true;
                    }

                    SetClass();
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
        bool HasScarletSorceress() => core.CheckInventory("Scarlet Sorceress", toInv: false);
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
                () => HasScarletSorceress() || core.CheckInventory(36298, toInv: true),
                () =>
                {
                    if (HasScarletSorceress() || core.CheckInventory(36298, toInv: true))
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

                    if (!core.CheckInventory(36298, toInv: true))
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
                                    .FirstOrDefault(c => core.CheckInventory(c, toInv: true))
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
        bool HasDSG() => core.CheckInventory(35996, toInv: false);
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

                    SetClass();
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

                    // Quest 5294 requires Dragonslayer Rank 10 in inventory (unbanked)
                    core.CheckInventory("Dragonslayer", toInv: true);
                    core.Logger(
                        "Level 50: Farming Enchanted Scale (75) & Dragon Claw (100) for Dragonslayer General"
                    );
                    core.AddDrop("Enchanted Scale", "Dragon Claw");
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
                    core.BuyItem("dragontown", 1286, 35996, shopItemID: 3937);
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

    #region SetClass (Party Role Composition & CBO Resolution — 100% Free-to-Play Classes Only)
    // Player 1: Main Solo DPS (never overlaps with Players 2-7 at the same milestone)
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

    // Player 2 (and Player 7 fallback in 7-player armies): Dedicated Healer / Support Buffer
    private static readonly string[] Player2SupportClasses =
    {
        "Lord of Order",
        "ArchPaladin",
        "StoneCrusher",
        "Oracle",
        "Healer",
    };

    // Player 3: Boss Hit-Chance Debuffer / Defense Shredder (Rogue at Step 1 -> Imperial Chunin at Lv 30)
    private static readonly string[] Player3DebuffClasses =
    {
        "Verus DoomKnight",
        "ArchPaladin",
        "StoneCrusher",
        "Imperial Chunin",
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

    // Secondary Support pool for Player 5 during Farm mode in larger armies (5-7 players)
    private static readonly string[] Player5FarmSupportClasses =
    {
        "StoneCrusher",
        "ArchPaladin",
        "Lord of Order",
        "Evolved Shaman",
        "Oracle",
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
        // - Player 2 stays on Primary Healer/Support (2-7 player armies)
        // - Player 5 stays on Secondary Buffer/Support (5-7 player armies)
        if (IsPlayer(2))
            return Player2SupportClasses;
        if (IsPlayer(5))
            return Player5FarmSupportClasses;

        return FarmClasses;
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

        static bool Settled(string? className) =>
            CoreBots.IsGenericClass(className)
            || Bot.Inventory.Items.Concat(Bot.Bank.Items).Any(item =>
                item.Category == ItemCategory.Class
                && item.Name.Equals(className, StringComparison.OrdinalIgnoreCase)
                && item.Quantity >= Rank10ClassPoints
            );

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
        CoreBots core = CoreBots.Instance;
        // Prefer the highest-priority class in the pool that is already Rank 10,
        // falling back to the highest-priority owned class.
        string? bestRank10 = pool.FirstOrDefault(className =>
            HasRank10Class(className) && core.CheckInventory(className, toInv: true)
        );
        if (!string.IsNullOrEmpty(bestRank10))
            return bestRank10;

        return pool.FirstOrDefault(className => core.CheckInventory(className, toInv: true))
            ?? "Generic";
    }

    private void RankIfNeeded(string? className, string label)
    {
        CoreBots core = CoreBots.Instance;
        if (string.IsNullOrEmpty(className) || CoreBots.IsGenericClass(className))
            return;

        if (!core.CheckInventory(className))
        {
            core.Logger($"WARNING: {label} class '{className}' not in inventory!");
            return;
        }

        if (core.CheckClassRank(false, className) >= 10)
            return;

        core.Logger($"{label} resolved -> {className}");
        RankUpClassForArmy(className);
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
