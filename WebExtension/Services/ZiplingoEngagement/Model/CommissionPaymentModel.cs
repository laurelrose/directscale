using DirectScale.Disco.Extension;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class CommissionPaymentModel
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int MerchantId { get; set; }
        public string PaymentUniqueId { get; set; }
        public int AssociateId { get; set; }
        public string CountryCode { get; set; }
        public string TaxId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fees { get; set; }
        public decimal Holdings { get; set; }
        public decimal Total { get; set; }
        public decimal ExchangeRate { get; set; }
        public string ExchangeCurrencyCode { get; set; }
        public PaymentProcessStatus PaymentStatus { get; set; }
        public string DatePaid { get; set; }
        public string TransactionNumber { get; set; }
        public int CheckNumber { get; set; }
        public string ErrorMessage { get; set; }
    }
}
