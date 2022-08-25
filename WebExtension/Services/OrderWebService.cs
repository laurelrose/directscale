using DirectScale.Disco.Extension;
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
        Task<List<string>> GetKitLevelFiveSkuList();
        Task<CommissionStats> GetCustomerStats(int associateId);
        Task<Kpi> GetKpi(int associateId, string kpiName);
    }
    public class OrderWebService : IOrderWebService
    {
        private readonly IOrderWebRepository _orderWebRepository;
        private readonly IOrderService _orderService;
        private readonly ICurrencyService _currencyService;
        private readonly IStatsService _statsService;

        public OrderWebService(IOrderWebRepository orderWebRepository,
            IOrderService orderService, ICurrencyService currencyService, IStatsService statsService)
        {
            _orderWebRepository = orderWebRepository ?? throw new ArgumentNullException(nameof(orderWebRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
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

        public async Task<List<string>> GetKitLevelFiveSkuList()
        {
            try
            {
                return _orderWebRepository.GetKitLevelFiveSkuList();
            }
            catch (Exception e)
            {

                return new List<string>();
            }
        }

        public async Task<CommissionStats> GetCustomerStats(int associateId)
        {
            CommissionStats stats = null;
            try
            {
                var statsList = await _statsService.GetStats(new int[] { associateId }, DateTime.Now);
                stats = statsList[associateId];
            }
            catch (Exception e)
            {

            }
            return stats;
        }

        public async Task<Kpi> GetKpi(int associateId, string kpiName)
        {
            Kpi kpi = null;
            try
            {
                var stats = await GetCustomerStats(associateId);
                kpi = stats.Kpis[kpiName];
            }
            catch (Exception e)
            {

            }
            return kpi;
        }
    }
}
