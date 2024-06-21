@echo off

cd /D "%~dp0"

echo Generating flatbuffers header file...

.\flatbuffers-schema\flatc.exe --csharp --gen-all --gen-object-api --gen-onefile -o .\FlatBuffer .\flatbuffers-schema\rlbot.fbs

IF EXIST .\FlatBuffer\rlbot.cs del .\FlatBuffer\rlbot.cs

REM the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
ren .\FlatBuffer\rlbot_generated.cs rlbot.cs

echo Done.