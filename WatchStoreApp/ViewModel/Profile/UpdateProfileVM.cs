namespace WatchStoreApp.ViewModel.Profile
{
    public class UpdateProfileVM
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; } = "";
    }
}
