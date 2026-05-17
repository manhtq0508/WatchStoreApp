namespace WatchStoreApp.Models
{
    public class Brand
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = null!;
        public string Origin { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Flag { get; set; } = 1;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
