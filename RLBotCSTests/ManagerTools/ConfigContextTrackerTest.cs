using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests.ManagerTools;

[TestClass]
public class ConfigContextTrackerTest
{
    [TestMethod]
    public void BasicNestedContextTest()
    {
        ConfigContextTracker ctx = new();
        using (ctx.Begin("A"))
        {
            ctx.Push("B", ConfigContextTracker.Type.Link);
            Assert.AreEqual("A.B", ctx.ToString());
            using (ctx.Begin("C"))
            {
                Assert.AreEqual("A.B->C", ctx.ToString());
            }
            ctx.Pop(); // B
        }
        ctx.Push("D");
        Assert.AreEqual("D", ctx.ToString());
    }
}
