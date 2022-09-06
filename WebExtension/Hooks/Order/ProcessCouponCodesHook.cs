using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Microsoft.Extensions.Logging;
using WebExtension.Repositories;

namespace WebExtension.Hooks.Order
{
    public class ProcessCouponCodesHook : IHook<ProcessCouponCodesHookRequest, ProcessCouponCodesHookResponse>
    {
        public const string ShareAndSave = "ShareAndSave";
        public const int ShareAndSaveCouponId = -999;
        private readonly IAssociateService _associateService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly ILogger<ProcessCouponCodesHook> _logger;
        private readonly ICustomLogRepository _customLogRepository;
        public ProcessCouponCodesHook(IAssociateService associateService,
            IRewardPointsService rewardPointsService, ILogger<ProcessCouponCodesHook> loggingService, ICustomLogRepository customLogRepository)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
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
                    var pointsBalance =  _rewardPointsService.GetRewardPoints(associateInfo.Result.AssociateId);
                    var point = (double)Math.Round(pointsBalance.Result, 2);
                    var adjusted = (double)Math.Round(adjustedSubtotal, 2);
                    var pointsToUse = (double)Math.Round(adjusted > point ? point : adjusted, 2);

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
                _customLogRepository.CustomErrorLog(request.AssociateId, 0, "Error in ProcessCouponCodesHook.ApplyShareAndSave", "Error : " + ex.Message);
                _logger.LogInformation($"ProcessCouponCodesHook.ApplyShareAndSave {request?.AssociateId} {request?.SubTotal} {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
    }
}
