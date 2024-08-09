using System.Collections.Frozen;
using rlbot.flat;

namespace RLBotCS.Conversion;

internal static class PsyonixLoadouts
{
    private static Random _random = new();

    /// Unused loadout names, used to avoid spawning multiple of the same Psyonix bot
    private static List<string> Unused = new();

    public static void Reset()
    {
        Unused.Clear();
    }

    public static PlayerLoadoutT? GetFromName(string name, int team)
    {
        int index = Unused.FindIndex(n => n.Contains(name));
        if (index == -1)
            return null;

        var loadout = DefaultLoadouts[Unused[index]][team];
        Unused.RemoveAt(index);
        return loadout;
    }

    public static (string, PlayerLoadoutT) GetNext(int team)
    {
        if (Unused.Count == 0)
            Unused = DefaultLoadouts.Keys.OrderBy(_ => _random.Next()).ToList();
        var fullName = Unused.Last();
        Unused.RemoveAt(Unused.Count - 1);
        var loadout = DefaultLoadouts[fullName][team];
        var name = fullName.Split('_')[1];
        return (name, loadout);
    }

    private static readonly FrozenDictionary<string, List<PlayerLoadoutT>> DefaultLoadouts =
        new Dictionary<string, List<PlayerLoadoutT>>()
        {
            {
                "Bombers_Casper",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 322,
                        WheelsId = 377,
                        BoostId = 67,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Bombers_Marley",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 21,
                        DecalId = 290,
                        WheelsId = 365,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 232,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Bombers_Myrtle",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 308,
                        WheelsId = 377,
                        BoostId = 67,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Bombers_Samara",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 315,
                        WheelsId = 377,
                        BoostId = 67,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Cyclones_Fury",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 346,
                        WheelsId = 381,
                        BoostId = 64,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Cyclones_Rainmaker",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 303,
                        WheelsId = 381,
                        BoostId = 33,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Cyclones_Squall",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 310,
                        WheelsId = 381,
                        BoostId = 69,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Cyclones_Storm",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 346,
                        WheelsId = 381,
                        BoostId = 61,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Dragons_Hound",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 316,
                        WheelsId = 372,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Dragons_Imp",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 26,
                        DecalId = 327,
                        WheelsId = 369,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Dragons_Mountain",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 347,
                        WheelsId = 365,
                        BoostId = 36,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Dragons_Viper",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 31,
                        DecalId = 354,
                        WheelsId = 363,
                        BoostId = 55,
                        AntennaId = 13,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Express_Beast",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 320,
                        WheelsId = 375,
                        BoostId = 41,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Express_Roundhouse",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 316,
                        WheelsId = 365,
                        BoostId = 58,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Express_Sabertooth",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 339,
                        WheelsId = 375,
                        BoostId = 41,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Express_Tusk",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 347,
                        WheelsId = 375,
                        BoostId = 41,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Guardians_C-Block",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 315,
                        WheelsId = 380,
                        BoostId = 50,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Guardians_Centice",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 26,
                        DecalId = 327,
                        WheelsId = 380,
                        BoostId = 45,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Guardians_Gerwin",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 305,
                        WheelsId = 380,
                        BoostId = 48,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Guardians_Junker",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 28,
                        DecalId = 332,
                        WheelsId = 380,
                        BoostId = 49,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Mammoths_Boomer",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 21,
                        DecalId = 292,
                        WheelsId = 367,
                        BoostId = 58,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Mammoths_Caveman",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 347,
                        WheelsId = 360,
                        BoostId = 53,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Mammoths_Foamer",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 340,
                        WheelsId = 367,
                        BoostId = 58,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Mammoths_Sticks",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 308,
                        WheelsId = 367,
                        BoostId = 58,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Monarchs_Khan",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 340,
                        WheelsId = 383,
                        BoostId = 37,
                        AntennaId = 0,
                        HatId = 228,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Monarchs_Raja",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 31,
                        DecalId = 357,
                        WheelsId = 383,
                        BoostId = 37,
                        AntennaId = 0,
                        HatId = 228,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Monarchs_Rex",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 28,
                        DecalId = 331,
                        WheelsId = 383,
                        BoostId = 64,
                        AntennaId = 0,
                        HatId = 228,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Monarchs_Sultan",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 309,
                        WheelsId = 383,
                        BoostId = 37,
                        AntennaId = 0,
                        HatId = 228,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rebels_Bandit",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 304,
                        WheelsId = 364,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rebels_Dude",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 337,
                        WheelsId = 364,
                        BoostId = 63,
                        AntennaId = 8,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rebels_Outlaw",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 21,
                        DecalId = 0,
                        WheelsId = 364,
                        BoostId = 36,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rebels_Poncho",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 320,
                        WheelsId = 364,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 238,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rovers_Armstrong",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 26,
                        DecalId = 326,
                        WheelsId = 373,
                        BoostId = 45,
                        AntennaId = 12,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rovers_Buzz",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 298,
                        WheelsId = 373,
                        BoostId = 49,
                        AntennaId = 206,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rovers_Shepard",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 312,
                        WheelsId = 373,
                        BoostId = 45,
                        AntennaId = 17,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Rovers_Yuri",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 28,
                        DecalId = 333,
                        WheelsId = 373,
                        BoostId = 52,
                        AntennaId = 186,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Seekers_Middy",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 23,
                        DecalId = 305,
                        WheelsId = 368,
                        BoostId = 33,
                        AntennaId = 0,
                        HatId = 235,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Seekers_Saltie",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 28,
                        DecalId = 331,
                        WheelsId = 368,
                        BoostId = 69,
                        AntennaId = 0,
                        HatId = 235,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Seekers_Scout",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 343,
                        WheelsId = 368,
                        BoostId = 69,
                        AntennaId = 0,
                        HatId = 235,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Seekers_Swabbie",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 31,
                        DecalId = 353,
                        WheelsId = 368,
                        BoostId = 33,
                        AntennaId = 0,
                        HatId = 235,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Skyhawks_Cougar",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 301,
                        WheelsId = 363,
                        BoostId = 58,
                        AntennaId = 0,
                        HatId = 232,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Skyhawks_Goose",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 301,
                        WheelsId = 363,
                        BoostId = 53,
                        AntennaId = 0,
                        HatId = 232,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Skyhawks_Iceman",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 301,
                        WheelsId = 363,
                        BoostId = 54,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Skyhawks_Maverick",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 301,
                        WheelsId = 363,
                        BoostId = 57,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Chipper",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 21,
                        DecalId = 293,
                        WheelsId = 368,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Heater",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 30,
                        DecalId = 345,
                        WheelsId = 377,
                        BoostId = 41,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Hollywood",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 298,
                        WheelsId = 361,
                        BoostId = 51,
                        AntennaId = 3,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Jester",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 26,
                        DecalId = 323,
                        WheelsId = 376,
                        BoostId = 34,
                        AntennaId = 0,
                        HatId = 225,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Merlin",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 24,
                        DecalId = 312,
                        WheelsId = 376,
                        BoostId = 62,
                        AntennaId = 0,
                        HatId = 243,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Slider",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 29,
                        DecalId = 342,
                        WheelsId = 376,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Stinger",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 31,
                        DecalId = 352,
                        WheelsId = 376,
                        BoostId = 55,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Sundown",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 22,
                        DecalId = 299,
                        WheelsId = 383,
                        BoostId = 36,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Tex",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 25,
                        DecalId = 319,
                        WheelsId = 360,
                        BoostId = 64,
                        AntennaId = 206,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
            {
                "Teammates_Wolfman",
                new()
                {
                    new()
                    {
                        TeamColorId = 33,
                        CustomColorId = 0,
                        CarId = 28,
                        DecalId = 330,
                        WheelsId = 376,
                        BoostId = 63,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                    new()
                    {
                        TeamColorId = 0,
                        CustomColorId = 0,
                        CarId = 0,
                        DecalId = 0,
                        WheelsId = 0,
                        BoostId = 0,
                        AntennaId = 0,
                        HatId = 0,
                        PaintFinishId = 0,
                        CustomFinishId = 0,
                        EngineAudioId = 0,
                        TrailsId = 0,
                        GoalExplosionId = 0,
                        LoadoutPaint = new LoadoutPaintT(),
                    },
                }
            },
        }.ToFrozenDictionary();
}
