# AnimalTurret
Make autoturrets target animals in range

The range matches what is in the game code for the turrets for all other targets (30m).

## Commands
  - /noat -- Disable targeting of animals for the turret you're standing next to.
  - /doat -- Enable targeting of animals for the turret you're standing next to.

## Configuration
```json
{
  "Animal targeting enabled by default": true,
  "Animals to exclude": [
    "chicken"
  ],
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```

Note that you can exclude certain animals by the short name, e.g. chicken, bear, boar, stag.
