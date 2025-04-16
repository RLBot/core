using System.Text;
using Bridge.Controller;
using Bridge.State;
using Bridge.TCP;
using rlbot.flat;
using RLBotCS.Conversion;
using Color = System.Drawing.Color;

namespace RLBotCS.ManagerTools;

public class Rendering(TcpMessenger tcpMessenger)
{
    private const int MaxClearsPerTick = 1024;

    public const int ResolutionWidthPixels = 1920;
    public const int ResolutionHeightPixels = 1080;
    public const int FontWidthPixels = 10;
    public const int FontHeightPixels = 20;

    private readonly RenderingSender _renderingSender = new(tcpMessenger);
    private readonly Dictionary<int, Dictionary<int, List<ushort>>> _clientRenderTracker = [];

    private readonly Queue<ushort> _RenderClearQueue = new();

    private int _numClears = 0;

    private ushort RenderItem(RenderTypeUnion renderItem, GameState gameState) =>
        renderItem.Value switch
        {
            Line3DT { Start: var start, End: var end, Color: var color } =>
                _renderingSender.AddLine3D(
                    FlatToModel.ToRenderAnchor(start, gameState),
                    FlatToModel.ToRenderAnchor(end, gameState),
                    FlatToModel.ToColor(color)
                ),
            PolyLine3DT { Points: var points, Color: var color } =>
                _renderingSender.AddLine3DSeries(
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
            } => _renderingSender.AddText2D(
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
            } => _renderingSender.AddText3D(
                text,
                FlatToModel.ToRenderAnchor(anchor, gameState),
                FlatToModel.ToColor(foreground),
                FlatToModel.ToColor(background),
                (byte)hAlign,
                (byte)vAlign,
                scale
            ),
            Rect2DT rect2Dt => SendRect2D(rect2Dt),
            Rect3DT rect3Dt => SendRect3D(rect3Dt, gameState),
            _ => throw new NotImplementedException("Unknown RenderMessage"),
        };

    private ushort SendRect2D(Rect2DT rect2Dt)
    {
        // Move rect left/up when width/height is negative
        var adjustedX = rect2Dt.Width < 0 ? rect2Dt.X - rect2Dt.Width : rect2Dt.X;
        var adjustedY = rect2Dt.Height < 0 ? rect2Dt.Y - rect2Dt.Height : rect2Dt.Y;

        // Fake a filled rectangle using a string with colored background
        var (text, scale) = MakeFakeRectangleString(
            (int)Math.Abs(rect2Dt.Width * ResolutionWidthPixels),
            (int)Math.Abs(rect2Dt.Height * ResolutionHeightPixels)
        );

        return _renderingSender.AddText2D(
            text,
            adjustedX,
            adjustedY,
            Color.Transparent, // Foreground
            FlatToModel.ToColor(rect2Dt.Color), // Background
            (byte)rect2Dt.HAlign,
            (byte)rect2Dt.VAlign,
            scale
        );
    }

    private ushort SendRect3D(Rect3DT rect3Dt, GameState gameState)
    {
        // Fake a filled rectangle using a string with colored background
        var (text, scale) = MakeFakeRectangleString(
            (int)Math.Abs(rect3Dt.Width * ResolutionWidthPixels),
            (int)Math.Abs(rect3Dt.Height * ResolutionHeightPixels)
        );

        return _renderingSender.AddText3D(
            text,
            FlatToModel.ToRenderAnchor(rect3Dt.Anchor, gameState),
            Color.Transparent,
            FlatToModel.ToColor(rect3Dt.Color),
            (byte)TextHAlign.Center,
            (byte)TextVAlign.Center,
            scale
        );
    }

    /// <summary>
    /// Computes a string in the shape of a rectangle. The rectangle has the given width and height in pixels when
    /// scaled the string is scaled with  returned scaling factor. We use this as a hack to created filled rectangles
    /// for rectangle rendering.
    /// </summary>
    /// <returns></returns>
    private (string, float) MakeFakeRectangleString(int width, int height)
    {
        int Gcd(int a, int b)
        {
            // Greatest common divisor by Euclidean algorithm https://stackoverflow.com/a/41766138
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }

        // We use the greatest common divisor to simplify the fraction (width/height)
        // minimizing the characters needed for the rectangle.
        int gcd = Gcd(width, height);
        int cols = (width / gcd) * (FontHeightPixels / FontWidthPixels);
        int rows = height / gcd;

        StringBuilder str = new StringBuilder(cols * rows + rows);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                str.Append(' ');
            }

            if (r + 1 < rows)
            {
                str.Append('\n');
            }
        }

        return (str.ToString(), gcd / (float)FontHeightPixels);
    }

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
        {
            renderGroup.Add(RenderItem(renderItem.Variety, gameState));
        }

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
            ClearRender(renderItem);

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
            ClearRender(renderItem);

        // Remove the client from the tracker
        _clientRenderTracker.Remove(clientId);
    }

    public void ClearAllRenders()
    {
        _renderingSender.AddFlushPersistentDebugLines();

        foreach (var clientRenders in _clientRenderTracker.Values)
        foreach (int renderId in clientRenders.Keys)
        foreach (ushort renderItem in clientRenders[renderId])
            ClearRender(renderItem);

        _clientRenderTracker.Clear();
    }

    public void ResetClearCount()
    {
        _numClears = 0;
    }

    private void ClearRender(ushort id)
    {
        if (_numClears < MaxClearsPerTick)
        {
            _renderingSender.RemoveRenderItem(id);
            _numClears++;
            return;
        }

        // If we've reached the limit, queue it up to clear the next tick
        _RenderClearQueue.Enqueue(id);
    }

    public bool SendRenderClears()
    {
        while (_RenderClearQueue.Count > 0 && _numClears < MaxClearsPerTick)
        {
            var renderItem = _RenderClearQueue.Dequeue();
            _renderingSender.RemoveRenderItem(renderItem);
            _numClears++;
        }

        _renderingSender.Send();

        return _RenderClearQueue.Count == 0;
    }
}
