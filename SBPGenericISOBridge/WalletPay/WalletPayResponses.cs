using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.WalletPay
{
    public class WalletPayResponses
    {
        public class WalletPayResp
        {
            public string message { get; set; }
            public string response { get; set; }
            public object responsedata { get; set; }
            public Data data { get; set; }
        }

        public class Data
        {
            public bool sent { get; set; }
        }
    }
}
