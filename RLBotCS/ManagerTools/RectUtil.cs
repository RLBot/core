using System.Numerics;

namespace RLBotCS.ManagerTools;

public static class RectUtil
{
    private static readonly int LeadingZerosForUShort = BitOperations.LeadingZeroCount(
        (uint)UInt16.MaxValue
    );

    /// <summary>
    /// Greatest common divisor by Euclidean algorithm.<br/>
    /// An optimized implementation based on https://stackoverflow.com/a/41766138.
    /// </summary>
    private static uint Gcd(uint a, uint b)
    {
        if (a >= b)
            a %= b;
        while (a != 0)
        {
            b %= a;
            if (b == 0)
                return a;
            a %= b;
        }
        return b;
    }

    /// <summary>
    /// Discards the same number of least significant bits from a and b
    /// if either is too large to fit into a ushort.
    /// </summary>
    private static (ushort, ushort, int) SafeCast(uint a, uint b)
    {
        if (a <= UInt16.MaxValue && b <= UInt16.MaxValue)
            return ((ushort)a, (ushort)b, 0);

        int shift = Int32.Max(
            LeadingZerosForUShort - BitOperations.LeadingZeroCount(a),
            LeadingZerosForUShort - BitOperations.LeadingZeroCount(b)
        );
        return ((ushort)(a >> shift), (ushort)(b >> shift), shift);
    }

    /// <summary>
    /// Represents the rectangle <tt>width</tt>×<tt>height</tt> with <tt>cols</tt>×<tt>rows</tt>
    /// rectangles with dimensions <tt>elementWidth</tt>×<tt>elementHeight</tt> scaled by <tt>scale</tt>.
    /// </summary>
    public static (ushort cols, ushort rows, float scale) RectSolve(
        uint width,
        uint height,
        uint elementWidth,
        uint elementHeight
    )
    {
        uint wh = width * elementHeight;
        uint hw = height * elementWidth;
        uint gcd = Gcd(wh, hw);
        (ushort cols, ushort rows, int shift) = SafeCast(wh / gcd, hw / gcd);

        return (cols, rows, ((float)(gcd >> shift)) / (elementWidth * elementHeight));
    }
}
