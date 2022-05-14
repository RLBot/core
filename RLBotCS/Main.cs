// See https://aka.ms/new-console-template for more information
using RLBotCS.GameState;
using RLBotCS.Server;
using RLBotModels.Message;
using RLBotSecret.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.TCP;

var converter = new Converter();
var port = 23233;

var messenger = new TcpMessenger(port);
var gotFirstMessage = false;

Console.WriteLine("RLBot is waiting for Rocket League to connect on port " + port);

var playerInputSender = new PlayerInputSender(messenger);
var gameState = new GameState();

var flatbufferServer = new FlatbufferServer(23234, messenger, gameState.playerMapping);
var serverListenerThread = new Thread(() => flatbufferServer.StartListener());
serverListenerThread.Start();

foreach (var messageClump in messenger)
{
    if (!gotFirstMessage)
    {
        Console.WriteLine("RLBot is now receiving messages from Rocket League!");
        gotFirstMessage = true;
    }

    var messageBundle = converter.Convert(messageClump);
    gameState.applyMessage(messageBundle);

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