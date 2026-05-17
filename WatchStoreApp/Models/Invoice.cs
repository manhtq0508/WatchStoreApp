namespace WatchStoreApp.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public DateTime Date { get; set; } // Order Date
        public string PaymentMethod { get; set; } = "";
        public string ShippingMethod { get; set; } = "";
        public string Address { get; set; } = "";
        public string ShippingNote { get; set; } = "";
        public string? DiscountCode { get; set; }
        public int? CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "Processing"; // Processing, Delivered, Cancelled

        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}

