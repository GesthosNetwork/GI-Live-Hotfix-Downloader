@echo off
cd ./FetchRel
dotnet publish -c Release
pause
taskkill /F /IM dotnet.exe