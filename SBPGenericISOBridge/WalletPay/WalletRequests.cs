using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.WalletPay
{
    public class WalletRequests
    {
        public class WalletToWalletReq
        {
            public string amt { get; set; }
            public string toacct { get; set; }
            public string frmacct { get; set; }
            public string paymentRef { get; set; }
            public string remarks { get; set; }
            public int channelID { get; set; }
            public string CURRENCYCODE { get; set; }
            public int TransferType { get; set; }
        }


        public class WalletAuthResp
        {
            public string message { get; set; }
            public string response { get; set; }
            public object responsedata { get; set; }
            public string data { get; set; }
        }

        public class Data
        {
            public string LockId { get; set; }
        }

        public class WalletCompReq
        {
            public string Mobile { get; set; }
            public string Amount { get; set; }
            public string LockedId { get; set; }
            public string LockedBy { get; set; }
        }

        public class WalletAuthUnlock
        {
            public string TrxnReference { get; set; }
            public string AccountNumber { get; set; }
        }
    }
}
