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
using Azure;

namespace WebExtension.Hooks.Order
{
    public class ProcessCouponCodesHook : IHook<ProcessCouponCodesHookRequest, ProcessCouponCodesHookResponse>
    {
        public static string ShareAndSave = "Share Rewards from ";
        public const string Firm2023 = "Firm2023";
        public string SourceName = "";
        public const int ShareAndSaveCouponId = -999;
        private readonly IAssociateService _associateService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly ILogger<ProcessCouponCodesHook> _logger;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly ICouponService _couponService;
        private readonly IItemService _itemService;
        public ProcessCouponCodesHook(IAssociateService associateService,
            IRewardPointsService rewardPointsService, ILogger<ProcessCouponCodesHook> loggingService, ICustomLogRepository customLogRepository, ICouponService couponService, IItemService itemService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _logger = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _couponService = couponService ?? throw new ArgumentNullException(nameof(couponService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
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
                //if(CustomerIDs.Contains(associateInfo.BackOfficeId))
                if (associateInfo.BackOfficeId == "19124")
                {
                    // Validate OrderTypes Standard and AutoShip
                    if (request.OrderType == OrderType.Standard || request.OrderType == OrderType.Autoship)
                    {
                        // Look up SKU 00860009468849 / ItemID 25
                        var itemFirm = request.LineItems.Where(i => i.ItemId == 25).FirstOrDefault();

                        if (itemFirm != null)
                        {
                            //_logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023: Item SKU {itemFirm.SKU} Qty: {itemFirm.Quantity} Total {itemFirm.ExtendedCost}");
                            _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"ProcessCouponCodesHook.ApplyFirm2023: Item SKU {itemFirm.SKU} Qty: {itemFirm.Quantity} Total {itemFirm.ExtendedCost}", "", "", "", "", "");
                            bool CouponCanBeUsed = true;
                            var couponUsage = _couponService.GetAssociateCouponUsage(request.AssociateId).Result.ToList();
                            if (couponUsage.Count > 0)
                            {
                                // Get usage of Firm2023 coupon by associate
                                var firmCouponUsage = couponUsage.Where(c => c.Info.Code == Firm2023).ToList();

                                foreach (var firmCoupon in firmCouponUsage)
                                {
                                    var startOfTheMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                                    //If coupon has been redeemed this month, coupon cannot be used in this order
                                    if (firmCoupon.DateUsed >= startOfTheMonth && firmCoupon.DateUsed < DateTime.Now)
                                        CouponCanBeUsed = false;
                                }
                            }

                            if (CouponCanBeUsed)
                            {

                                var usedCoupons = response.OrderCoupons.UsedCoupons?.ToList() ?? new List<OrderCoupon>();

                                var discountFirm2023 = itemFirm.ExtendedCost * 0.50;

                                usedCoupons.Add(new OrderCoupon(new Coupon
                                {
                                    Code = ShareAndSave,
                                    CouponId = ShareAndSaveCouponId,
                                    Discount = discountFirm2023

                                })
                                {
                                    DiscountAmount = discountFirm2023
                                });
                                _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"ProcessCouponCodesHook.ApplyFirm2023: Coupon will be applied for {discountFirm2023}", "", "", "", "", "");
                                //_logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023: Coupon will be applied for {discountFirm2023}");
                                response.OrderCoupons.DiscountTotal = response.OrderCoupons.DiscountTotal + discountFirm2023;
                                response.OrderCoupons.UsedCoupons = usedCoupons.ToArray();
                            }
                        }
                    }
                }
                else
                    _customLogRepository.SaveLog(request.AssociateId, 0, "ProcessCouponCodesHook", $"ProcessCouponCodesHook.ApplyFirm2023: Current AssociateID {associateInfo.BackOfficeId} CustomerID {associateInfo.AssociateId}", "", "", "", "", "");
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.AssociateId, 0, "Error in ProcessCouponCodesHook.ApplyFirm2023", "Error : " + ex.Message);
                _logger.LogInformation($"ProcessCouponCodesHook.ApplyFirm2023 {request?.AssociateId} {request?.SubTotal} {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public static List<string> CustomerIDs = new List<string>() {
            "19124","1912C","1912D","19130","19131","19133","19134","19136","19137","1913A","1913D","19142","19143","19144","19146","19148","19149","1914A",
            "1914C","1914D","19150","19152","19155","19157","19158","1915B","1915E","1915F","19160","19161","19166","19167","1916A","1916C","1916D","1916E",
            "19170","19171","19173","19177","1917A","1917C","1917D","1917E","1917F","19180","19181","19183","19185","19187","19188","1918B","1918F","19193",
            "19194","19196","19198","1919A","1919B","1919C","1919D","1919E","1919F","191A0","191A1","191A2","191A3","191A6","191A8","191A9","191AA","191AB",
            "191AC","191B0","191B1","191B2","191B6","191B8","191B9","191BB","191BD","191C0","191C1","191C9","191CF","191D1","191D2","191D5","191DB","191E2",
            "191E3","191E4","191ED","191F0","191F1","191F6","19205","19209","1920A","19211","19212","19215","19216","19217","19218","19219","1921A","1921B",
            "19222","19223","1922C","1922F","19232","19234","19235","19236","19239","1923D","1923E","19241","19242","19247","1924C","1924D","1924F","19253",
            "19255","19256","19258","1925B","1925C","1925D","19260","19261","19262","19263","19264","19266","1926D","19276","19279","1927A","1927B","1927E",
            "19284","1928A","19290","19296","1929B","1929C","192A7","192A9","192AA","192B1","192B2","192BB","192BF","192C0","192C1","192C2","192C8","192CA",
            "192D0","192D1","192D2","192D4","192D5","192D6","192DB","192DE","192DF","192E2","192E5","192E7","192E8","192EB","192F2","192F9","192FE","19302",
            "19303","19306","19309","1930D","1930F","19310","19311","19312","19315","19317","19318","1931B","1931D","19320","19321","19322","19329","1932D",
            "19336","19337","19338","19339","1933A","1933B","1933C","1933D","1933E","19346","19348","19349","1934B","1934D","1934E","19351","19353","1935C",
            "1936A","1936B","1937A","1937F","19380","19381","19382","19392","1939E","1939F","193A4","193AA","193BC","193C8","193C9","193D1","193D5","193D6",
            "193D7","193DC","193DD","193DF","193E7","193E8","193EB","193EF","193F1","193F3","193F7","193FC","193FE","19400","19405","19406","19409","1940A",
            "1940F","19410","19411","19412","19416","19417","19418","19419","1941A","1941C","1941E","19420","19422","19426","19428","1942A","1942C","1942D",
            "1942F","19431","19432","19433","19435","19437","1943B","1943C","1943D","1943F","19441","19442","19444","19445","19446","19448","1944B","1944C",
            "1944D","19451","19452","19454","19455","19457","1945A","1945B","1945E","1945E","19461","19464","19466","19468","1946B","1946C","1946F","19470",
            "19471","19472","1947A","1947B","1947C","1947D","1947E","19480","19485","19486","1948C","1948D","1948F","19490","19491","19493","19494","19496",
            "19497","19498","19499","1949C","1949E","1949F","194A2","194A4","194A5","194A6","194A8","194A9","194AD","194B0","194B1","194B2","194B8","194B9",
            "194BA","194BC","194BD","194C3","194C5","194C6","194C9","194CA","194D1","194D6","194D9","194DA","194DD","194E0","194E3","194EA","194EC","194ED",
            "194F3","194F4","194F6","194F7","194F8","194FA","194FF","19500","19502","19505","19506","1950A","1950B","1950E","1950F","19510","19511","19515",
            "19517","1951C","1951E","19522","19523","19525","1952A","1952C","1952D","19530","19531","19533","19534","19535","19536","1953A","1953B","1953E",
            "1953F","19541","19544","19547","19548","1954A","1954B","1954C","1954D","1954E","19553","19554","19558","19559","1955A","1955E","1955F","19560",
            "19561","19563","19564","19565","19566","19569","1956A","1956B","1956C","1956D","1956E","19570","19574","19575","19576","19579","1957B","1957C",
            "1957E","1957F","19582","19583","19584","19588","19589","1958B","1958C","1958D","1958F","19590","19591","19593","19594","19596","19597","19599",
            "1959A","1959B","1959D","1959E","195A0","195A1","195A2","195A3","195A6","195A8","195A9","195AC","195B0","195B5","195B7","195B9","195BA","195BB",
            "195BC","195BD","195BF","195C4","195C7","195C8","195C9","195CA","195CB","195CC","195CD","195D0","195D1","195D2","195D3","195D4","195D5","195D6",
            "195D7","195D9","195DA","195DB","195DD","195DE","195DF","195E2","195E3","195E4","195E6","195E7","195E9","195EC","195EE","195F2","195F3","195F4",
            "195F5","195F6","195FA","195FB","195FD","19601","19602","19603","19605","19606","19607","1960A","1960B","1960C","1960D","1960F","19610","19611",
            "19613","19614","19615","19618","1961E","19620","19621","19624","19625","19626","19628","19629","1962A","1962D","1962E","1962F","19631","19632",
            "19633","19634","19637","19638","19639","1963B","1963E","19640","19648","1964A","1964F","19650","19655","19659","1965A","1965E","19661","19664",
            "1966A","1966B","1966C","1966E","19672","19674","19677","19678","19679","1967B","1967D","1967E","19680","19682","19683","19688","19689","1968A",
            "1968B","1968D","1968E","1968F","19693","19695","1969C","196A0","196A1","196A2","196A3","196A7","196AC","196AD","196AE","196B5","196B6","196B8",
            "196BA","196BC","196C1","196C6","196C7","196C9","196CC","196D1","196D5","196D7","196D8","196DC","196DD","196DE","196DF","196E8","196EA","196EF",
            "196F2","196F3","196F4","196F7","196FE","196FF","19700","19703","19704","19705","19707","19708","1970B","1970C","19714","19715","19716","19718",
            "19719","1971A","1971C","1971E","19721","19722","19724","19725","19726","19727","19728","19729","1972B","1972C","19731","19732","19734","19736",
            "19737","1973A","1973C","1973D","1973E","19748","19749","1974B","1974C","19751","19752","19753","19754","19755","19756","19758","1975A","1975B",
            "1975C","1975E","1975F","19760","19761","19762","19765","19769","1976B","1976D","1976F","19771","19772","19774","19776","19779","1977B","1977E",
            "19780","19781","19782","19783","19786","19787","19788","1978A","1978C","1978C","1978E","1978F","19793","19794","19795","1979A","1979B","1979C",
            "1979F","197A1","197A2","197A6","197A7","197A8","197AB","197AD","197B0","197B3","197BC","197BD","197C3","197C6","197CA","197D0","197D1","197D2",
            "197D3","197D4","197D7","197D9","197DB","197DC","197DD","197E0","197E2","197E3","197E9","197EA","197EC","197EE","197F6","197F7","197F9","197FA",
            "19801","19802","19803","19803","19804","19808","1980D","1980E","19810","19811","19814","19815","1981B","1981D","19820","19822","19823","19827",
            "19828","19829","1982C","1982D","19832","19834","19835","19836","19838","1983A","1983C","1983D","1983E","1983F","19840","19843","19846","19848",
            "1984A","19850","19853","19854","19856","19857","19859","1985A","1985B","1985D","1985F","19860","19863","19865","19866","19866","19867","19868",
            "19869","1986C","1986D","19871","19872","19873","19874","19875","19876","19877","19878","1987A","1987B","1987C","1987D","1987E","1987F","19882",
            "19883","19884","19885","19886","19887","19888","19889","1988A","1988B","1988C","1988D","1988E","1988F","19890","19891","19892","19893","19894",
            "19895","19896","19897","19898","19899","1989A","1989B","1989C","1989D","1989E","1989F","198A0","198A1","198A3","198A5","198A5","198A6","198A8",
            "198A9","198AA","198AB","198AC","198AD","198B6","198B7","198B8","198BB","198BE","198BF","198C1","198C2","198C3","198C4","198C6"

        };
    }
}
