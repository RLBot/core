﻿using System.Threading.Channels;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using RLBotCS.ManagerTools;
using RLBotCS.Server;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.FlatbuffersMessage;

if (args.Length > 0 && args[0] == "--version")
{
    Console.WriteLine("RLBotServer v5.beta.4.5");
    Environment.Exit(0);
}

var logger = Logging.GetLogger("Main");

int rlbotSocketsPort;
int gamePort;

try
{
    // Parse RLBot sockets port
    rlbotSocketsPort = args.Length > 0 ? int.Parse(args[0]) : LaunchManager.RlbotSocketsPort;

    // Validate RLBot sockets port
    if (rlbotSocketsPort < 0 || rlbotSocketsPort > 65535)
    {
        throw new ArgumentOutOfRangeException(
            nameof(rlbotSocketsPort),
            "Port number must be between 0 and 65535."
        );
    }

    // Parse game port
    gamePort =
        args.Length > 1
            ? int.Parse(args[1])
            : LaunchManager.FindUsableGamePort(rlbotSocketsPort);

    // Validate game port
    if (gamePort < 0 || gamePort > 65535)
    {
        throw new ArgumentOutOfRangeException(
            nameof(gamePort),
            "Port number must be between 0 and 65535."
        );
    }
}
catch (ArgumentOutOfRangeException ex)
{
    logger.LogError(ex.Message);
    return;
}

logger.LogInformation("Server will start on port " + rlbotSocketsPort);
logger.LogInformation("Waiting for Rocket League to connect on port " + gamePort);

// Set up the handler to use bridge to talk with the game
var bridgeChannel = Channel.CreateUnbounded<IBridgeMessage>();
var bridgeWriter = bridgeChannel.Writer;

// Set up the TCP server for RLBots
var serverChannel = Channel.CreateUnbounded<IServerMessage>();
var serverWriter = serverChannel.Writer;

Thread rlbotServer = new(() =>
{
    MatchStarter matchStarter = new(bridgeWriter, gamePort, rlbotSocketsPort);
    FlatBuffersServer flatBuffersServer = new(
        rlbotSocketsPort,
        serverChannel,
        matchStarter,
        bridgeWriter
    );

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
