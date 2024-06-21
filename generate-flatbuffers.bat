@echo off

cd /D "%~dp0"

echo Generating flatbuffers header file...

.\RLBotCS\flatbuffers-schema\flatc.exe --csharp --gen-all --gen-object-api --gen-onefile -o .\RLBotCS\FlatBuffer .\RLBotCS\flatbuffers-schema\rlbot.fbs

IF EXIST .\RLBotCS\FlatBuffer\rlbot.cs del .\RLBotCS\FlatBuffer\rlbot.cs

REM the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
ren .\RLBotCS\FlatBuffer\rlbot_generated.cs rlbot.cs

echo Done.