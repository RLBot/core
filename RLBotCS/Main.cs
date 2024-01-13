using RLBotCS.GameControl;
using RLBotCS.GameState;
using RLBotCS.Server;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.TCP;

var converter = new Converter();

// read the port from the command line arg or default to 23233
var port = args.Length > 0 ? int.Parse(args[0]) : 23233;

var messenger = new TcpMessenger(port);
var gotFirstMessage = false;

Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + port);

var playerInputSender = new PlayerInputSender(messenger);
var gameState = new GameState();
var matchStarter = new MatchStarter(messenger, gameState);

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

foreach (var messageClump in messenger)
{
    if (!gotFirstMessage)
    {
        Console.WriteLine("RLBot is now receiving messages from Rocket League!");
        gotFirstMessage = true;
        flatbufferServer.StartCommunications();
    }

    var messageBundle = converter.Convert(messageClump);
    gameState.gameTickPacket.isUnlimitedTime = matchStarter.IsUnlimitedTime();
    gameState.applyMessage(messageBundle);

    // this helps to wait for a new map to load
    if (!gameState.MatchEnded())
    {
        matchStarter.applyMessageBundle(messageBundle);
    }

    try
    {
        flatbufferServer.SendGameStateToClients(gameState);
    }
    catch (Exception e)
    {
        Console.WriteLine("Exception in Core: {0}", e);
    }
}
