WIP (WORK IN PROGRESS)

This is the Skeleton of the Army Farmer Joe Project.

Test Script ArmyFarmerJoeStep1.cs, ArmyFarmerJoeDoAll has everything while step 1 runs only till the changes that that has been made.

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon, Gemini 4 Hydrogen.

# Army FarmerJoe DoAll

A multi-account cooperative progression automation framework designed for synchronized groups of **4 to 7 accounts** (**Players 1–4 required; Players 5–7 optional**). All accounts progress together through every level bracket, storyline, reputation tier, class rank-up, enhancement unlock, and boss encounter while assisting each other automatically.

---

## What the Script Does

* **Synchronized 4 to 7 Account Progression:**
  * Requires **4 accounts (`Player 1`–`Player 4`)** to cover all core party roles and supports up to **7 accounts (`Players 5–7` optional)** for extra utility and damage.
  * Keeps the entire group progressing step by step together: no account moves ahead until everyone finishes the current objective, preventing stalled runs.
* **Automatic Team Assist & Fair Drop Credit:**
  * Whenever an account finishes its own task in a step, it automatically follows and helps the remaining teammates evenly.
  * Helpers wait for the progressing player to hit the enemy first so kill credit and drops always go to the account that needs them, and prioritize defensive mechanics/shields on bosses when active.
* **Balanced Party Roles (Boss & Farm Modes):**
  * **Boss Setup:** Uses one unified, non-overlapping team composition (`Main DPS`, `Support`, `Debuffer`, `Buff/Amplifier`, plus optional Flex roles) across all single-target and boss fights.
  * **Farm Setup:** Pairs multi-target farming classes (`Players 1, 3, 4, 6, 7`) with dedicated supports (`Player 2`, plus `Player 5` in 5–7 player groups) to keep heals, haste, and buffs active during mob farming.
* **Smart Helper Support Hand-Off:**
  * During group class rank-up steps, the first account to reach Rank 10 temporarily switches to a backup Support role to heal and shield teammates still finishing.
  * Dedicated support accounts preserve their own support class while helping, and temporary helpers revert back to their normal role once the main supports take over.
* **Safe Equipment Swapping, Boost Gear Policy & Inventory/Bank Awareness:**
  * Active role switching (`SetClass`) selects classes directly from inventory while boost gear, daily/weekly reagents, and acquisition checks inspect both inventory and bank when needed.
  * Configurable `BoostGearPolicy` (`UpgradeIfBetter` / `KeepEquippedGear`), `KeepEquippedGearPlayers` (by Army slot or username), and per-account gear-lock options prevent overwriting superior pre-equipped endgame gear or special enhancements.
  * Preserves classic progression options (`OutFit`, `EquipOutfit`, `PetChoice`, and `SellStarterClasses`) with Army coordination and gear-preservation checks.
  * Automatically steps into a safe non-combat room before swapping classes, turning in combat quests, buying shop items, enhancing gear, or starting farm sessions, then applies each class's built-in skill rotation automatically.
* **Mandatory Private Room Setup & Subgroup Room Splitting:**
  * Defaults the shared room number to `"00000"` so every user must enter a valid private room number before starting, keeping the entire group in the same private instance.
  * Automatically splits the Army into 3-player subgroup private instances on 3-player-capped maps during reputation grinds so accounts beyond the room cap never collide, and puts helpers into sleep mode when a teammate is inside a higher-level locked room.

---

## Implemented Progression Stages

1. **Stage 1 (Levels 1–30):**
   * Tutorial completion and starter gear setup.
   * Early Support class (`ORA`) + Level 10 + Rank 10.
   * Evasion starter class (`ROG`) + Rank 10.
   * Level 25 + coordinated prerequisite quests + `NIN` + Rank 10.
   * Starter AoE class (`MAG`) + Rank 10 + initial party role assignment.
2. **Stage 2A (Bracket 30):**
   * Daily reagent check, approved Weeklies, approved Daily, and optional boost collection (`GetBoosts`).
   * Level 30 + post-level-up gear enhancement update.
   * **Tier-1 Class Lineup:** Coordinated reputation grinds (with automatic 3-player subgroup splitting on capped maps), prerequisite questlines, purchases, and Rank 10 for `MR` (AoE/Ranged), `DS` (Solo DPS), `IC` (Debuffer), `SHM` (Magic Amp), and `ES` (Sub-Support).
   * **Early Enhancements & Boost Gear:** Weapon special enhancement reputation + static +25% CP/EXP/Gold/Rep boost gear sync (via `BoostGearPolicy`) and base helm special enhancement via profession rank and coordinated boss chain.
3. **Stage 2B (Bracket 50):**
   * Level 50 + post-level-up gear enhancement update.
   * Coordinated multi-boss storyline progression unlocking mid-game upgrades.
   * **Tier-2 Upgrades:** Coordinated drop, prerequisite Rank 10, and upgrade questline for `SS` (Rank 10), gold/material farming for `DSG` (Rank 10), and a +15% all-damage boost weapon (paired with best owned static boost gear).
   * **Approved Bracket 50 Dailies:** Daily crafting metals/reputation quest and `SG` daily tokens & class unlock.
