namespace WatchStoreApp.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public int BrandId { get; set; }
        public Brand Brand { get; set; } = null!;
        public string Gender { get; set; } = "";
        public string WarrantyPeriod { get; set; } = "";
        public string WatchType { get; set; } = "";
        public string GlassMaterial { get; set; } = "";
        public decimal CaseDiameter { get; set; }
        public decimal CaseThickness { get; set; }
        public string WaterResistance { get; set; } = "";
        public string ProductGenre { get; set; } = "";
        public string StrapMaterial { get; set; } = "";
        public string StrapColor { get; set; } = "";
        public string WatchStyle { get; set; } = "";
        public decimal ImportPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl1 { get; set; } = "";
        public string ImageUrl2 { get; set; } = "";
        public string ImageUrl3 { get; set; } = "";
        public int Flag { get; set; } = 1;

        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

        public MechanicalWatch? MechanicalWatch { get; set; }
        public SmartWatch? SmartWatch { get; set; }
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

    }
}
