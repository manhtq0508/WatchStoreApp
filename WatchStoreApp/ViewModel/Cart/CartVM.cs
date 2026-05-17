namespace WatchStoreApp.ViewModel.Cart
{
    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new List<CartItemVM>();
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal TotalPrice => Items.Sum(i => i.Subtotal);
    }
}

