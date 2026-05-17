namespace WatchStoreApp.ViewModel.Import
{
    public class CreateBillVM
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public decimal Total { get; set; }

        public List<ImportDetailVM> ImportDetails { get; set; } = new List<ImportDetailVM>();

        public string EmployeeName { get; set; } = string.Empty;
        public IEnumerable<WatchStoreApp.Models.Product>? Products { get; set; }
    }
}
