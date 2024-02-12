﻿using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Microsoft.Extensions.Logging;
using WebExtension.Repositories;
using Azure;
using WebExtension.Services;

namespace WebExtension.Hooks.Order
{
    public class ProcessCouponCodesHook : IHook<ProcessCouponCodesHookRequest, ProcessCouponCodesHookResponse>
    {
        public static string ShareAndSave = "Share Rewards from ";
        public const string Firm2023 = "Firm2023";
        public string SourceName = "";
        public const int ShareAndSaveCouponId = -999;
        public const int Firm2023CouponID = -998;
        private readonly IAssociateService _associateService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly ILogger<ProcessCouponCodesHook> _logger;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IOrderWebService _orderWebService;
        public ProcessCouponCodesHook(IAssociateService associateService,
            IRewardPointsService rewardPointsService, ILogger<ProcessCouponCodesHook> loggingService, ICustomLogRepository customLogRepository, ICouponService couponService, IOrderWebService orderWebService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));            
            _orderWebService = orderWebService ?? throw new ArgumentNullException(nameof(orderWebService));
        }
        public async Task<ProcessCouponCodesHookResponse> Invoke(ProcessCouponCodesHookRequest request, Func<ProcessCouponCodesHookRequest, Task<ProcessCouponCodesHookResponse>> func)
        {
            var result = await func(request);
            var applyShareAndSave = ("true".Equals(Environment.GetEnvironmentVariable("Feature_ApplyShareAndSave_truefalse"), StringComparison.OrdinalIgnoreCase));
            if (applyShareAndSave)
            {
                ApplyShareAndSave(request, ref result);
            }

            ApplyFirm2023(request, ref result);
            
            return result;
        }
        private void ApplyShareAndSave(ProcessCouponCodesHookRequest request, ref ProcessCouponCodesHookResponse response)
        {
            try
            {
                var associateInfo = _associateService.GetAssociate(request.AssociateId).Result;
                                
                // Only apply "Share And Save" to Associate Types 2 & 3.
                if (associateInfo != null && (associateInfo.AssociateType == 2 || associateInfo.AssociateType == 3))
                {
                    var adjustedSubtotal = (decimal)request.SubTotal - (decimal)response.OrderCoupons.DiscountTotal;
                    var usedCoupons = response.OrderCoupons.UsedCoupons?.ToList() ?? new List<OrderCoupon>();
                    var pointsBalance =  _rewardPointsService.GetRewardPoints(associateInfo.AssociateId);
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
        private void ApplyFirm2023(ProcessCouponCodesHookRequest request, ref ProcessCouponCodesHookResponse response)
        {
            try
            {
                var associateInfo = _associateService.GetAssociate(request.AssociateId).Result;

                // Check CustomerID in list
                if(CustomerIDs.Contains(associateInfo.BackOfficeId))
                //if (associateInfo.BackOfficeId == "19124") for testing with single client, uncomment
                {
                    // Validate OrderTypes Standard and AutoShip
                    if (request.OrderType == OrderType.Standard || request.OrderType == OrderType.Autoship)
                    {
                        // Look up SKU 00860009468849 / ItemID 25
                        var itemFirm = request.LineItems.Where(i => i.ItemId == 25).FirstOrDefault();

                        if (itemFirm != null)
                        {
                            //_logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023: Item SKU {itemFirm.SKU} Qty: {itemFirm.Quantity} Total {itemFirm.ExtendedCost}");
                            
                            bool CouponCanBeUsed = true;

                            var startOfTheMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                            var usage = _orderWebService.GetCouponUsageByAssociateID(request.AssociateId, Firm2023CouponID, startOfTheMonth, DateTime.Now);
                            _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"Coupon has been used {usage} times this month", "", "", "", "", "");
                            if (usage > 0)
                                CouponCanBeUsed = false;

                            if (CouponCanBeUsed)
                            {
                                var usedCoupons = response.OrderCoupons.UsedCoupons?.ToList() ?? new List<OrderCoupon>();
                               
                                var discountFirm2023 = itemFirm.ExtendedPrice * 0.50;

                                usedCoupons.Add(new OrderCoupon(new Coupon
                                {
                                    Code = Firm2023,
                                    CouponId = Firm2023CouponID,
                                    Discount = discountFirm2023

                                })
                                {
                                    DiscountAmount = discountFirm2023
                                });
                                _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"Coupon will be applied for -${discountFirm2023}", "", "", "", "", "");
                                //_logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023: Coupon will be applied for {discountFirm2023}");
                                response.OrderCoupons.DiscountTotal = response.OrderCoupons.DiscountTotal + discountFirm2023;
                                response.OrderCoupons.UsedCoupons = usedCoupons.ToArray();
                            }
                        }
                    }
                }
                /*else
                    _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"ProcessCouponCodesHook.ApplyFirm2023: Current AssociateID {associateInfo.BackOfficeId} CustomerID {associateInfo.AssociateId}", "", "", "", "", "");*/
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.AssociateId, 0, "Error in ProcessCouponCodesHook.ApplyFirm2023", "Error : " + ex.Message);
                _logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023 {request?.AssociateId} {request?.SubTotal} {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public static List<string> CustomerIDs = new List<string>() {
            "15F91"

        };
    }
}
