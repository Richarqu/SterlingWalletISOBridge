using org.jpos.iso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.VoguePay
{
    public class VogueTranDet : ITransactionData
    {
        public string Account { get; set; }
        public string TranRef { get; set; }
        public string Amount { get; set; }
        public string ChargeAmount { get; set; }
        public string Currency { get; set; }
        public string Terminal { get; set; }
        public string TranCurrency { get; set; }
        public string TranAmount { get; set; } 
    }
    public class VogueTranAuth
    {
        public string Account { get; set; }
        public string TranRef { get; set; }
        public string Amount { get; set; }
        public string ChargeAmount { get; set; }
        public string Currency { get; set; }
        public string Terminal { get; set; }
        public string LockRef { get; set; }
        public string TranCurrency { get; set; }
        public string TranAmount { get; set; }
    }
    public class ProcessTran
    {
        public VogueTranDet WdrPayload(ISOMsg m)
        {
            string chargeAmt = "00000000";
            string charge = m.getString(28);
            if (!string.IsNullOrEmpty(charge))
            {
                chargeAmt = !(charge.Substring(0, 1) == "D") ? "-" + charge.Substring(1, charge.Length - 1) : charge.Substring(1, charge.Length - 1);
            }
            VogueTranDet wdrPayload = new VogueTranDet();
            wdrPayload = new VogueTranDet
            {
                TranRef = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41),
                Account = m.getString(102),
                Amount = m.getString(4),
                ChargeAmount = chargeAmt,
                Terminal = m.getString(41),
                Currency = m.getString(49),
                TranCurrency = m.getString(50),
                TranAmount = m.getString(5)
            };
            return wdrPayload;
        }
        public VogueTranDet PurPayload(ISOMsg m)
        {
            string chargeAmt = "00000000";
            string charge = m.getString(28);
            if (!string.IsNullOrEmpty(charge))
            {
                chargeAmt = !(charge.Substring(0, 1) == "D") ? "-" + charge.Substring(1, charge.Length - 1) : charge.Substring(1, charge.Length - 1);
            }
            VogueTranDet purPayload = new VogueTranDet();
            purPayload = new VogueTranDet
            {
                TranRef = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41),
                Account = m.getString(102),
                Amount = m.getString(4),
                ChargeAmount = chargeAmt,
                Terminal = m.getString(41),
                Currency = m.getString(49),
                TranCurrency = m.getString(50),
                TranAmount = m.getString(5)
            };
            return purPayload;
        }
        public VogueTranAuth AuthPayload(ISOMsg m)
        {
            string chargeAmt = "00000000";
            string charge = m.getString(28);
            if (!string.IsNullOrEmpty(charge))
            {
                chargeAmt = !(charge.Substring(0, 1) == "D") ? "-" + charge.Substring(1, charge.Length - 1) : charge.Substring(1, charge.Length - 1);
            }
            VogueTranAuth preAuthPayload = new VogueTranAuth();
            preAuthPayload = new VogueTranAuth
            {
                TranRef = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41),
                Account = m.getString(102),
                Amount = m.getString(4),
                ChargeAmount = chargeAmt,
                Terminal = m.getString(41),
                Currency = m.getString(49),
                LockRef = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41),
                TranCurrency = m.getString(50),
                TranAmount = m.getString(5)
            };
            return preAuthPayload;
        }
        //Account,TranRef, LockRef, Amount, ChargeAmount, Currency, Terminal
    }
    public class VogueTranResp
    {
        public string data { get; set; }
    }
    public class VogueTranRespJson
    {
        public string code { get; set; }
        public string message { get; set; }
        public  Datas data { get; set; }
        //public Dictionary<string, Data> data { get; set; }
    }
    public class WalletTranRespJson
    {
        public string code { get; set; }
        public string message { get; set; }
        public Datas data { get; set; }
        //public Dictionary<string, Data> data { get; set; }
    }
    public class Datas
    {
        public string Currency { get; set; }
        public string AuthorizationCode { get; set; }
        public string AvailableBalance { get; set; }
        public string LedgerBalance { get; set; }
    }
}
