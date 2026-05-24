# CONSCRIPT

A minimalist, tense survival roguelike about dodging conscription and deserting during the Russia-Ukraine war.

## Overview
**Conscript** puts you in the shoes of Sergei Badmaev, a 20-year-old from Ulan-Ude in the Republic of Buryatia. Living with his parents in a small apartment, he burns his draft summons and flees into the forest to avoid being sent to fight in Ukraine. The game explores the desperate struggle of evading military service in a country actively mobilizing for war. Every choice carries weight as winter closes in and patrols intensify.

## Sergei's Starting Gear (when he climbs out the window)
In the opening scene, when Sergei chooses to flee the military commissariat, he only manages to grab a very limited set of items before disappearing into the night:

- 10,000 ₽
- Winter jacket
- Small backpack
- His phone
- Small folding knife
- Lighter

This extremely constrained starting inventory is a core source of early-game tension and forces difficult trade-offs immediately.

## Dodging
- Hide in the forest
- Lie low in town
- Flee the country

## Deserting
- Flee the war after being conscripted

## Core Stats
- **Suspicion** — How close the authorities are to finding you (0-100%)
- **Health** — Your physical condition
- **Hunger** — How empty your stomach is (high hunger weakens you and forces desperate choices)
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
- Stats: Very low Suspicion, high Health, low Hunger.
- Choices: “Burn the Letter and Run”, “Ignore It For Now”, “Call Family”, etc.

**Deep Forest Camp (Mid-Game)**  
- Central image: Moss-covered lean-to or hollow stump in heavy snow.
- Stats: Medium Suspicion, declining Health, Exposure meter visible.
- Choices: “Gather Firewood”, “Improve Camouflage”, “Check Traps”, “Move Deeper”.

**On the Run (High Tension)**  
- Central image: You running at night with distant patrol flashlights behind you.
- Stats: High Suspicion and Exposure, low Health, high Hunger.
- Choices: “Push Deeper”, “Create False Trail”, “Hide in Stump”, etc.

**Bug Harvest Mini-Game**  
- Central image: You inside the stump pulling grubs from roots with a small cooking pot.
- Temporary choices: “Boil Them”, “Roast Over Fire”, “Collect More Bugs”.

## Goal
Survive as long as possible while avoiding capture.  
Possible endings include reaching the border, becoming a hidden “ghost” in the forest, or being caught and conscripted.

## Tone
Oppressive, realistic, and claustrophobic. The game does not glorify war — it portrays the quiet fear, moral weight, and harsh realities of trying to dodge or desert military service in modern Russia.

## Resources for real conscripts

If you or someone you know is facing conscription, these organizations provide confidential legal help, hotlines, and practical support:

- **[OVD-Info](https://ovdinfo.org)** — Legal help, hotlines, and mobilization tracking
- **[Idite Lesom](https://iditelesom.org)** ("Go to the Forest") — Project specifically helping people avoid mobilization

## Development

**Tech stack:** .NET 10 + Raylib-cs (inspired by the Starflight reimplementation in `~/repo/starflt`).

**Run the prototype:**

```bash
cd Conscript
dotnet run
```

**Current state:** The prototype now begins with a tense opening scene in the family apartment (Day 0, Evening, Early Autumn) where Sergei must decide how to react to the military knocking. Choosing to flee leads to the current Deep Forest survival screen (with the limited starting gear listed above). The UI is significantly polished, cinematic, and high-tension.

**Layout & Polish:**
- Clean dark header bar with strong "CONSCRIPT" title (left), Day/Time and current Location (center), and Season with icon (right).
- Fixed-width left sidebar (Status panel) containing War Intensity + clean, well-spaced stats with elegant thin colored progress bars for Suspicion / Health / Hunger / Exposure, plus Money (shown in ₽) and current Status.
- Large central scene area with richer atmospheric placeholder art (different art for the opening apartment scene vs. the forest).
- Large central scene area with **much richer atmospheric placeholder art**:
  - Layered forest (far → mid → near trees)
  - Detailed snow-covered lean-to shelter
  - Small walking figure with backpack
  - Falling snow, faint cold moonlight, strong cinematic vignette
  - Generous breathing room around the art
  - Narrative cards elegantly placed inside the image area
- Bottom action bar with four substantial, high-visual-weight buttons and refined control hint

**Interaction:** ← → / A D to highlight, ENTER to commit (or click with the mouse). Stats update live with every choice.

This is now a much more professional, immersive, and oppressive-feeling prototype while remaining 100% placeholder (no external art yet). Ready for real background images when you are.

Future work will add:
- Real background PNGs (we'll generate them together soon)
- Additional locations (Day 1 apartment, deeper forest variants, "on the run", bug harvest, etc.)
- A custom distressed or period-appropriate TTF font
- Day cycle, random events, hidden rolls, inventory, and proper game systems
- Win / lose / multiple ending states
