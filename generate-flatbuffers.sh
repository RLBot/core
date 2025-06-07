cd "$(dirname "$0")"

echo Generating flatbuffers header file...

./flatbuffers-schema/binaries/flatc --gen-all --csharp --gen-object-api --gen-onefile -o ./FlatBuffer ./flatbuffers-schema/schema/rlbot.fbs

# the file produced is called rlbot_generated.cs, rename it to RLBot.cs after removing the old one
rm -f ./FlatBuffer/RLBot.cs
mv ./FlatBuffer/rlbot_generated.cs ./FlatBuffer/RLBot.cs
sed -i 's/rlbot\.flat/RLBot.Flat/g' ./FlatBuffer/RLBot.cs

echo Done.