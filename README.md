# Idle Battle & Gacha Core Skeleton

[![English](https://img.shields.io/badge/lang-English-blue.svg)](https://github.com/cleanlii/unity-idlegame-prototype/blob/master/README.md)
[![SC](https://img.shields.io/badge/lang-中文-red.svg)](https://github.com/cleanlii/unity-idlegame-prototype/blob/master/README.zh-sc.md)

A Unity-based idle RPG prototype where players automatically grow through different routes, manage characters, and progress through a loop idle systems.

## Core Concept

### Easy Idle
The game runs automatically, allowing players to enjoy steady progress without the stress of complex controls or high-intensity challenges.

### Spire-like Mechanics

Inspired by Slay the Spire and Darkest Dungeon, this demo features a branching path system where players freely choose from three different routes and progress automatically:

- **Battle Route**: Engage in automated 1v1 combat to gain experience and coins
- **Economy Route**: Passively generate coins over time
- **Experience Route**: Passively generate experience points over time

### Asynchronous Gameplay

- **Offline Rewards**: Continue to accumulate experience and coins even while offline
- **(TODO) Quick Sessions**: Make use of short play sessions to complete time-limited events and earn bonus rewards
- **(TODO) Async. Multiplayer**: Rankings, asynchronous friend battles, and more.

---

## Project Structure

```
Assets/Scripts/
├── Core/                          # Core game systems
│   ├── GameManager.cs            # Main game coordinator and entry point
│   ├── BattleManager.cs          # Automated battle system and combat logic
│   ├── PlayerController.cs      # Player movement between routes
│   └── Systems/                  # Specialized game systems
│       ├── CharacterSystem.cs   # Character management and progression
│       ├── SpireSystem.cs       # Route management and switching logic
│       ├── SaveSystem.cs        # Data persistence and encryption
│       ├── IdleLogSystem.cs     # Real-time action logging system
│       ├── EcoSystem.cs         # (Placeholder for economic features)
│       └── GachaSystem.cs       # (Placeholder for gacha mechanics)
├── Data/                         # ScriptableObject configurations
│   ├── PlayerData.cs            # Player progression and statistics
│   ├── CharacterConfig.cs       # Character templates and growth curves
│   ├── CharacterData.cs         # Runtime character instances
│   ├── CharacterDatabase.cs     # Character collection and gacha weights
│   ├── EnemyConfig.cs           # Enemy templates
│   ├── EnemyData.cs             # Runtime enemy instances
│   ├── RouteConfig.cs           # Route reward configurations
│   └── OfflineRewardConfig.cs   # Offline progression settings
├── Utils/                        # Utility and helper classes
│   ├── ServiceLocator.cs        # Dependency injection pattern
│   ├── JsonUtils.cs             # Encrypted JSON serialization
│   ├── IdleGameConst.cs         # Game constants and file paths
│   └── LogEntryUI.cs            # UI components for log entries
└── UIManager.cs                  # UI coordination and animation
```

---

## System Overview

### **Battle Manager**
The `BattleManager` handles all combat logic with automated damage calculation and turn-based mechanics:

- **Automated Combat**: Characters and enemies attack at intervals based on attack speed
- **Damage Calculation**: Includes random variance, critical hits, and special abilities
- **Battle Refresh System**: When players die, automatically revive and restart battles
- **Combat Statistics**: Tracks damage dealt/taken, battle count, win streaks
- **Random Enemy**: Enemies are generated from highly customizable, template-based datasets.
- **Endless Dungeon**: Each enemy can be defined with unique stats, abilities, and even behaviors, ensuring endless replayability and theoretically infinite battles.

### **Idle Rewards System**
The ``GameManager`` and ``SpireSystem`` calculates both online rewards and offline rewards based on elapsed time and selected route:

**Reward Types:**
- **Battle Route**: XP + coins + ~~(TODO) special reward like equipment or buff~~
- **Economy Route**: High coin generation + no XP
- **Experience Route**: High XP generation + no coins

**Offline Reward Logic:**
- **Route-Based Rewards**: Different routes provide different offline benefits
- **Time Validation**: Anti-cheat measures prevent time manipulation (local only for now)
- **Efficiency Cap**: Offline rewards scale down over time and stop at a maximum limit.
- **Customizable**: The formulas are fully independent, allowing flexible design and balance strategies.

### **Character & Gacha Mechanic**
Players can obtain and train multiple characters through a gacha system, freely switching between them to optimize different strategies or collecting their favorite character.

**Character System:**
- **CharacterConfig**: Template defining base stats and growth curves
- **CharacterDB**: Collection manager with gacha probability weights
- **CharacterData**: Runtime instances with levels, experience, and battle stats
- **Rarity Flag**: Common (60% for gacha), Rare (25%), Epic (12%), Legendary (3%)

**Gacha System:**
- Weighted random selection based on rarity
- Character progression independent of gacha acquisition
- Item-based configuration for easy balancing

### **Upgrade System**
Multiple progression paths using a unified currency system:

**Progression Types:**
1. **Battle XP**: Gained from battle victories
2. **Purchased Experience**: Buy XP directly with coins
3. **Character Switching**: Manage multiple characters with independent progression

**Formula Examples:**
- **HP Growth**: `baseHP + (level-1) × hpGrowthPerLevel × rarityMultiplier`
- **XP Requirements**: `baseExp × level^growthFactor`
- **Damage Calculation**: `(baseAttack + levelBonus) × randomFactor × critMultiplier`

### **Currency System**
Single currency (coins) used for all transactions:

**Currency Sources:**
- Battle victories
- Economy route
- Offline rewards

**Currency Spending:**
- Purchase XP
- Gacha
- ~~(TODO: Equipment, upgrades, etc.)~~

### **Logging System**
Comprehensive logging system for all player actions and game events:

**Log Categories:**
- **Battle**: Damage dealt/taken, battle results, enemy encounters/spawns
- **Character**: Level ups, character gacha/~~switches~~, stat changes
- **Reward**: Coin gains/losses, XP purchases, offline rewards
- **System**: Route switches, save/load events

---

## Gameplay Loop

### **Active Gameplay (Online)**
```
Player Chooses Route → System Executes Route Logic → Player Gains Rewards
    ↓
Battle Route: Auto-combat → EXP + Coins
Economy Route: Time-based → Coins
Experience Route: Time-based → EXP
    ↓
Player Uses Coins → Buy EXP or Gacha → Character Progression
```

### **Passive Gameplay (Offline)**
```
Game Calculates Offline Time → Apply Route-Based Multipliers → Generate Rewards
    ↓
Battle: Simulate combat sessions for EXP/Coins
Economy: Generate coins based on hourly rate
Experience: Generate EXP based on hourly rate
    ↓
Apply Rewards on Return → Update Character Stats → Continue Progression
```

---

## Dev Notes

### **External Plugins**
- **DOTween**: Basic UI and gameplay animations
- **TextMeshPro**: Flexible UI text rendering
- **ScriptableObject**: Data-driven configuration and easy in-editor adjustments

### **Utilities**
- **JsonUtils**: Serialized and encrypted JSON file handling for safe saving/loading
- **ServiceLocator**: Centralized service (Manager/Controller/System) registration, retrieval, and lifecycle management
- **IdleGameConst**: Unified access point for constant parameters and global settings
- **LubanConfig**: External spreadsheet-to-data pipeline for scalable content editing

### **Testing Editor Tools**
TBD

### **Future Plan**
#### Structure
- **Spreadsheet-driven Data**: Designers can manage and balance content and supports backend service as well.
- **Resource Management**: Hotfix support for scalable live ops and efficient patching
- **Analytics Integration**: Backend data analytics tools for statistics tracking

#### Gameplay
- **Character Switch**: Allow players to choose the character whichever they like
- **Equipment System**: Gear progression for role-playing
- **Inventory System**: Unified system to handle live status preview (more detials), buffs, gears, and character roster
- **More Spire Features**: Map preview, boss milestones and more trackable progression
- ......

