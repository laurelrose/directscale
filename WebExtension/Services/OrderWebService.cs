using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Models;
using WebExtension.Repositories;

namespace WebExtension.Services
{
    public interface IOrderWebService
    {
        Task<List<OrderViewModel>> GetFilteredOrders(string search, DateTime beginDate, DateTime endDate);
    }
    public class OrderWebService : IOrderWebService
    {
        private readonly IOrderWebRepository _orderWebRepository;
        private readonly IOrderService _orderService;
        private readonly ICurrencyService _currencyService;
        public OrderWebService(IOrderWebRepository orderWebRepository,
            IOrderService orderService, ICurrencyService currencyService)
        {
            _orderWebRepository = orderWebRepository ?? throw new ArgumentNullException(nameof(orderWebRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        }

        public async Task<List<OrderViewModel>> GetFilteredOrders(string search, DateTime beginDate, DateTime endDate)
        {
            try
            {
                var orderIds = _orderWebRepository.GetFilteredOrderIds(search, beginDate, endDate);
                if (orderIds.Count > 0)
                {
                    var orders = await _orderService.GetOrders(orderIds.ToArray());

                    return orders.Select(o =>
                    {
                        return new OrderViewModel(o)
                        {
                            USDTotalFormatted = o.USDTotal.ToString(),
                            USDSubTotalFormatted = o.USDSubTotal.ToString()
                        };
                    }).ToList();
                }
            }
            catch (Exception e)
            {

            }
            return new List<OrderViewModel>();
        }
    }
}
