Army Farmer Joe do All

Based on @LoneWolf13 core from UltrasLW, Ty Sir.

Write with AI: GPT-6 Luna, Opus 5.5, Gemini 4 Argon, Gemini 4 Hydrogen.

# Army FarmerJoe DoAll

A multi-account cooperative progression framework (`Scripts/Army/ArmyFarmerJoeDoAll.cs` + `Scripts/Army/CoreArmyFJ.cs`) built for synchronized groups of **4 to 7 accounts** (**Players 1–4 required; Players 5–7 optional**). Every account progresses together from **Level 1 to Level 100 Endgame**—clearing level brackets, storylines, reputation tiers, class rank-ups, enhancement unlocks, and endgame gear—while assisting teammates automatically and staying aligned at every barrier.

---

## Original FarmerJoe vs. Army FarmerJoe DoAll

| Feature | Original FarmerJoe | Army FarmerJoe DoAll |
| :--- | :--- | :--- |
| **Execution Model** | Single-account solo progression | Synchronized **4–7 account** cooperative progression with per-step barriers |
| **Party Roles** | One generic Solo/Farm rotation per account | Dedicated **Boss Comp** (`Main DPS`, `Support`, `Debuffer`, `Buff/Amp`, `Flex`) & **Farm Comp** |
| **Teammate Assistance** | None | Automatic follow & assist, fair first-hit drop credit, and backup Support hand-off during rank-ups |
| **Room Management** | Standard room joins | Mandatory private room (`00000` guard), automatic **1–4 / 5–7** and **3-player** subgroup splitting, plus sleep mode on level-locked rooms |
| **Enhancements & Readiness** | Re-applied repeatedly across steps | **Pre-combat Ready Check** + **on-demand class-kit enhancements** (no wasted loops while skipping completed steps) |
| **Gear Protection** | Basic best-gear equip | Configurable `BoostGearPolicy`, per-player gear locks, and safe non-combat room swaps |
| **Endgame Scope** | Early-to-mid progression focus | Full **Lv 1–100** Free-to-Play progression including late-game classes, special weapon/helm/cape enhancements, and endgame boost gear (excluding weekly raid encounters) |

---

## Core Architecture Highlights

* **Synchronized 4–7 Account Barriers:** No account advances to the next bracket until all teammates finish the current objective.
* **Ready Check & On-Demand Class Kits:** Before combat begins, every account verifies its role class, applies that class's predetermined enhancement kit when needed, and waits at the barrier so the party enters fights together.
* **Smart Subgroup Splitting & Level-Lock Sleep:** Automatically splits 5–7 player groups on player-capped maps (**3-player** or **1–4 / 5–7** subgroups) and puts helpers into sleep mode when a teammate is inside a higher-level locked room.
* **Safe Equipment Swapping & Inventory/Bank Awareness:** Steps into a safe non-combat room before swapping classes, turning in quests, buying items, or enhancing gear, while checking both inventory and bank for reagents and boost gear.

---

## Complete Progression Stages (Level 1–100, 100% Free-to-Play)

1. **Stage 1 (`RunLevel1To30` — Levels 1–30):**
   * Tutorial completion and starter gear setup.
   * Early Support (`ORA`, Level 10, Rank 10), Evasion starter (`ROG`, Rank 10), Level 25 + prerequisite quests + `NIN` (Rank 10), and AoE starter (`MAG`, Rank 10) with initial party role assignment.
2. **Stage 2A (`RunBracket30` — Level 30):**
   * Approved Dailies & Weeklies + optional `GetBoosts`.
   * Level 30 + **Tier-1 Class Lineup** (reputations with 3-player subgroup splitting, prerequisites, and Rank 10): `MR`, `DS`, `IC`, `SHM`, and `ES`.
   * Base special weapon enhancement + static +25% CP/EXP/Gold/Rep boost gear sync + base special helm enhancement.
3. **Stage 2B (`RunBracket50` — Level 50):**
   * Level 50 + mid-game storyline progression.
   * **Tier-2 Upgrades:** `SS` (Rank 10), `DSG` (Rank 10), +15% all-damage weapon, and approved Level 50 Dailies (`SG` daily tokens & class unlock).
4. **Stage 2C (`RunBracket55`, `RunBracket60`, `RunBracket65` — Levels 55–65):**
   * **Bracket 55:** Level 55, `PYRO` (1x rank-up) + `BB` (Rank 10), `CRYO` (Rank 10 when unlocked), `ARA` (Rank 10), +25% CP/EXP/Gold/Rep boost cape, and approved Dailies (`BK`).
   * **Bracket 60:** Level 60, `DSS` (Rank 10), and first-tier special weapon enhancement.
   * **Bracket 65:** Level 65, `GB` (Rank 10 when seasonal shop is active), and approved Dailies (including the 10-day `LoO` progression chain & Rank 10).
5. **Stage 2D (`RunBracket70` — Level 70):**
   * Level 70 + **Endgame Support & Debuff Core:** `SC` (Rank 10) and `AP` (Rank 10).
   * Special physical/evasion and magical/healer helm enhancements.
6. **Stage 3A (`RunStep3A` — Level 75):**
   * Level 75 + Endgame reagent daily + full 13-chapter main storyline.
   * Additional classes (`SHM`, `ES`, `CS` / `FSR` Rank 10) and mid-tier special weapon enhancements.
