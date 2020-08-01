using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.WalletPay
{
    public class WalletResponses
    {

        public class WalletInqResp
        {
            public string message { get; set; }
            public string response { get; set; }
            public object responsedata { get; set; }
            public Data data { get; set; }
        }

        public class Data
        {
            public string customerid { get; set; }
            public string firstname { get; set; }
            public string nuban { get; set; }
            public float availablebalance { get; set; }
            public string lastname { get; set; }
            public string fullname { get; set; }
            public string mobile { get; set; }
            public string phone { get; set; }
            public string gender { get; set; }
            public string email { get; set; }
            public string currencycode { get; set; }
            public int restind { get; set; }
        }

        public class WalletTranDetails
        {
            public string message { get; set; }
            public string response { get; set; }
            public object responsedata { get; set; }
            public Datum[] data { get; set; }
        }

        public class Datum
        {
            public DateTime TRA_DATE { get; set; }
            public string currencycode { get; set; }
            public double amt { get; set; }
            public int deb_cre_ind { get; set; }
            public DateTime val_date { get; set; }
            public string remarks { get; set; }
            public object BalanceAD { get; set; }
            public object BalanceAC { get; set; }
            public object Balance { get; set; }
        }
        public class WalletCompResp
        {
            public string message { get; set; }
            public string response { get; set; }
            public object responsedata { get; set; }
            public bool data { get; set; }
        }


    }
}
