// See https://aka.ms/new-console-template for more information
using RLBotCS.GameState;
using RLBotCS.Server;
using RLBotModels.Message;
using RLBotSecret.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.TCP;
using RLBotCS.GameControl;

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
    gameState.gameTickPacket.worldGravityZ = matchStarter.GetGravity();
    gameState.applyMessage(messageBundle);

    // this helps to wait for a new map to load 
    if (gameState.NotMatchEnded())
    {
        matchStarter.applyMessageBundle(messageBundle);
    }

    flatbufferServer.SendGameStateToClients(gameState);

    MessAroundToProveThingsWork(playerInputSender, gameState, messageBundle);
}

void MessAroundToProveThingsWork(PlayerInputSender playerInputSender, GameState gameState, MessageBundle messageBundle)
{
    foreach (var message in messageBundle.messages)
    {
        if (message is CarSpawn)
        {
            Console.WriteLine(((CarSpawn)message).name + " has spawned.");
        }
        if (message is SpectateViewChange)
        {
            var actorId = ((SpectateViewChange)message).spectatedActorId;

            playerInputSender.SendPlayerInput(new RLBotModels.Control.PlayerInput()
            {
                actorId = actorId,
                carInput = new RLBotModels.Control.CarInput { jump = true }
            });
            foreach (var otherActor in gameState.playerMapping.getKnownPlayers())
            {
                if (otherActor.actorId != actorId)
                {
                    playerInputSender.SendPlayerInput(new RLBotModels.Control.PlayerInput()
                    {
                        actorId = otherActor.actorId,
                        carInput = new RLBotModels.Control.CarInput { jump = false }
                    });
                }
            }
            
        }
    }
}