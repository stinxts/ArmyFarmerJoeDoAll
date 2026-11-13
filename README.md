WIP (WORK IN PROGRESS)

This is the Skeleton of the Army Farmer Joe Project.

Test Script ArmyFarmerJoeStep1.cs, ArmyFarmerJoeDoAll has everything while step 1 runs only till the changes that that has been made.

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon, Gemini 4 Hydrogen.

# Army FarmerJoe DoAll

A multi-account cooperative progression automation framework designed for synchronized groups of **4 to 7 accounts** (**Players 1–4 required; Players 5–7 optional**). All accounts progress together through every milestone, quest step, reputation tier, class rank-up, and boss encounter using a shared synchronization barrier and dynamic combat-assist engine.

---

## Core Architecture & Features

* **4-Player Required Core + Optional Flex Slots (4–7 Accounts):**
  * Requires at least **4 accounts** (`Player 1`–`Player 4`) to form a complete endgame meta party (`Main DPS`, `StoneCrusher`, `ArchPaladin`, `Lord of Order`), with `Players 5–7` supported as optional flex accounts for additional debuffs, damage amplification, and dodge/AoE DPS.
* **Strict Per-Step Synchronization Barriers:**
  * Every milestone, quest, item acquisition, and class rank-up is wrapped in a granular cooperative objective (`RunCooperativeObjective`).
  * No account skips stages based on global shortcuts; each step strictly validates its own completion state (`core.isCompletedBefore`, inventory/reputation/rank checks) before advancing the group.
  * Built-in attempt limits (`maxWorkAttempts`) and failure broadcast signals (`FAILED`) prevent infinite loops and safely halt the group if an account cannot complete a requirement.
* **Load-Balanced Helper Assist & First-Hit Rule:**
  * As soon as an account finishes its local requirement for the active step, it immediately transitions into **Helper Mode** (`CoordinateFollowAndCombat`).
  * Helpers distribute evenly across remaining accounts (`pendingPlayers[helperIndex % pendingPlayers.Count]`).
  * Helpers wait for the progressing account to land the first hit (`HP < MaxHP`) before engaging, ensuring kill credit and drops are never stolen from the account that needs them.
  * Built-in shield-state awareness automatically redirects helper attacks to active shield entities before targeting invulnerable bosses.
* **Two Unified Party Compositions (Boss & Farm):**
  * **Boss Composition (`Solo` / `Boss` / `Dodge`):** Unifies `SoloClass`, `BossClass`, and `DodgeClass` into one complete, non-overlapping party composition (Main DPS, Support/SC, Debuffer/AP, Amp/LoO, and optional Flex slots) so the group never breaks formation on hard bosses.
  * **Farm Composition (`Farm`):** Pairs multi-target AoE classes (`Players 1, 3, 4, 6, 7`) with dedicated Support/Buffer accounts (`Player 2` in 4-player armies, and `Player 2` + `Player 5` in 5–7 player armies) to maintain active heals, haste, and crit buffs during multi-target farming.
* **Dynamic Support Hand-Off (`SupportHelperPriority`):**
  * During class rank-up steps, all progressing accounts equip the target class simultaneously.
  * The **first account to finish** Rank 10 and enter Helper Mode automatically equips its highest-tier Support class (`LoO` → `AP` → `SC` → `ORA` → `ES` → `HLR (Rare)` → `HLR`) to heal and shield the remaining accounts.
  * Accounts whose designated Boss role is already a Support class (e.g., `Player 2 = StoneCrusher`, `Player 3 = ArchPaladin`, `Player 4 = Lord of Order`) preserve their assigned `SoloClass` in Helper Mode instead of being overridden, and temporary support helpers revert once dedicated supports take over.
* **Inventory-Only Class Verification:**
  * Class availability and Rank 10 checks (`HasClassInInventory`, `HasAnyClassInInventory`, `ResolveClass`, `HasRank10Class`) check `Bot.Inventory.Items` directly (`ItemCategory.Class`) without wasting time opening or polling the Bank for classes.
* **Out-of-Combat Safe Equipment, Shop & Enhancement Swapping:**
  * Before swapping classes, purchasing from shops (`Bot.Wait.ForItemBuy()`), applying enhancements (`Adv.SmartEnhance`), or switching to Farm/REP mode, accounts automatically step out to a neutral non-combat instance (`SwitchClassOutsideCombatRoom` / `LeaveCombatRoomForEquipmentChange`) to prevent combat-lock failures.
* **Native Advanced Skill Set Integration:**
  * All accounts and helpers automatically load and execute their class's configured `Base` rotation from the client's native Advanced Skills engine (`StartAdvanced`) upon equipping or ranking up a class.
