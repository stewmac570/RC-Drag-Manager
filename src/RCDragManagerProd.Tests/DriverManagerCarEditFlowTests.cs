using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.UI.Forms;

namespace RCDragManagerProd.Tests;

[TestClass]
public class DriverManagerCarEditFlowTests
{
    [TestMethod]
    public void TryApplyEditedCar_UsesSelectedDisplayedCarIdentity_NotListPosition()
    {
        var selectedDriver = new Driver
        {
            Id = 100,
            Name = "Driver A",
            Cars = new List<Car>
            {
                new Car { CarID = 20, CarName = "Beta Car", ClassType = "Heads Up" },
                new Car { CarID = 10, CarName = "Alpha Car", ClassType = "Heads Up" }
            }
        };

        var displayedCarIds = new List<int> { 10, 20 };
        var editedValues = new Car { CarName = "Alpha Car Edited", ClassType = "Heads Up" };

        var applied = DriverManagerCarEditFlow.TryApplyEditedCar(
            selectedDriver,
            displayedCarIds,
            selectedDisplayCarIndex: 0,
            editedValues);

        Assert.IsTrue(applied);
        Assert.AreEqual("Alpha Car Edited", selectedDriver.Cars[1].CarName);
        Assert.AreEqual("Beta Car", selectedDriver.Cars[0].CarName);
    }
}
