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

Shared Private Room Enforcement: Room number Setup granting all players stay in the same room.

Cooperative Helper System: Army users now have a helper function, everyone can help who is stuck behind progress, now making everyone on the same stage ALWAYS

First-Hit Rule & Boss Mechanics: Helpers only attack after the helped users attack, ensuring participation.

Helper Load Balancing & Fail-Safes: Helpers splits evenly between all players, shop/turn-in steps have retry limits to prevent infinite loop

Role-Based Party Composition: Automatically assigns unique party role based on player slot.
Party Class Progression by Player Slot

All accounts unlock every class together, while SetClass() automatically assigns a unique Solo/Boss role to each player slot:

| Player Slot | Party Role | Step 1 (Lv 1–25) | Step 2A (Lv 30 Bracket) | Mid-Game (Lv 50–65) | Late / Endgame (Lv 70–100) |
| :---: | --- | --- | --- | --- | --- |
| Player 1 | Main Solo DPS | `Rogue` -> `Ninja` | `Dragonslayer` | `Dragonslayer General` -> `Glacial Berserker` -> `Cryomancer` | `Dragon of Time` -> `Void Highlord` -> `Chaos Avenger` -> `King's Echo` |
| Player 2 | Main Healer / Support | `Oracle` | `Oracle` | `StoneCrusher` | `ArchPaladin` -> `Lord of Order` |
| Player 3 | Boss Hit-Chance Debuffer | `Rogue` | `Imperial Chunin` | `Imperial Chunin` -> `StoneCrusher` | `ArchPaladin` |
| Player 4 | Magic Amp / Anti-Evade | `Mage` | `Shaman` | `Shaman` -> `Arachnomancer` -> `StoneCrusher` | `Archfiend` |
| Player 5 | Sub-Healer / Crit Debuff | `Oracle` | `Evolved Shaman` | `Evolved Shaman` -> `Glacial Berserker` | `Dragon of Time` |
| Player 6 | Ranged DPS / Support | `Oracle` | `Master Ranger` | `Master Ranger` -> `StoneCrusher` | `ArchPaladin` -> `Void Highlord` |
| Player 7 | Dodge DPS / Support | `Ninja` | `Ninja` | `DragonSoul Shinobi` | `Yami No Ronin` |

What it does effectivly RN:

It does the same as original Farmer Joe does with additions till level 30 bracket.
