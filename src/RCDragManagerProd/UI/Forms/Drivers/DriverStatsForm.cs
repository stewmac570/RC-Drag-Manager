using System;
using System.Windows.Forms;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverStatsForm : Form
    {
        private readonly Driver _driver;
        private readonly DriverStatsService _service;

        public DriverStatsForm(Driver selectedDriver, string databasePath)
            : this(selectedDriver, new DriverStatsService(new RaceSessionRepository(databasePath ?? Program.ConnectionString)))
        {
        }

        internal DriverStatsForm(Driver selectedDriver, DriverStatsService service)
        {
            InitializeComponent();
            _driver = selectedDriver ?? throw new ArgumentNullException(nameof(selectedDriver));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            LoadDriverStats();
        }

        private void LoadDriverStats()
        {
            lblHeader.Text = $"Driver Statistics — {_driver.Name}";
            lblSummary.Text = $"Wins: {_driver.TotalWins} | Losses: {_driver.TotalLosses} | " +
                              $"Events Entered: {_driver.EventsEntered} | Events Won: {_driver.EventsWon}";

            lvMatches.Items.Clear();

            foreach (var row in _service.GetMatchHistory(_driver))
            {
                lvMatches.Items.Add(new ListViewItem(new[]
                {
                    row.EventName,
                    row.EventDate,
                    row.RoundLabel,
                    row.OpponentName,
                    row.Result
                }));
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
