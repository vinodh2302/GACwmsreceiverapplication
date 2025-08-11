using System.ComponentModel.DataAnnotations;

namespace WMSSystems.Models
{
    public class PurchaseOrder
    {
        [Key]
        public string orderId { get; set; }
        public DateTime processingDate { get; set; }
        public string customerId { get; set; }
        public PurchaseOrderProduct products { get; set; }
    }

}
