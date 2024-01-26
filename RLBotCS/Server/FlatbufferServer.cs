using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Google.FlatBuffers;
using RLBotCS.GameControl;
using RLBotCS.GameState;
using RLBotCS.RLBotPacket;
using RLBotModels.Control;
using RLBotModels.Message;
using RLBotSecret.Controller;
using RLBotSecret.TCP;

namespace RLBotCS.Server
{
    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferServer
    {
        TcpListener server;
        TcpMessenger tcpGameInterface;
        PlayerMapping playerMapping;
        MatchStarter matchStarter;
        int sessionCount = 0;
        Dictionary<int, FlatbufferSession> sessions = new();
        bool startedCommunications = false;

        public FlatbufferServer(
            int port,
            TcpMessenger tcpGameInterface,
            PlayerMapping playerMapping,
            MatchStarter matchStarter
        )
        {
            this.tcpGameInterface = tcpGameInterface;
            this.playerMapping = playerMapping;
            this.matchStarter = matchStarter;

            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
        }

        public void StartCommunications()
        {
            startedCommunications = true;

            foreach (var session in sessions.Values)
            {
                session.SetStartCommunications(true);
            }
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Core Flatbuffer Server waiting for client connections...");
                    TcpClient client = server.AcceptTcpClient();
                    var ipEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    Console.WriteLine("Core is now serving a client that connected from port " + ipEndpoint.Port);

                    Thread t = new Thread(() => HandleClient(client));
                    t.Start();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Core's TCP server was terminated.");
                server.Stop();
            }
        }

        private void TryRunOnEachSession(Action<FlatbufferSession> action)
        {
            ImmutableArray<int> keys_copy = sessions.Keys.ToImmutableArray();
            foreach (var i in keys_copy)
            {
                if (!sessions.ContainsKey(i))
                {
                    continue;
                }

                var session = sessions[i];

                try
                {
                    action(session);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                    sessions.Remove(i);
                    session.Close(false);
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                    sessions.Remove(i);
                }
            }
        }

        internal void EnsureClientsPrepared(GameState.GameState gameState)
        {
            TryRunOnEachSession(session =>
            {
                session.ToggleStateSetting(matchStarter.IsStateSettingEnabled());
                session.ToggleRendering(matchStarter.IsRenderingEnabled());

                if (!session.IsReady || !session.NeedsIntroData)
                {
                    return;
                }

                if (matchStarter.GetMatchSettings() is TypedPayload matchSettings)
                {
                    session.SendIntroData(matchSettings, gameState);
                }
            });
        }

        internal void RemoveRenders()
        {
            TryRunOnEachSession(session =>
            {
                session.RemoveRenders();
            });
        }

        internal void SendMessagePacketToClients(MessageBundle messageBundle, float gameSeconds, int frameNum)
        {
            var messages = new rlbot.flat.MessagePacketT()
            {
                Messages = new List<rlbot.flat.GameMessageWrapperT>(),
                GameSeconds = gameSeconds,
                FrameNum = frameNum,
            };
            foreach (var message in messageBundle.messages)
            {
                if (message is PlayerInputUpdate update)
                {
                    if (playerMapping.PlayerIndexFromActorId(update.playerInput.actorId) is int playerIndex)
                    {
                        var playerInput = new rlbot.flat.PlayerInputChangeT()
                        {
                            PlayerIndex = playerIndex,
                            ControllerState = new rlbot.flat.ControllerStateT()
                            {
                                Throttle = update.playerInput.carInput.throttle,
                                Steer = update.playerInput.carInput.steer,
                                Pitch = update.playerInput.carInput.pitch,
                                Yaw = update.playerInput.carInput.yaw,
                                Roll = update.playerInput.carInput.roll,
                                Jump = update.playerInput.carInput.jump,
                                Boost = update.playerInput.carInput.boost,
                                Handbrake = update.playerInput.carInput.handbrake,
                            },
                            DodgeForward = update.playerInput.carInput.dodgeForward,
                            DodgeRight = update.playerInput.carInput.dodgeStrafe,
                        };

                        messages.Messages.Add(
                            new rlbot.flat.GameMessageWrapperT()
                            {
                                Message = rlbot.flat.GameMessageUnion.FromPlayerInputChange(playerInput),
                            }
                        );
                    }
                }
                else if (message is SpectateViewChange change)
                {
                    if (playerMapping.PlayerIndexFromActorId(change.spectatedActorId) is int playerIndex)
                    {
                        var spectate = new rlbot.flat.PlayerSpectateT() { PlayerIndex = playerIndex, };

                        messages.Messages.Add(
                            new rlbot.flat.GameMessageWrapperT()
                            {
                                Message = rlbot.flat.GameMessageUnion.FromPlayerSpectate(spectate),
                            }
                        );
                    }
                }
                else if (message is PlayerAccolade accolade)
                {
                    if (playerMapping.PlayerIndexFromActorId(accolade.actorId) is int playerIndex)
                    {
                        var playerAccolade = new rlbot.flat.PlayerStatEventT()
                        {
                            PlayerIndex = playerIndex,
                            StatType = accolade.accolade,
                        };

                        messages.Messages.Add(
                            new rlbot.flat.GameMessageWrapperT()
                            {
                                Message = rlbot.flat.GameMessageUnion.FromPlayerStatEvent(playerAccolade),
                            }
                        );
                    }
                }
            }

            var builder = new FlatBufferBuilder(1024);
            builder.Finish(rlbot.flat.MessagePacket.Pack(builder, messages).Value);

            var payload = TypedPayload.FromFlatBufferBuilder(DataType.MessagePacket, builder);
            TryRunOnEachSession(session =>
            {
                if (!session.IsReady || !session.WantsGameMessages)
                {
                    return;
                }

                session.SendPayloadToClient(payload);
            });
        }

        internal void SendGameStateToClients(GameTickPacket gameTickPacket)
        {
            TypedPayload payload = gameTickPacket.ToFlatbuffer();
            TryRunOnEachSession(session =>
            {
                if (!session.IsReady)
                {
                    return;
                }

                session.SendPayloadToClient(payload);
            });
        }

        public void Stop()
        {
            foreach (var session in sessions.Values)
            {
                session.Close(false);
            }

            sessions.Clear();
            server.Stop();
            Thread.Sleep(100);
            tcpGameInterface.Dispose();
        }

        public void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();

            var playerInputSender = new PlayerInputSender(tcpGameInterface);
            var renderingSender = new RenderingSender(tcpGameInterface);
            var gameController = new GameController(playerInputSender, renderingSender, matchStarter);

            var session = new FlatbufferSession(stream, gameController, playerMapping, startedCommunications);
            var id = Interlocked.Increment(ref sessionCount);
            sessions.Add(id, session);

            var wasDroppedCleanly = true;

            try
            {
                session.RunBlocking();
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Client unexpectedly terminated it's connection to core, dropping session.");
                wasDroppedCleanly = false;
            }
            catch (IOException e) when (e.InnerException is SocketException)
            {
                Console.WriteLine("Client unexpectedly terminated it's connection to core, dropping session.");
                wasDroppedCleanly = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                wasDroppedCleanly = false;
            }

            sessions.Remove(id);
            session.Close(wasDroppedCleanly);
            client.Close();
        }
    }
}
