# SBG-AutoSpectator
#### _A BepInEx5 mod to autimatically spectate the player that is closest to the hole and make sure the camera is point at the hole, and behind the player. Can be paired with SBG-UnattendedServer._

# TO-DO
#### Add a keybind to toggle this functionality
#### 

## Installation

- [Install BepInEx 5](https://github.com/BepInEx/BepInEx/releases) 
- Download and drop the SBG-LiveLeaderboard.dll file in the \BepInEx\plugins folder of your Super Battle Golf Installation
- After opening the game for the first time, you can adjust the frequency the camera will swap to a new leader (assuming there is one) in \BepInEx\config\com.kingcox22.sbg.autospectator.cfg

## Building for source
*set the env variable ``SUPER_BATTLE_GOLF_PATH`` to your Super Battle Golf installation directory.*
```sh
dotnet build
```







