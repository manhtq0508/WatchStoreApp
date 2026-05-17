namespace WatchStoreApp.Models
{
    public class ImportDetail
    {
        public int ImportDetailId { get; set; }
        public int ImportBillId { get; set; }
        public ImportBill ImportBill { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }
}
