@echo on
cd /d "%~dp0"
setlocal ENABLEDELAYEDEXPANSION

REM Esegui solo per build Release
set "CONFIG=%~1"
if /I "%CONFIG%" NEQ "Release" (
    echo Build %CONFIG%, nessun aggiornamento Git.
    pause
    exit /b 0
)

REM 1) Legge la versione da AssemblyInfo.cs (AssemblyFileVersion("X.Y.Z"))
set "ASMINFO=LaunchManager\Properties\AssemblyInfo.cs"

if not exist "%ASMINFO%" (
    echo ERRORE: non trovo "%ASMINFO%".
    pause
    exit /b 1
)

set "FULLVER="
for /f "usebackq tokens=*" %%L in ("%ASMINFO%") do (
    echo %%L | find "AssemblyFileVersion" >nul
    if !errorlevel! == 0 (
        for /f "tokens=2 delims=()" %%V in ("%%L") do (
            for /f "tokens=1 delims=\""" %%W in ("%%V") do (
                set "FULLVER=%%W"
            )
        )
    )
)

if "!FULLVER!"=="" (
    echo ERRORE: non trovo AssemblyFileVersion in %ASMINFO%.
    pause
    exit /b 1
)

set "FULLVER=!FULLVER:"=!"
set "TAGNAME=v!FULLVER!"
set "COMMIT_MSG=Update !TAGNAME!"

echo Versione: !FULLVER!
echo Commit : !COMMIT_MSG!
echo Tag    : !TAGNAME!
echo.

REM 2) Aggiunge tutti i file modificati
git add .
if errorlevel 1 (
    echo ERRORE: git add ha restituito %errorlevel%.
    pause
    exit /b 1
)
echo Dopo git add, errorlevel=%errorlevel%

REM 3) Se non ci sono cambiamenti, esce
git diff --cached --quiet
echo Dopo git diff --cached --quiet, errorlevel=%errorlevel%
if %errorlevel%==0 (
    echo Nessuna modifica da inviare.
    pause
    exit /b 0
)

REM 4) Commit automatico
git commit -m "!COMMIT_MSG!"

REM 5) Crea il tag se non esiste
git tag --list "!TAGNAME!" >nul
if %errorlevel%==0 (
    echo Il tag !TAGNAME! esiste gia'.
) else (
    echo Creo il tag !TAGNAME!...
    git tag "!TAGNAME!"
)

REM 6) Push branch + tag
git push origin main
git push origin "!TAGNAME!"

echo Fatto.
pause
