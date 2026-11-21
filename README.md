Army Farmer Joe do All

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon, Gemini 4 Hydrogen.

# Army FarmerJoe DoAll

A multi-account cooperative progression framework built for synchronized groups of **4 to 7 accounts** (**Players 1–4 required; Players 5–7 optional**). Every account progresses together from **Level 1 to Level 100 Endgame**—clearing level brackets, storylines, reputation tiers, class rank-ups, enhancement unlocks, and endgame gear—while assisting teammates automatically and staying aligned at every barrier.

---

## Original FarmerJoe vs. Army FarmerJoe DoAll

| Feature | Original FarmerJoe | Army FarmerJoe DoAll |
| :--- | :--- | :--- |
| **Execution Model** | Single-account solo progression | Synchronized **4–7 account** cooperative progression with per-step barriers |
| **Party Roles** | One generic Solo/Farm rotation per account | Role by **seat in the room** (`Main DPS`, `StoneCrusher`, `ArchPaladin`, `Lord of Order`, ...) so split rooms stay balanced, plus a **Farm Comp** |
| **Teammate Assistance** | None | Automatic follow & assist, fair first-hit drop credit, and backup Support hand-off during rank-ups |
| **Room Management** | Standard room joins | Mandatory private room (`00000` guard), Player 1 checks each map's player limit and splits the group into balanced rooms (e.g. **4 + 3** or **3 + 2 + 2**), an overflow room when a room is full, plus sleep mode on level-locked rooms |
| **Enhancements & Readiness** | Re-applied repeatedly across steps | **Pre-combat Ready Check** + **on-demand class-kit enhancements** (no wasted loops while skipping completed steps) |
| **Gear Protection** | Basic best-gear equip | Configurable `BoostGearPolicy`, per-player gear locks, and safe non-combat room swaps |

---

## Core Architecture Highlights

* **Synchronized 4–7 Account Barriers:** No account advances to the next bracket until all teammates finish the current objective.
* **Ready Check & On-Demand Class Kits:** Before combat begins, every account verifies its role class, applies that class's predetermined enhancement kit when needed, and waits at the barrier so the party enters fights together.
* **Smart Subgroup Splitting & Level-Lock Sleep:** Player 1 checks each map's player limit (remembered for next time) and splits 5–7 player groups into balanced rooms (e.g. **4 + 3** or **3 + 2 + 2**); if a room is still full, accounts move to an overflow room. Helpers only assist teammates in their own room and go into sleep mode when a teammate is inside a higher-level locked room.
* **Self-Recovery:** If one account's bot restarts (relogin or manual restart), the whole group rejoins automatically and fast-forwards past finished steps; if an account cannot continue, every account stops with the reason instead of waiting forever.
* **Safe Equipment Swapping & Inventory/Bank Awareness:** Steps into a safe non-combat room before swapping classes, turning in quests, buying items, or enhancing gear.

---

## Complete Progression Stages (Level 1–100)

1. **Stage 1 (`RunLevel1To30` — Levels 1–30):**
   * **Startup Cleanup & Dailies:** Safely exits combat before running `BankAllItems` (`BankAll` for unused AC inventory & house items; needed items are automatically unbanked later), syncs static boost gear & initial role class, and runs any already-unlocked Dailies/Weeklies once at startup (including per-day `Lord of Order` skip gates so inactive days skip in a single barrier) before progression.
   * Tutorial completion and starter gear setup.
   * Early Support (`ORA`, Level 10, Rank 10), Evasion starter (`ROG`, Rank 10), Level 25 + prerequisite quests + `NIN` (Rank 10), and AoE starter (`MAG`, Rank 10) with initial party role assignment.
2. **Stage 2 (`RunBracket30` — Level 30):**
   * Approved Dailies & Weeklies + optional `GetBoosts` (gated independently so `GetBoosts` never blocks skipping completed Bracket 30 steps).
   * Level 30 + **Tier-1 Class Lineup** (reputations with 3-player subgroup splitting, prerequisites, and Rank 10): `MR`, `DS`, `IC`, `SHM`, `ES`, and `BRD`.
   * Base special weapon enhancement + static +25% CP/EXP/Gold/Rep boost gear sync (`toInv: false` bank-aware skip checks) + base special helm enhancement.
