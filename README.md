WIP (WORK IN PROGRESS)

This is the Skeleton of the Army Farmer Joe Project.

# Army FarmerJoe DoAll

A cooperative, multi-account (**Army**) adaptation of the Skua `FarmerJoeDoAll` progression script. Instead of running accounts independently, this project synchronizes 2 to 7 accounts step-by-step so faster or higher-level accounts actively follow and assist progressing accounts through every quest, reputation grind, and boss fight.

Test Script ArmyFarmerJoeStep1.cs, ArmyFarmerJoeDoAll has everything while step 1 runs only till the changes that that has been made.

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon.

## Installation & Folder Structure

Place the files inside your Skua `Scripts` directory following this exact hierarchy:

```text
Scripts/
├── CoreArmyFJ.cs
└── Other/
    └── ArmyFarmerJoe/
        ├── ArmyFarmerJoeDoAll.cs
        └── ArmyFarmerJoeStep1.cs
```

## Script Changes & Progression (Current Release)

- **Shared Private Room Enforcement (`RoomNumber` Option):** Forces all Army accounts into the exact same private room (`1001–99999`), overriding per-account CBO room settings and disabling public-room splits on bosses.
- **Cooperative Helper System:** Accounts that finish a task early (or already meet the requirements) automatically follow and assist pending accounts instead of skipping ahead.
- **First-Hit Rule & Boss Mechanics:** Helpers use proper class skill rotations (`Supp` mode for healers/buffers, `Base` mode for DPS), wait for the assisted player to land the first hit before attacking, and respect boss shield phases (Escherion's Staff / Vath's Stalagbite).
- **Helper Load Balancing & Fail-Safes:** Helpers split evenly across multiple pending players, and shop/turn-in steps have retry limits to prevent infinite loops.
- **100% Free-to-Play Role-Based Party Composition:** `SetClass()` automatically assigns unique F2P party roles based on player slot so buffs and debuffs stack without duplicate DPS/Debuff classes (and removes obsolete class banking).
- **Step 1 (Levels 1–25 / Beginner Setup):** Synchronized Tutorial, starter gear, Level 10, Rogue (Rank 10), Level 25, Mazumi questline (coordinated monster-by-monster and boss-by-boss), Ninja (Rank 10), Mage (Rank 10), and **Oracle** (0 AC starter Healer/Support at Rank 10).
- **Step 2A (Level 30 Bracket):** Synchronized Elders' Blood daily, Level 30 + SmartEnhance, Master Ranger (Sandsea Rep 10 + Rank 10), Dragonslayer (Galanoth quests 165–168 one-by-one + Rank 10), **Imperial Chunin** (Yokai Rep 10 + Rank 10), **Shaman & Evolved Shaman** (Arcangrove Rep 10 + Rank 10), Blade of Awe unlock quests (2933–2937 one-by-one + Rep 10 + Awethur's Accoutrements), and Forge Helm Enhancement (Blacksmithing Rep 4 + Quest 8828 coordinated boss-by-boss).

## Party Class Progression by Player Slot (100% Free-to-Play)

All accounts unlock every class together, while `SetClass()` automatically assigns a unique **Solo/Boss** role to each player slot using strictly F2P farmable classes:

| Player Slot | Party Role | Step 1 (Lv 1–25) | Step 2A (Lv 30 Bracket) | Mid-Game (Lv 50–65) | Late / Endgame (Lv 70–100) |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **Player 1** | **Main Solo DPS** | `Rogue` -> `Ninja` | `Dragonslayer` | `Dragonslayer General` -> `Glacial Berserker` -> `Cryomancer` | `Dragon of Time` -> `Void Highlord` -> `Chaos Avenger` -> `King's Echo` |
| **Player 2** | **Main Healer / Support** | `Oracle` | `Oracle` | `StoneCrusher` | `ArchPaladin` -> `Lord of Order` |
| **Player 3** | **Boss Hit-Chance Debuffer** | `Rogue` | `Imperial Chunin` | `Imperial Chunin` -> `StoneCrusher` | `ArchPaladin` |
| **Player 4** | **Magic Amp / Anti-Evade** | `Mage` | `Shaman` | `Shaman` -> `Arachnomancer` -> `StoneCrusher` | `Archfiend` |
| **Player 5** | **Sub-Healer / Crit Debuff** | `Oracle` | `Evolved Shaman` | `Evolved Shaman` -> `Glacial Berserker` | `Dragon of Time` |
| **Player 6** | **Ranged DPS / Support** | `Oracle` | `Master Ranger` | `Master Ranger` -> `StoneCrusher` | `ArchPaladin` -> `Void Highlord` |
| **Player 7** | **Dodge DPS / Support** | `Ninja` | `Ninja` | `DragonSoul Shinobi` | `Yami No Ronin` |

### Shared AoE Farm & Dodge Progression (All Players)
- **AoE Farm Class (`ClassType.Farm`):** `Mage` (Step 1) -> `Master Ranger` -> `Imperial Chunin` -> `Shaman` (Step 2A) -> `Scarlet Sorceress` -> `Blaze Binder` -> `Archfiend` -> `Dragon of Time` -> `ArchMage`.
- **Dodge Class (`ClassType.Dodge`):** `Rogue` -> `Oracle` / `Ninja` (Step 1) -> `Imperial Chunin` (Step 2A) -> `Horc Evader` -> `Yami No Ronin`.
````
