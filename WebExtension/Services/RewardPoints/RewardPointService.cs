using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebExtension.Services.RewardPoints
{
    public interface IRewardPointService
    {
        Task<int> GetRepAssociateId(int orderAssociateId);
    }

    public class RewardPointService : IRewardPointService
    {
        private readonly IRewardPointRepository _rewardPointRepository;

        public RewardPointService(IRewardPointRepository rewardPointRepository)
        {
            _rewardPointRepository = rewardPointRepository ?? throw new ArgumentNullException(nameof(rewardPointRepository));
        }

        public async Task<int> GetRepAssociateId(int orderAssociateId)
        {
            var repId = await _rewardPointRepository.GetRepAssociateId(orderAssociateId);
            if (repId < 1)
            {
                throw new Exception($"Unable to find a Rep in upline for Order Associate {orderAssociateId}");
            }

            return repId;
        }

        public async Task SaveRewardPointCredits(Order order)
        {
            
        }

        private async Task<bool> IsFirstTimeOrderPurchase(Order order, out List<OrderLineItem> orderLineItems)
        {
            var isFirstTimeOrder = true;
            if (order.Custom.Field1.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                isFirstTimeOrder = false;
            }
            else if (await _rewardPointRepository.GetFirstTimeOrderPurchaseCount(order.AssociateId) > 1)
            {
                isFirstTimeOrder = false;
            }

            return isFirstTimeOrder;
        }

        private void IsFirstTimeItemPurchase()
        {

        }

    }
}
