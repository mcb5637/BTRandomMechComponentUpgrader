del ".\BTRandomMechComponentUpgrader.zip"
del ".\BTRandomMechComponentUpgraderBTX.zip"
del ".\BTXMinusWeapons.zip"

mkdir .\tmp\BTRandomMechComponentUpgrader
copy .\BTRandomMechComponentUpgrader\bin\Release\BTRandomMechComponentUpgrader.dll .\tmp\BTRandomMechComponentUpgrader
copy .\BTRandomMechComponentUpgrader\mod.json .\tmp\BTRandomMechComponentUpgrader
robocopy .\BTRandomMechComponentUpgrader\ComponentUpgradeList .\tmp\BTRandomMechComponentUpgrader\ComponentUpgradeList /E
robocopy .\BTRandomMechComponentUpgrader\ComponentUpgradeSubList .\tmp\BTRandomMechComponentUpgrader\ComponentUpgradeSubList /E
cd .\tmp
"C:\Program Files\7-Zip\7z.exe" a "..\BTRandomMechComponentUpgrader.zip" "BTRandomMechComponentUpgrader\" "..\LICENSE" "..\README.md"
cd ..\

mkdir .\tmp\BTRandomMechComponentUpgraderBTX
copy .\BTRandomMechComponentUpgrader\BTRandomMechComponentUpgraderBTX\mod.json .\tmp\BTRandomMechComponentUpgraderBTX
robocopy .\BTRandomMechComponentUpgrader\BTRandomMechComponentUpgraderBTX\ComponentUpgradeList .\tmp\BTRandomMechComponentUpgraderBTX\ComponentUpgradeList /E
robocopy .\BTRandomMechComponentUpgrader\BTRandomMechComponentUpgraderBTX\ComponentUpgradeSubList .\tmp\BTRandomMechComponentUpgraderBTX\ComponentUpgradeSubList /E
cd .\tmp
"C:\Program Files\7-Zip\7z.exe" a "..\BTRandomMechComponentUpgraderBTX.zip" "BTRandomMechComponentUpgraderBTX\" "..\LICENSE" "..\README.md"
cd ..\

mkdir .\tmp\BTXMinusWeapons
robocopy .\BTXMinusWeaponsAndLists\BTXMinusWeapons .\tmp\BTXMinusWeapons /E
cd .\tmp
"C:\Program Files\7-Zip\7z.exe" a "..\BTXMinusWeapons.zip" "BTXMinusWeapons\" "..\LICENSE" "..\README.md"
cd ..\

rmdir /s /q .\tmp
pause