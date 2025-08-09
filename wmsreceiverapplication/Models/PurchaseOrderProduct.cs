using System.ComponentModel.DataAnnotations;

namespace WMSSystems.Models
{
    public class PurchaseOrderProduct
    {
      
 
        [Key]
        public string orderId { get; set; }
        public Product product { get; set; }
        public int quantity { get; set; }
    }
}
