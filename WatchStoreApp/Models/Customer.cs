namespace WatchStoreApp.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Gender { get; set; } = "";
        public string IsAvailable { get; set; } = "Available";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

    }
}
