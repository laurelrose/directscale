using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HookExtension.Helper.Models
{
    public class Pagination
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        //public string SortBy { get; set; }
        //public string SortDirection { get; set; }
    }
}
