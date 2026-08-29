using RCDragManagerProd.AppServices;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="RaceConsoleService.ParseDialIn"/> — the input rule behind the
/// inline dial-in cell and the dial-in dialog (#416). Both entry points share this
/// parser so they accept exactly the same text.
/// </summary>
[TestClass]
public class DialInParseTests
{
    [TestMethod]
    public void ParseDialIn_Number_Succeeds()
    {
        var r = RaceConsoleService.ParseDialIn("3.220");

        Assert.IsTrue(r.Success);
        Assert.IsFalse(r.Cleared);
        Assert.AreEqual(3.220, r.DialIn);
    }

    [TestMethod]
    public void ParseDialIn_TrimsWhitespace()
    {
        var r = RaceConsoleService.ParseDialIn("  2.955  ");

        Assert.IsTrue(r.Success);
        Assert.AreEqual(2.955, r.DialIn);
    }

    [TestMethod]
    public void ParseDialIn_Blank_ClearsTheDialIn()
    {
        foreach (var text in new[] { "", "   ", null })
        {
            var r = RaceConsoleService.ParseDialIn(text);

            Assert.IsTrue(r.Success, $"'{text}' should clear, not fail");
            Assert.IsTrue(r.Cleared);
            Assert.IsNull(r.DialIn);
        }
    }

    [TestMethod]
    public void ParseDialIn_NotANumber_Fails()
    {
        var r = RaceConsoleService.ParseDialIn("abc");

        Assert.IsFalse(r.Success);
        Assert.IsNotNull(r.Error);
        Assert.IsNull(r.DialIn);
    }

    [TestMethod]
    public void ParseDialIn_ZeroOrNegative_Fails()
    {
        // A dial-in is an elapsed time, so zero and below are meaningless.
        foreach (var text in new[] { "0", "0.000", "-1.5" })
        {
            var r = RaceConsoleService.ParseDialIn(text);
            Assert.IsFalse(r.Success, $"'{text}' should be rejected");
            Assert.IsNotNull(r.Error);
        }
    }

    [TestMethod]
    public void ParseDialIn_FailureIsNotMistakenForAClear()
    {
        // The console clears a dial-in when Cleared is set, so a rejected entry
        // must never come back looking like a deliberate blank.
        var r = RaceConsoleService.ParseDialIn("not-a-time");

        Assert.IsFalse(r.Success);
        Assert.IsFalse(r.Cleared);
    }
}
