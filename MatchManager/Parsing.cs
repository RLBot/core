using rlbot.flat;
using Tomlyn.Model;
using Tomlyn;
using System.ComponentModel.DataAnnotations;
using RLBotModels.Message;
using RLBotCS.Conversion;

namespace MatchConfigManager
{
    public class ConfigParser
    {
        public TomlTable tomlConfig = [];
        public bool tomlLoaded = false;

        //TODO - Handle FileNotFound!
        public static TomlTable GetTable(string path)
        {
            return Toml.ToModel(File.ReadAllText(path));
        }

        //Generic to get the enum value of a given enum and the string name of the desired key
        static public T ParseEnum<T>(TomlTable table, string key, T fallback) where T : struct, Enum
        {
            if (Enum.TryParse((string)table[key], out T value)) {
                return value;
            }
            else
            {
                Console.WriteLine($"Warning! Unable to read {0}, using default instead", key);
                return fallback;
            }
        }

        //This stupid union is just because psyonix players have one extra property - sleep-deprived ddthj 
        public static PlayerClassUnion GetPlayerUnion(TomlTable player)
        {
            PlayerClassUnion playerClassUnion;
            PlayerClass playerClass = ParseEnum(player, "type", PlayerClass.PsyonixBotPlayer);

            switch (playerClass)
            {
                case PlayerClass.RLBotPlayer:
                    playerClassUnion = PlayerClassUnion.FromRLBotPlayer(new RLBotPlayerT());
                    break;
                case PlayerClass.HumanPlayer:
                    playerClassUnion = PlayerClassUnion.FromHumanPlayer(new HumanPlayerT());
                    break;
                case PlayerClass.PsyonixBotPlayer:
                    float botSkill = (float)(long)player["skill"];
                    playerClassUnion = PlayerClassUnion.FromPsyonixBotPlayer(new PsyonixBotPlayerT() { BotSkill = botSkill});
                    break;
                case PlayerClass.PartyMemberBotPlayer:
                    playerClassUnion = PlayerClassUnion.FromPartyMemberBotPlayer(new PartyMemberBotPlayerT());
                    Console.WriteLine("TODO - PartyMemeberBots are not implemented");
                    break;
                default:
                    playerClassUnion = PlayerClassUnion.FromHumanPlayer(new HumanPlayerT());
                    //TODO - this is lazy
                    Console.WriteLine("Warning! Could not determine player type, spawning human instead");
                    break;
            }
            playerClassUnion.Value = playerClass;

            return playerClassUnion;
        }

        public static PlayerConfigurationT GetPlayerConfig(TomlTable player)
        {
             PlayerConfigurationT playerConfig = new();

            /*  player contents:
             *      path (to bot.toml),
             *      type (rlbot, human, psyonix),
             *      team (0 or 1),
             *      skill (0.0 to 1.0 for psyonix types)
             */

            playerConfig.Variety = GetPlayerUnion(player); //Contains type and skill
            playerConfig.Team = (int)(long)player["team"];

            // parsing playerTable to get all the other goodies
            TomlTable playerTable = GetTable((string)player["path"]);

            TomlTable playerSettings = (TomlTable)playerTable["settings"];
            string path = (string)playerSettings["bot_starter"];
            playerConfig.Name = (string)playerSettings["name"];

            //parsing loadout
            TomlTable playerLoadout = GetTable((string)playerSettings["looks_config"]);
            playerConfig.Loadout = new PlayerLoadoutT() 
            {
                TeamColorId = (int)(long)playerLoadout["team_color_id"],
                CustomColorId = (int)(long)playerLoadout["custom_color_id"],
                CarId = (int)(long)playerLoadout["car_id"],
                DecalId = (int)(long)playerLoadout["decal_id"],
                WheelsId = (int)(long)playerLoadout["wheels_id"],
                BoostId = (int)(long)playerLoadout["boost_id"],
                AntennaId = (int)(long)playerLoadout["antenna_id"],
                HatId = (int)(long)playerLoadout["hat_id"],
                PaintFinishId = (int)(long)playerLoadout["paint_finish_id"],
                CustomFinishId = (int)(long)playerLoadout["custom_finish_id"],
                EngineAudioId = (int)(long)playerLoadout["engine_audio_id"],
                TrailsId = (int)(long)playerLoadout["trails_id"],
                GoalExplosionId = (int)(long)playerLoadout["goal_explosion_id"],
                LoadoutPaint = new LoadoutPaintT()
                {
                    CarPaintId = (int)(long)playerLoadout["car_paint_id"],
                    DecalPaintId = (int)(long)playerLoadout["decal_paint_id"],
                    WheelsPaintId = (int)(long)playerLoadout["wheels_paint_id"],
                    BoostPaintId = (int)(long)playerLoadout["boost_paint_id"],
                    AntennaPaintId = (int)(long)playerLoadout["antenna_paint_id"],
                    HatPaintId = (int)(long)playerLoadout["hat_paint_id"],
                    TrailsPaintId = (int)(long)playerLoadout["trails_paint_id"],
                    GoalExplosionPaintId = (int)(long)playerLoadout["goal_explosion_paint_id"],
                },
                PrimaryColorLookup = new ColorT(),
                SecondaryColorLookup = new ColorT(), // TODO - GetPrimary/Secondary call?
        };


            // TODO - what do we do with all this stuff ???
            int maxTickRate = (int)(long)playerSettings["max_tick_rate_preference"];

            TomlTable playerDetails = (TomlTable)playerTable["details"];
            string description = (string)playerDetails["description"];
            string funFact = (string)playerDetails["fun_fact"];
            string developer = (string)playerDetails["developer"];
            string language = (string)playerDetails["language"];
            List<string> tags = (List<string>)playerDetails["tags"];

            return playerConfig;
        }

