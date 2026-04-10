# FFX-2-TurnBased
A Fahrenheit based mod for FFX-2 that changes the battle system to be turn-based in a similar style to FFX.

### Showcase Video - vs Chac

https://www.youtube.com/watch?v=j4Zda7A3lCo

# Installation

This mod requires some data files to be patched to function correctly.

1. Download the [VBF Browser](https://www.nexusmods.com/finalfantasy12/mods/3) and extract the `ffx_ps2` folder from `YOUR_STEAM_LIBRARY\FINAL FANTASY FFX&FFX-2 HD Remaster\data\FFX2_Data.vbf` to a location of your choosing.

2. From `ffx_ps2\ffx2\master\jppc\battle\kernel` you will need to copy `rom.bin` into `Required File Patches\PlaceYourFilesHere`.

3. From `ffx_ps2\ffx2\master\new_uspc\battle\kernel` you will need to copy the following files into `Required File Patches\PlaceYourFilesHere`:
* a_ability.bin
* command.bin
* item.bin
* monmagic.bin

4. Now run `PatchFiles.bat` which will apply the patches to your files. This will create a directory in the `Output` folder:

`efl\x2\ffx_ps2...`

Copy this `efl` folder into the `ffx2TurnBased` folder. (Alongside the DLL, mod manifest etc.)

# Credits
[Fahrenheit](https://github.com/fahrenheit-crew/fahrenheit) - The Fahrenheit crew
[VBF Browser](https://www.nexusmods.com/finalfantasy12/mods/3) - Vaan, Topher and FFGriever
[xDelta3](https://github.com/jmacd/xdelta-gpl) - Joshua MacDonald

