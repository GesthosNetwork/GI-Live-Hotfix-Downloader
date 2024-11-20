@echo off

for /f "tokens=1,2 delims==" %%A in (.env) do set "%%A=%%B"

for /f "tokens=*" %%i in (version.release\%BRANCH%.txt) do (
    echo Processing: %%i
    python fetch_rel.py --branch %BRANCH% --client %CLIENT% --url %URL% %%i
)

pause