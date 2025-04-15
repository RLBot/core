﻿using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

record StopMatch(bool ShutdownServer) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (ShutdownServer)
        {
            context.IncomingMessagesWriter.TryComplete();
            return ServerAction.Stop;
        }

        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = false;
        context.LastTickPacket = null;
        context.MatchConfig = null;
        context.Bridge.TryWrite(new ClearRenders());
        context.Bridge.TryWrite(new EndMatch());

        foreach (var (writer, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(ShutdownServer));

        return ServerAction.Continue;
    }
}
