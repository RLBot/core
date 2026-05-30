using System.Collections.Immutable;

namespace RLBotCS.ManagerTools;

public static class RectUtil
{
    /// <summary>
    /// Defines the maximum represented aspect ratio (<tt>Limit</tt>/1)
    /// as well as the granularity of stored ratios.<br/>
    /// Number of entries and the total resulting size in memory
    /// for different <tt>Limit</tt> values are as follows:
    /// <code>
    /// ┌───────┬─────────┬────────────┐
    /// │ Limit │ entries │    size    │
    /// ├───────┼─────────┼────────────┤
    /// │   512 │   1174  │   9.17 kiB │
    /// │  1024 │   2563  │  20.02 kiB │
    /// │  2048 │   5555  │  43.4  kiB │
    /// │  4096 │  11977  │  93.57 kiB │
    /// │  8192 │  25673  │ 200.57 kiB │
    /// │ 16384 │  54778  │ 427.95 kiB │
    /// └───────┴─────────┴────────────┘
    /// </code>
    /// </summary>
    public const ushort Limit = 4096;

    private const float HalfPrecisionRangeHigh = 4096f;
    private const float HalfPrecisionRangeLow = 1.0f / HalfPrecisionRangeHigh;

    private static readonly ImmutableArray<float> ratios;
    private static readonly ImmutableArray<(ushort, ushort)> rects;

    static RectUtil()
    {
        SortedDictionary<float, (ushort, ushort)> dictionary = [];

        static int Gcd(int a, int b)
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

        for (ushort a = 1; a <= Limit; ++a)
        for (ushort b = 1; b <= a && a * b <= Limit; ++b)
            if (Gcd(a, b) == 1)
                dictionary.Add((float)a / b, (a, b));

        ratios = [.. dictionary.Keys];
        rects = [.. dictionary.Values];
    }

    private static float GeoMean(float a, float b)
    {
        if (
            a >= HalfPrecisionRangeHigh
            || b >= HalfPrecisionRangeHigh
            || a <= HalfPrecisionRangeLow
            || b <= HalfPrecisionRangeLow
        )
            return MathF.Sqrt(a) * MathF.Sqrt(b);
        return MathF.Sqrt(a * b);
    }

    private static bool LessThanGeoMean(float x, (float a, float b) m)
    {
        if (m.b >= HalfPrecisionRangeHigh)
            return x < MathF.Sqrt(m.a) * MathF.Sqrt(m.b);
        return x * x < m.a * m.b;
    }

    private static (ushort, ushort) Find(float value)
    {
        int higherIdx = ratios.BinarySearch(value);

        if (higherIdx >= 0)
            return rects[higherIdx];

        higherIdx = ~higherIdx;

        // No need to handle this because value >= 1.0 == ratios.First()
        //if (higherIdx == 0)
        //    return rects.First();

        if (higherIdx == ratios.Length)
            return rects.Last();

        int lowerIdx = higherIdx - 1;
        return rects[
            LessThanGeoMean(value, (ratios[lowerIdx], ratios[higherIdx]))
                ? lowerIdx
                : higherIdx
        ];
    }

    private static (ushort cols, ushort rows) Find(float width, float height)
    {
        if (width >= height)
            return Find(width / height);

        (ushort rows, ushort cols) = Find(height / width);
        return (cols, rows);
    }

    /// <summary>
    /// Approximates the rectangle <tt>width</tt>×<tt>height</tt> with <tt>cols</tt>×<tt>rows</tt>
    /// rectangles with dimensions <tt>elementWidth</tt>×<tt>elementHeight</tt> scaled by <tt>scale</tt>.
    /// </summary>
    public static (ushort cols, ushort rows, float scale) ApproximateRect(
        int width,
        int height,
        int elementWidth,
        int elementHeight
    )
    {
        float elementsInWidth = (float)width / elementWidth;
        float elementsInHeight = (float)height / elementHeight;
        (ushort cols, ushort rows) = Find(elementsInWidth, elementsInHeight);

        // Ideal horizontal and vertical scale are
        // ((float)width / cols) / elementWidth == ((float)width / elementWidth) / cols == elementsInWidth / cols
        // ((float)height / rows) / elementHeight == ((float)height / elementHeight) / rows == elementsInHeight / rows
        return (cols, rows, GeoMean(elementsInWidth / cols, elementsInHeight / rows));
    }
}
