namespace WatchStoreApp.ViewModel.Invoice
{
    public class ViewListInvoiceVM
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "";
    }
}

