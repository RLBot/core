using RLBotCS.GameControl;
using RLBotCS.Server;
using RLBotSecret.Conversion;
using RLBotSecret.GameState;
using RLBotSecret.TCP;
using Launcher = RLBotCS.MatchManagement.Launcher;

var converter = new Converter();

// read the port from the command line arg or default to 23233
var port = args.Length > 0 ? int.Parse(args[0]) : 23233;
Console.WriteLine("RLBot using port " + port);

var messenger = new TcpMessenger(port);
var gotFirstMessage = Launcher.IsRocketLeagueRunning();

Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + port);

var gameState = new GameState();
var matchStarter = new MatchStarter(messenger, gameState, port);

var flatbufferServer = new FlatbufferServer(23234, messenger, gameState.playerMapping, matchStarter);
var serverListenerThread = new Thread(() => flatbufferServer.StartListener());
serverListenerThread.Start();

// catch sudden termination to clean up the server
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    flatbufferServer.Stop();
    Console.WriteLine("Core has shut down successfully.");
};

// catch ctrl+c to clean up the server
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    flatbufferServer.Stop();
    Console.WriteLine("Core has shut down successfully.");
};

foreach (var messageClump in messenger.Read())
{
    if (!gotFirstMessage)
    {
        Console.WriteLine("RLBot is now receiving messages from Rocket League!");
        gotFirstMessage = true;
        flatbufferServer.StartCommunications();
    }

    matchStarter.LoadDeferredMatch();

    var messageBundle = converter.Convert(messageClump);
    gameState.matchLength = matchStarter.MatchLength();
    gameState.respawnTime = matchStarter.RespawnTime();
    gameState = StateTransformer.ApplyMessagesToState(messageBundle, gameState);

    // this helps to wait for a new map to load
    if (!gameState.MatchEnded)
        matchStarter.ApplyMessageBundle(messageBundle);

    try
    {
        if (gameState.MatchEnded)
            flatbufferServer.RemoveRenders();

        flatbufferServer.EnsureClientsPrepared(gameState);
        flatbufferServer.SendMessagePacketToClients(
            messageBundle,
            gameState.secondsElapsed,
            gameState.frameNum
        );
        flatbufferServer.SendGameStateToClients(gameState);
    }
    catch (Exception e)
    {
        Console.WriteLine("Exception in Core: {0}", e);
    }

    flatbufferServer.EndMatchIfNeeded();

    if (flatbufferServer.CheckRequestStopServer())
    {
        break;
    }
}

flatbufferServer.BlockingStop();
