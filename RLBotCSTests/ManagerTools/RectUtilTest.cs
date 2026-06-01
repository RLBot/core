using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests.ManagerTools;

[TestClass]
public class RectUtilTest
{
    const uint TestUpTo = 64;

    [TestMethod]
    public void ApproximateRectTest()
    {
        for (uint i = 1; i <= TestUpTo; ++i)
        {
            for (uint j = 1; j <= TestUpTo; ++j)
            {
                (ushort cols, ushort rows, float scale) = RectUtil.ApproximateRect(i, i, j, j);
                Assert.AreEqual(1, cols);
                Assert.AreEqual(1, rows);
                // Slightly iffy, but it passes.
                Assert.AreEqual((float)i / j, scale);
            }
        }

        for (uint i = 1; i <= TestUpTo; ++i)
        {
            float iMin = i * 0.96f;
            float iMax = i * 1.04f;
            for (uint j = 1; j <= TestUpTo; ++j)
            {
                float jMin = j * 0.96f;
                float jMax = j * 1.04f;
                for (uint k = 1; k <= TestUpTo; ++k)
                {
                    for (uint l = 1; l <= TestUpTo; ++l)
                    {
                        (ushort cols, ushort rows, float scale) = RectUtil.ApproximateRect(
                            i,
                            j,
                            k,
                            l
                        );
                        Assert.IsInRange(iMin, iMax, k * scale * cols);
                        Assert.IsInRange(jMin, jMax, l * scale * rows);
                    }
                }
            }
        }
    }
}
