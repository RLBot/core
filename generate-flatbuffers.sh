echo Generating flatbuffers header file...

./RLBotCS/flatbuffers-schema/flatc --csharp --gen-object-api --gen-onefile -o ./RLBotCS/FlatBuffer ./RLBotCS/flatbuffers-schema/rlbot.fbs

# the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
rm ./RLBotCS/FlatBuffer/rlbot.cs
mv ./RLBotCS/FlatBuffer/rlbot_generated.cs ./RLBotCS/FlatBuffer/rlbot.cs
