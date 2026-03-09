using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

[TestClass]
public class ByePolicyTests
{
    [TestMethod]
    public void IsBye_ReturnsTrueForNullAndFalseForRealDriver()
    {
        Assert.IsTrue(ByePolicy.IsBye(null));
        Assert.IsFalse(ByePolicy.IsBye(new Driver { Id = 99, Name = "Test Driver" }));
    }
}
