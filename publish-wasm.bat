@echo off
mode con: cols=120 lines=35 >nul 2>&1
title Publish Pinyin Island WASM

echo ===================================================
echo     Publishing Pinyin Island (WebAssembly)
echo ===================================================

REM Generate Version Tag (Format: YYYYMMDD_HHMMSS)
set CUR_DATE=%DATE:~0,4%%DATE:~5,2%%DATE:~8,2%
set CUR_TIME=%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
set CUR_TIME=%CUR_TIME: =0%
set VERSION_TAG=%CUR_DATE%_%CUR_TIME%

set ZIP_NAME=PinyinIsland_Wasm_%VERSION_TAG%.zip
set PROJECT_PATH=.\PinyinIsland.Client\PinyinIsland.Client.csproj
set TARGET_FOLDER=..\publish\Wasm
set TARGET_WWWROOT=%TARGET_FOLDER%\wwwroot
set CONFIGURATION=Release

echo Project:        %PROJECT_PATH%
echo Target Folder:  %TARGET_FOLDER%
echo Version Tag:    %VERSION_TAG%
echo Configuration:  %CONFIGURATION%
echo.

echo === [1/6] RESTORE STEP ===
dotnet restore %PROJECT_PATH%
if errorlevel 1 (
    echo [ERROR] Restore failed. Aborting publish.
    pause
    exit /b 1
)

echo.
echo === [2/6] PUBLISH STEP ===
dotnet publish %PROJECT_PATH% ^
    --configuration %CONFIGURATION% ^
    --output %TARGET_FOLDER% ^
    /p:BlazorWebAssemblyEnableLinking=true ^
    /p:PublishTrimmed=true
if errorlevel 1 (
    echo [ERROR] Publish failed. Check your project settings.
    pause
    exit /b 1
)

echo.
echo === [3/6] WRITE VERSION AND ASSETS ===
echo { "version": "%VERSION_TAG%" } > "%TARGET_WWWROOT%\version.json"

REM Copy favicon if available from host project
if exist ".\PinyinIsland\wwwroot\favicon.ico" (
    copy /Y ".\PinyinIsland\wwwroot\favicon.ico" "%TARGET_WWWROOT%\favicon.ico" >nul
)

echo.
echo === [4/6] GENERATE STATIC HOSTING CONFIGS (_redirects and _headers) ===
REM SPA routing redirect for Cloudflare Pages / Netlify
echo /* /index.html 200 > "%TARGET_WWWROOT%\_redirects"

REM Header configurations
echo /*.wasm.br > "%TARGET_WWWROOT%\_headers"
echo   Content-Type: application/wasm >> "%TARGET_WWWROOT%\_headers"
echo   Content-Encoding: br >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo /*.wasm.gz >> "%TARGET_WWWROOT%\_headers"
echo   Content-Type: application/wasm >> "%TARGET_WWWROOT%\_headers"
echo   Content-Encoding: gzip >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo /version.json >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo /manifest.json >> "%TARGET_WWWROOT%\_headers"
echo   Content-Type: application/manifest+json >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo /service-worker.js >> "%TARGET_WWWROOT%\_headers"
echo   Content-Type: application/javascript >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo /service-worker-assets.js >> "%TARGET_WWWROOT%\_headers"
echo   Content-Type: application/javascript >> "%TARGET_WWWROOT%\_headers"
echo   Cache-Control: no-cache >> "%TARGET_WWWROOT%\_headers"

echo.
echo === [5/6] PROCESS index.publish.html (IF APPLICABLE) ===
if exist "%TARGET_WWWROOT%\index.publish.html" (
    echo Replacing index.html with index.publish.html...
    copy /Y "%TARGET_WWWROOT%\index.publish.html" "%TARGET_WWWROOT%\index.html"
    if exist "%TARGET_WWWROOT%\index.publish.html.br" (
        copy /Y "%TARGET_WWWROOT%\index.publish.html.br" "%TARGET_WWWROOT%\index.html.br"
    )
    if exist "%TARGET_WWWROOT%\index.publish.html.gz" (
        copy /Y "%TARGET_WWWROOT%\index.publish.html.gz" "%TARGET_WWWROOT%\index.html.gz"
    )
) else (
    echo Standard index.html found. Skipping index.publish.html replacement.
)

echo.
echo === [6/6] ZIP wwwroot CONTENT ===
if exist "C:\Program Files\7-Zip\7z.exe" (
    echo Compressing using 7-Zip...
    "C:\Program Files\7-Zip\7z.exe" a "%TARGET_FOLDER%\%ZIP_NAME%" "%TARGET_WWWROOT%\*" -mx=9 -tzip
) else (
    echo 7-Zip not found at default location. Using PowerShell Compress-Archive...
    powershell -NoProfile -Command "Compress-Archive -Path '%TARGET_WWWROOT%\*' -DestinationPath '%TARGET_FOLDER%\%ZIP_NAME%' -Force"
)

if errorlevel 1 (
    echo [ERROR] Failed to create zip package.
    pause
    exit /b 1
)

echo.
echo ===================================================
echo     PUBLISH COMPLETED SUCCESSFULLY!
echo ===================================================
echo Output ZIP: %TARGET_FOLDER%\%ZIP_NAME%
echo Output Dir: %TARGET_WWWROOT%
echo ===================================================
echo.
pause
