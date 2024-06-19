using System.Threading.Channels;
using RLBotCS.ManagerTools;
using RLBotCS.Server;
using RLBotCS.Server.FlatbuffersMessage;
using Bridge.TCP;

int gamePort = LaunchManager.FindUsableGamePort();
Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + gamePort);

// Set up the handler to use bridge to talk with the game
var bridgeChannel = Channel.CreateUnbounded<IBridgeMessage>();
var bridgeWriter = bridgeChannel.Writer;

// Set up the TCP server for RLBots
var serverChannel = Channel.CreateUnbounded<IServerMessage>();
var serverWriter = serverChannel.Writer;

Thread rlbotServer = new(() =>
{
    MatchStarter matchStarter = new(bridgeWriter, gamePort);
    FlatBuffersServer flatBuffersServer =
        new(LaunchManager.RlbotSocketsPort, serverChannel, matchStarter, bridgeWriter);

    try
    {
        flatBuffersServer.BlockingRun();
    }
    finally
    {
        flatBuffersServer.Cleanup();
    }
});
rlbotServer.Start();

Thread bridgeHandler = new(() =>
{
    TcpMessenger tcpMessenger = new(gamePort);
    BridgeHandler bridgeHandler = new(serverWriter, bridgeChannel.Reader, tcpMessenger);

    try
    {
        bridgeHandler.BlockingRun();
    }
    finally
    {
        bridgeHandler.Cleanup();
    }
});
bridgeHandler.Start();

// Block until everything properly shuts down
void WaitForShutdown()
{
    rlbotServer.Join();
    Console.WriteLine("RLBot server has shut down successfully.");

    bridgeWriter.TryWrite(new Stop());
    bridgeWriter.TryComplete();

    bridgeHandler.Join();
    Console.WriteLine("Bridge handler has shut down successfully.");
}

// Catch sudden termination to clean up the server
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    serverWriter.TryComplete();

    WaitForShutdown();
    Console.WriteLine("Core has shut down successfully.");
};

// Catch Ctrl+C to clean up the server
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    serverWriter.TryComplete();

    WaitForShutdown();
    Console.WriteLine("Core has shut down successfully.");
};

// Wait for a normal shutdown
WaitForShutdown();