4. **Stage 2C (Brackets 55, 60, and 65):**
   * **Bracket 55:** Level 55 + post-level-up gear updates, token-based prerequisite class (`PYRO` 1x rank-up) & dual reputations for `BB` (Rank 10), coordinated tower storyline & daily tokens for `CRYO` (Rank 10 when unlocked), coordinated war storyline + reputation for `ARA` (Rank 10), +25% CP/EXP/Gold/Rep boost cape from coordinated raid boss, and approved Dailies toward `BK`.
   * **Bracket 60:** Level 60 + post-level-up gear updates, coordinated island storyline & boss tokens for `DSS` (Rank 10), and first-tier special weapon enhancement unlocked through coordinated profession Rank 5 rounds and boss drops.
   * **Bracket 65:** Level 65 + post-level-up gear updates, coordinated winter storyline & reputation for `GB` (acquired and ranked to Rank 10 when its seasonal shop is active), plus approved Dailies (daily reputation quest + the 10-day `LoO` progression chain & Rank 10), followed by optional `SellStarterClasses` cleanup and `OutFit` / `EquipOutfit` / `PetChoice` acquisition & equip.
5. **Stage 2D (Bracket 70):**
   * Level 70 + post-level-up gear enhancement update.
   * **Endgame Support & Debuff Core:** Coordinated storyline + multi-faction Rank 10 reputations for `SC` (Rank 10), plus alignment/crafting prerequisites, prerequisite storylines, and full 5-part quest chain for `AP` (Rank 10).
   * **Special Helm Enhancements:** Coordinated unlocks for physical/evasion and magical/healer special helm enhancements via faction Rank 10, starter class Rank 10 (`ROG` / `HLR`), and boss material farming.
6. **Stage 3A (Level 75):**
   * Level 75 + post-level-up gear enhancement update.
   * **Main 13-Chapter Storyline (`13LoC`):** Full coordinated 13-saga storyline from prologue through the final boss encounter.
   * **Mid-Late Classes:** `SHM` + `ES` verification/Rank 10 and storyline faction Rank 10 + `CS` (variant acquisition & Rank 10).
   * **Special Weapon Enhancements:** Coordinated unlocks for base weapon special enhancement and three mid-tier weapon special enhancements via profession Rank 6, prerequisite storylines, raid boss drops, and faction Rank 10, followed by post-stage `SellStarterClasses` cleanup and `OutFit` / `EquipOutfit` / `PetChoice` sync.

---

## Party Role & Class Progression (100% Free-to-Play)

> **Note:** Paid, promotional, and membership-locked classes are excluded (only free base classes and legacy starter variants are used).

### 1. Boss Composition Progression (Per Milestone)

| Stage / Milestone | Player 1 (`Main DPS` — Req.) | Player 2 (`Support` — Req.) | Player 3 (`Debuffer` — Req.) | Player 4 (`Buff / Amp` — Req.) | Player 5 (`Flex Debuff` — Opt.) | Player 6 (`Flex Amp/DPS` — Opt.) | Player 7 (`Flex Dodge/DPS` — Opt.) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Stage 1 (Lv 1–30)** | `NIN` | `ORA` | `ROG` | `MAG` | `ORA` / `HLR` | `ORA` / `HLR` | `NIN` / `ORA` |
| **Stage 2A (Bracket 30)** | `DS` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2B (Bracket 50)** | `DSG` | `ORA` | `IC` | `SHM` | `ES` | `SS` | `NIN` |
| **Stage 2C (Brackets 55–65)** | `CRYO` / `DSG` | `ORA` | `IC` | `LoO` / `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Stage 2D–3A (Brackets 70–75)** | `CRYO` / `DSG` | `SC` | `AP` | `LoO` / `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Target Endgame (Planned Lv 75–100)** | `KE` / `AI` / `CAV` / `VHL` / `DOT` | `SC` | `AP` | `LoO` | `VDK` / `GB` / `ES` | `AF` / `SS` | `YNR` / `DSS` |

---

### 2. Automatic Class Priority Order

Each slot automatically picks the highest-priority Rank 10 class owned in inventory (or the highest-priority owned class while leveling):

| Composition / Slot | Priority Order (Highest $\rightarrow$ Lowest) |
| :--- | :--- |
| **Boss Comp — Player 1 (`Main DPS`, Req.)** | `KE` $\rightarrow$ `AI` $\rightarrow$ `CAV` $\rightarrow$ `VHL` $\rightarrow$ `VDK` $\rightarrow$ `DOT` $\rightarrow$ `CRYO` $\rightarrow$ `DSG` $\rightarrow$ `GB` $\rightarrow$ `DS` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ROG` |
| **Boss Comp — Player 2 (`Support`, Req.)** | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 3 (`Debuffer`, Req.)** | `AP` $\rightarrow$ `IC` $\rightarrow$ `ROG (Rare)` $\rightarrow$ `ROG` $\rightarrow$ `NIN` $\rightarrow$ `ORA` |
| **Boss Comp — Player 4 (`Buff / Amp`, Req.)** | `LoO` $\rightarrow$ `ARA` $\rightarrow$ `SHM` $\rightarrow$ `MAG (Rare)` $\rightarrow$ `MAG` $\rightarrow$ `ORA` |
| **Boss Comp — Player 5 (`Flex Debuff`, Opt.)** | `VDK` $\rightarrow$ `GB` $\rightarrow$ `ES` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 6 (`Flex Amp/DPS`, Opt.)** | `AF` $\rightarrow$ `SS` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 7 (`Flex Dodge/DPS`, Opt.)** | `YNR` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Player 2 (`Primary Farm Support`)** | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Player 5 (`Secondary Farm Support`)** | `LoO` $\rightarrow$ `SC` $\rightarrow$ `AP` $\rightarrow$ `ORA` $\rightarrow$ `ES` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Players 1, 3, 4, 6, 7 (`AoE Farmers`)** | `AM` $\rightarrow$ `AI` $\rightarrow$ `DOT` $\rightarrow$ `AF` $\rightarrow$ `BB` $\rightarrow$ `SS` $\rightarrow$ `PYRO` $\rightarrow$ `SHM` $\rightarrow$ `IC` $\rightarrow$ `MR` $\rightarrow$ `MAG` |
