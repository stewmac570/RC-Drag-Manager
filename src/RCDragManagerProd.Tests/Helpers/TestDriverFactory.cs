using System.Collections.Generic;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests.Helpers;

internal static class TestDriverFactory
{
    public static List<Driver> CreateProLadderPack()
    {
        return new List<Driver>
        {
            new Driver { Id = 1, Name = "Ava Stone", QualTime = 3.910 },
            new Driver { Id = 2, Name = "Blake Turner", QualTime = 3.955 },
            new Driver { Id = 3, Name = "Casey Reed", QualTime = 4.005 },
            new Driver { Id = 4, Name = "Drew Cole", QualTime = 4.080 }
        };
    }

    public static List<Driver> CreateProLadderByePack()
    {
        return new List<Driver>
        {
            new Driver { Id = 1, Name = "Ava Stone", QualTime = 3.910 },
            new Driver { Id = 2, Name = "Blake Turner", QualTime = 3.955 },
            new Driver { Id = 3, Name = "Casey Reed", QualTime = 4.005 },
            new Driver { Id = 4, Name = "Drew Cole", QualTime = 4.080 },
            new Driver { Id = 5, Name = "Evan Hart", QualTime = 4.120 }
        };
    }

    public static List<Driver> CreateRoundRobinPack(int count)
    {
        var names = new[]
        {
            "Ava Stone", "Blake Turner", "Casey Reed", "Drew Cole",
            "Evan Hart", "Flynn Ward", "Gray Nash", "Harper Quinn"
        };

        var drivers = new List<Driver>(count);
        for (var i = 0; i < count; i++)
        {
            drivers.Add(new Driver { Id = i + 1, Name = names[i] });
        }

        return drivers;
    }
}
