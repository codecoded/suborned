#!/bin/bash
set -e
S=/Users/sky/Projects/suborned/out/scratch/latch/075oldbuild/stage
O=$HOME/Projects/the-suborned/game/Assets
rm -rf "$S"; mkdir -p "$S"; cd "$S"
mkdir -p code/Editor/OldTown code/Runtime/OldTown
cp -pR $O/TheSuborned/Editor/DecoKit $O/TheSuborned/Editor/DecoKit.meta code/Editor/OldTown/
cp -pR $O/TheSuborned/Scripts/Sky $O/TheSuborned/Scripts/Sky.meta $O/TheSuborned/Scripts/Robots $O/TheSuborned/Scripts/Robots.meta $O/TheSuborned/Scripts/Crowd $O/TheSuborned/Scripts/Crowd.meta code/Runtime/OldTown/
cp -p $O/TheSuborned/Scripts/DKCutaway.cs $O/TheSuborned/Scripts/DKCutaway.cs.meta $O/TheSuborned/Scripts/FirstPersonController.cs $O/TheSuborned/Scripts/FirstPersonController.cs.meta code/Runtime/OldTown/
mkdir -p art/OldTownArt/Data art/OldTownArt/Settings
cp -cpR $O/TheSuborned/Art/DecoKit $O/TheSuborned/Art/DecoKit.meta art/OldTownArt/
cp -cpR $O/TheSuborned/Art/RetroFutureMobility $O/TheSuborned/Art/RetroFutureMobility.meta art/OldTownArt/
cp -p $O/TheSuborned/Data/RobotGlyphs.txt $O/TheSuborned/Data/RobotGlyphs.txt.meta art/OldTownArt/Data/
cp -p $O/Settings/PC_RPAsset.asset $O/Settings/PC_RPAsset.asset.meta $O/Settings/PC_Renderer.asset $O/Settings/PC_Renderer.asset.meta art/OldTownArt/Settings/
cp -p $O/TheSuborned/Data/Civic/HarbourFog.mat $O/TheSuborned/Data/Civic/HarbourFog.mat.meta $O/TheSuborned/Shaders/Civic/HarbourFog.shader $O/TheSuborned/Shaders/Civic/HarbourFog.shader.meta art/OldTownArt/Settings/
find code -name '*.cs' | wc -l
du -sh art
