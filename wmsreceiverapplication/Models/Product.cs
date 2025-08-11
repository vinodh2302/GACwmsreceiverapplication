using System.ComponentModel.DataAnnotations;

namespace WMSSystems.Models
{
    public class Product
    {
        [Key]
        public string productCode { get; set; }
        public string productTitle { get; set; }
        public string productDescription { get; set; }
        public string productLength { get; set; }
        public string productWidth { get; set; }
        public string productHeight { get; set; }

    }
}
