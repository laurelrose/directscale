using System;

namespace WebExtension.Services.RewardPoints.Models
{
    public class RewardPointCredit
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public DateTime OrderCommissionDate { get; set; }
        public int OrderAssociateId { get; set; }
        public string OrderAssociateName { get; set; }
        public int OrderItemId { get; set; }
        public string OrderItemSku { get; set; }
        public string OrderItemDescription { get; set; }
        public double OrderItemCredits { get; set; }
        public int AwardedAssociateId { get; set; }
        public RewardPointCreditType CreditType { get; set; }
        public PayoutStatus PayoutStatus { get; set; }
        public int? CommissionPeriodId { get; set; }
    }
}
