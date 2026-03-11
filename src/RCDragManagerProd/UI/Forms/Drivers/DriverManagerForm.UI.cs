using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverManagerForm
    {
        private void SetupDriverDetailsGrid()
        {
            lvDriverDetails.Columns.Clear();
            lvDriverDetails.View = View.Details;
            lvDriverDetails.FullRowSelect = true;
            lvDriverDetails.Columns.Add("Field", 150);
            lvDriverDetails.Columns.Add("Value", 300);
        }

        private void LoadDrivers()
        {
            var previousId = selectedDriver?.Id ?? 0;

            lstDrivers.Items.Clear();
            var drivers = repository.GetAllDrivers();

            foreach (var d in drivers.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                lstDrivers.Items.Add($"{d.Id}: {d.Name}");

            if (previousId > 0)
            {
                for (int i = 0; i < lstDrivers.Items.Count; i++)
                {
                    var txt = lstDrivers.Items[i].ToString();
                    if (txt.StartsWith(previousId.ToString() + ":", StringComparison.Ordinal))
                    {
                        lstDrivers.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private static int ParseIdFromListText(object listItem)
        {
            if (listItem == null) return 0;
            var s = listItem.ToString();
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var idx = s.IndexOf(':');
            if (idx <= 0) return 0;
            return int.TryParse(s.Substring(0, idx).Trim(), out var id) ? id : 0;
        }

        private void AddDetail(string field, string value)
        {
            var it = new ListViewItem(field);
            it.SubItems.Add(value ?? "");
            lvDriverDetails.Items.Add(it);
        }

        private void ShowDriverDetails(int driverId)
        {
            selectedDriver = repository.GetDriverById(driverId);

            lvDriverDetails.BeginUpdate();
            lvDriverDetails.Items.Clear();

            if (selectedDriver == null)
            {
                lvDriverDetails.EndUpdate();
                btnDriverStats.Enabled = false;
                return;
            }

            btnDriverStats.Enabled = true;

            AddDetail("Name", selectedDriver.Name);
            AddDetail("Qual Time", selectedDriver.QualTime?.ToString("0.000") ?? "");
            AddDetail("State", selectedDriver.State ?? "");
            AddDetail("Notes", selectedDriver.Notes ?? "");
            AddDetail("Wins", selectedDriver.TotalWins.ToString());
            AddDetail("Losses", selectedDriver.TotalLosses.ToString());
            AddDetail("Events Entered", selectedDriver.EventsEntered.ToString());
            AddDetail("Events Won", selectedDriver.EventsWon.ToString());

            AddDetail("--- Cars ---", "");
            if (selectedDriver.Cars != null)
            {
                foreach (var car in selectedDriver.Cars)
                {
                    var dial = car.DefaultDialIn?.ToString("0.000") ?? "-";
                    var item = new ListViewItem("Car") { Tag = car.CarID };
                    item.SubItems.Add($"{car.CarName} - {car.ClassType} - {dial}");
                    lvDriverDetails.Items.Add(item);
                }
            }

            lvDriverDetails.EndUpdate();
        }
    }
}
