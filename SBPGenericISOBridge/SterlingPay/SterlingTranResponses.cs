using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.SterlingPay
{
    class SterlingTranResponses
    {
    }
    public class FTReversalResp
    {
        public Ftresponseext FTResponseExt { get; set; }
    }

    public class Ftresponseext
    {
        public string ReferenceID { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseText { get; set; }
        public string Balance { get; set; }
        public string COMMAMT { get; set; }
        public string CHARGEAMT { get; set; }
        public string FTID { get; set; }
    }
    public class FundsTransResp
    {
        public Ftresponse FTResponse { get; set; }
    }

    public class Ftresponse
    {
        public string ReferenceID { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseText { get; set; }
        public string Balance { get; set; }
        public string COMMAMT { get; set; }
        public string CHARGEAMT { get; set; }
        public string FTID { get; set; }
    }
    public class AccountDetails
    {
        public string account { get; set; }
        public string isoBalance { get; set; }
        public string acctcurrency { get; set; }
        public string useableBal { get; set; }
        public string phoneNo { get; set; }
    }
    public class SterlingRevDTO
    {
        public int ID { get; set; }
        public string ReversalFTID { get; set; }
        public string FTID { get; set; }
        public bool IsReversed { get; set; }
        public string DateUpdated { get; set; }
        public string RequestPIN { get; set; }
        public string Balance { get; set; }
    }
    public class SterlingDTO
    {
        public string UniqueID { get; set; }
        public string Account { get; set; }
        public string TranAmount { get; set; }
        public string TranSurcharge { get; set; }
        public bool IsReversed { get; set; }
        public string TerminalID { get; set; }
        public string RequestPIN { get; set; }
        public string ReferenceID { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseText { get; set; }
        public string Balance { get; set; }
        public string Acctcurrency { get; set; }
        public string CommAMT { get; set; }
        public string FTID { get; set; }
        public string ReversalFTID { get; set; }
        public string ChargeAMT { get; set; }
        public string DateInserted { get; set; }
        public string DateUpdated { get; set; }
        public string TranType { get; set; }
        public string ATMTillAccount { get; set; }
    }
    public class SterlingGetDTO
    {
        public string UniqueID { get; set; }
        public string TerminalID { get; set; }
    }
    //public class ISODetails
    //{
    //    public string procCode { get; set; }
    //    public string debitAcct { get; set; }
    //    public string creditAcct { get; set; }
    //    public string amt { get; set; }
    //    public string isoCharge { get; set; }
    //    public string terminalId { get; set; }
    //    public string terminalLocation { get; set; }
    //    public string rrn { get; set; }
    //    public string stan { get; set; }
    //    public string debitCurrency { get; set; }
    //    public string revTranDet { get; set; }
    //    public string uniqueID { get; set; }
    //    public string structuredData { get; set; }
    //}
    public class ATMTillDetails
    {
        [JsonProperty("ATM_DETAILS")]
        public ATM_DETAILS ATM_DETAILS { get; set; }
    }

    public class ATM_DETAILS
    {
        [JsonProperty("Record")]
        public Record Record { get; set; }
    }

    public class Record
    {
        [JsonProperty("_ID")]
        public string _ID { get; set; }
        [JsonProperty("ATM.BRANCH.CODE")]
        public string ATMBRANCHCODE { get; set; }
        [JsonProperty("DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [JsonProperty("COMPANY.CODE")]
        public string COMPANYCODE { get; set; }
        [JsonProperty("DEF.CR.ACCT")]
        public string DEFCRACCT { get; set; }
        [JsonProperty("PROC.CODE")]
        public object PROCCODE { get; set; }
        [JsonProperty("CR.CCY")]
        public string CRCCY { get; set; }
        [JsonProperty("CR.ACCT")]
        public string CRACCT { get; set; }
        [JsonProperty("USE.DEF.ACCT")]
        public string USEDEFACCT { get; set; }
        [JsonProperty("CO.CODE")]
        public string COCODE { get; set; }
    }
    public class FTRequest
    {
        public FT_Request FT_Request { get; set; }
    }

    public class FT_Request
    {
        public string TransactionBranch { get; set; }
        public string TransactionType { get; set; }
        public string DebitAcctNo { get; set; }
        public string DebitCurrency { get; set; }
        public string CreditCurrency { get; set; }
        public string DebitAmount { get; set; }
        public string CreditAccountNo { get; set; }
        public string CommissionCode { get; set; }
        public string VtellerAppID { get; set; }
        public string narrations { get; set; }
        public string SessionId { get; set; }
        public string TrxnLocation { get; set; }
    }
    public class ResponseCode
    {
        public string Response(string code)
        {
            string rsp = string.Empty;
            switch (code)
            {
                case "00":
                    rsp = "00";
                    break;
                case "x1005":
                    rsp = "51";
                    break;
                case "RS_400":
                    rsp = "06";
                    break;
                case "RS_401":
                    rsp = "06";
                    break;
                case "OK":
                    rsp = "00";
                    break;
                case "CB02":
                    rsp = "01";
                    break;
            }
            return rsp;
        }
        public string Currency(string code)
        {
            string cur = string.Empty;
            switch (code)
            {
                case "566":
                    cur = "NGN";
                    break;
                case "840":
                    cur = "USD";
                    break;
                case "826":
                    cur = "GBP";
                    break;
                default:
                    cur = "NGN";
                    break;
            }
            return cur;
        }
    }
    public class VogueRecordDTO
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Currency { get; set; }
        public string AuthorizationCode { get; set; }
        public string AvailableBalance { get; set; }
        public string LedgerBalance { get; set; }
        public string UniqueID { get; set; }
        public string ATMTillAccount { get; set; }
        public string FTID { get; set; }
    }
    public class WalletRecordDTO
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Currency { get; set; }
        public string AuthorizationCode { get; set; }
        public string AvailableBalance { get; set; }
        public string LedgerBalance { get; set; }
        public string UniqueID { get; set; }
        public string ATMTillAccount { get; set; }
        public string FTID { get; set; }
    }
    public class UnlockTranDTO
    {
        public Unlockamount UnlockAmount { get; set; }
    }

    public class Unlockamount
    {
        public string LockID { get; set; }
    }

    public class LockTranDTO
    {
        public Lockamount LockAmount { get; set; }
    }

    public class Lockamount
    {
        public string account { get; set; }
        public string amount { get; set; }
        public string description { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }
    }

    public class LockTranResp
    {
        public Lockamountresponse LockAmountResponse { get; set; }
    }

    public class Lockamountresponse
    {
        public string Responsecode { get; set; }
        public string ResponseDescription { get; set; }
        public string LockID { get; set; }
    }

}
