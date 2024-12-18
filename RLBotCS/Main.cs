using System.Threading.Channels;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using RLBotCS.ManagerTools;
using RLBotCS.Server;
using RLBotCS.Server.FlatbuffersMessage;

var logger = Logging.GetLogger("Main");

int rlbotSocketsPort;
try
{
    rlbotSocketsPort = args.Length > 0 ? int.Parse(args[0]) : LaunchManager.RlbotSocketsPort;

    // additional validation to ensure it's a valid port number
    if (rlbotSocketsPort < 0)
    {
        throw new FormatException();
    }
    else if (rlbotSocketsPort > 65535)
    {
        throw new OverflowException();
    }
}
catch (FormatException)
{
    logger.LogError("Invalid port number provided. Please provide a valid port number.");
    return;
}
catch (OverflowException)
{
    logger.LogError("Port number provided is too large. Please provide a valid port number.");
    return;
}

logger.LogInformation("Server will start on port " + rlbotSocketsPort);

int gamePort = LaunchManager.FindUsableGamePort(rlbotSocketsPort);
logger.LogInformation("Waiting for Rocket League to connect on port " + gamePort);

// Set up the handler to use bridge to talk with the game
var bridgeChannel = Channel.CreateUnbounded<IBridgeMessage>();
var bridgeWriter = bridgeChannel.Writer;

// Set up the TCP server for RLBots
var serverChannel = Channel.CreateUnbounded<IServerMessage>();
var serverWriter = serverChannel.Writer;

Thread rlbotServer =
    new(() =>
    {
        MatchStarter matchStarter = new(bridgeWriter, gamePort, rlbotSocketsPort);
        FlatBuffersServer flatBuffersServer =
            new(rlbotSocketsPort, serverChannel, matchStarter, bridgeWriter);

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

Thread bridgeHandler =
    new(() =>
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
void WaitForShutdown(bool log = true)
{
    rlbotServer.Join();
    if (log)
        logger.LogInformation("TCP handler has shut down successfully.");

    bridgeWriter.TryComplete();

    bridgeHandler.Join();
    if (log)
        logger.LogInformation("Bridge handler has shut down successfully.");
}

void Terminate()
{
    logger.LogInformation("Shutting down server...");
    serverWriter.TryComplete();
    WaitForShutdown();
    logger.LogInformation("Server shut down successfully.");
}

// Catch sudden termination to clean up the server
AppDomain.CurrentDomain.ProcessExit += (_, _) => Terminate();

// Catch Ctrl+C to clean up the server
Console.CancelKeyPress += (_, _) => Terminate();

// Wait for a normal shutdown
WaitForShutdown(false);
