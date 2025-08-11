using System.ComponentModel.DataAnnotations;

namespace WMSSystems.Models
{
    public class PurchaseOrderProduct
    {


        [Key]
        public string orderId { get; set; }
        public string productId { get; set; }
        public int quantity { get; set; }
    }
}
