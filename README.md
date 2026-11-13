WIP (WORK IN PROGRESS)

This is the Skeleton of the Army Farmer Joe Project.

Test Script ArmyFarmerJoeStep1.cs, ArmyFarmerJoeDoAll has everything while step 1 runs only till the changes that that has been made.

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon.

# Army FarmerJoe DoAll

A multi-account cooperative progression automation framework designed for synchronized groups of **2 to 7 accounts**. All accounts progress together through every milestone, quest step, reputation tier, class rank-up, and boss encounter using a shared synchronization barrier and dynamic combat-assist engine.

---

## Core Architecture & Features

* **Strict Per-Step Synchronization Barriers:**
  * Every milestone, quest, item acquisition, and class rank-up is wrapped in a granular cooperative objective (`RunCooperativeObjective`).
  * No account skips stages based on global shortcuts; each step strictly validates its own completion state before advancing the group.
  * Built-in attempt limits and failure broadcast signals (`FAILED`) prevent infinite loops and safely halt the group if an account cannot complete a requirement.
* **Load-Balanced Helper Assist & First-Hit Rule:**
  * As soon as an account finishes its local requirement for the active step, it immediately transitions into **Helper Mode** (`CoordinateFollowAndCombat`).
  * Helpers distribute evenly across remaining accounts (`pendingPlayers[helperIndex % pendingPlayers.Count]`).
  * Helpers wait for the progressing account to land the first hit (`HP < MaxHP`) before engaging, ensuring kill credit and drops are never stolen from the account that needs them.
  * Built-in shield-state awareness automatically redirects helper attacks to active shield entities before targeting invulnerable bosses.
* **Dynamic Support Hand-Off (Option 1 — First-Helper-To-Support):**
  * During class rank-up steps, all progressing accounts equip the target class simultaneously.
  * The **first account to finish** Rank 10 and enter Helper Mode automatically equips its highest-tier Support class (`LoO` → `AP` → `SC` → `ORA` → `HLR`) to heal and shield the remaining accounts.
  * Once **Player 2** (the dedicated Support slot) finishes its own Rank 10, Player 2 automatically takes over the Support role and the temporary support helper switches back to its primary DPS/Debuff class.
* **Out-of-Combat Safe Equipment & Enhancement Swapping:**
  * Before swapping classes or applying enhancements during active group combat, accounts automatically step out to a neutral non-combat instance (`SwitchClassOutsideCombatRoom` / `LeaveCombatRoomForEquipmentChange`).
  * This prevents server-side combat locks caused by residual kill credits, monster aggro, or active party buffs/HoTs from blocking equipment or enhancement changes.
* **Native Advanced Skill Set Integration:**
  * All accounts and helpers automatically load and execute their class's configured `Base` rotation from the client's native Advanced Skills engine (`StartAdvanced`) upon equipping or ranking up a class.
* **Enforced Shared Private Instance:**
  * A dedicated configuration option (`RoomNumber`, default `91827`) forces private instance mode across all accounts and overrides local client settings on startup and after configuration reloads.

---

## Implemented Progression Stages

1. **Stage 1 (Levels 1–30):**
   * Tutorial completion & starter equipment setup.
   * Early Support class acquisition (`ORA`) + Level 10 + cooperative `ORA` Rank 10.
   * Evasion starter class acquisition (`ROG`) + cooperative `ROG` Rank 10.
   * Level 25 + multi-step prerequisite questline (coordinated target-by-target) + `NIN` acquisition + cooperative `NIN` Rank 10.
   * Starter AoE class acquisition (`MAG`) + cooperative `MAG` Rank 10 + final role resolution.
2. **Stage 2A (Bracket 30):**
   * Daily reagent check & optional experience/reputation boost collection.
   * Level 30 + full equipment enhancement update.
   * **Tier-1 AoE / Ranged Class (`MR`):** Reputation Rank 10 → Purchase → Cooperative Rank 10.
   * **Tier-1 Solo DPS Class (`DS`):** 4-part prerequisite questline (coordinated 1-by-1) → Purchase → Cooperative Rank 10.
   * **Tier-1 Hit-Chance Debuffer Class (`IC`):** Reputation Rank 10 → Purchase → Cooperative Rank 10.
   * **Tier-1 Magic Amp & Sub-Support Classes (`SHM` & `ES`):** Reputation Rank 10 → Purchase & Cooperative Rank 10 for both classes.
   * **Weapon Special Enhancement Progression:** 5-part unlock questline (coordinated 1-by-1) → Reputation Rank 10 → Gear unlock.
   * **Base Helm Enhancement Progression:** Profession Rank 4 → Multi-boss questline (coordinated boss-by-boss with shield-state handling).

