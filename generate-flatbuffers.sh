cd "$(dirname "$0")"

echo Generating flatbuffers header file...

./flatbuffers-schema/flatc --gen-all --csharp --gen-object-api --gen-onefile -o ./FlatBuffer ./flatbuffers-schema/rlbot.fbs

# the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
rm -f ./FlatBuffer/rlbot.cs
mv ./FlatBuffer/rlbot_generated.cs ./FlatBuffer/rlbot.cs

echo Done.