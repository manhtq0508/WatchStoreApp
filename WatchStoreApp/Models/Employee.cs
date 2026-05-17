namespace WatchStoreApp.Models
{
    public class Employee
    {
        public int EmployeeId {  get; set; }
        public string Name {  get; set; } = null!;
        public string CardNumber { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Role {  get; set; } = "";
        public string IsAvailable { get; set; } = "Available";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public ICollection<ImportBill> ImportBills { get; set; } = new List<ImportBill>();
    }
}
