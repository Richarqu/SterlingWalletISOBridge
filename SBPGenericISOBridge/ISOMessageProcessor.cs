using System;
using org.jpos.iso;
using ThreadPool = org.jpos.util.ThreadPool;
using System.Net.Sockets;
using log4net;
using System.Configuration;
using Newtonsoft.Json;
//using SterlingWalletISOBridge.VoguePay;
using SterlingWalletISOBridge.Validation;
using System.Reflection.Emit;
using ikvm.runtime;
using SterlingWalletISOBridge.WalletPay;

namespace SterlingWalletISOBridge
{
    class ISOMessageProcessor : ISORequestListener
    {
        private static string iv = ConfigurationManager.AppSettings["iv"];
        private static string key = ConfigurationManager.AppSettings["key"];
        private string text = ConfigurationManager.AppSettings["text"];
        private byte[] ivByte = System.Text.Encoding.UTF8.GetBytes(iv);
        private byte[] keyByte = System.Text.Encoding.UTF8.GetBytes(key);
        private string vogueAcct = ConfigurationManager.AppSettings["vogueAcct"];
        private string custID = ConfigurationManager.AppSettings["custID"];
        private string authKey = ConfigurationManager.AppSettings["authKey"];
        private string vogueAuth = ConfigurationManager.AppSettings["vogueAuth"];
        private string vogueBal = ConfigurationManager.AppSettings["vogueBal"];
        ISOMisc u = new ISOMisc();
        //private ThreadPool _pool;
        private static readonly ILog logger =
               LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ISOMessageProcessor() : base()
        {

        }

