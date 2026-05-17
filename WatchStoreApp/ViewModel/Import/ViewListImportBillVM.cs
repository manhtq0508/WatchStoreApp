namespace WatchStoreApp.ViewModel.Import
{
    public class ViewListImportBillVM
    {
        public int ImportBillId { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }
}
