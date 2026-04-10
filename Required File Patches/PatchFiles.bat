@echo off
setlocal

echo Creating directories...

mkdir .\Output\efl\x2\ffx_ps2\ffx2\master\new_uspc\battle\kernel
mkdir .\Output\efl\x2\ffx_ps2\ffx2\master\jppc\battle\kernel

echo Applying patches...

xdelta3.exe -f -d -s .\PlaceYourFilesHere\monmagic.bin .\Patches\monmagic.xdelta .\Output\efl\x2\ffx_ps2\ffx2\master\new_uspc\battle\kernel\monmagic.bin
xdelta3.exe -f -d -s .\PlaceYourFilesHere\command.bin .\Patches\command.xdelta .\Output\efl\x2\ffx_ps2\ffx2\master\new_uspc\battle\kernel\command.bin
xdelta3.exe -f -d -s .\PlaceYourFilesHere\item.bin .\Patches\item.xdelta .\Output\efl\x2\ffx_ps2\ffx2\master\new_uspc\battle\kernel\item.bin
xdelta3.exe -f -d -s .\PlaceYourFilesHere\a_ability.bin .\Patches\a_ability.xdelta .\Output\efl\x2\ffx_ps2\ffx2\master\new_uspc\battle\kernel\a_ability.bin

xdelta3.exe -f -d -s .\PlaceYourFilesHere\rom.bin .\Patches\rom.xdelta .\Output\efl\x2\ffx_ps2\ffx2\master\jppc\battle\kernel\rom.bin

echo Patching complete!

pause