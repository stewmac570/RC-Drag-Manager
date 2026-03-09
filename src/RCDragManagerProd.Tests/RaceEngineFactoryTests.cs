using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RaceEngineFactoryTests
{
    [TestMethod]
    public void Create_ReturnsExpectedAdapter_ForSupportedModes()
    {
        Assert.IsInstanceOfType(RaceEngineFactory.Create("pro ladder"), typeof(ProLadderEngineAdapter));
        Assert.IsInstanceOfType(RaceEngineFactory.Create("random"), typeof(RandomEngineAdapter));
        Assert.IsInstanceOfType(RaceEngineFactory.Create("round robin"), typeof(RoundRobinEngineAdapter));
    }
}
