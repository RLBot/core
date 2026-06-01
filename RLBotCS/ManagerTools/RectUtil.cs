using System.Collections.Immutable;

namespace RLBotCS.ManagerTools;

public static class RectUtil
{
    /// <summary>
    /// The maximum number of subdivisions of the [0,1] interval to store.
    /// The maximum possible resulting number of entries is ⌈<tt>MaxSubdivisions</tt>/2⌉,
    /// but only those whose sum of the numerator and denominator does
    /// not excede <tt>Rendering.RectangleStringMaxLength</tt> are included,
    /// so ideally this should be a highly composite number.<br/>
    /// For 55440, there are 17635 entries, corresponding to 137.77 kiB of memory.
    /// </summary>
    private const ushort MaxSubdivisions = 55440;

    private const float HalfPrecisionRangeHigh = 4096f;
    private const float HalfPrecisionRangeLow = 1.0f / HalfPrecisionRangeHigh;

    private static readonly ImmutableArray<float> ratios;
    private static readonly ImmutableArray<(ushort, ushort)> rects;

    static RectUtil()
    {
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

        SortedDictionary<float, (ushort, ushort)> dictionary = [];
        float fMaxSubdivisions = MaxSubdivisions;
        for (ushort i = MaxSubdivisions / 2 + MaxSubdivisions % 2; i <= MaxSubdivisions; ++i)
        {
            ushort gcd = (ushort)Gcd(i, MaxSubdivisions);
            ushort num = (ushort)(i / gcd);
            ushort den = (ushort)(MaxSubdivisions / gcd);
            if (num + den <= Rendering.RectangleStringMaxLength)
                dictionary.Add(i / fMaxSubdivisions, (num, den));
        }

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

    private static (ushort, ushort) FindImpl(float value)
    {
        int higherIdx = ratios.BinarySearch(value);

        if (higherIdx >= 0)
            return rects[higherIdx];

        higherIdx = ~higherIdx;

        // No need to handle this because value >= 0.5 == ratios.First()
        //if (higherIdx == 0)
        //    return rects.First();

        // No need to handle this because value <= 1.0 == ratios.Last()
        //if (higherIdx == ratios.Length)
        //    return rects.Last();

        int lowerIdx = higherIdx - 1;
        return rects[value * 2 < ratios[lowerIdx] + ratios[higherIdx] ? lowerIdx : higherIdx];
    }

    private static (ushort, ushort) Find(float value)
    {
        if (value >= 0.5)
            return FindImpl(value);

        (ushort num, ushort den) = FindImpl(1f - value);
        return ((ushort)(den - num), den);
    }

    private static (ushort cols, ushort rows) Find(float width, float height)
    {
        if (width <= height)
            return Find(width / height);

        (ushort rows, ushort cols) = Find(height / width);
        return (cols, rows);
    }

    /// <summary>
    /// Approximates the rectangle <tt>width</tt>×<tt>height</tt> with <tt>cols</tt>×<tt>rows</tt>
    /// rectangles with dimensions <tt>elementWidth</tt>×<tt>elementHeight</tt> scaled by <tt>scale</tt>.
    /// </summary>
    public static (ushort cols, ushort rows, float scale) ApproximateRect(
        uint width,
        uint height,
        uint elementWidth,
        uint elementHeight
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
