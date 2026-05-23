# CONSCRIPT

A minimalist, tense survival roguelike about dodging conscription and deserting during the Russia-Ukraine war.

## Overview
**Conscript** puts you in the shoes of a 28-year-old Russian man who burns his draft summons and flees into the forest to avoid being sent to fight in Ukraine. The game explores the desperate struggle of evading military service in a country actively mobilizing for war. Every choice carries weight as winter closes in and patrols intensify.

## Core Stats
- **Suspicion** — How close the authorities are to finding you (0-100%)
- **Health** — Your physical condition
- **Morale** — Your mental state (low morale can force risky or passive decisions)
- **Exposure** — How visible your shelter and tracks are to search parties
- **Supplies** — Food, firewood, and essential resources
- **Season** — Early Winter (resources decay faster and survival becomes much harder)

## Gameplay & Choice Mechanics
The game is played in **daily turns**. You receive **3 Action Points** each day to spend on activities such as:

- Foraging for food and firewood
- Building or improving camouflaged shelters (lean-tos, caves, hollow stumps)
- Setting and checking animal traps
- Cooking (including creative options like bugs or foraged items)
- Moving to a new location
- Camouflaging your trail or masking your scent
- Resting or listening to the radio for news

**Every action involves meaningful trade-offs.**  
Risky choices (lighting fires, staying in one place too long, or moving during the day) can raise **Suspicion** and **Exposure**. The game uses simple hidden rolls modified by your stats, preparations, and current conditions. Poor decisions can quickly lead to patrols closing in, illness, or freezing to death.

At the end of each day, a random event occurs — ranging from harsh weather and supply discoveries to military search sweeps.

## Example Screens

The game uses a clean, atmospheric UI with a large central image and persistent stat panel on the left.

**Day 1 - City Apartment (Starting Screen)**  
- Central image: You sitting at a table staring at the draft summons envelope.
- Stats: Very low Suspicion, high Health & Morale.
- Choices: “Burn the Letter and Run”, “Ignore It For Now”, “Call Family”, etc.

**Deep Forest Camp (Mid-Game)**  
- Central image: Moss-covered lean-to or hollow stump in heavy snow.
- Stats: Medium Suspicion, declining Health, Exposure meter visible.
- Choices: “Gather Firewood”, “Improve Camouflage”, “Check Traps”, “Move Deeper”.

**On the Run (High Tension)**  
- Central image: You running at night with distant patrol flashlights behind you.
- Stats: High Suspicion and Exposure, low Health and Morale.
- Choices: “Push Deeper”, “Create False Trail”, “Hide in Stump”, etc.

**Bug Harvest Mini-Game**  
- Central image: You inside the stump pulling grubs from roots with a small cooking pot.
- Temporary choices: “Boil Them”, “Roast Over Fire”, “Collect More Bugs”.

## Goal
Survive as long as possible while avoiding capture.  
Possible endings include reaching the border, becoming a hidden “ghost” in the forest, or being caught and conscripted.

## Tone
Oppressive, realistic, and claustrophobic. The game does not glorify war — it portrays the quiet fear, moral weight, and harsh realities of trying to dodge or desert military service in modern Russia.

## Development

**Tech stack:** .NET 10 + Raylib-cs (inspired by the Starflight reimplementation in `~/repo/starflt`).

**Run the prototype:**

```bash
cd Conscript
dotnet run
```

**Current state:** A single interactive screen — the "Day 1 City Apartment" starting scene.

- Left sidebar: persistent stats (Suspicion / Health / Morale / Exposure / Provisions) with live-updating bars.
- Central framed "image": procedurally drawn lonely apartment with table, seated figure, window, and the draft summons envelope.
- Bottom choices: 4 starting decisions. Use ↑/↓ or W/S to highlight, ENTER or 1-4 to act.
- Every choice immediately mutates the stats so you can feel the trade-offs and rising tension.
- ESC/Q to quit.

This is the foundation. Future work will add:
- Additional locations (deep forest, on the run, bug harvest)
- Proper scene art (PNG textures)
- A custom font
- Day/end-of-day random events
- Win/lose conditions and multiple endings
