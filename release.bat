@echo off

for /f "tokens=1,2 delims==" %%A in (.env) do set "%%A=%%B"
for /f "tokens=1 delims=_" %%A in ("%BRANCH%") do set BRANCH_VERSION=%%A.0
for /f "tokens=1,2 delims= " %%A in (version.release\%BRANCH%.txt) do (
    for /f "tokens=1,2 delims=_" %%C in ("%%B") do (
        set %%A_CODE=%%C
        set %%A_SUFFIX=%%D
    )
)

set TARGET_DIR=.\resources\OSRELWin%BRANCH_VERSION%_R%res_CODE%_S%silence_CODE%_D%data_CODE%\GenshinImpact_Data\Persistent
if not exist "%TARGET_DIR%" md "%TARGET_DIR%"

set path1=.\resources\client_game_res\%BRANCH%\output_%res_CODE%_%res_SUFFIX%\client\%CLIENT%
set path2=.\resources\client_design_data\%BRANCH%\output_%silence_CODE%_%silence_SUFFIX%\client_silence\General\AssetBundles
set path3=.\resources\client_design_data\%BRANCH%\output_%data_CODE%_%data_SUFFIX%\client\General\AssetBundles

if exist "%path1%\release_res_versions_external" (
    ren "%path1%\release_res_versions_external" "res_versions_persist"
    move "%path1%\res_versions_persist" "%TARGET_DIR%\res_versions_persist"
) else if exist "%path1%\res_versions_external" (
    ren "%path1%\res_versions_external" "res_versions_persist"
    move "%path1%\res_versions_persist" "%TARGET_DIR%\res_versions_persist"
)

if exist "%path2%\data_versions" (
    ren "%path2%\data_versions" "silence_data_versions_persist"
    move "%path2%\silence_data_versions_persist" "%TARGET_DIR%\silence_data_versions_persist"
)

if exist "%path3%\data_versions" (
    ren "%path3%\data_versions" "data_versions_persist"
    move "%path3%\data_versions_persist" "%TARGET_DIR%\data_versions_persist"
)

if exist "%path3%\data_versions_medium" (
    ren "%path3%\data_versions_medium" "data_versions_medium_persist"
    move "%path3%\data_versions_medium_persist" "%TARGET_DIR%\data_versions_medium_persist"
)

for %%D in ("%path1%\AssetBundles" "%path1%\AudioAssets" "%path1%\VideoAssets" "%path2%" "%path3%") do (
    if "%%~nxD"=="AudioAssets" (
        dir /b "%%D" | findstr /v /i "audio_versions" >nul && (
            robocopy "%%D" "%TARGET_DIR%\AudioAssets" /e /move /xf audio_versions
        )
    ) else if "%%~nxD"=="VideoAssets" (
        dir /b "%%D" | findstr /v /i "video_versions" >nul && (
            robocopy "%%D" ".\resources\OSRELWin%BRANCH_VERSION%_R%res_CODE%_S%silence_CODE%_D%data_CODE%\GenshinImpact_Data\StreamingAssets\VideoAssets" /e /move /xf video_versions
        )
    ) else robocopy "%%D" "%TARGET_DIR%\AssetBundles" /e /move
)

echo %res_CODE% > "%TARGET_DIR%\res_revision"
echo %silence_CODE% > "%TARGET_DIR%\silence_revision"
echo %data_CODE% > "%TARGET_DIR%\data_revision"

for %%F in ("%TARGET_DIR%\AssetBundles\svc_catalog" resources\client_design_data resources\client_game_res) do (
    if exist "%%F" del /q "%%F" 2>nul & rd /s /q "%%F" 2>nul
)

echo Process completed.
pause
