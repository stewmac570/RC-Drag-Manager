using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class SessionSetupForm : Form
    {
        private DriverRepository repository;
        private List<(Driver driver, Car car)> eventRoster;

        public RaceSession RaceSessionResult { get; private set; }

        public SessionSetupForm(DriverRepository repo)
        {
            InitializeComponent();

            repository = repo;
            eventRoster = new List<(Driver, Car)>();

            rbHeadsUp.CheckedChanged += RbHeadsUp_CheckedChanged;
            rbBracket.CheckedChanged += RbBracket_CheckedChanged;
            rbDialIn.CheckedChanged += RbDialIn_CheckedChanged;

            btnAddNewDriver.Click += BtnAddNewDriver_Click;
            btnAddDriverFromList.Click += BtnAddDriverFromList_Click;
            btnConfirmSeeds.Click += BtnConfirmSeeds_Click;
            btnStartRace.Click += BtnStartRace_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void RbHeadsUp_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHeadsUp.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }
        }

        private void RbBracket_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBracket.Checked)
            {
                lblFixedDial.Visible = true;
                txtFixedDial.Visible = true;
            }
        }

        private void RbDialIn_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDialIn.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }
        }

        private void BtnAddNewDriver_Click(object sender, EventArgs e)
        {
            // Placeholder for future add-new-driver UI
            MessageBox.Show("Add New Driver not implemented.");
        }

        private void BtnAddDriverFromList_Click(object sender, EventArgs e)
        {
            var drivers = repository.GetAllDrivers();

            foreach (var driver in drivers)
            {
                foreach (var car in driver.Cars)
                {
                    if (!eventRoster.Any(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID))
                    {
                        eventRoster.Add((driver, car));

                        var item = new ListViewItem(new string[]
                        {
                            driver.Name,
                            car.CarName,
                            car.ClassType,
                            car.DefaultDialIn?.ToString("0.000") ?? "-"
                        });
                        lvEventRoster.Items.Add(item);
                    }
                }
            }
        }

        private void BtnConfirmSeeds_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Seed confirmation not implemented yet.");
        }

        private void BtnStartRace_Click(object sender, EventArgs e)
        {
            string classType = rbHeadsUp.Checked ? "Heads Up" :
                               rbBracket.Checked ? "Bracket Class" : "Dial-In";

            double? fixedDial = null;
            if (rbBracket.Checked && double.TryParse(txtFixedDial.Text, out double fd))
                fixedDial = fd;

            RaceSessionResult = new RaceSession
            {
                EventName = txtEventName.Text.Trim(),
                EventDate = dateRaceDate.Value.Date,
                RaceType = cmbRaceType.SelectedItem.ToString(),
                ClassType = classType,
                FixedDialIn = fixedDial,
                DriverEntries = eventRoster.Select(er => new RaceSessionDriverEntry
                {
                    DriverID = er.driver.Id,
                    DriverName = er.driver.Name,
                    CarID = er.car.CarID,
                    CarName = er.car.CarName,
                    ClassType = er.car.ClassType,
                    DialIn = er.car.DefaultDialIn,
                    QualifyingTime = null,
                    Seed = null
                }).ToList()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
