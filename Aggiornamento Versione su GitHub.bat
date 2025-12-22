@echo on
REM ==========================================
REM  Aggiorna il repo Launch Manager 2024
REM  - Legge la versione da AssemblyInfo.cs
REM  - Aggiunge tutti i file modificati
REM  - Commit automatico
REM  - Crea tag vX.Y.Z e lo pusha
REM ==========================================

setlocal ENABLEDELAYEDEXPANSION

REM 1) Percorso di AssemblyInfo.cs (relativo alla cartella del progetto)
set ASMINFO=LaunchManager\Properties\AssemblyInfo.cs

if not exist "%ASMINFO%" (
    echo ERRORE: non trovo "%ASMINFO%".
    pause
    exit /b 1
)

REM 2) Legge la riga AssemblyFileVersion("X.Y.Z.W")
for /f "usebackq tokens=*" %%L in ("%ASMINFO%") do (
    echo %%L | find "AssemblyFileVersion" >nul
    if !errorlevel! == 0 (
        set LINE=%%L
    )
)

if "%LINE%"=="" (
    echo ERRORE: non trovo AssemblyFileVersion in %ASMINFO%.
    pause
    exit /b 1
)

REM Estrae X.Y.Z da X.Y.Z.W
for /f "tokens=2 delims==\"" %%V in ("%LINE%") do (
    set FULLVER=%%V
)

REM FULLVER tipo 1.2.3.0 -> prendiamo i primi tre segmenti
for /f "tokens=1-3 delims=." %%a in ("%FULLVER%") do (
    set TAGVER=%%a.%%b.%%c
)

set TAGNAME=v%TAGVER%

echo Versione trovata in AssemblyInfo: %FULLVER%
echo Tag che verra' creato: %TAGNAME%
echo.

REM 3) git add
git add .

REM 4) controlla se ci sono modifiche
git diff --cached --quiet
if %errorlevel%==0 (
    echo Nessuna modifica da commitare. Creo solo il tag (se non esiste)...
) else (
    set COMMIT_MSG=Auto update %TAGNAME%
    echo Creo commit: "%COMMIT_MSG%"
    git commit -m "%COMMIT_MSG%"
    if %errorlevel% NEQ 0 (
        echo ERRORE nel commit.
        pause
        exit /b 1
    )
)

REM 5) controlla se il tag esiste gia'
git tag --list "%TAGNAME%" >nul
if %errorlevel%==0 (
    echo Il tag %TAGNAME% esiste gia'. Non lo ricreo.
) else (
    echo Creo tag %TAGNAME%...
    git tag "%TAGNAME%"
)

REM 6) push di branch e tag
echo Faccio push di main e del tag %TAGNAME%...
git push origin main
git push origin "%TAGNAME%"

echo.
echo ==========================================
echo Operazione completata.
echo Branch main e tag %TAGNAME% aggiornati.
echo ==========================================
pause
