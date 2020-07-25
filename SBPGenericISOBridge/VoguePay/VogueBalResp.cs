using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.VoguePay
{
    public class VogueBalResp
    {
        public string code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public string Currency { get; set; }
        public string AvailableBalance { get; set; }
        public string LedgerBalance { get; set; }
    }
}
