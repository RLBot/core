using System.Threading.Channels;
using RLBotCS.GameControl;
using RLBotCS.MatchManagement;
using RLBotCS.Server;
using RLBotSecret.TCP;

int gamePort = Launcher.FindUsableGamePort();
Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + gamePort);

TcpMessenger tcpMessenger = new TcpMessenger(gamePort);
Mutex tcpSync = new Mutex();

// Setup the handler to use bridge to talk with the game
Channel<BridgeMessage> bridgeChannel = Channel.CreateUnbounded<BridgeMessage>();
ChannelWriter<BridgeMessage> bridgeWriter = bridgeChannel.Writer;

// Setup the tcp server for rlbots
Channel<ServerMessage> serverChannel = Channel.CreateUnbounded<ServerMessage>();
ChannelWriter<ServerMessage> serverWriter = serverChannel.Writer;

Thread rlbotServer = new Thread(() =>
{
    MatchStarter matchStarter = new MatchStarter(bridgeWriter, gamePort);
    FlatbufferServer flatbufferServer = new FlatbufferServer(
        Launcher.RLBotSocketsPort,
        serverChannel,
        matchStarter
    );
    flatbufferServer.BlockingRun();
    flatbufferServer.Cleanup();
});
rlbotServer.Start();

Thread bridgeHandler = new Thread(() =>
{
    BridgeHandler bridgeHandler = new BridgeHandler(serverWriter, bridgeChannel.Reader, tcpMessenger, tcpSync);
    bridgeHandler.BlockingRun();
    bridgeHandler.Cleanup();
});
bridgeHandler.Start();

// block until everything properly shuts down
void WaitForShutdown()
{
    rlbotServer.Join();
    Console.WriteLine("RLBot server has shut down successfully.");

    bridgeWriter.TryWrite(BridgeMessage.Stop());
    bridgeWriter.TryComplete();
    bridgeHandler.Join();
    Console.WriteLine("Bridge handler has shut down successfully.");
}

// catch sudden termination to clean up the server
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    serverWriter.TryComplete();

    WaitForShutdown();
    Console.WriteLine("Core has shut down successfully.");
};

// catch ctrl+c to clean up the server
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("Core is shutting down...");
    serverWriter.TryComplete();

    WaitForShutdown();
    Console.WriteLine("Core has shut down successfully.");
};

// wait for a normal shutdown
WaitForShutdown();
