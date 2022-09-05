using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Microsoft.Extensions.Logging;

namespace WebExtension.Hooks.Order
{
    public class ProcessCouponCodesHook : IHook<ProcessCouponCodesHookRequest, ProcessCouponCodesHookResponse>
    {
        public const string ShareAndSave = "ShareAndSave";
        public const int ShareAndSaveCouponId = -999;
        private readonly IAssociateService _associateService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly ILogger<ProcessCouponCodesHook> _logger;
        public ProcessCouponCodesHook(IAssociateService associateService,
            IRewardPointsService rewardPointsService, ILogger<ProcessCouponCodesHook> loggingService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }
        public async Task<ProcessCouponCodesHookResponse> Invoke(ProcessCouponCodesHookRequest request, Func<ProcessCouponCodesHookRequest, Task<ProcessCouponCodesHookResponse>> func)
        {
            var result = await func(request);
            ApplyShareAndSave(request, ref result);
            return result;
        }
        private void ApplyShareAndSave(ProcessCouponCodesHookRequest request, ref ProcessCouponCodesHookResponse response)
        {
            try
            {
                var associateInfo = _associateService.GetAssociate(request.AssociateId);
                if (associateInfo != null)
                {
                    var adjustedSubtotal = (decimal)request.SubTotal - (decimal)response.OrderCoupons.DiscountTotal;
                    var usedCoupons = response.OrderCoupons.UsedCoupons?.ToList() ?? new List<OrderCoupon>();
                    var pointsBalance =  Convert.ToDecimal(_rewardPointsService.GetRewardPoints(associateInfo.Result.AssociateId));
                    var pointsToUse = (double)Math.Round(adjustedSubtotal > pointsBalance ? pointsBalance : adjustedSubtotal, 2);

                    usedCoupons.Add(new OrderCoupon(new Coupon
                    {
                        Code = ShareAndSave,
                        CouponId = ShareAndSaveCouponId,
                        Discount = pointsToUse

                    })
                    {
                        DiscountAmount = pointsToUse
                    });
                    response.OrderCoupons.DiscountTotal = response.OrderCoupons.DiscountTotal + pointsToUse;
                    response.OrderCoupons.UsedCoupons = usedCoupons.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"ProcessCouponCodesHook.ApplyShareAndSave {request?.AssociateId} {request?.SubTotal} {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
    }
}
