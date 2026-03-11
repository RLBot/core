using Bridge.State;
using RLBot.Flat;

using Deltas = (float GameTimeDelta, float ArrivalDelta);

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

    private readonly CircularBuffer<Deltas> _rlbotSamples = new(_maxSamples);
    private readonly SortedDictionary<string, CircularBuffer<bool>> _samples = new();
    private float time = 0;

    public void AddRLBotSample(Deltas deltas)
    {
        _rlbotSamples.AddLast(deltas);
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

    public static float GetPercentile(IEnumerable<float> data, float p)
    {
        var sorted = data.OrderBy(x => x).ToList();
        int n = sorted.Count;
        if (n == 0) return default;

        double rank = p * (n - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);

        return (float)(sorted[lower] + (rank - lower) * (sorted[upper] - sorted[lower]));
    }

    public void RenderSummary(Rendering rendering, GameState gameState, float deltaTime)
    {
        time += deltaTime;
        if (time < _timeSkip)
            return;
        time = 0;


        var arrivalDeltas = _rlbotSamples.Iter().Select(t => t.ArrivalDelta);
        var gameTimeDeltas = _rlbotSamples.Iter().Select(t => t.GameTimeDelta);

        float averageTickDelta = gameTimeDeltas.Sum() / _maxSamples;
        float averageTickRate = 1f / averageTickDelta;

        // Find deltas larger than expected at 60hz, allowing 10% margin
        float misses60 = arrivalDeltas
            .Count(d =>
                (d - (1f / 60f)) > (0.1f / 60f));

        // Find deltas larger than expected at 120hz, allowing 10% margin
        float misses120 = arrivalDeltas
            .Count(d =>
                (d - (1f / 120f)) > (0.1f / 120f));

        string message = $"""
        RLBot @ {averageTickRate:0}hz {(1f - misses60 / 120f) * 100f:0}%|{(1f - misses120 / 120f) * 100f:0}%
         p95 {GetPercentile(arrivalDeltas, 0.95f) * 1000f:0.0}ms p99 {GetPercentile(arrivalDeltas, 0.99f) * 1000f:0.0}ms
        """;
        bool shouldRender = misses120 > 1;

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
            X = 10f / Rendering.ResolutionWidthPixels,
            Y = 200f / Rendering.ResolutionHeightPixels,
            Text = message,
            Foreground = TextColor,
            Background = BackColor,
            Scale = 1,
            HAlign = TextHAlign.Left,
            VAlign = TextVAlign.Top,
        };

        var renderMessages = new List<RenderMessageT>()
        {
            new RenderMessageT() { Variety = RenderTypeUnion.FromString2D(renderText) },
        };

        if (shouldRender)
            rendering.AddRenderGroup(ClientId, RenderGroupId, renderMessages, gameState);
        else
            rendering.RemoveRenderGroup(ClientId, RenderGroupId);
    }

    public void ClearAll() => _samples.Clear();
}
