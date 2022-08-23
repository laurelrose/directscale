using DirectScale.Disco.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class OrderData
    {
        public bool IsPaid { get; set; }
        public string BackofficeId { get; set; }
        public int SponsorId { get; set; }
        public int EnrollerId { get; set; }
        public int AssociateId { get; set; }
        public string Email { get; set; }
        public double USDTotal { get; set; }
        public double Subtotal { get; set; }
        public double Total { get; set; }
        public double Tax { get; set; }
        public double ShipCost { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderType OrderType { get; set; }
        public int OrderNumber { get; set; }
        public int LocalInvoiceNumber { get; set; }
        public string CompanyName { get; set; }
        public string ProductNames { get; set; }
        public string CompanyDomain { get; set; }
        public string ErrorDetails { get; set; }
        public string LogoUrl { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string EnrollerName { get; set; }
        public string EnrollerMobile { get; set; }
        public string EnrollerEmail { get; set; }
        public string SponsorName { get; set; }
        public string SponsorMobile { get; set; }
        public string SponsorEmail { get; set; }
        public string DateShipped { get; set; }
        public int AutoshipId { get; set; }
        public List<OrderLineItem> ProductInfo { get; set; }
        public string PaymentMethod { get; set; }
        public Address ShippingAddress { get; set; }
        public Address BillingAddress { get; set; }
    }
}
