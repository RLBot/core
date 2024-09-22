using Bridge.State;
using rlbot.flat;

namespace RLBotCS.ManagerTools;

public class PerfMonitor
{
    private const int ClientId = 0;
    private const int RenderGroupId = 1;
    private const int _maxSamples = 120;
    private const float _timeSkip = 0.5f;

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

    private readonly CircularBuffer<float> _rlbotSamples = new(_maxSamples);
    private readonly SortedDictionary<string, CircularBuffer<bool>> _samples = new();
    private float time = 0;

    public void AddRLBotSample(float timeDiff)
    {
        _rlbotSamples.AddLast(timeDiff);
    }

    public void AddSample(string name, bool gotInput)
    {
        if (!_samples.ContainsKey(name))
        {
            _samples[name] = new(_maxSamples);
        }

        _samples[name].AddLast(gotInput);
    }

    public void RemoveBot(string name)
    {
        _samples.Remove(name);
    }

    public void RenderSummary(Rendering rendering, GameState gameState, float deltaTime)
    {
        time += deltaTime;
        if (time < _timeSkip)
            return;
        time = 0;

        float averageTimeDiff = _rlbotSamples.Count > 0 ? _rlbotSamples.Iter().Average() : 1;
        float timeDiffPercentage = 1 / (120f * averageTimeDiff);

        string message = $"RLBot: {timeDiffPercentage * 100:0.0}%";
        bool shouldRender = timeDiffPercentage < 0.9999 || timeDiffPercentage > 1.0001;

        foreach (var (name, samples) in _samples)
        {
            int gotInputCount = samples.Iter().Count(sample => sample);
            float gotInputPercentage = (float)gotInputCount / samples.Count;

            message += $"\n{name}: {gotInputPercentage * 100:0.0}%";

            if (gotInputPercentage < 0.9999 || gotInputPercentage > 1.0001)
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
