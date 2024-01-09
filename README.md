# AnimalTurret for Rust
Make autoturrets target animals in range

The range matches what is in the game code for the turrets for all other targets (30m).

## Commands
  - /noat -- Disable targeting of animals for the turret you're standing next to.
  - /doat -- Enable targeting of animals for the turret you're standing next to.

## Configuration
```json
{
  "Animal targeting enabled by default": true,
  "Animal targeting by NPC AutoTurrets": false,
  "Animals to exclude": [
    "chicken"
  ],
  "Honor Friends/Clans/Teams for commands": false,
  "Use Friends plugins for commands": false,
  "Use Clans plugins for commands": false,
  "Use Rust teams for commands": false,
  "Update period for turrets": 5f,
  "debug": true,
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 7
  }
}
```

Note that you can exclude certain animals by the short name, e.g. chicken, bear, boar, stag.
