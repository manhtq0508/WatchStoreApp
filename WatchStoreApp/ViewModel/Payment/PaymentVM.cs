using WatchStoreApp.ViewModel.Cart;

namespace WatchStoreApp.ViewModel.Payment
{
    public class PaymentVM
    {
        public CartVM Cart { get; set; } = new CartVM();
        public int? ProductId { get; set; } // For "Buy Now" - single product
        public int Quantity { get; set; } = 1; // For "Buy Now" - quantity
        
        // Customer Information
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        
        // Shipping Information
        public string Address { get; set; } = "";
        public string ShippingMethod { get; set; } = "Standard"; // Standard, Express
        public string ShippingNote { get; set; } = "";
        
        // Payment Information
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Credit Card
        
        // Discount
        public string? DiscountCode { get; set; }
        
        // Calculated values
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal BaseShippingFee { get; set; }


        public string? ReturnUrl { get; set; }

    }
}