        public MatchSettingsT GetMatchSettings(string path)
        {

            // TODO - there is little childproofing here. Will children ever use the toml file?
            if (!tomlLoaded)
            {
                tomlConfig = GetTable(path);
                tomlLoaded = true;
            }

            MatchSettingsT matchSettings = new();

            TomlTable settings = (TomlTable)tomlConfig["match"];

            int num_bots = (int)(long)settings["num_participants"];

            matchSettings.GameMode = ParseEnum(settings, "game_mode", rlbot.flat.GameMode.Soccer);
            matchSettings.GameMapUpk = (string)settings["game_map_upk"];
            matchSettings.SkipReplays = (bool)settings["skip_replays"];
            matchSettings.InstantStart = (bool)settings["start_without_countdown"];
            matchSettings.EnableRendering = (bool)settings["enable_rendering"];
            matchSettings.EnableStateSetting = (bool)settings["enable_state_setting"];
            matchSettings.ExistingMatchBehavior = ParseEnum(settings, "existing_match_behavior", ExistingMatchBehavior.Restart_If_Different);
            matchSettings.AutoSaveReplay = (bool)settings["auto_save_replay"];

            TomlTable mutators = (TomlTable)tomlConfig["mutators"];

            matchSettings.MutatorSettings = new MutatorSettingsT()
            {
                MatchLength = ParseEnum(mutators, "match_length", MatchLength.Five_Minutes),
                MaxScore = ParseEnum(mutators, "max_score", MaxScore.Unlimited),
                OvertimeOption = ParseEnum(mutators, "overtime", OvertimeOption.Unlimited),
                GameSpeedOption = ParseEnum(mutators, "game_speed", GameSpeedOption.Default),
                BallMaxSpeedOption = ParseEnum(mutators, "ball_max_speed", BallMaxSpeedOption.Default),
                BallTypeOption = ParseEnum(mutators, "ball_type", BallTypeOption.Default),
                BallWeightOption = ParseEnum(mutators, "ball_weight", BallWeightOption.Default),
                BallSizeOption = ParseEnum(mutators, "ball_size", BallSizeOption.Default),
                BallBouncinessOption = ParseEnum(mutators, "ball_bounciness", BallBouncinessOption.Default),
                BoostOption = ParseEnum(mutators, "boost_amount", BoostOption.Normal_Boost),
                RumbleOption = ParseEnum(mutators, "rumble", RumbleOption.Default),
                BoostStrengthOption = ParseEnum(mutators, "boost_strength", BoostStrengthOption.One),
                GravityOption = ParseEnum(mutators, "gravity", GravityOption.Default),
                DemolishOption = ParseEnum(mutators, "demolish", DemolishOption.Default),
                RespawnTimeOption = ParseEnum(mutators, "respawn_time", RespawnTimeOption.Three_Seconds),
            };

            TomlTableArray players = (TomlTableArray)tomlConfig["bots"];
            Console.WriteLine($"Bots array len: {0}", players.Count);
            
            List<PlayerConfigurationT> playerConfigs = [];
            
            foreach (TomlTable player in players){
                playerConfigs.Add(GetPlayerConfig(player));
            }
            matchSettings.PlayerConfigurations = playerConfigs;
            return matchSettings;
        }
    }
}
