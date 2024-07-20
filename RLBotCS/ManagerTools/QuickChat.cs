using Bridge.State;
using rlbot.flat;

namespace RLBotCS.ManagerTools;

public class QuickChat
{
    private static readonly int _clientId = 0;
    private static readonly int _renderGroupId = 0;

    private static readonly int _maxDisplayMessages = 8;
    private static readonly int _chatDisplaySeconds = 5;

    private static readonly ColorT _orangeColor =
        new()
        {
            A = 255,
            R = 255,
            G = 200,
            B = 80
        };
    private static readonly ColorT _blueColor =
        new()
        {
            A = 255,
            R = 120,
            G = 200,
            B = 255
        };
    private static readonly ColorT _backgroundColor =
        new()
        {
            A = 150,
            R = 0,
            G = 0,
            B = 0
        };

    private readonly LinkedList<(float, MatchCommT)> _chats = new();
    private bool _hasUpdate = false;

    public void AddChat(MatchCommT matchComm, float gameTime)
    {
        if (matchComm.Display == null)
            return;

        _chats.AddLast((gameTime, matchComm));
        _hasUpdate = true;

        if (_chats.Count > _maxDisplayMessages)
            _chats.RemoveFirst();
    }

    private void RemoveOldChats(float gameTime)
    {
        while (_chats.Count > 0 && _chats.First.Value.Item1 + _chatDisplaySeconds < gameTime)
        {
            _hasUpdate = true;
            _chats.RemoveFirst();
        }
    }

    public void RenderChats(Rendering rendering, GameState gameState)
    {
        RemoveOldChats(gameState.SecondsElapsed);

        // Prevent excessive render calls
        if (!_hasUpdate)
            return;
        _hasUpdate = false;

        int xVal = 10;
        List<RenderMessageT> renderMessages = new();

        foreach (var chat in _chats)
        {
            var textColor = chat.Item2.Team == 0 ? _blueColor : _orangeColor;

            String2DT message =
                new()
                {
                    Text = chat.Item2.Display,
                    Y = 10f / 1920f,
                    X = xVal / 1080f,
                    Scale = 1,
                    Foreground = textColor,
                    Background = _backgroundColor,
                    HAlign = TextHAlign.Left,
                    VAlign = TextVAlign.Top,
                };

            renderMessages.Add(
                new RenderMessageT() { Variety = RenderTypeUnion.FromString2D(message), }
            );

            xVal += 20;
        }

        if (renderMessages.Count > 0)
            rendering.AddRenderGroup(_clientId, _renderGroupId, renderMessages, gameState);
        else
            rendering.RemoveRenderGroup(_clientId, _renderGroupId);
    }

    public void ClearChats() => _chats.Clear();
}
