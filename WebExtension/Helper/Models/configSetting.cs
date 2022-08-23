using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Helper.Models
{
    public class configSetting
    {
        public string BaseURL { get; set; }
        public string DirectScaleSecret { get; set; }
        public string ExtensionSecrets { get; set; }
        public string Client { get; set; }
        public string Environment { get; set; }
    }
}
