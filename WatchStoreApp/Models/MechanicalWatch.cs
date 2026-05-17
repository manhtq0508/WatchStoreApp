using System.ComponentModel.DataAnnotations.Schema;

namespace WatchStoreApp.Models
{
    public class MechanicalWatch
    {
        public int MechanicalWatchId { get; set; }
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string CalendarFunction { get; set; } = "";
        public string Functions { get; set; } = "";
        public string Movement { get; set; } = "";
        public string CaseShape { get; set; } = "";
    }
}
