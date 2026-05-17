using Microsoft.AspNetCore.Mvc.Rendering;

namespace WatchStoreApp.ViewModel.Customer
{
    public class EditCustomerVM
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Gender { get; set; } = "";
        public string IsAvailable { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; } = "";

        public IEnumerable<SelectListItem> GenderList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> IsAvailableList { get; set; } = new List<SelectListItem>();
    }
}
