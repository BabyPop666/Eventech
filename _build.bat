@echo off
cd /d "%~dp0"
echo BUILD START %DATE% %TIME% > _build_log.txt
dotnet build EvenTech.sln >> _build_log.txt 2>&1
echo. >> _build_log.txt
echo BUILD_EXIT=%ERRORLEVEL% >> _build_log.txt
echo BUILD END %DATE% %TIME% >> _build_log.txt
