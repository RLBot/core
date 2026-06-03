using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests.ManagerTools;

[TestClass]
public class RectUtilTest
{
    private static (float, float) RoundingBounds(uint n)
    {
        return (MathF.BitIncrement(n - 0.5f), MathF.BitDecrement(n + 0.5f));
    }

    [TestMethod]
    public void RectSolveTest_Generic()
    {
        const uint TestUpTo = 64;

        for (uint i = 1; i <= TestUpTo; ++i)
        {
            for (uint j = 1; j <= TestUpTo; ++j)
            {
                (ushort cols, ushort rows, float scale) = RectUtil.RectSolve(i, i, j, j);
                Assert.AreEqual(1, cols);
                Assert.AreEqual(1, rows);
                // Slightly iffy, but it passes.
                Assert.AreEqual((float)i / j, scale);
            }
        }

        for (uint i = 1; i <= TestUpTo; ++i)
        {
            (float iMin, float iMax) = RoundingBounds(i);
            for (uint j = 1; j <= TestUpTo; ++j)
            {
                (float jMin, float jMax) = RoundingBounds(j);
                for (uint k = 1; k <= TestUpTo; ++k)
                {
                    for (uint l = 1; l <= TestUpTo; ++l)
                    {
                        (ushort cols, ushort rows, float scale) = RectUtil.RectSolve(
                            i,
                            j,
                            k,
                            l
                        );
                        Assert.IsLessThan(Rendering.RectangleStringMaxLength + 1, cols + rows);
                        Assert.IsInRange(iMin, iMax, k * scale * cols);
                        Assert.IsInRange(jMin, jMax, l * scale * rows);
                    }
                }
            }
        }
    }

    [TestMethod]
    public void RectSolveTest_Practical()
    {
        const uint TestUpTo = 7680;

        for (uint i = 1; i <= TestUpTo; ++i)
        {
            (float iMin, float iMax) = RoundingBounds(i);
            for (uint j = 1; j <= TestUpTo; ++j)
            {
                (float jMin, float jMax) = RoundingBounds(j);
                (ushort cols, ushort rows, float scale) = RectUtil.RectSolve(
                    i,
                    j,
                    Rendering.FontWidthPixels,
                    Rendering.FontHeightPixels
                );
                Assert.IsLessThan(Rendering.RectangleStringMaxLength + 1, cols + rows);
                Assert.IsInRange(iMin, iMax, Rendering.FontWidthPixels * scale * cols);
                Assert.IsInRange(jMin, jMax, Rendering.FontHeightPixels * scale * rows);
            }
        }
    }
}
