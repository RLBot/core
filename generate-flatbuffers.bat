@echo off

cd /D "%~dp0"

echo Generating flatbuffers header file...

.\flatbuffers-schema\binaries\flatc.exe --csharp --gen-all --gen-object-api --gen-onefile -o .\FlatBuffer .\flatbuffers-schema\schema\rlbot.fbs

IF EXIST .\FlatBuffer\rlbot.cs del .\FlatBuffer\rlbot.cs

REM the file produced is called rlbot_generated.cs, rename it to RLBot.cs after removing the old one
ren .\FlatBuffer\rlbot_generated.cs RLBot.cs

REM CMD doesn't have native text replacement tools, use PowerShell
REM Replaces 'rlbot.flat' with 'RLBot.Flat'
powershell -Command "(Get-Content .\FlatBuffer\RLBot.cs) -replace 'rlbot\.flat', 'RLBot.Flat' | Set-Content .\FlatBuffer\RLBot.cs"

echo Done.