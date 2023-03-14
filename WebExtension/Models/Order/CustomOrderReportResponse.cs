using DirectScale.Disco.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class CustomOrderReportResponse
    {
        public string search { get; set; }
        public DateTime begin { get; set; }
        public DateTime end { get; set; }
        public List<OrderViewModel> orders { get; set; } = new List<OrderViewModel>();
    }

    public class OrderViewModel
    {
        public int OrderNumber { get; set; }
        public int AssociateId { get; set; }
        public string BackOfficeId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public int LocalInvoiceNumber { get; set; }
        public OrderType OrderType { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public double USDTotal { get; set; }
        public double USDSubTotal { get; set; }
        public string SpecialInstructions { get; set; }
        public string USDSubTotalFormatted { get; set; }
        public string USDTotalFormatted { get; set; }
        public List<Package> Packages { get; set; }
        public List<Payment> Payments { get; set; }

        public OrderViewModel(DirectScale.Disco.Extension.Order order)
        {
            if (order != null)
            {
                OrderNumber = order.OrderNumber;
                AssociateId = order.AssociateId;
                BackOfficeId = order.BackofficeId;
                Name = order.Name;
                OrderType = order.OrderType;
                LocalInvoiceNumber = order.LocalInvoiceNumber;
                Status = order.Status;
                InvoiceDate = order.InvoiceDate;
                OrderDate = order.OrderDate;
                USDSubTotal = order.USDSubTotal;
                USDTotal = order.USDTotal;
                SpecialInstructions = order.SpecialInstructions;
                Packages = order.Packages.Select(p => new Package { CountryCode = p.ShippingAddress.CountryCode }).ToList();
                Payments = order.Payments.Select(p => new Payment { Reference = p.Reference, Response = p.PaymentResponse }).ToList();
            }
        }
    }

    public class OrderCountry
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class Package
    {
        public string CountryCode { get; set; }
    }

    public class Payment
    {
        public string Reference { get; set; }
        public string Response { get; set; }
    }
}
