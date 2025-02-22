using Bridge.State;
using rlbot.flat;

namespace RLBotCS.ManagerTools;

public class QuickChat
{
    private const int ClientId = 0;
    private const int RenderGroupId = 0;

    private const int MaxDisplayMessages = 8;
    private const int ChatDisplaySeconds = 5;

    private static readonly ColorT OrangeColor = new()
    {
        A = 255,
        R = 255,
        G = 200,
        B = 80,
    };
    private static readonly ColorT BlueColor = new()
    {
        A = 255,
        R = 120,
        G = 200,
        B = 255,
    };
    private static readonly ColorT BackgroundColor = new()
    {
        A = 150,
        R = 0,
        G = 0,
        B = 0,
    };

    private readonly LinkedList<(float, string, MatchCommT)> _chats = new();
    private bool _hasUpdate = false;

    public void AddChat(MatchCommT matchComm, string name, float gameTime)
    {
        if (matchComm.Display == null)
            return;

        _chats.AddLast((gameTime, name, matchComm));
        _hasUpdate = true;

        if (_chats.Count > MaxDisplayMessages)
            _chats.RemoveFirst();
    }

    private void RemoveOldChats(float gameTime)
    {
        while (
            _chats.First != null && _chats.First.Value.Item1 + ChatDisplaySeconds < gameTime
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

        float yVal = 10f;
        List<RenderMessageT> renderMessages = new();

        foreach (var (_, name, chat) in _chats)
        {
            var textColor = chat.Team == 0 ? BlueColor : OrangeColor;

            String2DT message = new()
            {
                Text = name + ": " + chat.Display,
                X = 10f / Rendering.ResolutionWidthPixels,
                Y = yVal / Rendering.ResolutionHeightPixels,
                Scale = 1,
                Foreground = textColor,
                Background = BackgroundColor,
                HAlign = TextHAlign.Left,
                VAlign = TextVAlign.Top,
            };

            renderMessages.Add(
                new RenderMessageT() { Variety = RenderTypeUnion.FromString2D(message) }
            );

            yVal += Rendering.FontHeightPixels;
        }

        if (renderMessages.Count > 0)
            rendering.AddRenderGroup(ClientId, RenderGroupId, renderMessages, gameState);
        else
            rendering.RemoveRenderGroup(ClientId, RenderGroupId);
    }

    public void ClearChats() => _chats.Clear();
}
