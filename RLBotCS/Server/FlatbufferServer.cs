using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Google.FlatBuffers;
using RLBotCS.GameControl;
using RLBotSecret.Controller;
using RLBotSecret.State;
using RLBotSecret.Models.Message;
using RLBotSecret.TCP;
using RLBotSecret.Types;

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
        private bool requestedServerStop = false;

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
                while (!requestedServerStop)
                {
                    Console.WriteLine("Core Flatbuffer Server waiting for client connections...");
                    TcpClient client = server.AcceptTcpClient();
                    var ipEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    Console.WriteLine("Core is now serving a client that connected from port " + ipEndpoint?.Port);

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

        internal bool CheckRequestStopServer()
        {
            return requestedServerStop;
        }

        internal void BlockingStop()
        {
            while (sessions.Count != 0)
            {
                Console.WriteLine("Core is waiting for all connections to close");
                Thread.Sleep(1000);
            }

            server.Stop();
            Thread.Sleep(100);
            tcpGameInterface.Dispose();
        }

        internal void EndMatchIfNeeded()
        {
            if (!sessions.Values.Any(session => session.HasRequestedMatchStop()))
            {
                return;
            }

            Console.WriteLine("Core has received a request to end the match.");
            matchStarter.EndMatch();

            if (sessions.Values.Any(session => session.HasRequestedServerStop()))
            {
                requestedServerStop = true;
                Console.WriteLine("Core has received a request to stop the server.");
            }

            Console.WriteLine("Core has ended the match.");

            TryRunOnEachSession(session =>
            {
                if (requestedServerStop || session.CloseAfterMatch)
                {
                    session.SetMatchEnded();
                }
            });
        }

        internal void EnsureClientsPrepared(GameState gameState)
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

        internal void SendMessagePacketToClients(MessageBundle messageBundle, float gameSeconds, uint frameNum)
        {
            var messages = new rlbot.flat.MessagePacketT()
            {
                Messages = new List<rlbot.flat.GameMessageWrapperT>(),
                GameSeconds = gameSeconds,
                FrameNum = frameNum,
            };
            foreach (var message in messageBundle.Messages)
            {
                if (message is PlayerInputUpdate update)
                {
                    if (playerMapping.PlayerIndexFromActorId(update.PlayerInput.ActorId) is uint playerIndex)
                    {
                        var playerInput = new rlbot.flat.PlayerInputChangeT()
                        {
                            PlayerIndex = playerIndex,
                            ControllerState = new rlbot.flat.ControllerStateT()
                            {
                                Throttle = update.PlayerInput.CarInput.Throttle,
                                Steer = update.PlayerInput.CarInput.Steer,
                                Pitch = update.PlayerInput.CarInput.Pitch,
                                Yaw = update.PlayerInput.CarInput.Yaw,
                                Roll = update.PlayerInput.CarInput.Roll,
                                Jump = update.PlayerInput.CarInput.Jump,
                                Boost = update.PlayerInput.CarInput.Boost,
                                Handbrake = update.PlayerInput.CarInput.Handbrake,
                            },
                            DodgeForward = update.PlayerInput.CarInput.DodgeForward,
                            DodgeRight = update.PlayerInput.CarInput.DodgeStrafe,
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
                    if (playerMapping.PlayerIndexFromActorId(change.SpectatedActorId) is uint playerIndex)
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
                    if (playerMapping.PlayerIndexFromActorId(accolade.ActorId) is uint playerIndex)
                    {
                        var playerAccolade = new rlbot.flat.PlayerStatEventT()
                        {
                            PlayerIndex = playerIndex,
                            StatType = accolade.Accolade,
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

        internal void SendGameStateToClients(GameState gameTickPacket)
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
