using Newtonsoft.Json;
using System;
using System.Linq;
using WebExtension.Services.ZiplingoEngagementService.Model;
using WebExtension.Services.ZiplingoEngagement.Model;
using DirectScale.Disco.Extension.Services;
using DirectScale.Disco.Extension;
using System.Collections.Generic;
using DirectScale.Disco.Extension.Hooks.Commissions;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Net.Security;
using WebExtension.Repositories;
using WebExtension.Services.DailyRun.Models;
using System.Text.RegularExpressions;

namespace WebExtension.Services.ZiplingoEngagementService
{
    public interface IZiplingoEngagementService
    {
        void CallOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship, bool isRewardPoint=false, int sponsorId=0);
        void CreateEnrollContact(Order order);
        void CreateContact(Application req, ApplicationResponse response);
        void UpdateContact(Associate req);
        void ResetSettings(CommandRequest commandRequest);
        void SendOrderShippedEmail(int packageId, string trackingNumber);
        void AssociateBirthDateTrigger();
        void AssociateWorkAnniversaryTrigger();
        EmailOnNotificationEvent OnNotificationEvent(NotificationEvent notification);
        void FiveDayRunTrigger(List<AutoshipInfo> autoships);
        void AssociateStatusChangeTrigger(int associateId, int oldStatusId, int newStatusId);
        void ExpirationCardTrigger(List<CardInfo> cardinfo);
        LogRealtimeRankAdvanceHookResponse LogRealtimeRankAdvanceEvent(LogRealtimeRankAdvanceHookRequest req);
        void UpdateAssociateType(int associateId, string oldAssociateType, string newAssociateType, int newAssociateTypeId);
        void ExecuteCommissionEarned();
        void SentNotificationOnServiceExpiryBefore2Weeks();
        void CreateAutoshipTrigger(Autoship autoshipInfo);
        void AssociateStatusSync(List<GetAssociateStatusModel> associateStatuses);
        void UpdateAutoshipTrigger(Autoship updatedAutoshipInfo);
        void CallFullRefundOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship);
    }
    public class ZiplingoEngagementService : IZiplingoEngagementService
    {
        private readonly IZiplingoEngagementRepository _ZiplingoEngagementRepository;
        private readonly ICompanyService _companyService;
        private readonly ICustomLogRepository _customLogRepository;
        private static readonly string ClassName = typeof(ZiplingoEngagementService).FullName;
        private readonly IOrderService _orderService;
        private readonly IAssociateService _distributorService;
        private readonly ITreeService _treeService;
        private readonly IRankService _rankService;
        private readonly IPaymentProcessingService _paymentProcessingService;
        private readonly IRewardPointsService _rewardPointsService;

        public ZiplingoEngagementService(IZiplingoEngagementRepository repository, 
            ICompanyService companyService,
            ICustomLogRepository customLogRepository, 
            IOrderService orderService, 
            IAssociateService distributorService, 
            ITreeService treeService, 
            IRankService rankService,
            IPaymentProcessingService paymentProcessingService,
            IRewardPointsService rewardPointsService
            )
        {
            _ZiplingoEngagementRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _distributorService = distributorService ?? throw new ArgumentNullException(nameof(distributorService));
            _treeService = treeService ?? throw new ArgumentNullException(nameof(treeService));
            _rankService = rankService ?? throw new ArgumentNullException(nameof(rankService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _paymentProcessingService = paymentProcessingService ?? throw new ArgumentNullException(nameof(paymentProcessingService));
        }

        public async void CallOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship, bool isRewardPoint=false, int sponsorId=0)
        {
            try
            {
                if (isRewardPoint == true && sponsorId != 0)
                {
                    order.AssociateId = sponsorId;
                }
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var associateRewardPointsData = await _rewardPointsService.GetAvailableRewardPoints(order.AssociateId);
                    var rewardPoints = 0.0; 
                    var CardLastFourDegit = _ZiplingoEngagementRepository.GetLastFoutDegitByOrderNumber(order.OrderNumber);
                    if (associateRewardPointsData.Length > 0)
                    {
                        rewardPoints = associateRewardPointsData.OrderByDescending(o => o.AvailableDate).First().RemainingBalance;
                    }
                    OrderData data = new OrderData
                    {
                        ShipMethodId = order.Packages.Select(a => a.ShipMethodId).FirstOrDefault(),
                        AssociateId = order.AssociateId,
                        BackofficeId = order.BackofficeId,
                        Email = order.Email,
                        InvoiceDate = order.InvoiceDate,
                        IsPaid = order.IsPaid,
                        LocalInvoiceNumber = order.LocalInvoiceNumber,
                        Name = order.Name,
                        Phone = order.BillPhone,
                        OrderDate = order.OrderDate,
                        OrderNumber = order.OrderNumber,
                        OrderType = order.OrderType,
                        TotalRewardPoints = rewardPoints, //Total Reward Points added
                        Tax = order.Totals.Select(m => m.Tax).FirstOrDefault(),
                        ShipCost = order.Totals.Select(m => m.Shipping).FirstOrDefault(),
                        Subtotal = order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                        USDTotal = order.USDTotal,
                        Total = order.Totals.Select(m => m.Total).FirstOrDefault(),
                        TotalPaid = order.Totals.Select(m => m.PaidAmount).FirstOrDefault(),
                        TotalDue = order.Totals.Select(m => m.TotalDue).FirstOrDefault(),
                        PaymentMethod = CardLastFourDegit,
                        ProductInfo = order.LineItems,
                        ProductNames = string.Join(",", order.LineItems.Select(x => x.ProductName).ToArray()),
                        ErrorDetails = FailedAutoship ? order.Payments.FirstOrDefault().PaymentResponse.ToString() : "",
                        CompanyDomain = company.Result.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress,
                        BillingAddress = order.BillAddress,
                        ShippingAddress = order.Packages?.FirstOrDefault()?.ShippingAddress
                    };
                    var strData = JsonConvert.SerializeObject(data);
                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = order.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0,0,"Error with in :"+eventKey, e.Message);
            }
        }

        public async void CallOrderZiplingoEngagementTriggerForShipped(OrderDetailModel order, string eventKey, bool FailedAutoship = false)
        {
            try
            {
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(order.Order.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(order.Order.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(order.Order.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(order.Order.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var CardLastFourDegit = _ZiplingoEngagementRepository.GetLastFoutDegitByOrderNumber(order.Order.OrderNumber);

                    // Track Shipping -----------------------------
                    var TrackingUrl = "";
                    var ShippingTrackingInfo = _ZiplingoEngagementRepository.GetShippingTrackingInfo();
                    if (order.TrackingNumber != null)
                    {
                        foreach (var shipInfo in ShippingTrackingInfo)
                        {
                            Match m1 = Regex.Match(order.TrackingNumber, shipInfo.TrackPattern, RegexOptions.IgnoreCase);
                            if (m1.Success)
                            {
                                TrackingUrl = shipInfo.ShippingUrl + order.TrackingNumber;
                                break;
                            }
                        }
                    }

                    // Track Shipping -----------------------------
                    OrderData data = new OrderData
                    {
                        ShipMethodId = order.ShipMethodId, //ShipMethodId added
                        AssociateId = order.Order.AssociateId,
                        BackofficeId = order.Order.BackofficeId,
                        Email = order.Order.Email,
                        InvoiceDate = order.Order.InvoiceDate,
                        IsPaid = order.Order.IsPaid,
                        LocalInvoiceNumber = order.Order.LocalInvoiceNumber,
                        Name = order.Order.Name,
                        Phone = order.Order.BillPhone,
                        OrderDate = order.Order.OrderDate,
                        OrderNumber = order.Order.OrderNumber,
                        OrderType = order.Order.OrderType,
                        Tax = order.Order.Totals.Select(m => m.Tax).FirstOrDefault(),
                        ShipCost = order.Order.Totals.Select(m => m.Shipping).FirstOrDefault(),
                        Subtotal = order.Order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                        USDTotal = order.Order.USDTotal,
                        Total = order.Order.Totals.Select(m => m.Total).FirstOrDefault(),
                        PaymentMethod = CardLastFourDegit,
                        ProductInfo = order.Order.LineItems,
                        ProductNames = string.Join(",", order.Order.LineItems.Select(x => x.ProductName).ToArray()),
                        ErrorDetails = FailedAutoship ? order.Order.Payments.FirstOrDefault().PaymentResponse.ToString() : "",
                        CompanyDomain = company.Result.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        TrackingNumber = order.TrackingNumber,
                        TrackingUrl = TrackingUrl,
                        Carrier = order.Carrier,
                        DateShipped = order.DateShipped,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        AutoshipId = order.AutoshipId,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress,
                        BillingAddress = order.Order.BillAddress,
                        ShippingAddress = order.Order.Packages?.FirstOrDefault()?.ShippingAddress
                    };
                    var strData = JsonConvert.SerializeObject(data);
                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = order.Order.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }

        public async void CallOrderZiplingoEngagementTriggerListForBirthDayWishes(List<AssociateInfoList> assoInfo, string eventKey)
        {
            try
            {
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    List<AssociateDetail> objassoListDetail = new List<AssociateDetail>();
                    foreach (var assodetail in assoInfo)
                    {
                        AssociateDetail objassDetail = new AssociateDetail();
                        int enrollerID = 0;
                        int sponsorID = 0;
                        if (_treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                        {
                            enrollerID = _treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                        }
                        if (_treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                        {
                            sponsorID = _treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                        }

                        Associate sponsorSummary = new Associate();
                        Associate enrollerSummary = new Associate();
                        if (enrollerID <= 0)
                        {
                            enrollerSummary = new Associate();
                        }
                        else
                        {
                            enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                        }
                        if (sponsorID > 0)
                        {
                            sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                        }
                        else
                        {
                            sponsorSummary = enrollerSummary;
                        }
                        AssociateInfo data = new AssociateInfo
                        {
                            AssociateId = assodetail.AssociateId,
                            EmailAddress = assodetail.EmailAddress,
                            Birthdate = assodetail.Birthdate,
                            FirstName = assodetail.FirstName,
                            LastName = assodetail.LastName,
                            CompanyDomain = company.Result.BackOfficeHomePageURL,
                            LogoUrl = settings.LogoUrl,
                            CompanyName = settings.CompanyName,
                            EnrollerId = enrollerSummary.AssociateId,
                            SponsorId = sponsorSummary.AssociateId,
                            CommissionActive = true,
                            EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                            EnrollerMobile = enrollerSummary.PrimaryPhone,
                            EnrollerEmail = enrollerSummary.EmailAddress,
                            SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                            SponsorMobile = sponsorSummary.PrimaryPhone,
                            SponsorEmail = sponsorSummary.EmailAddress
                        };
                        objassDetail.associateId = assodetail.AssociateId;
                        objassDetail.data = JsonConvert.SerializeObject(data);
                        objassoListDetail.Add(objassDetail);
                    }

                    var strData = objassoListDetail;
                    ZiplingoEngagementListRequest request = new ZiplingoEngagementListRequest { companyname = settings.CompanyName, eventKey = eventKey, dataList = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTriggersList");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }

        public async void CallOrderZiplingoEngagementTriggerListForWorkAnniversary(List<AssociateWorkAnniversaryInfoList> assoList, string eventKey)
        {
            try
            {
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    List<AssociateDetail> objassoListDetail = new List<AssociateDetail>();
                    foreach (var assodetail in assoList)
                    {
                        AssociateDetail objassDetail = new AssociateDetail();
                        int enrollerID = 0;
                        int sponsorID = 0;
                        if (_treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                        {
                            enrollerID = _treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                        }
                        if (_treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                        {
                            sponsorID = _treeService.GetNodeDetail(new NodeId(assodetail.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                        }

                        Associate sponsorSummary = new Associate();
                        Associate enrollerSummary = new Associate();
                        if (enrollerID <= 0)
                        {
                            enrollerSummary = new Associate();
                        }
                        else
                        {
                            enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                        }
                        if (sponsorID > 0)
                        {
                            sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                        }
                        else
                        {
                            sponsorSummary = enrollerSummary;
                        }
                        AssociateInfo data = new AssociateInfo
                        {
                            AssociateId = assodetail.AssociateId,
                            EmailAddress = assodetail.EmailAddress,
                            SignupDate = assodetail.SignupDate,
                            TotalWorkingYears = assodetail.TotalWorkingYears,
                            FirstName = assodetail.FirstName,
                            LastName = assodetail.LastName,
                            CompanyDomain = company.Result.BackOfficeHomePageURL,
                            LogoUrl = settings.LogoUrl,
                            CompanyName = settings.CompanyName,
                            EnrollerId = enrollerSummary.AssociateId,
                            SponsorId = sponsorSummary.AssociateId,
                            CommissionActive = true,
                            EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                            EnrollerMobile = enrollerSummary.PrimaryPhone,
                            EnrollerEmail = enrollerSummary.EmailAddress,
                            SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                            SponsorMobile = sponsorSummary.PrimaryPhone,
                            SponsorEmail = sponsorSummary.EmailAddress
                        };
                        objassDetail.associateId = assodetail.AssociateId;
                        objassDetail.data = JsonConvert.SerializeObject(data);
                        objassoListDetail.Add(objassDetail);
                    }
                    var strData = objassoListDetail;
                    ZiplingoEngagementListRequest request = new ZiplingoEngagementListRequest { companyname = settings.CompanyName, eventKey = eventKey, dataList = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTriggersList");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }

        public async void CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(AssociateRankAdvancement assoRankAdvancementInfo, string eventKey)
        {
            try
            {
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(assoRankAdvancementInfo.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(assoRankAdvancementInfo.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(assoRankAdvancementInfo.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(assoRankAdvancementInfo.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var associateSummary = await _distributorService.GetAssociate(assoRankAdvancementInfo.AssociateId);
                    AssociateRankAdvancement data = new AssociateRankAdvancement
                    {
                        Rank = assoRankAdvancementInfo.Rank,
                        AssociateId = assoRankAdvancementInfo.AssociateId,
                        FirstName = assoRankAdvancementInfo.FirstName,
                        LastName = assoRankAdvancementInfo.LastName,
                        PrimaryPhone = associateSummary.PrimaryPhone,
                        EmailAddress = associateSummary.EmailAddress,
                        CompanyDomain = company.Result.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        RankName = assoRankAdvancementInfo.RankName,
                        CommissionActive = true,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress
                    };
                    var strData = JsonConvert.SerializeObject(data);
                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoRankAdvancementInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/RankAdvancement");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }

        public async void CallOrderZiplingoEngagementTriggerForAssociateChangeStatus(AssociateStatusChange assoStatusChangeInfo, string eventKey)
        {
            try
            {
                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(assoStatusChangeInfo.AssociateId));
                int enrollerID = 0;
                int sponsorID = 0;
                if (_treeService.GetNodeDetail(new NodeId(assoStatusChangeInfo.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                {
                    enrollerID = _treeService.GetNodeDetail(new NodeId(assoStatusChangeInfo.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                }
                if (_treeService.GetNodeDetail(new NodeId(assoStatusChangeInfo.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                {
                    sponsorID = _treeService.GetNodeDetail(new NodeId(assoStatusChangeInfo.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                }

                Associate sponsorSummary = new Associate();
                Associate enrollerSummary = new Associate();
                if (enrollerID <= 0)
                {
                    enrollerSummary = new Associate();
                }
                else
                {
                    enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                }
                if (sponsorID > 0)
                {
                    sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                }
                else
                {
                    sponsorSummary = enrollerSummary;
                }
                AssociateStatusChange data = new AssociateStatusChange
                {
                    OldStatusId = assoStatusChangeInfo.OldStatusId,
                    OldStatus = assoStatusChangeInfo.OldStatus,
                    NewStatusId = assoStatusChangeInfo.NewStatusId,
                    NewStatus = assoStatusChangeInfo.NewStatus,
                    AssociateId = assoStatusChangeInfo.AssociateId,
                    FirstName = assoStatusChangeInfo.FirstName,
                    LastName = assoStatusChangeInfo.LastName,
                    CompanyDomain = company.Result.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    EmailAddress = assoStatusChangeInfo.EmailAddress,
                    WebAlias = UserName
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoStatusChangeInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData, associateStatus = assoStatusChangeInfo.NewStatusId };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ChangeAssociateStatus");
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }

        public async void CreateEnrollContact(Order order)
        {
            try
            {
                var company = _companyService.GetCompany();
                var associateInfo = _distributorService.GetAssociate(order.AssociateId);
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(order.AssociateId));
                int enrollerID = 0;
                int sponsorID = 0;
                if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                {
                    enrollerID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                }
                if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                {
                    sponsorID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                }

                Associate sponsorSummary = new Associate();
                Associate enrollerSummary = new Associate();
                if (enrollerID <= 0)
                {
                    enrollerSummary = new Associate();
                }
                else
                {
                    enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                }
                if (sponsorID > 0)
                {
                    sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                }
                else
                {
                    sponsorSummary = enrollerSummary;
                }
                var associateOrders = await _orderService.GetOrdersByAssociateId(order.AssociateId, "");
                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = associateInfo.Result.AssociateId,
                    AssociateType = associateInfo.Result.AssociateBaseType,
                    BackOfficeId = associateInfo.Result.BackOfficeId,
                    firstName = associateInfo.Result.DisplayFirstName,
                    lastName = associateInfo.Result.DisplayLastName,
                    address = associateInfo.Result.Address.AddressLine1 + " " + associateInfo.Result.Address.AddressLine2 + " " + associateInfo.Result.Address.AddressLine3,
                    city = associateInfo.Result.Address.City,
                    birthday = associateInfo.Result.BirthDate,
                    CountryCode = associateInfo.Result.Address.CountryCode,
                    distributerId = associateInfo.Result.BackOfficeId,
                    phoneNumber = associateInfo.Result.TextNumber,
                    region = associateInfo.Result.Address.CountryCode,
                    state = associateInfo.Result.Address.State,
                    zip = associateInfo.Result.Address.PostalCode,
                    UserName = UserName,
                    WebAlias = UserName,
                    OrderDate = order.OrderDate,
                    CompanyUrl = company.Result.BackOfficeHomePageURL,
                    CompanyDomain = company.Result.BackOfficeHomePageURL,
                    LanguageCode = associateInfo.Result.LanguageCode,
                    CommissionActive = true,
                    emailAddress = associateInfo.Result.EmailAddress,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = associateInfo.Result.SignupDate.ToUniversalTime(),
                    ActiveAutoship = associateOrders.Where(o => o.OrderType == OrderType.Autoship).Any()
                };

                var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonZiplingoEngagementRequest, "Contact/CreateContactV2");
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in CreateEnrollContact", e.Message);
            }
        }

        public async void CreateContact(Application req, ApplicationResponse response)
        {
            try
            {
                if (req.AssociateId == 0)
                    req.AssociateId = response.AssociateId;

                if (string.IsNullOrEmpty(req.BackOfficeId))
                    req.BackOfficeId = response.BackOfficeId;

                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                int enrollerID = 0;
                int sponsorID = 0;
                if (_treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                {
                    enrollerID = _treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                }
                if (_treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                {
                    sponsorID = _treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                }

                Associate sponsorSummary = new Associate();
                Associate enrollerSummary = new Associate();
                if (enrollerID <= 0)
                {
                    enrollerSummary = new Associate();
                }
                else
                {
                    enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                }
                if (sponsorID > 0)
                {
                    sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                }
                else
                {
                    sponsorSummary = enrollerSummary;
                }
                var associateSummary = await _distributorService.GetAssociate(req.AssociateId);
                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = req.AssociateId,
                    AssociateStatus = req.StatusId,
                    AssociateType = req.AssociateBaseType,
                    BackOfficeId = req.BackOfficeId,
                    birthday = req.BirthDate,
                    address = req.ApplicantAddress.AddressLine1 + " " + req.ApplicantAddress.AddressLine2 + " " + req.ApplicantAddress.AddressLine3,
                    city = req.ApplicantAddress.City,
                    CommissionActive = true,
                    CountryCode = req.ApplicantAddress.CountryCode,
                    distributerId = req.BackOfficeId,
                    emailAddress = req.EmailAddress,
                    firstName = req.FirstName,
                    lastName = req.LastName,
                    phoneNumber = req.TextNumber,
                    region = req.ApplicantAddress.CountryCode,
                    state = req.ApplicantAddress.State,
                    zip = req.ApplicantAddress.PostalCode,
                    UserName = req.Username,
                    WebAlias = req.Username,
                    CompanyUrl = company.Result.BackOfficeHomePageURL,
                    CompanyDomain = company.Result.BackOfficeHomePageURL,
                    LanguageCode = req.LanguageCode,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = associateSummary.SignupDate.ToUniversalTime(),
                    ActiveAutoship = false
                };

                var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonZiplingoEngagementRequest, "Contact/CreateContactV2");

                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail("Enrollment");
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = req.AssociateId, companyname = settings.CompanyName, eventKey = "Enrollment", data = jsonZiplingoEngagementRequest };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in CreateContact", e.Message);
            }
        }

        public async void UpdateContact(Associate req)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var company = _companyService.GetCompany();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(req.AssociateId));
                var AssociateInfo = _distributorService.GetAssociate(req.AssociateId);
                int enrollerID = 0;
                int sponsorID = 0;
                if (_treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                {
                    enrollerID = _treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                }
                if (_treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                {
                    sponsorID = _treeService.GetNodeDetail(new NodeId(req.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                }

                Associate sponsorSummary = new Associate();
                Associate enrollerSummary = new Associate();
                if (enrollerID <= 0)
                {
                    enrollerSummary = new Associate();
                }
                else
                {
                    enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                }
                if (sponsorID > 0)
                {
                    sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                }
                else
                {
                    sponsorSummary = enrollerSummary;
                }
                var associateOrders = await _orderService.GetOrdersByAssociateId(req.AssociateId, "");
                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = AssociateInfo.Result.AssociateId,
                    AssociateStatus = AssociateInfo.Result.StatusId,
                    AssociateType = AssociateInfo.Result.AssociateBaseType,
                    BackOfficeId = AssociateInfo.Result.BackOfficeId,
                    birthday = AssociateInfo.Result.BirthDate,
                    address = AssociateInfo.Result.Address.AddressLine1 + " " + AssociateInfo.Result.Address.AddressLine2 + " " + AssociateInfo.Result.Address.AddressLine3,
                    city = AssociateInfo.Result.Address.City,
                    CommissionActive = true,
                    CountryCode = AssociateInfo.Result.Address.CountryCode,
                    distributerId = AssociateInfo.Result.BackOfficeId,
                    emailAddress = AssociateInfo.Result.EmailAddress,
                    firstName = AssociateInfo.Result.DisplayFirstName,
                    lastName = AssociateInfo.Result.DisplayLastName,
                    phoneNumber = AssociateInfo.Result.TextNumber,
                    region = AssociateInfo.Result.Address.CountryCode,
                    state = AssociateInfo.Result.Address.State,
                    zip = AssociateInfo.Result.Address.PostalCode,
                    LanguageCode = AssociateInfo.Result.LanguageCode,
                    UserName = UserName,
                    WebAlias = UserName,
                    CompanyUrl = company.Result.BackOfficeHomePageURL,
                    CompanyDomain = company.Result.BackOfficeHomePageURL,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = AssociateInfo.Result.SignupDate.ToUniversalTime(),
                    ActiveAutoship = associateOrders.Where(o => o.OrderType == OrderType.Autoship).Any()
                };

                var jsonReq = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonReq, "Contact/CreateContactV2");
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in UpdateContact", e.Message);
            }
        }

        public void AssociateStatusChangeTrigger(int associateId, int oldStatusId, int newStatusId)
        {
            try
            {
                AssociateStatusChange obj = new AssociateStatusChange();
                var distributorInfo = _distributorService.GetAssociate(associateId);
                obj.OldStatusId = oldStatusId;
                obj.OldStatus = _ZiplingoEngagementRepository.GetStatusById(oldStatusId);
                obj.NewStatusId = newStatusId;
                obj.NewStatus = _ZiplingoEngagementRepository.GetStatusById(newStatusId);
                obj.AssociateId = associateId;
                obj.FirstName = distributorInfo.Result.DisplayFirstName;
                obj.LastName = distributorInfo.Result.DisplayLastName;
                obj.EmailAddress = distributorInfo.Result.EmailAddress;
                CallOrderZiplingoEngagementTriggerForAssociateChangeStatus(obj, "ChangeAssociateStatus");
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in ChangeAssociateStatus", ex.Message);
            }
        }

        public async void SendOrderShippedEmail(int packageId, string trackingNumber)
        {
            var orderModel = new OrderDetailModel();
            var shipInfo = _ZiplingoEngagementRepository.GetOrderNumber(packageId);
            orderModel.TrackingNumber = trackingNumber;
            orderModel.Carrier = shipInfo.Carrier;
            orderModel.ShipMethodId = shipInfo.ShipMethodId;
            orderModel.DateShipped = shipInfo.DateShipped;
            orderModel.Order = await _orderService.GetOrderByOrderNumber(shipInfo.OrderNumber);
            if (orderModel.Order.OrderType == OrderType.Autoship)
            {
                var autoShipInfo = _ZiplingoEngagementRepository.GetAutoshipFromOrder(shipInfo.OrderNumber);
                orderModel.AutoshipId = autoShipInfo.AutoshipId;
                CallOrderZiplingoEngagementTriggerForShipped(orderModel, "AutoOrderShipped");
            }
            if (orderModel.Order.OrderType == OrderType.Standard)
            {
                CallOrderZiplingoEngagementTriggerForShipped(orderModel, "OrderShipped");
            }
        }

        public void AssociateBirthDateTrigger()
        {
            string eventKey = "AssociateBirthdayWishes";
            var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
            if (eventSetting != null && eventSetting?.Status == true)
            {
                var associateInfo = _ZiplingoEngagementRepository.AssociateBirthdayWishesInfo();
                if (associateInfo == null) return;

                for (int i = 0; i < associateInfo.Count; i = i + 100)
                {
                    List<AssociateInfoList> assoList = new List<AssociateInfoList>();
                    var items = associateInfo.Skip(i).Take(100);
                    foreach (var assoInfo in items)
                    {
                        AssociateInfoList objasso = new AssociateInfoList();
                        objasso.AssociateId = assoInfo.AssociateId;
                        objasso.Birthdate = assoInfo.Birthdate;
                        objasso.EmailAddress = assoInfo.EmailAddress;
                        objasso.FirstName = assoInfo.FirstName;
                        objasso.LastName = assoInfo.LastName;
                        assoList.Add(objasso);
                    }
                    CallOrderZiplingoEngagementTriggerListForBirthDayWishes(assoList, "AssociateBirthdayWishes");
                }
            }
        }

        public void AssociateWorkAnniversaryTrigger()
        {
            string eventKey = "AssociateWorkAnniversary";
            var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
            if (eventSetting != null && eventSetting?.Status == true)
            {
                var associateInfo = _ZiplingoEngagementRepository.AssociateWorkAnniversaryInfo();
                if (associateInfo == null) return;

                for (int i = 0; i < associateInfo.Count; i = i + 100)
                {
                    List<AssociateWorkAnniversaryInfoList> assoList = new List<AssociateWorkAnniversaryInfoList>();
                    var items = associateInfo.Skip(i).Take(100);
                    foreach (var assoInfo in items)
                    {
                        AssociateWorkAnniversaryInfoList objasso = new AssociateWorkAnniversaryInfoList();
                        objasso.AssociateId = assoInfo.AssociateId;
                        objasso.Birthdate = assoInfo.Birthdate;
                        objasso.EmailAddress = assoInfo.EmailAddress;
                        objasso.FirstName = assoInfo.FirstName;
                        objasso.LastName = assoInfo.LastName;
                        objasso.SignupDate = assoInfo.SignupDate;
                        objasso.TotalWorkingYears = assoInfo.TotalWorkingYears;
                        assoList.Add(objasso);
                    }
                    CallOrderZiplingoEngagementTriggerListForWorkAnniversary(assoList, eventKey);
                }
            }
        }

        public EmailOnNotificationEvent OnNotificationEvent(NotificationEvent notification)
        {
            switch (notification.EventType)
            {
                case EventType.RankAdvancement:
                    return CallRankAdvancementEvent(notification);
            }
            return null;
        }
        public LogRealtimeRankAdvanceHookResponse LogRealtimeRankAdvanceEvent(LogRealtimeRankAdvanceHookRequest req)
        {
            return LogRankAdvancement(req);
        }

        public LogRealtimeRankAdvanceHookResponse LogRankAdvancement(LogRealtimeRankAdvanceHookRequest req)
        {
            try
            {
                AssociateRankAdvancement obj = new AssociateRankAdvancement();
                var rankName = _rankService.GetRankName(req.NewRank).GetAwaiter().GetResult();
                var associateInfo = _distributorService.GetAssociate(req.AssociateId).GetAwaiter().GetResult();
                obj.Rank = req.NewRank;
                obj.RankName = rankName.ToString();
                obj.AssociateId = req.AssociateId;
                obj.FirstName = associateInfo.DisplayFirstName;
                obj.LastName = associateInfo.DisplayLastName;
                CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(obj, "RankAdvancement");
                return null;
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in RankAdvancement", ex.Message);
            }
            return null;
        }

        public IRestResponse CallZiplingoEngagementApi(string jsonData, string apiMethod)
        {
            var settings = _ZiplingoEngagementRepository.GetSettings();
            var apiUrl = settings.ApiUrl + apiMethod;
            var client = new RestClient(apiUrl);
            var messageRequest = new RestRequest(Method.POST);

            client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);

            messageRequest.AddHeader("cache-control", "no-cache");
            messageRequest.AddHeader("content-type", "application/json");

            messageRequest.AddParameter("application/json", jsonData, ParameterType.RequestBody);
            client.Timeout = 3600000;

            ServicePointManager.ServerCertificateValidationCallback = new
                RemoteCertificateValidationCallback
                (
                    delegate { return true; }
                );

            return client.Execute(messageRequest);
        }

        public EmailOnNotificationEvent CallRankAdvancementEvent(NotificationEvent notification)
        {
            string str = JsonConvert.SerializeObject(notification.EventValue);
            AssociateRankAdvancement obj = JsonConvert.DeserializeObject<AssociateRankAdvancement>(str);
            var rank = obj.Rank;
            var rankName = _rankService.GetRankName(rank);
            var distributorinfo = _distributorService.GetAssociate(notification.AssociateId);
            obj.RankName = rankName.ToString();
            obj.AssociateId = distributorinfo.Result.AssociateId;
            obj.FirstName = distributorinfo.Result.DisplayFirstName;
            obj.LastName = distributorinfo.Result.DisplayLastName;
            CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(obj, "RankAdvancement");
            return null;
        }

        public void ResetSettings(CommandRequest commandRequest)
        {
            try
            {
                _ZiplingoEngagementRepository.ResetSettings();
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in ResetSettings", ex.Message);
            }
        }

        public void UpdateAssociateType(int associateId, string oldAssociateType, string newAssociateType, int newAssociateTypeId)
        {
            try
            {
                var company = _companyService.GetCompany();
                var associateTypeModel = new AssociateTypeModel();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var associateSummary = _distributorService.GetAssociate(associateId);
                associateTypeModel.AssociateId = associateId;
                associateTypeModel.FirstName = associateSummary.Result.DisplayFirstName;
                associateTypeModel.LastName = associateSummary.Result.DisplayLastName;
                associateTypeModel.Email = associateSummary.Result.EmailAddress;
                associateTypeModel.Phone = (associateSummary.Result.TextNumber == "" || associateSummary.Result.TextNumber == null)
                    ? associateSummary.Result.PrimaryPhone
                    : associateSummary.Result.TextNumber;
                associateTypeModel.OldAssociateBaseType = oldAssociateType;
                associateTypeModel.NewAssociateBaseType = newAssociateType;
                associateTypeModel.CompanyDomain = company.Result.BackOfficeHomePageURL;
                associateTypeModel.LogoUrl = settings.LogoUrl;
                associateTypeModel.CompanyName = settings.CompanyName;

                var strData = JsonConvert.SerializeObject(associateTypeModel);

                AssociateTypeChange request = new AssociateTypeChange
                {
                    associateTypeId = newAssociateTypeId,
                    associateid = associateId,
                    companyname = settings.CompanyName,
                    eventKey = "AssociateTypeChange",
                    data = strData
                };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ChangeAssociateType");

            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in AssociateTypeChange", e.Message);
            }
        }

        public async void FiveDayRunTrigger(List<AutoshipInfo> autoships)
        {
            try
            {
                var company = await _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                for (int i = 0; i < autoships.Count; i = i + 100)
                {
                    List<FivedayAutoshipModel> autoshipList = new List<FivedayAutoshipModel>();
                    var items = autoships.Skip(i).Take(100);
                    foreach (var autoship in items)
                    {
                        FivedayAutoshipModel autoObj = new FivedayAutoshipModel();
                        autoObj.AssociateId = autoship.AssociateId;
                        autoObj.AutoshipId = autoship.AutoshipId;
                        autoObj.UplineID = autoship.UplineID;
                        autoObj.BackOfficeID = autoship.BackOfficeID;
                        autoObj.FirstName = autoship.FirstName;
                        autoObj.LastName = autoship.LastName;
                        autoObj.PrimaryPhone = autoship.PrimaryPhone;
                        autoObj.StartDate = autoship.StartDate;
                        autoObj.NextProcessDate = autoship.NextProcessDate;
                        autoObj.SponsorName = autoship.SponsorName;
                        autoObj.SponsorEmail = autoship.SponsorEmail;
                        autoObj.SponsorMobile = autoship.SponsorMobile;
                        autoObj.OrderNumber = autoship.OrderNumber;
                        autoObj.CompanyDomain = company.BackOfficeHomePageURL;
                        autoObj.LogoUrl = settings.LogoUrl;
                        autoObj.CompanyName = settings.CompanyName;
                        autoshipList.Add(autoObj);
                    }
                    CallFiveDayRunTrigger(autoshipList);
                }

            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in FiveDayAutoship", e.Message);
            }
        }

        public async void CallFiveDayRunTrigger(List<FivedayAutoshipModel> autoshipList)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var company = await _companyService.GetCompany();
                List<AssociateDetail> objautoshipListDetail = new List<AssociateDetail>();
                foreach (var autoship in autoshipList)
                {
                    AssociateDetail associateDetail = new AssociateDetail();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(autoship.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(autoship.AssociateId, 0), TreeType.Enrollment).Result?.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(autoship.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(autoship.AssociateId, 0), TreeType.Unilevel).Result?.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var associateSummary = await _distributorService.GetAssociate(autoship.AssociateId);
                    AssociateInfo data = new AssociateInfo
                    {
                        AssociateId = autoship.AssociateId,
                        EmailAddress = associateSummary.EmailAddress,
                        Birthdate = associateSummary.BirthDate.ToShortDateString(),
                        FirstName = associateSummary.LegalFirstName,
                        LastName = associateSummary.LegalLastName,
                        CompanyDomain = company.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        CommissionActive = true,
                        FivedayAutoshipDetails = autoship,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayLastName + ' ' + sponsorSummary.DisplayLastName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress
                    };
                    associateDetail.associateId = autoship.AssociateId;
                    associateDetail.data = JsonConvert.SerializeObject(data);
                    objautoshipListDetail.Add(associateDetail);
                }

                var strData = objautoshipListDetail;
                ZiplingoEngagementListRequest request = new ZiplingoEngagementListRequest { companyname = settings.CompanyName, eventKey = "FiveDayAutoship", dataList = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTriggersList");
            }

            catch (Exception e)
            {
                //_logger.LogError($"5DayTrigger", $"Exception occurred attempting to 5DayTrigger", e);
            }
        }

        public void ExpirationCardTrigger(List<CardInfo> cardinfo)
        {
            try
            {
                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();

                foreach (CardInfo info in cardinfo)
                {
                    try
                    {
                        AssociateCardInfoModel assoObj = new AssociateCardInfoModel();
                        assoObj.FirstName = info.FirstName;
                        assoObj.LastName = info.LastName;
                        assoObj.PrimaryPhone = info.PrimaryPhone;
                        assoObj.Email = info.PrimaryPhone;
                        assoObj.CardDate = info.ExpirationDate;
                        assoObj.CardLast4Degit = info.Last4DegitOfCard;
                        assoObj.CompanyDomain = company.Result.BackOfficeHomePageURL;
                        assoObj.LogoUrl = settings.LogoUrl;
                        assoObj.CompanyName = settings.CompanyName;

                        var strData = JsonConvert.SerializeObject(assoObj);
                        ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = info.AssociateId, companyname = settings.CompanyName, eventKey = "UpcomingExpiryCard", data = strData };
                        var jsonReq = JsonConvert.SerializeObject(request);
                        CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                    }
                    catch (Exception ex)
                    {
                        _customLogRepository.CustomErrorLog(0, 0, "Error with in UpcomingExpiryCard", ex.Message);
                    }
                }

            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in UpcomingExpiryCard", e.Message);
            }
        }

        public async void ExecuteCommissionEarned()
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();

                var paymentData = await _paymentProcessingService.FindPaidPayments(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date, "");

                for (int i = 0; i < paymentData.Length; i = i + 100)
                {
                    List<CommissionPayment> paymentDataList = new List<CommissionPayment>();
                    var items = paymentData.Skip(i).Take(100);
                    foreach (var data in items)
                    {
                        CommissionPayment pyModel = new CommissionPayment();
                        pyModel.BatchId = data.BatchId;
                        pyModel.Details = data.Details;
                        pyModel.MerchantId = data.MerchantId;
                        pyModel.PaymentUniqueId = data.PaymentUniqueId;
                        pyModel.CountryCode = data.CountryCode;
                        pyModel.TaxId = data.TaxId;
                        pyModel.Amount = data.Amount;
                        pyModel.Fees = data.Fees;
                        pyModel.Holdings = data.Holdings;
                        pyModel.Total = data.Total;
                        pyModel.ExchangeRate = data.ExchangeRate;
                        pyModel.ExchangeCurrencyCode = data.ExchangeCurrencyCode;
                        pyModel.PaymentStatus = data.PaymentStatus;
                        pyModel.DatePaid = data.DatePaid;
                        pyModel.TransactionNumber = data.TransactionNumber;
                        pyModel.CheckNumber = data.CheckNumber;
                        pyModel.ErrorMessage = data.ErrorMessage;
                        pyModel.MerchantCustomFields = data.MerchantCustomFields;
                        pyModel.AssociateId = data.AssociateId;
                        paymentDataList.Add(pyModel);
                    }
                    CallExecuteCommissionEarnedTrigger(paymentDataList);
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in ExecuteCommissionEarned", e.Message);
            }
        }

        public async void CallExecuteCommissionEarnedTrigger(List<CommissionPayment> payments)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var company = await _companyService.GetCompany();

                List<AssociateDetail> objpayListDetail = new List<AssociateDetail>();
                foreach (var payment in payments)
                {
                    AssociateDetail associateDetail = new AssociateDetail();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(payment.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(payment.AssociateId, 0), TreeType.Enrollment).Result?.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(payment.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(payment.AssociateId, 0), TreeType.Unilevel).Result?.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = _distributorService.GetAssociate(enrollerID).Result;
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = _distributorService.GetAssociate(sponsorID).Result;
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var associateSummary = await _distributorService.GetAssociate(payment.AssociateId);
                    AssociateInfoCommissionEarned data = new AssociateInfoCommissionEarned
                    {
                        AssociateId = payment.AssociateId,
                        EmailAddress = associateSummary.EmailAddress,
                        Birthdate = associateSummary.BirthDate.ToShortDateString(),
                        FirstName = associateSummary.LegalFirstName,
                        LastName = associateSummary.LegalLastName,
                        CompanyDomain = company.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        CommissionActive = true,
                        MerchantCustomFields = payment.MerchantCustomFields,
                        CommissionDetails = MapCommissionPayment(payment),
                        CommissionPaymentDetails = payment.Details,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayFirstName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress
                    };
                    associateDetail.associateId = payment.AssociateId;
                    associateDetail.data = JsonConvert.SerializeObject(data);
                    objpayListDetail.Add(associateDetail);
                }

                var strData = objpayListDetail;
                ZiplingoEngagementListRequest request = new ZiplingoEngagementListRequest { companyname = settings.CompanyName, eventKey = "CommissionEarned", dataList = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTriggersList");
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "Exception occurred in ExecuteCommissionEarned ", JsonConvert.SerializeObject(e.Message));
            }
        }

        public CommissionPaymentModel MapCommissionPayment(CommissionPayment commission)
        {
            if (commission != null)
            {
                return new CommissionPaymentModel
                {
                    Id = commission.Id,
                    Amount = commission.Amount,
                    AssociateId = commission.AssociateId,
                    BatchId = commission.BatchId,
                    CheckNumber = commission.CheckNumber,
                    CountryCode = commission.CountryCode,
                    DatePaid = commission.DatePaid,
                    ErrorMessage = commission.ErrorMessage,
                    ExchangeCurrencyCode = commission.ExchangeCurrencyCode,
                    ExchangeRate = commission.ExchangeRate,
                    Fees = commission.Fees,
                    Holdings = commission.Holdings,
                    MerchantId = commission.MerchantId,
                    PaymentStatus = commission.PaymentStatus,
                    PaymentUniqueId = commission.PaymentUniqueId,
                    TaxId = commission.TaxId,
                    Total = commission.Total,
                    TransactionNumber = commission.TransactionNumber
                };
            }

            return null;
        }

        public void SentNotificationOnServiceExpiryBefore2Weeks()
        {
            var serviceInfo = _ZiplingoEngagementRepository.GetServiceExpirationInfoBefore2Weeks();
            if (serviceInfo.Count() > 0)
            {
                ExpirationServiceTrigger(serviceInfo);
            }
            else
            {
                _customLogRepository.CustomErrorLog(0, 0, "No Expiration Service Info Found", "No Expiration Service Info Found");
            }
        }

        public void ExpirationServiceTrigger(List<ServiceInfo> cardinfo)
        {
            try
            {
                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();

                foreach (ServiceInfo info in cardinfo)
                {
                    try
                    {
                        var strData = JsonConvert.SerializeObject(info);
                        ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = info.AssociateId, companyname = settings.CompanyName, eventKey = "UpcomingExpiryService", data = strData };
                        var jsonReq = JsonConvert.SerializeObject(request);
                        CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                    }
                    catch (Exception ex)
                    {
                        _customLogRepository.CustomErrorLog(0, 0, "Exception occurred in Get Service Expiring Info Before 2/4 weeks",ex.Message);
                    }
                }

            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Exception occurred attempting to 2/4 Week trigger", e.Message);
            }
        }

        public async void CreateAutoshipTrigger(Autoship autoshipInfo)
        {
            try
            {
                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
				var associateInfo = await _distributorService.GetAssociate(autoshipInfo.AssociateId);
				AutoshipInfoMap req = new AutoshipInfoMap();

                req.AssociateId = autoshipInfo.AssociateId;
                req.AutoshipId = autoshipInfo.AutoshipId;
                req.AutoshipType = autoshipInfo.AutoshipType.ToString();
                req.CurrencyCode = autoshipInfo.CurrencyCode;
                req.Custom = autoshipInfo.Custom;
                req.Frequency = autoshipInfo.Frequency.ToString();
                req.FrequencyString = autoshipInfo.FrequencyString;
                req.LastChargeAmount = autoshipInfo.LastChargeAmount;
                req.LastProcessDate = autoshipInfo.LastProcessDate;
                req.LineItems = autoshipInfo.LineItems;
                req.NextProcessDate = autoshipInfo.NextProcessDate;
                req.PaymentMerchantId = autoshipInfo.PaymentMerchantId;
                req.PaymentMethodId = autoshipInfo.PaymentMethodId;
                req.ShipAddress = autoshipInfo.ShipAddress;
				req.FirstName = associateInfo.DisplayFirstName;
				req.LastName = associateInfo.DisplayLastName;
				req.Email = associateInfo.EmailAddress;
				req.Phone = associateInfo.PrimaryPhone;
				req.ProductNames = String.Join(",", autoshipInfo.LineItems.Select(l => l.ProductName));
				req.ShipMethodId = autoshipInfo.ShipMethodId;
                req.StartDate = autoshipInfo.StartDate;
                req.Status = autoshipInfo.Status;
                req.SubTotal = autoshipInfo.SubTotal;
                req.TotalCV = autoshipInfo.TotalCV;
                req.TotalQV = autoshipInfo.TotalQV;

                var strData = JsonConvert.SerializeObject(req);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = autoshipInfo.AssociateId, companyname = settings.CompanyName, eventKey = "CreateAutoship", data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Exception occured in attempting CreateAutoshipTrigger", e.Message);
            }
        }

        public async void UpdateAutoshipTrigger(Autoship updatedAutoshipInfo)
        {
            try
            {
                var company = _companyService.GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
				var associateInfo = await _distributorService.GetAssociate(updatedAutoshipInfo.AssociateId);
				AutoshipInfoMap req = new AutoshipInfoMap();

                req.AssociateId = updatedAutoshipInfo.AssociateId;
                req.AutoshipId = updatedAutoshipInfo.AutoshipId;
                req.AutoshipType = updatedAutoshipInfo.AutoshipType.ToString();
                req.CurrencyCode = updatedAutoshipInfo.CurrencyCode;
                req.Custom = updatedAutoshipInfo.Custom;
                req.Frequency = updatedAutoshipInfo.Frequency.ToString();
                req.FrequencyString = updatedAutoshipInfo.FrequencyString;
                req.LastChargeAmount = updatedAutoshipInfo.LastChargeAmount;
                req.LastProcessDate = updatedAutoshipInfo.LastProcessDate;
                req.LineItems = updatedAutoshipInfo.LineItems;
                req.NextProcessDate = updatedAutoshipInfo.NextProcessDate;
                req.PaymentMerchantId = updatedAutoshipInfo.PaymentMerchantId;
                req.PaymentMethodId = updatedAutoshipInfo.PaymentMethodId;
                req.ShipAddress = updatedAutoshipInfo.ShipAddress;
				req.FirstName = associateInfo.DisplayFirstName;
				req.LastName = associateInfo.DisplayLastName;
				req.Email = associateInfo.EmailAddress;
				req.Phone = associateInfo.PrimaryPhone;
				req.ProductNames = String.Join(",", updatedAutoshipInfo.LineItems.Select(l => l.ProductName));
				req.ShipMethodId = updatedAutoshipInfo.ShipMethodId;
                req.StartDate = updatedAutoshipInfo.StartDate;
                req.Status = updatedAutoshipInfo.Status;
                req.SubTotal = updatedAutoshipInfo.SubTotal;
                req.TotalCV = updatedAutoshipInfo.TotalCV;
                req.TotalQV = updatedAutoshipInfo.TotalQV;

                var strData = JsonConvert.SerializeObject(req);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = updatedAutoshipInfo.AssociateId, companyname = settings.CompanyName, eventKey = "AutoshipChanged", data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception ex)
            {
            }
        }

        public void AssociateStatusSync(List<GetAssociateStatusModel> associateStatuses)
        {
            try
            {
                if (associateStatuses.Count != 0)
                {
                    foreach (var item in associateStatuses)
                    {
                        var associateSummary = _distributorService.GetAssociate(item.AssociateID).Result;
                        associateSummary.StatusId = item.CurrentStatusId;
                        UpdateContact(associateSummary);
                    }
                }
                else
                {
                    _customLogRepository.CustomErrorLog(0, 0, "No Data Found for associate status change", "");
                }

            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error while calling sync call for associate statuses", ex.Message);
            }

        }


        //FullRefundOrder
        public async void CallFullRefundOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship)
        {
            try
            {
                var eventSetting = _ZiplingoEngagementRepository.GetEventSettingDetail(eventKey);
                if (eventSetting != null && eventSetting?.Status == true)
                {
                    var company = _companyService.GetCompany();
                    var settings = _ZiplingoEngagementRepository.GetSettings();
                    int enrollerID = 0;
                    int sponsorID = 0;
                    if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment).Result.UplineId != null)
                    {
                        enrollerID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment)?.Result.UplineId.AssociateId ?? 0;
                    }
                    if (_treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel).Result.UplineId != null)
                    {
                        sponsorID = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Unilevel)?.Result.UplineId.AssociateId ?? 0;
                    }

                    Associate sponsorSummary = new Associate();
                    Associate enrollerSummary = new Associate();
                    if (enrollerID <= 0)
                    {
                        enrollerSummary = new Associate();
                    }
                    else
                    {
                        enrollerSummary = await _distributorService.GetAssociate(enrollerID);
                    }
                    if (sponsorID > 0)
                    {
                        sponsorSummary = await _distributorService.GetAssociate(sponsorID);
                    }
                    else
                    {
                        sponsorSummary = enrollerSummary;
                    }
                    var CardLastFourDegit = _ZiplingoEngagementRepository.GetLastFoutDegitByOrderNumber(order.OrderNumber);
                    RefundOrder data = new RefundOrder
                    {
                        AssociateId = order.AssociateId,
                        BackofficeId = order.BackofficeId,
                        Email = order.Email,
                        InvoiceDate = order.InvoiceDate,
                        IsPaid = order.IsPaid,
                        LocalInvoiceNumber = order.LocalInvoiceNumber,
                        Name = order.Name,
                        Phone = order.BillPhone,
                        OrderDate = order.OrderDate,
                        OrderNumber = order.OrderNumber,
                        OrderType = order.OrderType,
                        Tax = order.Totals.Select(m => m.Tax).FirstOrDefault(),
                        ShipCost = order.Totals.Select(m => m.Shipping).FirstOrDefault(),
                        Subtotal = order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                        USDTotal = order.USDTotal,
                        RefundAmount=order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                        RefundDate= DateTime.Now,
                        Total = order.Totals.Select(m => m.Total).FirstOrDefault(),
                        PaymentMethod = CardLastFourDegit,
                        ProductInfo = order.LineItems,
                        ProductNames = string.Join(",", order.LineItems.Select(x => x.ProductName).ToArray()),
                        ErrorDetails = FailedAutoship ? order.Payments.FirstOrDefault().PaymentResponse.ToString() : "",
                        CompanyDomain = company.Result.BackOfficeHomePageURL,
                        LogoUrl = settings.LogoUrl,
                        CompanyName = settings.CompanyName,
                        EnrollerId = enrollerSummary.AssociateId,
                        SponsorId = sponsorSummary.AssociateId,
                        EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                        EnrollerMobile = enrollerSummary.PrimaryPhone,
                        EnrollerEmail = enrollerSummary.EmailAddress,
                        SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                        SponsorMobile = sponsorSummary.PrimaryPhone,
                        SponsorEmail = sponsorSummary.EmailAddress,
                        BillingAddress = order.BillAddress,
                        ShippingAddress = order.Packages?.FirstOrDefault()?.ShippingAddress
                    };
                    var strData = JsonConvert.SerializeObject(data);
                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = order.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                    var jsonReq = JsonConvert.SerializeObject(request);
                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                }
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in :" + eventKey, e.Message);
            }
        }
    }
}
