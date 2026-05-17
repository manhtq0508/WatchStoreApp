namespace WatchStoreApp.ViewModel.Account
{
    public class UpdateAccountVM
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public string CardNumber { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Role { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; } = "";
    }
}