* **Mandatory Shared Private Instance (`RoomNumber`):**
  * The `RoomNumber` option defaults to `"00000"` (`Option<string>`) with no automatic fallback; `ApplyArmyRoomSettings` + `Army.ValidatePrivateRoomNumber` require each user to configure a valid shared private room number (`1001–99999`) before the run starts.

---

## Implemented Progression Stages

1. **Stage 1 (Levels 1–30):**
   * Tutorial completion & starter equipment setup.
   * Early Support class acquisition (`ORA`) + Level 10 + cooperative `ORA` Rank 10.
   * Evasion starter class acquisition (`ROG`) + cooperative `ROG` Rank 10.
   * Level 25 + coordinated Mazumi prerequisite questline (`90`–`92` using `core.isCompletedBefore(90)`) + `NIN` acquisition + cooperative `NIN` Rank 10.
   * Starter AoE class acquisition (`MAG`) + cooperative `MAG` Rank 10 + final role resolution.
2. **Stage 2A (Bracket 30):**
   * Daily reagent check & optional experience/reputation boost collection.
   * Level 30 + full equipment enhancement update.
   * **Tier-1 AoE / Ranged Class (`MR`):** Sandsea Reputation Rank 10 → Purchase → Cooperative Rank 10.
   * **Tier-1 Solo DPS Class (`DS`):** 4-part prerequisite questline (coordinated 1-by-1) → Purchase → Cooperative Rank 10.
   * **Tier-1 Hit-Chance Debuffer Class (`IC`):** Yokai Reputation Rank 10 → Purchase → Cooperative Rank 10.
   * **Tier-1 Magic Amp & Sub-Support Classes (`SHM` & `ES`):** Arcangrove Reputation Rank 10 → Purchase & Cooperative Rank 10 for both classes.
   * **Weapon Special Enhancement Progression (`Blade of Awe`):** 5-part unlock questline (coordinated 1-by-1) → Blade of Awe Reputation Rank 10 → `Awethur's Accoutrements` unlock.
   * **Base Helm Enhancement Progression:** Blacksmithing Reputation Rank 4 (Traditional Quest `2777`) → Multi-boss questline (coordinated boss-by-boss with shield-state handling).
3. **Stage 2B (Bracket 50):**
   * Level 50 + full equipment `SmartEnhance`.
   * **Throne of Darkness Storyline (`HedgeMaze` & `TowerOfMirrors`, Quests `5298–5332`):** Full coordinated multi-boss progression up to `Scarletta` / `Groglurk`.
   * **Scarlet Sorceress (`SS`):** Coordinated `Blood Sorceress` drop → Quest `6236` upgrade → Cooperative Rank 10.
   * **Dragonslayer General (`DSG`):** Cooperative Gold & Dragon-scale/material farm → Purchase → Cooperative Rank 10.
   * **Burning Blade (+15% All Damage Boost):** Cooperative acquisition & equip check (respecting `Burning Blade of Abezeth` if already owned).
4. **Stage 2C (Brackets 55, 60, and 65):**
   * **Bracket 55:**
     * Level 55 + `SmartEnhance`.
     * **Blaze Binder (`BB`):** `Pyromancer` token daily (and buy to Rank 1/1 CP) → DoomWood REP 10 → Embersea REP 10 → Purchase & Cooperative Rank 10.
     * **Cryomancer (`CRYO`):** Coordinated `/frozentower` storyline (`3907–3937`) → Cryomancer daily quests (`3966` / `3965`) → Purchase & Cooperative Rank 10 once 84x tokens are reached.
     * **Arachnomancer (`ARA`):** Coordinated `/twilightedge` (`3428`) + `/weaverwar` (`3429–3439`) → Ravenloss REP 10 → Purchase & Cooperative Rank 10.
   * **Bracket 60:**
     * Level 60 + `SmartEnhance`.
     * **DragonSoul Shinobi (`DSS`):** Coordinated Yokai Island storyline across `/shogunwar`, `/shinringrove`, `/greenshell`, `/heiwavalley`, and `/shadowfortress` → 300x Dragon Shinobi Tokens via Quest `7924` (`1st Head of Orochi`) → Purchase & Cooperative Rank 10.
     * **Forge Weapon Enhancement — `Lacerate` (Quest `8739`):** Mazumi Quest `92` prerequisite → Traditional Blacksmithing REP Rank 5 (`Quest 2777` + 6 shared Army rounds of Quest `8736` across `/hydrachallenge`, `/maul`, and `/towerofdoom`) → Coordinated Lacerate boss drops & Quest `318` (`Black Knight Orb`) → `Lacerate` turn-in.
   * **Bracket 65:**
     * Level 65 + `SmartEnhance`.
     * **Glacial Berserker (`GB`):** Frozen Tower prerequisite + coordinated Glacera storyline up to Quest `5601` (`Soricomorpha`) → Glacera REP 10 → Seasonal `/icewindpass` Shop `1339` Purchase & Cooperative Rank 10 when active.

