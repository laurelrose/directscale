﻿using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebExtension.Repositories;

namespace WebExtension.Hooks.Order
{
    public class GetCouponAdjustedVolumeHook : IHook<GetCouponAdjustedVolumeHookRequest, GetCouponAdjustedVolumeHookResponse>
    {
        private readonly ILogger<GetCouponAdjustedVolumeHook> _logger;
        private readonly ICustomLogRepository _customLogRepository;
        public GetCouponAdjustedVolumeHook(ILogger<GetCouponAdjustedVolumeHook> loggingService, ICustomLogRepository customLogRepository)
        {
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }
        public async Task<GetCouponAdjustedVolumeHookResponse> Invoke(GetCouponAdjustedVolumeHookRequest request, Func<GetCouponAdjustedVolumeHookRequest, Task<GetCouponAdjustedVolumeHookResponse>> func)
        {
            var result = await func(request);
            
            try
            {
                var shareAndSaveCoupon = request.Totals[0].Coupons.UsedCoupons
                           .FirstOrDefault(x => x.Info.CouponId == ProcessCouponCodesHook.ShareAndSaveCouponId);

                if (shareAndSaveCoupon != null)
                {
                    if (result.CouponAdjustedVolume.Qv > 0)
                    {
                        await _customLogRepository.SaveLog(0, (int)request?.Totals[0].OrderNumber, "GetCouponAdjustedVolumeHook", $"OriginalDiscount {shareAndSaveCoupon.Info.Discount} Adjusted Volume (QV/CV) {result.CouponAdjustedVolume.Qv} / {result.CouponAdjustedVolume.Cv} ", "", "", "", "", "");
                    }
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(request.Totals[0].OrderNumber, 0, "Error in GetCouponAdjustedVolumeHookResponse", "Error : " + e.Message);
                _logger.LogInformation($"Error adjust volume AutoshipCoupon orderNumber: {request?.Totals[0]?.OrderNumber} {e.Message}");
            }

            try
            {
                var firm2023Coupon = request.Totals[0].Coupons.UsedCoupons
                           .FirstOrDefault(x => x.Info.Code == ProcessCouponCodesHook.Firm2023);

                if (firm2023Coupon != null)
                {                    
                    if (result.CouponAdjustedVolume.Qv > 0)
                    {
                        await _customLogRepository.SaveLog(0, (int)request?.Totals[0].OrderNumber, "GetCouponAdjustedVolumeHook", $"OriginalDiscount {firm2023Coupon.Info.Discount} Adjusted Volume (QV/CV) {result.CouponAdjustedVolume.Qv} / {result.CouponAdjustedVolume.Cv} ", "", "", "", "", "");
                    }
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(request.Totals[0].OrderNumber, 0, "Error in GetCouponAdjustedVolumeHookResponse", "Error : " + e.Message);
                _logger.LogInformation($"Error adjust volume Coupon orderNumber: {request?.Totals[0]?.OrderNumber} {e.Message}");
            }


            return result;
        }
    }
}