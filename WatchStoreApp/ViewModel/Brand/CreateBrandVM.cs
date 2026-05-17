namespace WatchStoreApp.ViewModel.Brand
{
    public class CreateBrandVM
    {
        public string BrandName { get; set; } = null!;
        public string Origin { get; set; } = "";
        public IFormFile ImageFile { get; set; }
    }
}
