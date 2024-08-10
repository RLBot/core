using Bridge.State;
using rlbot.flat;

namespace RLBotCS.ManagerTools;

// https://github.com/RLBotPrivate/RLBot/blob/psyonix/src/main/cpp/RLBotInterface/src/RLBot/Performance/PerformanceMonitor.cpp
public class PerfMonitor
{
    private const int ClientId = 0;
    private const int RenderGroupId = 1;
    private const int _maxSamples = 120;
    private const int _tickSkip = 60;

    private static readonly ColorT TextColor = new ColorT()
    {
        A = 255,
        R = 255,
        G = 255,
        B = 255,
    };
    private static readonly ColorT BackColor = new ColorT()
    {
        A = 100,
        R = 0,
        G = 0,
        B = 0,
    };

    private readonly SortedDictionary<string, LinkedList<bool>> _samples = new();
    private int tick = 0;

    public void AddSample(string name, bool gotInput)
    {
        if (!_samples.ContainsKey(name))
        {
            _samples[name] = new();
        }

        _samples[name].AddLast(gotInput);

        if (_samples[name].Count > _maxSamples)
        {
            _samples[name].RemoveFirst();
        }
    }

    public void RemoveBot(string name)
    {
        _samples.Remove(name);
    }

    public void RenderSummary(Rendering rendering, GameState gameState)
    {
        tick = (tick + 1) % _tickSkip;
        if (tick != 0)
            return;

        string message = "RLBot";
        bool shouldRender = false;

        foreach (var (name, samples) in _samples)
        {
            int gotInputCount = samples.Count(sample => sample);
            float gotInputPercentage = (float)gotInputCount / samples.Count;

            message += $"\n{name}: {gotInputPercentage * 100:0.0}%";

            if (gotInputPercentage < 0.999)
                shouldRender = true;
        }

        var renderText = new String2DT()
        {
            Y = 10f / 1920f,
            X = 200f / 1080f,
            Text = message,
            Foreground = TextColor,
            Background = BackColor,
            Scale = 1,
            HAlign = TextHAlign.Left,
            VAlign = TextVAlign.Top,
        };

        var renderMessages = new List<RenderMessageT>()
        {
            new RenderMessageT() { Variety = RenderTypeUnion.FromString2D(renderText), },
        };

        if (shouldRender)
            rendering.AddRenderGroup(ClientId, RenderGroupId, renderMessages, gameState);
        else
            rendering.RemoveRenderGroup(ClientId, RenderGroupId);
    }

    public void ClearAll() => _samples.Clear();
}