7. **Stage 3B (`RunStep3B` — Level 80):**
   * Level 80 + daily fragment progression (for Level 100 weapon enhancement).
   * +30% all-damage weapon (`BBoA`, solo-only barrier with sleep mode for completed accounts).
   * **Late-Mid Classes:** `DOT` (Rank 10, **1–4 / 5–7** subgroup splitting), `AF` (Rank 10, pre-entry reagent check + **1–4 / 5–7** subgroup splitting), and `YNR` (Rank 10, synchronized Army-wide Legion vs. Non-Legion route).
8. **Stage 3C (`RunStep3C` — Level 90):**
   * Level 90 + **Endgame Debuff/DPS:** `VDK` (Rank 10).
   * Full 5-part special cape enhancement chain (with safe armor-swap protection during hazard-suit encounters).
9. **Stage 4A (`RunStep4A` — Level 100 Endgame Final):**
   * Level 100 + endgame dailies + **Endgame Classes:** `VHL`, `AM`, and `AI` (Non-Insignia route), ranked to Rank 10 when acquired.
   * **Endgame Weapon Enhancements & Boost Gear:** `Valiance` + `Ravenous` unlocks, `IAF` armor (+40% damage to all major races), `HRS` weapon (+40% all damage), and the final `Awe-scention` chain (`AW`), followed by optional `SellStarterClasses` and `OutFit` / `EquipOutfit` / `PetChoice` sync.

---

## Party Role & Class Compositions (100% Free-to-Play)

> **Note:** Paid, promotional, and membership-locked classes are excluded (only free base classes and legacy starter variants are used).

### 1. Boss Composition Progression

| Stage | P1 `Main DPS` *(Req.)* | P2 `Support` *(Req.)* | P3 `Debuffer` *(Req.)* | P4 `Buff / Amp` *(Req.)* | P5 `Flex Debuff` *(Opt.)* | P6 `Flex Amp/DPS` *(Opt.)* | P7 `Flex Dodge/DPS` *(Opt.)* |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Stage 1** *(Lv 1–30)* | `NIN` | `ORA` | `ROG` | `MAG` | `ORA` / `HLR` | `ORA` / `HLR` | `NIN` / `ORA` |
| **Stage 2A** *(Lv 30)* | `DS` | `ORA` | `IC` | `SHM` | `ES` | `MR` | `NIN` |
| **Stage 2B** *(Lv 50)* | `DSG` | `ORA` | `IC` | `SHM` | `ES` | `SS` | `NIN` |
| **Stage 2C** *(Lv 55–65)* | `CRYO` / `DSG` | `ORA` | `IC` | `LoO` / `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Stage 2D–3A** *(Lv 70–75)* | `CRYO` / `DSG` | `SC` | `AP` | `LoO` / `ARA` | `GB` / `ES` | `SS` | `DSS` |
| **Stage 3B–3C** *(Lv 80–90)* | `DOT` / `CRYO` | `SC` | `AP` | `LoO` | `VDK` / `GB` | `AF` / `SS` | `YNR` / `DSS` |
| **Stage 4A** *(Lv 100)* | `AI` / `VHL` / `DOT` | `SC` | `AP` | `LoO` | `VDK` / `GB` | `AF` / `SS` | `YNR` / `DSS` |

---

### 2. Automatic Class Priority Order

Each slot automatically equips the highest-priority Rank 10 class owned in inventory (or highest-priority owned class while leveling):

| Mode | Slot / Role | Priority Order *(Highest $\rightarrow$ Lowest)* |
| :--- | :--- | :--- |
| **Boss** | **Player 1** — `Main DPS` *(Req.)* | `KE` $\rightarrow$ `AI` $\rightarrow$ `CAV` $\rightarrow$ `VHL` $\rightarrow$ `VDK` $\rightarrow$ `DOT` $\rightarrow$ `CRYO` $\rightarrow$ `DSG` $\rightarrow$ `GB` $\rightarrow$ `DS` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ROG` |
| **Boss** | **Player 2** — `Support` *(Req.)* | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss** | **Player 3** — `Debuffer` *(Req.)* | `AP` $\rightarrow$ `IC` $\rightarrow$ `ROG (Rare)` $\rightarrow$ `ROG` $\rightarrow$ `NIN` $\rightarrow$ `ORA` |
| **Boss** | **Player 4** — `Buff / Amp` *(Req.)* | `LoO` $\rightarrow$ `ARA` $\rightarrow$ `SHM` $\rightarrow$ `MAG (Rare)` $\rightarrow$ `MAG` $\rightarrow$ `ORA` |
| **Boss** | **Player 5** — `Flex Debuff` *(Opt.)* | `VDK` $\rightarrow$ `GB` $\rightarrow$ `ES` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss** | **Player 6** — `Flex Amp/DPS` *(Opt.)* | `AF` $\rightarrow$ `SS` $\rightarrow$ `MR` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Boss** | **Player 7** — `Flex Dodge/DPS` *(Opt.)* | `YNR` $\rightarrow$ `DSS` $\rightarrow$ `NIN` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm** | **Player 2** — `Primary Farm Support` | `SC` $\rightarrow$ `ORA` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm** | **Player 5** — `Secondary Farm Support` | `LoO` $\rightarrow$ `SC` $\rightarrow$ `AP` $\rightarrow$ `ORA` $\rightarrow$ `ES` $\rightarrow$ `HLR (Rare)` $\rightarrow$ `HLR` |
| **Farm** | **Players 1, 3, 4, 6, 7** — `AoE Farmers` | `AM` $\rightarrow$ `AI` $\rightarrow$ `DOT` $\rightarrow$ `AF` $\rightarrow$ `BB` $\rightarrow$ `SS` $\rightarrow$ `PYRO` $\rightarrow$ `SHM` $\rightarrow$ `IC` $\rightarrow$ `MR` $\rightarrow$ `MAG` |
