using System.ComponentModel.DataAnnotations.Schema;

namespace WatchStoreApp.Models
{
    public class SmartWatch
    {
        public int SmartWatchId { get; set; }
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string BateryLife { get; set; } = "";
        public string DisplayResolution { get; set; } = "";
        public string DisplayTechnology { get; set; } = "";
        public string ScreenSize { get; set; } = "";
        public string Sensors { get; set; } = "";
    }
}
