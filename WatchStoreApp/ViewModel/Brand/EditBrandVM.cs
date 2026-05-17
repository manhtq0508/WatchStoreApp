namespace WatchStoreApp.ViewModel.Brand
{
    public class EditBrandVM
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = null!;
        public string Origin { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public IFormFile ImageFile { get; set; }
    }
}
