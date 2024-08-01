using Bridge.State;
using rlbot.flat;

namespace RLBotCS.ManagerTools;

public class QuickChat
{
    private const int ClientId = 0;
    private const int RenderGroupId = 0;

    private const int MaxDisplayMessages = 8;
    private const int ChatDisplaySeconds = 5;

    private static readonly ColorT OrangeColor =
        new()
        {
            A = 255,
            R = 255,
            G = 200,
            B = 80
        };
    private static readonly ColorT BlueColor =
        new()
        {
            A = 255,
            R = 120,
            G = 200,
            B = 255
        };
    private static readonly ColorT BackgroundColor =
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

        if (_chats.Count > MaxDisplayMessages)
            _chats.RemoveFirst();
    }

    private void RemoveOldChats(float gameTime)
    {
        while (
            _chats.First != null
                ? _chats.First.Value.Item1 + ChatDisplaySeconds < gameTime
                : false
        )
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
            var textColor = chat.Item2.Team == 0 ? BlueColor : OrangeColor;

            String2DT message =
                new()
                {
                    Text = chat.Item2.Display,
                    Y = 10f / 1920f,
                    X = xVal / 1080f,
                    Scale = 1,
                    Foreground = textColor,
                    Background = BackgroundColor,
                    HAlign = TextHAlign.Left,
                    VAlign = TextVAlign.Top,
                };

            renderMessages.Add(
                new RenderMessageT() { Variety = RenderTypeUnion.FromString2D(message), }
            );

            xVal += 20;
        }

        if (renderMessages.Count > 0)
            rendering.AddRenderGroup(ClientId, RenderGroupId, renderMessages, gameState);
        else
            rendering.RemoveRenderGroup(ClientId, RenderGroupId);
    }

    public void ClearChats() => _chats.Clear();
}
