using System.ComponentModel.DataAnnotations;

namespace WMSSystems.Models
{
    public class Product
    {
        [Key]
        public string productCode { get; set; }
        public string productTitle { get; set; }
        public string productDescription { get; set; }
        public int productLength { get; set; }
        public int productWidth { get; set; }
        public int productHeight { get; set; }

    }
}
