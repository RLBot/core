echo Generating flatbuffers header file...

..\flatbuffers-schema\flatc.exe --csharp --gen-object-api --gen-onefile -o .\ ..\flatbuffers-schema\rlbot.fbs

REM the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
DEL ./RLBotCS/FlatBuffer/rlbot.cs
ren ./RLBotCS/FlatBuffer/rlbot_generated.cs rlbot.cs
