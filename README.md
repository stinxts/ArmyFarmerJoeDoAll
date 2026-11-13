WIP (WORK IN PROGRESS)

This is the Skeleton of the Army Farmer Joe Project.

Instalattion

CoreArmyFJ.cs goes into Scripts
ArmyFarmerJoeDoAll.cs and ArmyFarmerJoeStep1.cs goes into Scripts/Other/ArmyFarmerJoe

What the Script Does:

ArmyFarmerJoeDoAll is the WIP version with all the FarmerJoeDoAll things inside waiting to be reformated for the army version.

ArmyFarmerJoeStep1.cs is the tester, it makes a barrier and calls out ArmyFarmerJoeDoAll.cs until what i've already implemented then stops the bot.

Edit Options Maintened, options added to 7 players army, 2 req, 4 recommended, 7 optional.

CoreArmy based on CoreLoneWolf.cs from UltrasLW Thankyou @LoneWolf13

Everything used AI so here's the AI's i've used so far (f tokens):
GPT-6 LUNA, Claude Opus 5.5, Antigravity Gemini 4 Argon.

What has been implemented:

Cooperative Helper System: Accounts that finish a task early (or already meet the requirements) automatically follow and assist pending accounts instead of skipping ahead.

First-Hit Rule & Boss Mechanics: Helpers use proper class skill rotations, wait for the assisted player to land the first hit before attacking, and respect boss shield phases (Escherion's Staff / Vath's Stalagbite).

Helper Load Balancing & Fail-Safes: Helpers split evenly across multiple pending players, and shop/turn-in steps have retry limits to prevent infinite loops.

Auto Class Setup (SetClass): Automatically resolves, ranks up to 10, and equips the best Solo, Farm, and Dodge classes after every major acquisition (removed obsolete class banking).

Step 1 (Levels 1–25 / Beginner Setup): Synchronized Tutorial, starter gear, Level 10, Rogue (Rank 10), Level 25, Mazumi questline (coordinated monster-by-monster and boss-by-boss), Ninja (Rank 10), and Mage (Rank 10).

Step 2A (Level 30 Bracket): Synchronized Elders' Blood daily, Level 30 + SmartEnhance, Master Ranger (Sandsea Rep 10 + Rank 10), Dragonslayer (Galanoth quests 165–168 one-by-one + Rank 10), Blade of Awe unlock quests (2933–2937 one-by-one + Rep 10 + Awethur's Accoutrements), and Forge Helm Enhancement (Blacksmithing Rep 4 + Quest 8828 coordinated boss-by-boss).
