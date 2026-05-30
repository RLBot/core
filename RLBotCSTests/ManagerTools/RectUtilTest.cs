using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests.ManagerTools;

[TestClass]
public class RectUtilTest
{
    const int TestUpTo = 64;

    [TestMethod]
    public void ApproximateRectTest()
    {
        for (int i = 1; i <= TestUpTo; ++i)
        {
            for(int j = 1; j <= TestUpTo; ++j)
            {
                (ushort cols, ushort rows, float scale) = RectUtil.ApproximateRect(i, i, j, j);
                Assert.AreEqual(1, cols);
                Assert.AreEqual(1, rows);
                // Slightly iffy, but it passes.
                Assert.AreEqual((float)i / j, scale);
            }
        }

        for (int i = 1; i <= TestUpTo; ++i)
        {
            float iMin = i * 0.95f;
            float iMax = i * 1.05f;
            for (int j = 1; j <= TestUpTo; ++j)
            {
                float jMin = j * 0.95f;
                float jMax = j * 1.05f;
                for (int k = 1; k <= TestUpTo; ++k)
                {
                    for (int l = 1; l <= TestUpTo; ++l)
                    {
                        (ushort cols, ushort rows, float scale) = RectUtil.ApproximateRect(i, j, k, l);
                        Assert.IsInRange(iMin, iMax, k * scale * cols);
                        Assert.IsInRange(jMin, jMax, l * scale * rows);
                    }
                }
            }
        }
    }
}
