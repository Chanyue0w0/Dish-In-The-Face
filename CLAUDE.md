# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2D game project called "Dish-In-The-Face" with both day and night gameplay phases. The project uses Unity's Universal Render Pipeline (URP) and includes features like FMOD for audio, Addressables for asset management, and Spine for animations.

## Key Commands

### Unity Project Commands
- **Open Unity Editor**: Open the project in Unity 2022.3 or later
- **Play Mode**: Use Unity Editor's Play button to test the game
- **Build**: File > Build Settings in Unity Editor

### Testing
Run tests through Unity Test Framework in the Unity Editor:
- Window > General > Test Runner

## Architecture Overview

### Core Systems

**Game Phases**:
- Day Game: Map editing and restaurant management (Assets/Scripts/Day Game/)
- Night Game: Main gameplay with combat and guest management (Assets/Scripts/NightGame/)

**Manager Pattern**: 
The project uses singleton managers for core systems:
- `RoundManager`: Controls game rounds and win/lose conditions
- `GuestGroupManager`: Manages guest spawning and behavior
- `FoodsGroupManager`: Handles food items and interactions
- `ChairGroupManager`: Manages seating arrangements
- `HotPointManager`: Tracks player heat/reputation system

**Player System** (Assets/Scripts/Player/):
- `PlayerMovement`: Movement and dash mechanics
- `PlayerInteraction`: Item pickup and interactions
- `PlayerAttackController`: Combat system
- `PlayerStatus`: Health and state management

**Input Handling**: 
Uses Unity's new Input System with `PlayerInputActions.inputactions` configuration

## Project Structure

- **Assets/Scripts/**: Main game logic
  - Demo Script/: Prototype and testing scripts
  - NightGame/: Night phase gameplay (Managers/, UI/, Guests/, Foods/, etc.)
  - Day Game/: Day phase systems (Map Grid/, UI)
  - Player/: Player controller scripts
  - Effect/: Visual effects and particle systems
  - Audio/: FMOD integration and audio management

- **Assets/Prefabs/**: Reusable game objects
  - Demo Prefebs/: Tables, enemies, food items
  - VFX/: Visual effect prefabs
  - Night Game/: Night phase specific prefabs

- **Assets/ArtWork/**: Game sprites and textures
  - Backgrounds/, Furnitures/, Foods/, Guests/, Items/

## External Dependencies

- **FMOD**: Audio system (Assets/Plugins/FMOD/)
- **Spine**: 2D skeletal animation (Assets/Spine/)
- **Addressables**: Asset management system
- **TextMesh Pro**: UI text rendering
- **Cinemachine**: Camera control
- **Input System**: New Unity Input System

## Localization

The game supports multiple languages:
- English (en-US)
- Traditional Chinese (zh-TW)
Localization files are in Assets/Localization/

## Important Notes

- The project uses singleton patterns extensively - check for existing instances before creating managers
- Character encoding: Some scripts contain Chinese comments (Traditional Chinese)
- Unity version: Ensure compatibility with Unity 2022.3 LTS or later
- The project uses URP (Universal Render Pipeline) - shader compatibility is important
- FMOD banks are stored in Assets/FMODBanks/ and StreamingAssets/