3. **Stage 3 (`RunBracket50` — Level 50):**
   * Level 50 + mid-game storyline progression.
   * **Tier-2 Upgrades:** `SS` (Rank 10), `DSG` (Rank 10), +15% all-damage weapon, and approved Level 50 Dailies (`SG` daily tokens & class unlock).
4. **Stage 4 (`RunBracket55`, `RunBracket60`, `RunBracket65` — Levels 55–65):**
   * **Bracket 55:** Level 55, `PYRO` (1x rank-up) + `BB` (Rank 10), `CRYO` (Rank 10 when unlocked), `ARA` (Rank 10), +25% CP/EXP/Gold/Rep boost cape (bank-aware skip check), and approved Dailies (`BK`).
   * **Bracket 60:** Level 60, `DSS` (Rank 10), and first-tier special weapon enhancement.
   * **Bracket 65:** Level 65, preloaded coordinated Glacera & IceWind storyline (`3941–3954`, `5587–5601`) + Glacera Rank 10, `GB` (Rank 10 when seasonal shop is active), and approved Dailies (including the 10-day `LoO` progression chain & Rank 10).
5. **Stage 5 (`RunBracket70` — Level 70):**
   * Level 70 + **Endgame Support & Debuff Core:** `SC` (Rank 10) and `AP` (Rank 10).
   * Special physical/evasion and magical/healer helm enhancements.
6. **Stage 6 (`RunStep3A` — Level 75):**
   * Level 75 + Endgame reagent daily + `Chaorrupter Unlocked` (+50% Chaos / +120% Drakath) + full 13-chapter main storyline.
   * Additional classes (`SHM`, `ES`, `CS` / `FSR` Rank 10; `FSR` is farmed once the account owns `Cryomancer`), mid-tier special weapon enhancements (`Smite`, `Praxis`), and the base special cape enhancement (`Forge (Cape)`).
7. **Stage 7 (`RunStep3B` — Level 80):**
   * Level 80 + daily fragment progression (for Level 100 weapon enhancement).
   * +30% all-damage weapon (`BBoA`, solo-only barrier with sleep mode for completed accounts).
   * **Late-Mid Classes:** `DOT` (Rank 10, **1–4 / 5–7** subgroup splitting), `AF` (Rank 10, pre-entry reagent check + **1–4 / 5–7** subgroup splitting), and `YNR` (Rank 10, synchronized Army-wide Legion vs. Non-Legion route).
8. **Stage 8 (`RunStep3C` — Level 90):**
   * Level 90 + **Endgame Debuff/DPS & AoE:** `VDK` (Rank 10), `SOF` (Rank 10), `Liberta Aeterna` (+75% Elemental), and `KA` (Rank 10, using `CopyHelpedActions` for special boss mechanics).
   * Base `Forge (Cape)` check + full 5-part special cape enhancement chain (`Absolution`, `Vainglory`, `Avarice`, `Penitence`, `Lament`) with safe armor-swap protection during hazard-suit encounters.
9. **Stage 9 (`RunStep4A` — Level 100 Endgame Final):**
   * Level 100 + endgame dailies + **Endgame Classes:** `VHL`, `AM`, `AI` (Non-Insignia route), and optional `King's Echo`, ranked to Rank 10 when acquired.
   * **Endgame Weapon Enhancements & Boost Gear:** `Valiance`, `Ravenous`, and `Elysium` unlocks, `IAF` armor (+40% damage to all major races), `HRS` weapon (+40% all damage), and the final `Awe-scention` chain (`AW`), followed by optional `SellStarterClasses` and `OutFit` / `EquipOutfit` / `PetChoice` sync.

---

## Party Role & Class Compositions

> **Note:** The Army only farms free classes. Popular paid or member classes are used **only if the account already owns them** (never bought).

### 1. Roles Follow the Room

Each account takes the role of its seat **inside its room**, so every room keeps a working team when a map splits the Army.

| Room size | Seat 1 | Seat 2 | Seat 3 | Seat 4 | Seats 5–7 |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **All 7 together** | `Main DPS` | `StoneCrusher` | `ArchPaladin` | `Lord of Order` | `Verus DoomKnight`, `Bard`, `Shaman` |
| **4 players** | `Main DPS` | `StoneCrusher` | `ArchPaladin` | `Lord of Order` | |
| **3 players** | `Main DPS` | `StoneCrusher` | `ArchPaladin` | | |
| **2 players** | `Main DPS` | `ArchPaladin` | | | |

