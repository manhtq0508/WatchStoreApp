namespace WatchStoreApp.ViewModel.Cart
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}