        public bool process(ISOSource isos, ISOMsg m)
        {
            //VogueRespCodes vogueRespCodes = new VogueRespCodes();
            //VogueTranResp vogueWdrResp = new VogueTranResp();
            //VoguePayApis voguePayApis = new VoguePayApis();
            //AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
            //ProcessTran processMsg = new ProcessTran();
            ProcessISO _processISO = new ProcessISO();
            //VogueTranProcess vogueTranProcess = new VogueTranProcess();
            WalletTranProcess walletTranProcess = new WalletTranProcess();
            try
            {
                string isotext = ISOUtil.dumpString(m.pack());
                Console.WriteLine("Aboout to process message: ");
                Console.WriteLine(isotext);
                var brk = u.BreakMsg(m);
                Console.WriteLine(brk);

                var rsp = @ConfigurationManager.AppSettings["DefaultRspCode"];

                string auth_id = string.Empty;
                string availBal = string.Empty;
                string ledBal = string.Empty;
                string trxnType = string.Empty;
                string charge = string.Empty;
                string trans_ref = string.Empty; 
                string isoBal = string.Empty;
                //get mti
                string mti = m.getMTI();

                switch (mti)
                {
                    case "0800":
                        rsp = "00";
                        break;
                    case "0200":
                        var iSOResult = _processISO.GetISODetails(m);
                        trxnType = iSOResult.procCode.Substring(0, 2);
                        trans_ref = trxnType + iSOResult.stan + iSOResult.rrn + iSOResult.terminalId;
                        if (iSOResult.isoCharge != "")
                        {
                            if (iSOResult.isoCharge.Substring(0, 1) == "D")
                            {
                                //Add Charges to Total Transaction Amount
                                charge = iSOResult.isoCharge.Substring(1,iSOResult.isoCharge.Length - 1);
                            }
                            else
                            {
                                //Deduct subcharge from Total Transaction Amount
                                charge = "-"+iSOResult.isoCharge.Substring(1, iSOResult.isoCharge.Length - 1);
                            }
                        }
                        var acc_Currency = string.Empty;
                        var appRsp = string.Empty;
                        //Process the various transaction types
                        switch (trxnType)
                        {
                            case "00":
                                var purResult = walletTranProcess.WalletTransaction(m, "purchase");
                                if (purResult != null)
                                {
                                    rsp = purResult.code;
                                    if (rsp == "00")
                                    {
                                        //get available bal and ledger bal and compute the isobal.
                                        availBal = purResult.data.AvailableBalance;
                                        ledBal = purResult.data.LedgerBalance;
                                        isoBal = u.GetISOBalanceFormat(purResult.data.Currency, availBal, ledBal);
                                        m.set(44, purResult.data.AuthorizationCode);
                                    }
                                }
                                else { rsp = "96"; logger.Error($"Purchase failed, purResult returns null"); }
                                break;
                            case "01":
                                var wdrResult = walletTranProcess.WalletTransaction(m, "withdrawal");
                                if (wdrResult != null)
                                {
                                    rsp = wdrResult.code;
                                    if (rsp == "00")
                                    {
                                        //get available bal and ledger bal and compute the isobal.
                                        availBal = wdrResult.data.AvailableBalance;
                                        ledBal = wdrResult.data.LedgerBalance;
                                        isoBal = u.GetISOBalanceFormat(wdrResult.data.Currency, availBal, ledBal);
                                        m.set(44, wdrResult.data.AuthorizationCode);
                                    }
                                }
                                else { rsp = "96"; logger.Error($"Withdrawal failed, wdrResult returns null"); }
                                break;
                            case "31":
                                var balResult = walletTranProcess.WalletBalance(m);
                                if (balResult != null || balResult.data != null)
                                {
                                    availBal = (balResult.data.availablebalance * 100).ToString();
                                    ledBal = (balResult.data.availablebalance * 100).ToString();
                                    isoBal = u.GetISOBalanceFormat("566", availBal, ledBal);
                                    rsp = "00";
                                }
                                else { rsp = "96"; logger.Error($"Inquiry failed, balResult returns null"); }

                                break;
                            case "37":
                                var balResult37 = walletTranProcess.WalletBalance(m);
                                if (balResult37 != null || balResult37.data != null)
                                {
                                    //get available bal and ledger bal and compute the isobal.
                                    availBal = (balResult37.data.availablebalance * 100).ToString();
                                    ledBal = (balResult37.data.availablebalance * 100).ToString();
                                    isoBal = u.GetISOBalanceFormat("566", availBal, ledBal);
                                    rsp = "00";
                                }
                                else { rsp = "96"; logger.Error($"Inquiry failed, balResult returns null"); }
                                break;
                            case "40":
                                var aTrfResult = walletTranProcess.WalletTransaction(m, "transfer");
                                if (aTrfResult != null)
                                {
                                    rsp = aTrfResult.code;
                                    if (rsp == "00")
                                    {
                                        //get available bal and ledger bal and compute the isobal.
                                        availBal = aTrfResult.data.AvailableBalance;
                                        ledBal = aTrfResult.data.LedgerBalance;
                                        isoBal = u.GetISOBalanceFormat(aTrfResult.data.Currency, availBal, ledBal);
                                        m.set(44, aTrfResult.data.AuthorizationCode);
                                    }
                                }
                                else { rsp = "96"; logger.Error($"Account transfer failed, aTrfResult returns null"); }
                                break;
                            case "50":
                                var trfResult = walletTranProcess.WalletTransaction(m, "transfer");
                                if (trfResult != null)
                                {
                                    rsp = trfResult.code;
                                    if (rsp == "00")
                                    {
                                        //get available bal and ledger bal and compute the isobal.
                                        availBal = trfResult.data.AvailableBalance;
                                        ledBal = trfResult.data.LedgerBalance;
                                        isoBal = u.GetISOBalanceFormat(trfResult.data.Currency, availBal, ledBal);
                                        m.set(44, trfResult.data.AuthorizationCode);
                                    }
                                }
                                else { rsp = "96"; logger.Error($"Transfer failed, trfResult returns null"); }
                                break;
                            default:
                                rsp = "96";
                                logger.Error($"Transaction type - {trxnType} in 0200 not captured.");
                                break;
                        }
                        break;
                    case "0420":
                        var iSOResult2 = _processISO.GetISODetails(m);
                        trxnType = iSOResult2.procCode.Substring(0, 2);
                        var reversal_Field = m.getString(90);
                        string origMessageType = reversal_Field.Substring(0, 4);
                        switch (origMessageType)
                        {
                            case "0100":
                                //preauth reversal
                                switch (trxnType)
                                {
                                    case "00":
                                        var authPurRevResult = walletTranProcess.WalletAuthTransaction(m, "pre-authReversal");
                                        if (authPurRevResult != null)
                                        {
                                            rsp = authPurRevResult.code;
                                            if (rsp == "00")
                                            {
                                                //get available bal and ledger bal and compute the isobal.
                                                availBal = authPurRevResult.data.AvailableBalance;
                                                ledBal = authPurRevResult.data.LedgerBalance;
                                                isoBal = u.GetISOBalanceFormat(authPurRevResult.data.Currency, availBal, ledBal);
                                            }
                                        }
                                        else { rsp = "96"; logger.Error($"pre-auth reversal failed, authPurRevResult returns null"); }
                                        break;
                                    default:
                                        rsp = "96";
                                        logger.Error($"Transaction type - {trxnType} in 0100 reversal not captured.");
                                        break;
                                }
                                break;
                            case "0200":
                                switch (trxnType)
                                {
                                    case "00":
                                        var purRevResult = walletTranProcess.WalletTransaction(m, "purchaseReversal");
                                        if (purRevResult != null)
                                        {
                                            rsp = purRevResult.code;
                                            if (rsp == "00")
                                            {
                                                //get available bal and ledger bal and compute the isobal.
                                                availBal = purRevResult.data.AvailableBalance;
                                                ledBal = purRevResult.data.LedgerBalance;
                                                isoBal = u.GetISOBalanceFormat(purRevResult.data.Currency, availBal, ledBal);
                                                //m.set(44, purResult.data.AuthorizationCode);
                                            }
                                        }
                                        else { rsp = "96"; logger.Error($"Purchase reversal failed, purRevResult returns null"); }
                                        break;
                                    case "01":
                                        var wdrRevResult = walletTranProcess.WalletTransaction(m, "withdrawalReversal");
                                        if (wdrRevResult != null)
                                        {
                                            rsp = wdrRevResult.code;
                                            if (rsp == "00")
                                            {
                                                //get available bal and ledger bal and compute the isobal.
                                                availBal = wdrRevResult.data.AvailableBalance;
                                                ledBal = wdrRevResult.data.LedgerBalance;
                                                isoBal = u.GetISOBalanceFormat(wdrRevResult.data.Currency, availBal, ledBal);
                                                //m.set(44, purResult.data.AuthorizationCode);
                                            }
                                        }
                                        else { rsp = "96"; logger.Error($"Withdrawal reversal failed, wdrRevResult returns null"); }
                                        break;
                                    case "40":
                                        var aTrfRevResult = walletTranProcess.WalletTransaction(m, "transferReversal");
                                        if (aTrfRevResult != null)
                                        {
                                            rsp = aTrfRevResult.code;
                                            if (rsp == "00")
                                            {
                                                //get available bal and ledger bal and compute the isobal.
                                                availBal = aTrfRevResult.data.AvailableBalance;
                                                ledBal = aTrfRevResult.data.LedgerBalance;
                                                isoBal = u.GetISOBalanceFormat(aTrfRevResult.data.Currency, availBal, ledBal);
                                                //m.set(44, purResult.data.AuthorizationCode);
                                            }
                                        }
                                        else { rsp = "96"; logger.Error($"Account transfer reversal failed, aTrfRevResult returns null"); }
                                        break;
                                    case "50":
                                        var trfRevResult = walletTranProcess.WalletTransaction(m, "transferReversal");
                                        if (trfRevResult != null)
                                        {
                                            rsp = trfRevResult.code;
                                            if (rsp == "00")
                                            {
                                                //get available bal and ledger bal and compute the isobal.
                                                availBal = trfRevResult.data.AvailableBalance;
                                                ledBal = trfRevResult.data.LedgerBalance;
                                                isoBal = u.GetISOBalanceFormat(trfRevResult.data.Currency, availBal, ledBal);
                                                //m.set(44, purResult.data.AuthorizationCode);
                                            }
                                        }
                                        else { rsp = "96"; logger.Error($"Transfer reversal failed, trfRevResult returns null"); }
                                        break;
                                    default:
                                        rsp = "96";
                                        logger.Error($"Transaction type - {trxnType} in 0200 reversal not captured.");
                                        break;
                                }
                                break;
                            default:
                                rsp = "96";
                                logger.Error($"Message type - {origMessageType} not captured for 0420 original message.");
                                break;
                        }
                        break;
                    case "0100":
                        var iSOResult3 = _processISO.GetISODetails(m);
                        trxnType = iSOResult3.procCode.Substring(0, 2);
                        switch (trxnType)
                        {
                            case "00":
                                var authPurResult = walletTranProcess.WalletAuthTransaction(m, "pre-auth");
                                if (authPurResult != null)
                                {
                                    rsp = authPurResult.code;
                                    if (rsp == "00")
                                    {
                                        auth_id = authPurResult.data.AuthorizationCode.Substring(authPurResult.data.AuthorizationCode.Length - 6,6);
                                        m.set(38, auth_id);
                                        m.set(44, authPurResult.data.AuthorizationCode);
                                    }
                                }
                                else { rsp = "96"; logger.Error($"pre-auth failed, authPurResult returns null"); }
                                break;
                            default:
                                rsp = "96";
                                logger.Error($"Transaction type - {trxnType} in 0100 not captured.");
                                break;
                        }
                        m.set("127.6", "11");
                        m.set("127.9", "NXXXXXXXXXXXXXXXXX");
                        break;
                    case "0220":
                        var iSOResult4 = _processISO.GetISODetails(m);
                        trxnType = iSOResult4.procCode.Substring(0, 2);
                        switch (trxnType)
                        {
                            case "00":
                                var authPurResult = walletTranProcess.WalletAuthTransaction(m, "completion");
                                if (authPurResult != null)
                                {
                                    rsp = authPurResult.code;
                                    //if (rsp == "00")
                                    //{
                                    //    auth_id = authPurResult.data.AuthorizationCode.Substring(authPurResult.data.AuthorizationCode.Length - 6, 6);
                                    //    m.set(38, auth_id);
                                    //    m.set(44, authPurResult.data.AuthorizationCode);
                                    //}
                                }
                                else { rsp = "96"; logger.Error($"pre-auth failed, authPurResult returns null"); }
                                break;
                            default:
                                rsp = "96";
                                logger.Error($"Transaction type - {trxnType} in 0220 not captured.");
                                break;
                        }
                        //trans_ref = trxnType + iSOResult4.stan + iSOResult4.rrn + iSOResult4.terminalId;
                        break;
                }

                //Set the Response Code
                m.setResponseMTI();
                m.set(39, rsp);
                if (isoBal != "")
                {
                    m.set(54, isoBal);
                }
                isos.send(m);

                //Display the breakdown
                isotext = ISOUtil.dumpString(m.pack());
                Console.WriteLine("Response Message: ");
                Console.WriteLine(isotext);
                brk = u.BreakMsg(m);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(brk);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Message sent ==>");
                Console.ForegroundColor = ConsoleColor.White;

            }
            catch (ISOException e)
            {
                logger.Error(e);
            }
            return true;
        }
    }
}