Examples with 7 accounts: one room = 1 DPS · 2 SC · 3 AP · 4 LoO · 5 VDK · 6 Bard · 7 Shaman; split 4+3 = (DPS, SC, AP, LoO) + (DPS, SC, AP); split 3+2+2 = (DPS, SC, AP) + (DPS, AP) + (DPS, AP).

**Farm:** seat 2 of every room stays on support (plus seat 5 when all 7 share a room); everyone else uses AoE farm classes.

### 2. Automatic Class Priority Order

Each role equips the highest-priority Rank 10 class owned (or the highest-priority owned class while leveling). Same-skill variants count as the main class.

| Role | Priority Order *(Highest $\rightarrow$ Lowest)* |
| :--- | :--- |
| `Main DPS` | `Legion Revenant` $\rightarrow$ `Chaos Avenger` $\rightarrow$ `Arcana Invoker` $\rightarrow$ `King's Echo` $\rightarrow$ `Verus DoomKnight` $\rightarrow$ `Guardian` $\rightarrow$ `Chrono ShadowHunter/Slayer` $\rightarrow$ `Void Highlord` $\rightarrow$ `Kathool Adept` $\rightarrow$ `Dragon of Time` $\rightarrow$ `Martial Artist` $\rightarrow$ `Chaos Slayer` $\rightarrow$ `Scion of Flames` $\rightarrow$ `Frost SpiritReaver` $\rightarrow$ `Cryomancer` $\rightarrow$ `Dragonslayer General` $\rightarrow$ `Glacial Berserker` $\rightarrow$ `Dragonslayer` $\rightarrow$ `Ninja` $\rightarrow$ `Rogue` |
| `StoneCrusher` | `StoneCrusher` / `Infinity Titan` $\rightarrow$ `Bard` / `Troubador of Love` $\rightarrow$ `Oracle` $\rightarrow$ `Healer (Rare)` $\rightarrow$ `Healer` |
| `ArchPaladin` | `ArchPaladin` $\rightarrow$ `Paladin Chronomancer` $\rightarrow$ `Healer (Rare)` $\rightarrow$ `Oracle` $\rightarrow$ `Healer` |
| `Lord of Order` | `Lord of Order` $\rightarrow$ `Archfiend` $\rightarrow$ `Arachnomancer` $\rightarrow$ `Shaman` $\rightarrow$ `Mage` $\rightarrow$ `Oracle` |
| `Verus DoomKnight` | `Verus DoomKnight` $\rightarrow$ `Kathool Adept` $\rightarrow$ `Glacial Berserker` $\rightarrow$ `ShadowScythe General` $\rightarrow$ `Evolved Shaman` $\rightarrow$ `Master Ranger` $\rightarrow$ `Oracle` $\rightarrow$ `Healer` |
| `Bard` | `Bard` / `Troubador of Love` $\rightarrow$ `Oracle` $\rightarrow$ `Healer` |
| `Shaman` | `Shaman` $\rightarrow$ `Evolved Shaman` $\rightarrow$ `Healer` |
| **Farm (AoE)** | `Legion Revenant` $\rightarrow$ `ArchMage` $\rightarrow$ `King's Echo` $\rightarrow$ `Arcana Invoker` $\rightarrow$ `Dragon of Time` $\rightarrow$ `Kathool Adept` $\rightarrow$ `Archfiend` $\rightarrow$ `Necrotic Chronomancer` $\rightarrow$ `Abyssal Angel` $\rightarrow$ `Daimon` $\rightarrow$ `Blaze Binder` $\rightarrow$ `Eternal Inversionist` $\rightarrow$ `Firelord Summoner` $\rightarrow$ `Scarlet Sorceress` $\rightarrow$ `ShadowScythe General` $\rightarrow$ `Darkblood StormKing` $\rightarrow$ `Chaos Slayer` $\rightarrow$ `Pyromancer` $\rightarrow$ `Shaman` $\rightarrow$ `Imperial Chunin` $\rightarrow$ `Master Ranger` $\rightarrow$ `Royal BattleMage` $\rightarrow$ `Mage` |
