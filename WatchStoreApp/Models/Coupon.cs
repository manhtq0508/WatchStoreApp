namespace WatchStoreApp.Models
{
    public class Coupon
    {
        public int CouponId { get; set; }
        public string CouponCode { get; set; } = null!;
        public decimal DiscountRate { get; set; }
        public DateTime StartDate {  get; set; }
        public DateTime ExpireDate {  get; set; }
        public int Flag { get; set; } = 1;

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
