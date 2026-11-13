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
* **Safe Equipment Swapping & Inventory-Only Checks:**
  * Checks owned classes directly from inventory without wasting time opening or searching the bank.
  * Automatically steps into a safe non-combat room before swapping classes, buying items from shops, enhancing gear, or starting farm sessions, then applies each class's built-in skill rotation automatically.
* **Mandatory Private Room Setup:**
  * Defaults the shared room number to `"00000"` so every user must enter a valid private room number before starting, keeping the entire group in the same private instance.

---

## Implemented Progression Stages

1. **Stage 1 (Levels 1–30):**
   * Tutorial completion and starter gear setup.
   * Early Support class (`ORA`) + Level 10 + Rank 10.
   * Evasion starter class (`ROG`) + Rank 10.
   * Level 25 + coordinated prerequisite quests + `NIN` + Rank 10.
   * Starter AoE class (`MAG`) + Rank 10 + initial party role assignment.
2. **Stage 2A (Bracket 30):**
   * Daily reagent check and optional boost collection.
   * Level 30 + full gear enhancement update.
   * **Tier-1 Class Lineup:** Coordinated reputation grinds, prerequisite questlines, purchases, and Rank 10 for `MR` (AoE/Ranged), `DS` (Solo DPS), `IC` (Debuffer), `SHM` (Magic Amp), and `ES` (Sub-Support).
   * **Early Enhancements:** Weapon special enhancement questline + reputation unlock, and base helm enhancement via profession rank and coordinated boss chain.
3. **Stage 2B (Bracket 50):**
   * Level 50 + gear enhancement update.
   * Coordinated multi-boss storyline progression unlocking mid-game upgrades.
   * **Tier-2 Upgrades:** Coordinated drop/upgrade questline for `SS` (Rank 10), gold/material farming for `DSG` (Rank 10), and a +15% all-damage boost weapon.
4. **Stage 2C (Brackets 55, 60, and 65):**
   * **Bracket 55:** Level 55 + gear updates, token-based prerequisite class & dual reputations for `BB` (Rank 10), coordinated tower storyline & daily tokens for `CRYO` (Rank 10 when unlocked), and coordinated war storyline + reputation for `ARA` (Rank 10).
   * **Bracket 60:** Level 60 + gear updates, coordinated island storyline & boss tokens for `DSS` (Rank 10), and mid-game weapon proc enhancement unlocked through coordinated profession reputation rounds and boss drops.
   * **Bracket 65:** Level 65 + gear updates, coordinated winter storyline & reputation for `GB` (acquired and ranked to Rank 10 when its seasonal shop is active).

---

## Party Role & Class Progression (100% Free-to-Play)

> **Note:** Paid, promotional, and membership-locked classes are excluded (only free base classes and legacy starter variants are used).

### 1. Boss Composition Progression (Per Milestone)

| Stage / Milestone | Player 1 (`Main DPS` — Req.) | Player 2 (`Support` — Req.) | Player 3 (`Debuffer` — Req.) | Player 4 (`Buff / Amp` — Req.) | Player 5 (`Flex Debuff` — Opt.) | Player 6 (`Flex Amp/DPS` — Opt.) | Player 7 (`Flex Dodge/DPS` — Opt.) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Stage 1 (Lv 1–30)** | `NIN` | `ORA` | `ROG` | `MAG` | `ORA` / `HLR` | `ORA` / `HLR` | `NIN` / `ORA` |
| **Stage 2A (Bracket 30)** | `DS` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2B (Bracket 50)** | `DSG` | `ORA` | `IC` | `SHM` | `ES` | `SS` | `NIN` |
| **Stage 2C (Brackets 55–65)** | `CRYO` / `DSG` | `ORA` | `IC` | `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Target Endgame (Planned Lv 70–100)** | `KE` / `AI` / `CAV` / `VHL` / `DOT` | `SC` | `AP` | `LoO` | `VDK` / `GB` / `ES` | `AF` / `SS` | `YNR` / `DSS` |

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
