# Happy Voxels

A small multiplayer sandbox built on a GPU mesh voxelizer (Unity, C#, HLSL compute and geometry shaders, Photon Fusion 2).
Statues are voxelized on the GPU when a session starts. Players carve voxels away or add them back. Every edit syncs to the
other players, and a late joiner gets the edits of everyone still in the session.

Video: https://www.youtube.com/watch?v=GVTXc7cteQI

## How it works
- **GPU octree.** A complete linear octree, stored breadth-first in one buffer and built with one thread per node.
- **Voxelization.** One thread per triangle walks the octree without a stack, skipping whole subtrees whose boxes miss the
  triangle. Overlap is a 13-axis separating-axis triangle–box test.
- **Rendering.** Filled voxels are compacted with an append buffer and drawn as points; a geometry shader expands each point
  into a cube.
- **Networking.** Edits travel as small commands (model, voxel, radius, add/remove). Each player replays their own edit history
  to newcomers, so a late joiner sees the edits of everyone still in the session.

## Run it
Windows 10/11, a DX11 GPU with compute and geometry shaders, and an internet connection (Photon Cloud).
Open the project in Unity 2022.3.62f2, open `Assets/_HappyVoxels/Scenes/Start.unity`, press Play and click Start.
Git must be installed (UniTask, NaughtyAttributes and ParrelSync are Git packages). The Photon App ID is blank in the source:
paste your own Fusion App ID into `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` to run it in the editor.

Controls: WASD move, mouse look, left click removes voxels, right click adds them back, Esc quits.

## Third-party
Photon Fusion 2, DOTween, TextMesh Pro, ParrelSync, UniTask, NaughtyAttributes and Unity's URP packages.
Model: Bronze Shark Statue by Tina, Poly Haven (CC0). Avatar: Photon's capsule from the Fusion Intro Sample.
The 2024 shaders were based on Felipe Lira's UniversalShaderExamples (CustomLit.shader, CustomShading.hlsl); this version
replaces that code with URP's UniversalFragmentPBR.
