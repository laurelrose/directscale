using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Helper
{
    public static class DateTimeFormat
    {
        public static string[] GetDateTimeFormat = new string[] {
                "yyyy-MM-dd",
                "MM-dd-yy h:mm tt",
                "MM-dd-yy hh:mm tt",
                "MM/dd/yy h:mm tt",
                "MM/dd/yy hh:mm tt",

                "M/d/yyyy h:mm", "M/d/yy h:mm",
                "M/d/yyyy HH:mm", "M/d/yy HH:mm",

                "M/dd/yy HH:mm", "M/dd/yy h:mm",
                "MM/d/yy HH:mm", "MM/d/yy h:mm",

                "MM/dd/yy H:mm",
                "MM/dd/yy HH:mm",
                "MM/dd/yyyy HH:mm",
                "MM/dd/yyyy", "M/dd/yyyy",
                "M/dd/yyyy", "M/d/yyyy",
                "M/dd/yy", "M/d/yy",
                "MM/dd/yy", "M/d/yy",

                "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                "M/dd/yyyy h:mm", "M/d/yyyy h:mm",
                "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm",
                "MM/d/yyyy HH:mm:ss.ffffff"
            };
}
}
