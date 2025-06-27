set KSP_PATH=F:/SteamLibrary/steamapps/common/Kerbal Space Program
set LUNA_SERVER_PATH=F:\SteamLibrary\steamapps\common\Kerbal Space Program\LMPServer
set CURRENT_DIR=%~dp0

cd /d %KSP_PATH%/GameData
mklink /j LunaFixes "%CURRENT_DIR%/Build/GameData/LunaFixes"

cd LunaMultiplayer/PartSync
mklink /j LunaFixes "%CURRENT_DIR%/Build/GameData/LunaMultiplayer/PartSync/LunaFixes"

cd /d %LUNA_SERVER_PATH%/Plugins
mklink /j LunaFixes "%CURRENT_DIR%/Build/net6.0"