---

## Party Role & Class Progression Table (100% Free-to-Play)

> **Note:** All paid, promotional, and membership-locked classes are excluded (legacy free starter `(Rare)` variants such as `Healer (Rare)`, `Mage (Rare)`, and `Rogue (Rare)` are supported purely as fallbacks for their base classes). The system operates on **two unified compositions** (ignoring any manual per-account client class overrides):
> 1. **Boss Composition (`Solo` / `Boss` / `Dodge`):** Full non-overlapping synergistic party setup with **Players 1–4** converging to the endgame meta core (`Main DPS` + `SC` + `AP` + `LoO`) and **Players 5–7** adding flex debuffs, damage amps, and dodge DPS.
> 2. **Farm Composition (`Farm`):** Multi-target AoE farmers paired with dedicated Support/Buffer accounts (`Player 2` in 4-player groups, and `Player 2` + `Player 5` in 5–7 player groups) to maintain active heals, haste, and crit buffs during high-density farming.

### 1. Boss Composition Progression (Per Milestone)

| Stage / Milestone | Player 1 (`Main DPS` — Req.) | Player 2 (`Support` — Req.) | Player 3 (`Debuffer` — Req.) | Player 4 (`Buff / Amp` — Req.) | Player 5 (`Flex Debuff` — Opt.) | Player 6 (`Flex Amp/DPS` — Opt.) | Player 7 (`Flex Dodge/DPS` — Opt.) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Stage 1 (Lv 1–30)** | `NIN` | `ORA` | `ROG` | `MAG` | `ORA` / `HLR` | `ORA` / `HLR` | `NIN` / `ORA` |
| **Stage 2A (Bracket 30)** | `DS` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2B (Bracket 50)** | `DSG` | `ORA` | `IC` | `SHM` | `ES` | `SS` | `NIN` |
| **Stage 2C (Brackets 55–65)** | `CRYO` / `DSG` | `ORA` | `IC` | `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Target Endgame (Planned Lv 70–100)** | `KE` / `AI` / `CAV` / `VHL` / `DOT` | `SC` | `AP` | `LoO` | `VDK` / `GB` / `ES` | `AF` / `SS` | `YNR` / `DSS` |

---

### 2. Automatic Composition Priority (`SetClass`)

Each slot is resolved from **left to right** (highest priority first), selecting the best Rank 10 class currently in the account's Inventory (falling back to the highest-priority owned class in Inventory):

| Composition / Slot | Priority Order (Highest $\rightarrow$ Lowest) |
| :--- | :--- |
| **Boss Comp — Player 1 (`P1-MainDPS`, Req.)** | `KE` $\rightarrow$ `AI` $\rightarrow$ `CAV` $\rightarrow$ `VHL` $\rightarrow$ `VDK` $\rightarrow$ `DOT` $\rightarrow$ `CRYO` $\rightarrow$ `DSG` $\rightarrow$ `GB` $\rightarrow$ `DS` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ROG` |
| **Boss Comp — Player 2 (`P2-Support(SC)`, Req.)** | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 3 (`P3-Debuff(AP)`, Req.)** | `AP` $\rightarrow$ `IC` $\rightarrow$ `ROG (Rare)` $\rightarrow$ `ROG` $\rightarrow$ `NIN` $\rightarrow$ `ORA` |
| **Boss Comp — Player 4 (`P4-BuffAmp(LoO)`, Req.)** | `LoO` $\rightarrow$ `ARA` $\rightarrow$ `SHM` $\rightarrow$ `MAG (Rare)` $\rightarrow$ `MAG` $\rightarrow$ `ORA` |
| **Boss Comp — Player 5 (`P5-FlexDebuff`, Opt.)** | `VDK` $\rightarrow$ `GB` $\rightarrow$ `ES` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 6 (`P6-FlexAmpDPS`, Opt.)** | `AF` $\rightarrow$ `SS` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss Comp — Player 7 (`P7-FlexDodgeDPS`, Opt.)** | `YNR` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Player 2 (`Primary Farm Support`)** | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Player 5 (`Secondary Farm Support`)** | `LoO` $\rightarrow$ `SC` $\rightarrow$ `AP` $\rightarrow$ `ORA` $\rightarrow$ `ES` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm Comp — Players 1, 3, 4, 6, 7 (`AoE Farmers`)** | `AM` $\rightarrow$ `AI` $\rightarrow$ `DOT` $\rightarrow$ `AF` $\rightarrow$ `BB` $\rightarrow$ `SS` $\rightarrow$ `PYRO` $\rightarrow$ `SHM` $\rightarrow$ `IC` $\rightarrow$ `MR` $\rightarrow$ `MAG` |
