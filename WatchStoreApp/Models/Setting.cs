using System.ComponentModel.DataAnnotations;

namespace WatchStoreApp.Models
{
    public class Setting
    {
        public int SettingId { get; set; }
        [DisplayFormat(DataFormatString = "{0:0}", ApplyFormatInEditMode = true)]
        public decimal ShippingFee { get; set; }
        public string Banner1_1 { get; set; } = "";
        public string Banner1_2 { get; set; } = "";
        public string Banner1_3 { get; set; } = "";
        public string Banner2 { get; set; } = "";
        public string Banner3 { get; set; } = "";
        public string Banner4 { get; set; } = "";
    }
}
