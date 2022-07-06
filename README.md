ShaderGraph built-in importer with custom shading and bug fixes. Target version is 2019.4 (VRChat). Shader Graph supports built-in pipeline natively in 2021+, this package only aims to fix bugs, make it usable in VRChat with both lit and unlit modes and provide some improvements. Experimental, might have breaking changes in the future.


### How to use:
- Download and install Unity 2021 LTS and import Shader Graph (12.1.6) from the package manager
- Create a Shader using Shader Graph with built-in target
- Save Asset, Show In Project and Copy Shader
- Install [Git](https://git-scm.com/) if you haven't already and restart Unity
- Import this package using git URL `https://github.com/z3y/ShaderGraph-BuiltIn.git` with the Package Manager in Unity 2019.4
- Create a new shader graph importer `Create > Shader > Shader Graph Importer`
- Paste and Import


### Features:
- Bakery features
- Bicubic lightmap
- Alpha to coverage
- LTCGI
- Audio Link

Notable bug fixes: GPU Instancing, Single Pass, Single Pass Instanced

Bugs:
- Unity has a shader graph variant limit but its too low. Increase it in `Preferences > Shader Graph > Shader Variant Limit`
- Stage keywords dont work on Quest, use only All stages


Shader Graph and Built-In shader code owned by Unity, Licensed under Unity Companion License:
https://unity3d.com/legal/licenses/Unity_Companion_License

For other files licenses are included in folders or files

#
If you want to ask something add me z3y#3214 or join https://discord.gg/bw46tKgRFT

Nodes: https://github.com/z3y/ShaderGraph-Nodes
