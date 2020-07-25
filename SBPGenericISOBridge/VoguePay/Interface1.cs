using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.VoguePay
{
    interface ITransactionData
    {
        string Account { get; set; }
        string TranRef { get; set; }
        string Amount { get; set; }
        string ChargeAmount { get; set; }
        string Currency { get; set; }
        string Terminal { get; set; }
        string TranCurrency { get; set; }
        string TranAmount { get; set; }
    }
}
