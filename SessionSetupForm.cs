using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class SessionSetupForm : Form
    {
        private DriverRepository repository;
        private List<(Driver driver, Car car)> allEligibleDrivers;
        private List<(Driver driver, Car car)> eventRoster;

        public RaceSession RaceSessionResult { get; private set; }

        public SessionSetupForm(DriverRepository repo)
        {
            InitializeComponent();

            repository = repo;
            eventRoster = new List<(Driver, Car)>();
            allEligibleDrivers = new List<(Driver, Car)>();

            rbHeadsUp.CheckedChanged += ClassSelectionChanged;
            rbBracket.CheckedChanged += ClassSelectionChanged;
            rbDialIn.CheckedChanged += ClassSelectionChanged;

            btnAddNewDriver.Click += BtnAddNewDriver_Click;
            btnStartRace.Click += BtnStartRace_Click;
            btnCancel.Click += BtnCancel_Click;

            lvEventRoster.CheckBoxes = true;
            lvEventRoster.ItemChecked += LvEventRoster_ItemChecked;

            RefreshDriverList();
        }

        private void LvEventRoster_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            var tuple = (ValueTuple<Driver, Car>)e.Item.Tag;

            if (e.Item.Checked)
            {
                if (!eventRoster.Any(x => x.driver.Id == tuple.Item1.Id && x.car.CarID == tuple.Item2.CarID))
                {
                    eventRoster.Add(tuple);
                }
            }
            else
            {
                eventRoster.RemoveAll(x => x.driver.Id == tuple.Item1.Id && x.car.CarID == tuple.Item2.CarID);
            }
        }

        private void ClassSelectionChanged(object sender, EventArgs e)
        {
            if (rbHeadsUp.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }
            else if (rbBracket.Checked)
            {
                lblFixedDial.Visible = true;
                txtFixedDial.Visible = true;
            }
            else if (rbDialIn.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }

            RefreshDriverList();
        }

        private void RefreshDriverList()
        {
            lvEventRoster.Items.Clear();
            allEligibleDrivers.Clear();
            eventRoster.Clear();

            var drivers = repository.GetAllDrivers();

            foreach (var driver in drivers)
            {
                foreach (var car in driver.Cars)
                {
                    bool eligible = false;

                    if (rbHeadsUp.Checked && car.ClassType == "Heads Up")
                        eligible = true;
                    else if (rbBracket.Checked && car.ClassType == "Index")
                        eligible = true;
                    else if (rbDialIn.Checked && car.ClassType == "Dial")
                        eligible = true;

                    if (eligible)
                    {
                        allEligibleDrivers.Add((driver, car));

                        var item = new ListViewItem(new string[]
                        {
                            driver.Name,
                            car.CarName,
                            car.ClassType,
                            car.DefaultDialIn?.ToString("0.000") ?? "-"
                        });

                        item.Tag = (driver, car);
                        lvEventRoster.Items.Add(item);
                    }
                }
            }
        }

        private void BtnAddNewDriver_Click(object sender, EventArgs e)
        {
            var addDialog = new AddDriverAndCarDialog();
            if (addDialog.ShowDialog() == DialogResult.OK)
            {
                var newDriver = new Driver
                {
                    Name = addDialog.DriverName,
                    Cars = new List<Car>()
                };
                repository.AddDriver(newDriver);

                var insertedDriver = repository.GetAllDrivers().First(d => d.Name == newDriver.Name);

                var newCar = new Car
                {
                    CarName = addDialog.CarName,
                    ClassType = addDialog.ClassType,
                    DefaultDialIn = addDialog.DialIn
                };
                repository.AddCar(insertedDriver.Id, newCar);

                RefreshDriverList();
            }
        }

        private void BtnStartRace_Click(object sender, EventArgs e)
        {
            if (eventRoster.Count < 2)
            {
                MessageBox.Show("Please select at least 2 drivers.");
                return;
            }

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
                DriverEntries = eventRoster.Select(er =>
                {
                    double? dialIn = null;
                    double? qualTime = null;

                    if (classType == "Heads Up")
                    {
                        qualTime = er.driver.QualTime;
                    }
                    else if (classType == "Dial-In")
                    {
                        dialIn = er.car.DefaultDialIn;
                    }
                    else if (classType == "Bracket Class")
                    {
                        dialIn = fixedDial;
                    }

                    return new RaceSessionDriverEntry
                    {
                        DriverID = er.driver.Id,
                        DriverName = er.driver.Name,
                        CarID = er.car.CarID,
                        CarName = er.car.CarName,
                        ClassType = er.car.ClassType,
                        DialIn = dialIn,
                        QualifyingTime = qualTime,
                        Seed = null
                    };
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
