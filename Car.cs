namespace RCDragManagerProd
{
    public class Car
    {
        public int Id { get; set; }

        public int CarID // ✅ Add this alias to fix legacy code
        {
            get => Id;
            set => Id = value;
        }

        public int DriverId { get; set; }
        public string CarName { get; set; }
        public string ClassType { get; set; }
        public double? DefaultDialIn { get; set; }
    }
}
