using Microsoft.AspNetCore.Mvc.Rendering;

namespace WatchStoreApp.ViewModel.Product
{
    public class EditMechanicalVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public int BrandId { get; set; }
        public string Gender { get; set; } = "";
        public string WarrantyPeriod { get; set; } = "";
        public string WatchType { get; set; } = "Mechanical";
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
        public string? ImageUrl1Path { get; set; }
        public string? ImageUrl2Path { get; set; }
        public string? ImageUrl3Path { get; set; }

        public IFormFile? ImageUrl1 { get; set; }
        public IFormFile? ImageUrl2 { get; set; }
        public IFormFile? ImageUrl3 { get; set; }

        public int Flag { get; set; } = 1;
        public string CalendarFunction { get; set; } = "";
        public string Functions { get; set; } = "";
        public string Movement { get; set; } = "";
        public string CaseShape { get; set; } = "";
        public IEnumerable<SelectListItem> BrandList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> GenderList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StrapMaterialList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> WatchStyleList { get; set; } = new List<SelectListItem>();
    }
}
