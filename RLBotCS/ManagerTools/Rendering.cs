using Bridge.Controller;
using Bridge.State;
using Bridge.TCP;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.ManagerTools;

public class Rendering(TcpMessenger tcpMessenger)
{
    private static int MaxClearsPerTick = 1024;

    private readonly RenderingSender _renderingSender = new(tcpMessenger);
    private readonly Dictionary<int, Dictionary<int, List<ushort>>> _clientRenderTracker = [];

    private readonly Queue<ushort> _RenderClearQueue = new();

    private ushort? RenderItem(RenderTypeUnion renderItem, GameState gameState) =>
        renderItem.Value switch
        {
            Line3DT { Start: var start, End: var end, Color: var color }
                => _renderingSender.AddLine3D(
                    FlatToModel.ToRenderAnchor(start, gameState),
                    FlatToModel.ToRenderAnchor(end, gameState),
                    FlatToModel.ToColor(color)
                ),
            PolyLine3DT { Points: var points, Color: var color }
                => _renderingSender.AddLine3DSeries(
                    points.Select(FlatToModel.ToVectorFromT).ToList(),
                    FlatToModel.ToColor(color)
                ),
            String2DT
            {
                Text: var text,
                X: var x,
                Y: var y,
                Foreground: var foreground,
                Background: var background,
                HAlign: var hAlign,
                VAlign: var vAlign,
                Scale: var scale
            }
                => _renderingSender.AddText2D(
                    text,
                    x,
                    y,
                    FlatToModel.ToColor(foreground),
                    FlatToModel.ToColor(background),
                    (byte)hAlign,
                    (byte)vAlign,
                    scale
                ),
            String3DT
            {
                Text: var text,
                Anchor: var anchor,
                Foreground: var foreground,
                Background: var background,
                HAlign: var hAlign,
                VAlign: var vAlign,
                Scale: var scale
            }
                => _renderingSender.AddText3D(
                    text,
                    FlatToModel.ToRenderAnchor(anchor, gameState),
                    FlatToModel.ToColor(foreground),
                    FlatToModel.ToColor(background),
                    (byte)hAlign,
                    (byte)vAlign,
                    scale
                ),
            _ => null
        };

    public void AddRenderGroup(
        int clientId,
        int renderId,
        List<RenderMessageT> renderItems,
        GameState gameState
    )
    {
        var clientRenders = _clientRenderTracker.GetValueOrDefault(clientId, []);

        // Clear the previous render group, if any
        RemoveRenderGroup(clientId, renderId);

        List<ushort> renderGroup = [];
        foreach (RenderMessageT renderItem in renderItems)
            if (RenderItem(renderItem.Variety, gameState) is { } renderItemId)
                renderGroup.Add(renderItemId);

        _renderingSender.Send();

        // Add to the tracker
        clientRenders[renderId] = renderGroup;
        _clientRenderTracker[clientId] = clientRenders;
    }

    public void RemoveRenderGroup(int clientId, int renderId)
    {
        if (!_clientRenderTracker.TryGetValue(clientId, out var clientRenders))
            return;

        if (!clientRenders.TryGetValue(renderId, out var renderItems))
            return;

        foreach (ushort renderItem in renderItems)
            _RenderClearQueue.Enqueue(renderItem);

        // Remove the renderId from the client
        clientRenders.Remove(renderId);
    }

    public void ClearClientRenders(int clientId)
    {
        if (!_clientRenderTracker.TryGetValue(clientId, out var clientRenders))
            return;

        // Tell the game to remove all the renders
        foreach (int renderId in clientRenders.Keys)
        foreach (ushort renderItem in clientRenders[renderId])
            _RenderClearQueue.Enqueue(renderItem);

        // Remove the client from the tracker
        _clientRenderTracker.Remove(clientId);
    }

    public void ClearAllRenders()
    {
        foreach (var (clientId, clientRenders) in _clientRenderTracker)
        foreach (int renderId in clientRenders.Keys)
        foreach (ushort renderItem in clientRenders[renderId])
            _RenderClearQueue.Enqueue(renderItem);

        _clientRenderTracker.Clear();
    }

    public bool SendRenderClears()
    {
        if (_RenderClearQueue.Count == 0)
            return true;

        Console.WriteLine($"{_RenderClearQueue.Count}");

        int clears = 0;
        while (_RenderClearQueue.Count > 0 && clears < MaxClearsPerTick)
        {
            var renderItem = _RenderClearQueue.Dequeue();
            _renderingSender.RemoveRenderItem(renderItem);
            clears++;
        }

        _renderingSender.Send();

        return _RenderClearQueue.Count == 0;
    }
}