---

## Party Role & Class Progression Table (100% Free-to-Play)

> **Note:** All paid, promotional, membership-locked, and rare classes are excluded. Every account follows a strictly Free-to-Play (F2P) progression path with zero duplicate Solo/Support/Debuff classes across 4-player groups (and unique flex assignments up to 7 players).

### 1. Milestone Class Progression (Per Player)

| Stage / Milestone | Player 1 (`Main DPS`) | Player 2 (`Healer / Support`) | Player 3 (`Debuffer / Def Shred`) | Player 4 (`Magic Amp / Evasion Suppress`) | Player 5 (`Sub-Healer / Ranged`) | Player 6 (`Ranged DPS / Backup Supp`) | Player 7 (`Dodge DPS / Backup Supp`) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Stage 1 (Lv 1–30)** | `NIN` | `ORA` | `ROG` | `MAG` | `ORA` | `ROG` / `ORA` | `NIN` / `ORA` |
| **Stage 2A (Bracket 30)** | `DS` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2B (Bracket 50)** | `DSG` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2C (Bracket 70)** | `GB` / `CRYO` | `SC` | `AP` | `ARA` | `ES` | `SC` / `AP` | `DSS` |
| **Stage 2D (Bracket 75–99)** | `DOT` | `LoO` | `AP` | `AF` | `GB` | `AP` / `SC` | `YNR` |
| **Stage 2E (Bracket 100)** | `KE` / `AI` / `CAV` / `VHL` | `LoO` | `VDK` / `AP` | `AF` / `SC` | `DOT` | `VHL` / `AP` | `YNR` |

---

### 2. Automatic Slot Resolution Priority (`SetClass`)

Each slot is resolved from **left to right** (highest priority first), selecting the best Rank 10 class owned by that account:

| Player / Mode | Priority Order (Highest $\rightarrow$ Lowest) |
| :--- | :--- |
| **Player 1 (`SoloClass`)** | `KE` $\rightarrow$ `AI` $\rightarrow$ `CAV` $\rightarrow$ `VHL` $\rightarrow$ `VDK` $\rightarrow$ `DOT` $\rightarrow$ `CRYO` $\rightarrow$ `DSG` $\rightarrow$ `GB` $\rightarrow$ `DS` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ROG` |
| **Player 2 (`SoloClass` & `FarmClass`)** | `LoO` $\rightarrow$ `AP` $\rightarrow$ `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR` |
| **Player 3 (`SoloClass`)** | `VDK` $\rightarrow$ `AP` $\rightarrow$ `SC` $\rightarrow$ `IC` $\rightarrow$ `ROG` $\rightarrow$ `NIN` |
| **Player 4 (`SoloClass`)** | `AF` $\rightarrow$ `SC` $\rightarrow$ `ARA` $\rightarrow$ `SHM` $\rightarrow$ `MAG` $\rightarrow$ `ORA` |
| **Player 5 (`SoloClass`)** | `DOT` $\rightarrow$ `GB` $\rightarrow$ `ES` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR` |
| **Player 6 (`SoloClass`)** | `VHL` $\rightarrow$ `AP` $\rightarrow$ `SC` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR` |
| **Player 7 (`SoloClass`)** | `YNR` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ORA` $\rightarrow$ `HLR` |
| **Players 1, 3–7 (`FarmClass`)** | `AM` $\rightarrow$ `AI` $\rightarrow$ `DOT` $\rightarrow$ `AF` $\rightarrow$ `BB` $\rightarrow$ `SS` $\rightarrow$ `PYRO` $\rightarrow$ `SHM` $\rightarrow$ `IC` $\rightarrow$ `MR` $\rightarrow$ `MAG` |
| **All Players (`DodgeClass`)** | `YNR` $\rightarrow$ `HE` $\rightarrow$ `IC` $\rightarrow$ `NIN` $\rightarrow$ `ORA` $\rightarrow$ `ROG` |
````
