﻿using System;

namespace WebExtension.Services.RewardPoints.Models
{
    public class CommissionPeriodInfo
    {
        public int CommissionPeriodId { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime CommitDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
