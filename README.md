# FFX-2: Turn Based
A [Fahrenheit](https://github.com/fahrenheit-crew/fahrenheit) based mod for FFX-2 that changes the battle system to be turn-based in a similar style to FFX.

*More details about what this mod changes and how it works are documented in this repo's [wiki](https://github.com/jmcx532/FFX-2-TurnBased/wiki).*

### Showcase Video - vs Chac

https://www.youtube.com/watch?v=j4Zda7A3lCo

# Installation

This mod requires some data files to be patched to function correctly.

*1. Download a release, or build from source, and copy the `ffx2TurnBased` folder into your Fahrenheit `mods` folder.*

2. Download the [VBF Browser](https://www.nexusmods.com/finalfantasy12/mods/3) and extract the `ffx_ps2` folder from `YOUR_STEAM_LIBRARY\FINAL FANTASY FFX&FFX-2 HD Remaster\data\FFX2_Data.vbf` to a location of your choosing. 

3. From `ffx_ps2\ffx2\master\jppc\battle\kernel` you will need to copy `rom.bin` into `Required File Patches\PlaceYourFilesHere`.

4. From `ffx_ps2\ffx2\master\new_uspc\battle\kernel` you will need to copy the following files into `Required File Patches\PlaceYourFilesHere`:
* a_ability.bin
* command.bin
* item.bin
* monmagic.bin

5. From `ffx_ps2\ffx2\master\jppc\battle\mon\` you will need to copy the following files into `Required File Patches\PlaceYourFilesHere\MonsterFiles`:
* m152.bin
* m290.bin
* m291.bin
* m292.bin
  
*Note: These files will be inside a subfolder: `\mon\_m152` as an example.*

6. Now run `PatchFiles.bat` which will apply the patches to your files. This will create a directory in the `Output` folder called `efl` which will contain the patched files: `efl\x2\ffx_ps2...`

Copy the `efl` folder into the `ffx2TurnBased` folder. (Alongside the DLL, mod manifest etc.)

# Credits
[Fahrenheit](https://github.com/fahrenheit-crew/fahrenheit) - The Fahrenheit crew

[VBF Browser](https://www.nexusmods.com/finalfantasy12/mods/3) - Vaan, Topher and FFGriever

[xDelta3](https://github.com/jmacd/xdelta-gpl) - Joshua MacDonald

