using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Helpers;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverStatsForm : Form
    {
        private Driver driver;
        private string dbPath;

        public DriverStatsForm(Driver selectedDriver, string databasePath)
        {
            InitializeComponent();
            driver = selectedDriver;
            dbPath = databasePath;
            LoadDriverStats();
        }

        private void LoadDriverStats()
        {
            // Top summary label
            lblHeader.Text = $"Driver Statistics — {driver.Name}";
            lblSummary.Text = $"Wins: {driver.TotalWins} | Losses: {driver.TotalLosses} | " +
                              $"Events Entered: {driver.EventsEntered} | Events Won: {driver.EventsWon}";

            // Prepare session repo
            var sessionRepository = new RaceSessionRepository(dbPath);
            var sessionSummaries = sessionRepository.GetAllSessions();

            lvMatches.Items.Clear();

            foreach (var summary in sessionSummaries)
            {
                var session = sessionRepository.LoadSession(summary.Id);

                // Only continue if driver participated in session
                var driverEntry = session.DriverEntries.FirstOrDefault(d => d.DriverID == driver.Id);
                if (driverEntry == null)
                    continue;

                foreach (var result in session.SavedResults)
                {
                    string resultLabel;
                    string opponentName;
                    bool isMatchInvolvingDriver = false;
                    int opponentId = -1;
                    bool isWin = false;

                    if (result.WinnerDriverId == driver.Id)
                    {
                        isWin = true;
                        opponentId = result.LoserDriverId;
                        isMatchInvolvingDriver = true;
                    }
                    else if (result.LoserDriverId == driver.Id)
                    {
                        isWin = false;
                        opponentId = result.WinnerDriverId;
                        isMatchInvolvingDriver = true;
                    }

                    if (!isMatchInvolvingDriver)
                        continue;

                    var opponentEntry = session.DriverEntries.FirstOrDefault(d => d.DriverID == opponentId);
                    opponentName = opponentEntry != null ? opponentEntry.DriverName : "BYE";


                    resultLabel = isWin ? "Win" : "Loss";

                    var match = MatchLookupHelper.FindMatchInSession(session, result.MatchId);
                    string roundLabel = match?.RoundLabel ?? "";

                    var item = new ListViewItem(new[]
                    {
                        session.EventName,
                        session.EventDate.ToString("yyyy-MM-dd"),
                        roundLabel,
                        opponentName,
                        resultLabel
                    });

                    lvMatches.Items.Add(item);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
