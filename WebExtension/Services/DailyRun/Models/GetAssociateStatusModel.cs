﻿using System;

namespace WebExtension.Services.DailyRun.Models
{
    public class GetAssociateStatusModel
    {
        public DateTime last_modified{ get; set; }
        public int AssociateID { get; set; }
        public int CurrentStatusId { get; set; }
        public string StatusName { get; set; }
    }
}
