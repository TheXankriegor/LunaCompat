set KSP_PATH=F:/SteamLibrary/steamapps/common/Kerbal Space Program
set LUNA_SERVER_PATH=D:\Code\LunaMultiplayer\LMPServer
set CURRENT_DIR=%~dp0

cd /d %KSP_PATH%/GameData
mklink /j LunaCompat "%CURRENT_DIR%/Build/GameData/LunaCompat"

cd LunaMultiplayer/PartSync
mklink /j LunaCompat "%CURRENT_DIR%/Build/GameData/LunaMultiplayer/PartSync/LunaCompat"

cd /d %LUNA_SERVER_PATH%/Plugins
mklink /j LunaCompat "%CURRENT_DIR%/Build/LunaCompat"