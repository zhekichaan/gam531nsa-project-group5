# 🎮 Final Project -- OpenTK 3D Horror Game

**GAM531 -- Seneca College**

This project is a **3D first-person horror experience** built using
**OpenTK**, featuring a dynamic monster AI, flashlight mechanics,
terrain generation, skybox rendering, audio systems, and interactive
gameplay elements such as collectible batteries.\
The goal is to escape by findind the car, explore the dark forest, manage battery power,
interact with objects, and avoid the roaming monster.

------------------------------------------------------------------------

## 🚀 Features

### 🔦 Flashlight System

-   Toggleable flashlight (F key)
-   Dynamic lighting using shaders
-   Battery power drains over time
-   Collectible batteries throughout the environment

### 👹 Monster AI

-   Patrol → Detect → Chase state machine
-   Flashlight increases detection radius
-   Monster reacts to sound triggers
-   Game ends when the player is caught

### 🌲 World Generation

-   Procedural terrain
-   PSX-style trees, bushes, and props
-   Invisible walls prevent leaving the map
-   Night-sky HDRI skybox

### 🎧 Audio System

-   Footsteps, monster growls, ambient noise
-   Monster audio volume scales by proximity
-   OpenAL-based audio backend

### 📦 Project Structure

    FinalProject/
    │
    ├── Assets/
    │   ├── Audio/
    │   ├── Models/
    │   ├── Shaders/
    │   └── Textures/
    │
    ├── Backends/
    │   ├── ImguiImplOpenGL3.cs
    │   └── ImguiImplOpenTK4.cs
    │
    ├── Common/
    │   ├── AudioComponent.cs
    │   ├── BoundingBox.cs
    │   ├── Camera.cs
    │   ├── Mesh.cs
    │   ├── MonsterAI.cs
    │   ├── Shader.cs
    │   ├── Texture.cs
    │   └── WorldObject.cs
    │
    ├── Helpers/
    │   ├── CollectableBattery.cs
    │   ├── Flashlight.cs
    │   └── WorldGenerator.cs
    │
    ├── Game.cs
    └── Program.cs

------------------------------------------------------------------------

## 🛠️ Required NuGet Packages

  Package          Purpose
  ---------------  -------------------------
  ImGui.NET        Debug UI
  AssimpNet        Mesh loading
  StbImageSharp    Texture loading
  OpenTK           Rendering, Input, Audio

------------------------------------------------------------------------

## 🎮 Controls

  Key           Action
  ------------- -----------------------
  **W A S D**   Move
  **Mouse**     Look around
  **F**         Toggle Flashlight
  **E**         Interact
  **ESC**       Release cursor / menu

------------------------------------------------------------------------

## 🕹️ Gameplay Loop

1.  Explore the forest using your flashlight.\
2.  Manage limited battery power.\
3.  Collect batteries to restore energy.\
4.  Avoid the monster --- if it catches you, the game ends.

------------------------------------------------------------------------

## 🖼️ Credits

### 3D Models

-   Car Model: https://ggbot.itch.io/psx-style-cars\
-   Flashlight: https://elbolilloduro.itch.io/exploration-objects\
-   Trees + Bushes: https://elegantcrow.itch.io/psx-retro-style-tree-pack\
-   Monster: https://retro-spud.itch.io/psx-elk-demon-npc-monster\
-   Skybox: https://ambientcg.com/view?id=NightSkyHDRI014

### Audio

-   Monster SFX: https://mixkit.co/free-sound-effects/monster/

### Members

-   Yevhen Chernytskyi
-   Gabriel Khan-Figueroa
-   Kencho Lodhen
-   Aaron Ngo
-   Preet Bhagyesh Patel
-   Pouya Rad

------------------------------------------------------------------------


