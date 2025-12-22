@echo on
cd /d "%~dp0"

REM 1) Aggiunge tutti i file modificati
git add .

REM 2) Se non ci sono cambiamenti, esce
git diff --cached --quiet
if %errorlevel%==0 (
    echo Nessuna modifica da inviare.
    pause
    exit /b 0
)

REM 3) Chiede il messaggio di commit
set /p COMMIT_MSG=Messaggio commit (es. Update 1.3.3): 

git commit -m "%COMMIT_MSG%"

REM 4) Chiede se creare/pushare un tag
set /p TAGNAME=Nome tag (lascia vuoto per nessun tag, es. v1.3.3): 

git push origin main

if not "%TAGNAME%"=="" (
    git tag "%TAGNAME%"
    git push origin "%TAGNAME%"
)

echo Fatto.
pause
