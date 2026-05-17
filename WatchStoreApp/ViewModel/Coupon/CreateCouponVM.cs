namespace WatchStoreApp.ViewModel.Coupon
{
    public class CreateCouponVM
    {
        public string CouponCode { get; set; } = null!;
        public decimal DiscountRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
