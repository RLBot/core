﻿using System.Threading.Channels;
using RLBotCS.ManagerTools;
using RLBotCS.Server;
using RLBotCS.Server.FlatbuffersMessage;
using RLBotSecret.TCP;

int gamePort = Launching.FindUsableGamePort();
Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + gamePort);

// Setup the handler to use bridge to talk with the game
Channel<IBridgeMessage> bridgeChannel = Channel.CreateUnbounded<IBridgeMessage>();
ChannelWriter<IBridgeMessage> bridgeWriter = bridgeChannel.Writer;

// Setup the tcp server for rlbots
Channel<IServerMessage> serverChannel = Channel.CreateUnbounded<IServerMessage>();
ChannelWriter<IServerMessage> serverWriter = serverChannel.Writer;

Thread rlbotServer = new Thread(() =>
{
    MatchStarter matchStarter = new MatchStarter(bridgeWriter, gamePort);
    FlatBuffersServer flatBuffersServer =
        new(Launching.RLBotSocketsPort, serverChannel, matchStarter, bridgeWriter);

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

Thread bridgeHandler = new Thread(() =>
{
    TcpMessenger tcpMessenger = new TcpMessenger(gamePort);
    BridgeHandler bridgeHandler = new BridgeHandler(serverWriter, bridgeChannel.Reader, tcpMessenger);

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

// block until everything properly shuts down
void WaitForShutdown()
{
    rlbotServer.Join();
    Console.WriteLine("RLBot server has shut down successfully.");

    bridgeWriter.TryWrite(new Stop());
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
