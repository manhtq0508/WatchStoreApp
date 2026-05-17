namespace WatchStoreApp.Models
{
    public class ImportBill
    {
        public int ImportBillId { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal Total { get; set; }

        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    }
}
