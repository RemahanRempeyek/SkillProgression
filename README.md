# Skill Progression

A Stardew Valley SMAPI mod that adds skill-based character progression.

## Features

### Combat Progression

- Additional Max Health based on Combat Level
- Additional Defense based on Combat Level
- Configurable bonuses for Combat Levels 1–10
- Preserves vanilla Combat health bonuses

### Farming Progression

#### Harvest Quality

- Normal → Silver
- Silver → Gold
- Gold → Iridium

#### Harvest Quantity

- Chance to gain +1 additional item from crop harvests

#### Artisan Machine Speed

- Reduces processing time for supported machines based on Farming Level
- Supports selected vanilla and Cornucopia machines
- Incubators are excluded

## Configuration

Skill Progression uses Generic Mod Config Menu (GMCM) for in-game configuration.

### Combat

- Max Health bonus per Combat Level
- Defense bonus per Combat Level

### Farming

- Harvest quality chances for Levels 1–10
- Harvest quantity chance for Levels 1–10
- Artisan machine speed reduction for Levels 1–10

## Compatibility

- Stardew Valley 1.6.15
- SMAPI 4.5.2.5
- Android testing environment

## Languages

- English
- Indonesian

## Development

Main source files:

- ModEntry.cs
- ModConfig.cs
- FarmingQualityPatch.cs
- ArtisanSpeedPatch.cs
- I18n.cs

## Project Status

Skill Progression is currently under active development.
