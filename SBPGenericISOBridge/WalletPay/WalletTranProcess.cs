using log4net;
using Newtonsoft.Json;
using org.jpos.iso;
using SterlingWalletISOBridge.SterlingPay;
using SterlingWalletISOBridge.VoguePay;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Emit;
using static SterlingWalletISOBridge.WalletPay.WalletPayResponses;
using static SterlingWalletISOBridge.WalletPay.WalletRequests;
using static SterlingWalletISOBridge.WalletPay.WalletResponses;

namespace SterlingWalletISOBridge.WalletPay
{
    public class WalletTranProcess
    {
        private string walletPoolAcct = ConfigurationManager.AppSettings["walletPool"];
        private string walletLockEPoint = ConfigurationManager.AppSettings["walletLockEPoint"];
        private string walletGetTranEP = ConfigurationManager.AppSettings["walletGetTransaction"];
        private string walletCompEPoint = ConfigurationManager.AppSettings["walletCompEPoint"];
        private static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WalletInqResp WalletBalance(ISOMsg m)
        {
            WalletApis walletApis = new WalletApis();
            ProcessISO processISO = new ProcessISO();
            WalletInqResp walletBalResp = new WalletInqResp();
            var iSOResult = processISO.GetISODetails(m);
            string debitAcct = iSOResult.debitAcct;
            try
            {
                string balEndpoint = ConfigurationManager.AppSettings["walletInqEPoint"];
                if (iSOResult != null)
                {
                    string param = $"req.mobile=0{iSOResult.debitAcct}";
                    var jsonResponse = walletApis.WalletGet(balEndpoint, param);
                    logger.Error($"Balance jsonResponse received is: {jsonResponse}");
                    var balRespRaw = string.IsNullOrEmpty(jsonResponse) ? null : JsonConvert.DeserializeObject<WalletInqResp>(jsonResponse);
                    logger.Error($"Balance raw response received is: {balRespRaw.data}");
                    if (balRespRaw != null && balRespRaw.response == "00")
                    {
                        walletBalResp = balRespRaw;
                    }
                    else { walletBalResp = null; }
                }
                else { walletBalResp = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method VogueBalance: {ex}");
                walletBalResp = null;
            }
            return walletBalResp;
        }
        public WalletTranRespJson WalletTransaction(ISOMsg m,string tranType)
        {
            WalletTranRespJson walletTranRespJson = new WalletTranRespJson();
            WalletTranDetails tranDetails = new WalletTranDetails();
            WalletApis walletApis = new WalletApis();
            string availBal = string.Empty;
            string tranAmt = string.Empty;
            string rsp = string.Empty;
            try
            {
                ProcessISO processISO = new ProcessISO();
                var iSOResult = processISO.GetISODetails(m);

                switch (tranType)
                {
                    case "transfer":
                    case "purchase":
                    case "withdrawal":
                        //get customer's wallet bal...
                        var getWalletBal = WalletBalance(m);
                        if (getWalletBal != null && getWalletBal.data != null)
                        {
                            //get available bal from Vogue Customer Account
                            availBal = getWalletBal.data.availablebalance.ToString();
                            //tranAmt = payload.Amount;
                            tranAmt = iSOResult.amt;
                            double charge = 0;
                            if (Math.Abs(Convert.ToDouble(iSOResult.isoCharge)) > 0)
                            {
                                charge = Math.Abs(Convert.ToDouble(iSOResult.isoCharge)) / 100;
                            }
                            if ((Convert.ToDouble(availBal)) > (Convert.ToDouble(tranAmt) + charge))
                            {
                                var trfResult = tranType == "transfer" ? DoTransfer(iSOResult, iSOResult.creditAcct) : DoTransfer(iSOResult, walletPoolAcct);
                                if (trfResult != null)
                                {
                                    rsp = trfResult.response;
                                    if (rsp == "00")
                                    {
                                        var walletBal = WalletBalance(m);
                                        if (tranType != "completion")
                                        {
                                            //For completion, do no insert new record, identify the pre-auth in the database and update record...with completion details...
                                            //insert record to DB
                                            InsertTransactionDet(trfResult, iSOResult, walletBal.data.availablebalance.ToString(), charge);
                                        }
                                        var dataInfo = new Datas()
                                        {
                                            AvailableBalance = (walletBal.data.availablebalance * 100).ToString(),
                                            Currency = "566",
                                            LedgerBalance = (walletBal.data.availablebalance * 100).ToString(),
                                            AuthorizationCode = ""
                                        };
                                        walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = dataInfo,
                                            message = "Successful"
                                        };
                                    }
                                    else
                                    {
                                        rsp = "06";
                                        //return response sterling do transfer is not successful
                                        logger.Error($"Transfer response from WalletBase for account - 0{iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(trfResult)} is {rsp}");
                                        rsp = trfResult.response == "51" ? "51" : rsp;
                                        walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = null,
                                            message = ""
                                        };
                                    }
                                }
                                else
                                {
                                    //return response sterling dotransfer failed
                                    rsp = "06";
                                    logger.Error($"Transfer response from Wallet core is null for pool account - {walletPoolAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                    walletTranRespJson = new WalletTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else
                            {
                                //return response that customer's balance at Vogue is too low
                                rsp = "51";
                                logger.Error($"Customer's balance for account - {iSOResult.debitAcct} and iso transaction {JsonConvert.SerializeObject(iSOResult)}, {Convert.ToDouble(availBal)} is less than transaction amount {Convert.ToDouble(tranAmt)}: rsp = 51");
                                walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }

                        }
                        else
                        {
                            //return response that unable to get balance from Vogue
                            rsp = "06";
                            logger.Error($"Get Wallet Balance response for account - {iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                            walletTranRespJson = new WalletTranRespJson()
                            {
                                code = rsp,
                                data = null,
                                message = ""
                            };
                        }
                        break;

                    case "transferReversal":
                    case "purchaseReversal":
                    case "withdrawalReversal":
                        //get previous transaction details.
                        string tranRef = iSOResult.procCode.Substring(0, 2) + iSOResult.revTranDet.Substring(4, 6) + iSOResult.rrn + iSOResult.terminalId;
                        string paramGetTran = $"TrxnID={tranRef}&UserID=0{iSOResult.debitAcct}";
                        var tranDetailsRaw = walletApis.WalletGet(walletGetTranEP, paramGetTran);

                        logger.Error($"raw response for get previous transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(tranDetailsRaw)}");
                        tranDetails = string.IsNullOrEmpty(tranDetailsRaw) ? null : JsonConvert.DeserializeObject<WalletTranDetails>(tranDetailsRaw);
                        if (tranDetails != null && tranDetails.response == "00")
                        {
                            //do transfer to reverse funds
                            string creditAcct = "0" + iSOResult.debitAcct;
                            iSOResult.debitAcct = iSOResult.procCode.Substring(0, 2) == "50" ? iSOResult.creditAcct : walletPoolAcct;
                            var revTrfResult = DoTransfer(iSOResult, creditAcct);
                            if (revTrfResult != null)
                            {
                                if(revTrfResult.response == "00")
                                {
                                    //update table for previous transaction
                                    var walletBal = WalletBalance(m);
                                    var getTranPayload = new SterlingGetDTO()
                                    {
                                        TerminalID = iSOResult.terminalId,
                                        UniqueID = tranRef
                                    };
                                    var toRevDetails = GetTransactionDet(getTranPayload);
                                    toRevDetails.Balance = walletBal.data.availablebalance.ToString();
                                    toRevDetails.IsReversed = true;
                                    toRevDetails.DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    UpdateTransactionDet(toRevDetails);
                                    //compute response
                                    var dataInfo = new Datas()
                                    {
                                        AvailableBalance = (walletBal.data.availablebalance * 100).ToString(),
                                        Currency = "566",
                                        LedgerBalance = (walletBal.data.availablebalance * 100).ToString(),
                                        AuthorizationCode = ""
                                    };
                                    walletTranRespJson = new WalletTranRespJson()
                                    {
                                        code = rsp,
                                        data = dataInfo,
                                        message = "Successful"
                                    };
                                }
                                else
                                {
                                    rsp = "06";
                                    //return response sterling do transfer is not successful
                                    logger.Error($"Transfer reversal response from WalletBase for account - 0{iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(revTrfResult)} is {revTrfResult.response}");
                                    rsp = revTrfResult.response == "51" ? "51" : rsp;
                                    walletTranRespJson = new WalletTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else
                            {
                                //return response sterling dotransfer failed
                                rsp = "06";
                                logger.Error($"Transfer reversal response from Wallet Dotransfer is null for pool account - {walletPoolAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                        }
                        else
                        {
                            rsp = "06";
                            logger.Error($"{tranType} - GetTranDetails failed for tranRef - {tranRef} and iso transaction - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                            walletTranRespJson = new WalletTranRespJson()
                            {
                                code = rsp,
                                data = null,
                                message = ""
                            };
                        }
                        break;
                    default:
                        walletTranRespJson = null;
                        break;
                }
               }
            catch (Exception ex)
            {
                logger.Error($"Exception at method VogueSendTransaction for trantype {tranType}: {ex}");
                walletTranRespJson = null;
            }
            return walletTranRespJson;
        }
        public WalletTranRespJson WalletAuthTransaction(ISOMsg m, string authType)
        {
            //send pre-auth to vogue
            //for successful auth response, lock transaction amount in vogue account with sterling
            //respond to customer after successful lock
            WalletTranRespJson walletTranRespJson = new WalletTranRespJson();
            try
            {
                //WalletTranDetails tranDetails = new WalletTranDetails();
                WalletAuthResp walletAuthResp = new WalletAuthResp();
                WalletApis walletApis = new WalletApis();
                ResponseCode respCode = new ResponseCode();
                VogueRespCodes vogueRespCodes = new VogueRespCodes();
                ProcessISO processISO = new ProcessISO();
                SterlingTranProcess sterlingTranPro = new SterlingTranProcess();
                WalletTranDetails walletTranDetails = new WalletTranDetails();
                WalletCompResp walletCompResp = new WalletCompResp();
                string rsp = string.Empty;
                double charge = 0;
                var iSOResult = processISO.GetISODetails(m);

                switch (authType)
                {
                    case "pre-auth":
                        //check bal on Vogue Account with Sterling
                        var getWalletBal = WalletBalance(m);
                        if (getWalletBal != null && getWalletBal.data != null)
                        {
                            charge = 0;
                            if (Math.Abs(Convert.ToDouble(iSOResult.isoCharge)) > 0)
                            {
                                charge = Math.Abs(Convert.ToDouble(iSOResult.isoCharge)) / 100;
                            }
                            if ((Convert.ToDouble(getWalletBal.data.availablebalance)) > ((Convert.ToDouble(iSOResult.amt) / 100) + charge))
                            {
                                //Do wallet lock
                                string param = $"req.mobile=0{iSOResult.debitAcct}&req.amount={(Convert.ToDouble(iSOResult.amt) / 100) + charge}&req.lockedBy={iSOResult.uniqueID}";
                                var walletLockResp = walletApis.WalletGet(walletLockEPoint, param);
                                logger.Error($"raw response for lock transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(walletLockResp)}");
                                walletAuthResp = string.IsNullOrEmpty(walletLockResp) ? null : JsonConvert.DeserializeObject<WalletAuthResp>(walletLockResp);
                                //if lock is successful, return success response
                                if (walletAuthResp != null)
                                {
                                    rsp = walletAuthResp.response == "00" ? "00" : "06";
                                    if (rsp == "00")
                                    {
                                        var walletBal = WalletBalance(m);
                                        //authorizationcode is to be passed in F44 while authid is to be randomly generated.
                                        //insert pre-auth
                                        var datas = new Datas()
                                        {
                                            //AuthorizationCode = walletAuthResp.data.LockId,
                                            AuthorizationCode = walletAuthResp.data,
                                            AvailableBalance = "",
                                            Currency = "",
                                            LedgerBalance = ""
                                        };
                                        walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = datas,
                                            message = ""
                                        };
                                        var authResult = new WalletPayResp()
                                        {
                                            message = walletAuthResp.data,
                                            data = null,
                                            response = "00",
                                            responsedata = ""
                                        };
                                        InsertTransactionDet(authResult, iSOResult, walletBal.data.availablebalance.ToString(), charge);
                                    }
                                    else
                                    {
                                        rsp = "06";
                                        logger.Error($"{authType} - LockAmount response from Wallet is {rsp} for account - {iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = null,
                                            message = ""
                                        };
                                    }
                                }
                                else
                                {
                                    //return response sterling dotransfer failed
                                    rsp = "06";
                                    logger.Error($"{authType} - LockAmount response from WalletAPI is {rsp} for account - {iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                    walletTranRespJson = new WalletTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else
                            {
                                //return response that Vogue account with Sterling has no response
                                rsp = "51";
                                logger.Error($"Wallet customer's balance for account - {iSOResult.debitAcct} and iso transaction {JsonConvert.SerializeObject(iSOResult)}, {Convert.ToDouble(getWalletBal.data.availablebalance)} is less than transaction amount {Convert.ToDouble((Convert.ToDouble(iSOResult.amt) / 100) + charge)}: rsp = 51");
                                walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                        }
                        else
                        {
                            rsp = "06";
                            logger.Error($"{authType} - GetBalance response from WalletAPI is null for account - {iSOResult.debitAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                            walletTranRespJson = new WalletTranRespJson()
                            {
                                code = rsp,
                                data = null,
                                message = ""
                            };
                        }
                        break;
                    //CAN PRE-AUTHREVERSAL DOUBLE AS COMPLETION IN WALLET TRANSACTION PROCESSING?
                    case "pre-authReversal":
                        //get previous transaction details.
                        //do unlock with it.
                        string tranRef = iSOResult.procCode.Substring(0, 2) + iSOResult.revTranDet.Substring(4, 6) + iSOResult.rrn + iSOResult.terminalId;
                        var unlockLoad = new WalletAuthUnlock()
                        {
                            AccountNumber = "0" + iSOResult.debitAcct,
                            TrxnReference = tranRef
                        };
                        var walletUnlockResp = walletApis.WalletPost(unlockLoad, walletCompEPoint);
                        logger.Error($"raw response for reverse unlock transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(walletUnlockResp)}");
                        walletCompResp = string.IsNullOrEmpty(walletUnlockResp) ? null : JsonConvert.DeserializeObject<WalletCompResp>(walletUnlockResp);
                        //if lock is successful, return success response
                        if (walletCompResp != null)
                        {
                            rsp = walletCompResp.response == "00" ? "00" : "06";
                            if (rsp == "00")
                            {
                                //update table for previous transaction
                                var walletBal = WalletBalance(m);
                                var getTranPayload = new SterlingGetDTO()
                                {
                                    TerminalID = iSOResult.terminalId,
                                    UniqueID = tranRef
                                };
                                var toRevDetails = GetTransactionDet(getTranPayload);
                                toRevDetails.Balance = walletBal.data.availablebalance.ToString();
                                toRevDetails.IsReversed = true;
                                toRevDetails.DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                UpdateTransactionDet(toRevDetails);
                                //compute response
                                var dataInfo = new Datas()
                                {
                                    AvailableBalance = (walletBal.data.availablebalance * 100).ToString(),
                                    Currency = "566",
                                    LedgerBalance = (walletBal.data.availablebalance * 100).ToString(),
                                    AuthorizationCode = ""
                                };
                                walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = dataInfo,
                                    message = "Successful"
                                };
                            }
                            else
                            {
                                rsp = "06";
                                logger.Error($"{authType} - pre-auth reversal response from Wallet is {rsp} for account - {iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                        }
                        else
                        {
                            rsp = "06";
                            logger.Error($"{authType} - pre-auth reversal unlock response from WalletAPI is null for account - {iSOResult.debitAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)}, rsp = 06");
                            walletTranRespJson = new WalletTranRespJson()
                            {
                                code = rsp,
                                data = null,
                                message = ""
                            };
                        }
                        break;
                    case "completion":
                        //get previous transaction details.
                        //do unlock with it.
                        //debit customer's account.
                        //if debit fails, attempt to lock account back.
                        string preAuthRef = iSOResult.procCode.Substring(0, 2) + iSOResult.revTranDet.Substring(4, 6) + iSOResult.rrn + iSOResult.terminalId;
                        var compUnlockLoad = new WalletAuthUnlock()
                        {
                            AccountNumber = "0" + iSOResult.debitAcct,
                            TrxnReference = preAuthRef
                        };
                        var walletCompUnlockResp = walletApis.WalletPost(compUnlockLoad, walletCompEPoint);
                        logger.Error($"raw response for completion unlock transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(walletCompUnlockResp)}");
                        walletCompResp = string.IsNullOrEmpty(walletCompUnlockResp) ? null : JsonConvert.DeserializeObject<WalletCompResp>(walletCompUnlockResp);
                        if (walletCompResp != null)
                        {
                            rsp = walletCompResp.response == "00" ? "00" : "06";
                            if (rsp == "00")
                            {
                                //debit customer's account.
                                var trfResult = DoTransfer(iSOResult, walletPoolAcct);
                                if (trfResult != null)
                                {
                                    rsp = trfResult.response;
                                    if (rsp == "00")
                                    {
                                        logger.Error($"Completion debit processed successfully for tran details: {JsonConvert.SerializeObject(iSOResult)}");
                                        var walletBal = WalletBalance(m);
                                        //For completion, do no insert new record, identify the pre-auth in the database and update record...with completion details...
                                        var getPreTranPayload = new SterlingGetDTO()
                                        {
                                            TerminalID = iSOResult.terminalId,
                                            UniqueID = preAuthRef
                                        };
                                        var toCompDetails = GetTransactionDet(getPreTranPayload);
                                        toCompDetails.Balance = walletBal.data.availablebalance.ToString();
                                        toCompDetails.IsReversed = true;
                                        toCompDetails.DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        UpdateTransactionDet(toCompDetails);
                                        var dataInfo = new Datas()
                                        {
                                            AvailableBalance = (walletBal.data.availablebalance * 100).ToString(),
                                            Currency = "566",
                                            LedgerBalance = (walletBal.data.availablebalance * 100).ToString(),
                                            AuthorizationCode = ""
                                        };
                                        walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = dataInfo,
                                            message = "Successful"
                                        };
                                    }
                                    else
                                    {
                                        rsp = "06";
                                        logger.Error($" completion transfer response from WalletBase for account - 0{iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(trfResult)} is {rsp}");
                                        //return response sterling do transfer is not successful
                                        //lock initial amount back
                                        //Do wallet lock. This lock should be able to lock amount in customer's account even if there is inssufficient funds.
                                        string reLockParam = $"req.mobile=0{iSOResult.debitAcct}&req.amount={(Convert.ToDouble(iSOResult.amt) / 100) + charge}&req.lockedBy={iSOResult.uniqueID}";
                                        var walletCompReLockResp = walletApis.WalletGet(walletLockEPoint, reLockParam);
                                        logger.Error($"raw response for relocking failed completion debit transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(walletCompReLockResp)}");

                                        walletTranRespJson = new WalletTranRespJson()
                                        {
                                            code = rsp,
                                            data = null,
                                            message = ""
                                        };
                                    }
                                }
                                else
                                {
                                    //return response sterling dotransfer failed
                                    rsp = "06";
                                    logger.Error($"Completion transfer response from Wallet core is null for pool account - {walletPoolAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                    //return response sterling do transfer is not successful
                                    //lock initial amount back
                                    //Do wallet lock. This lock should be able to lock amount in customer's account even if there is inssufficient funds.
                                    string reLockParam = $"req.mobile=0{iSOResult.debitAcct}&req.amount={(Convert.ToDouble(iSOResult.amt) / 100) + charge}&req.lockedBy={iSOResult.uniqueID}";
                                    var walletCompReLockResp = walletApis.WalletGet(walletLockEPoint, reLockParam);
                                    logger.Error($"raw response for relocking failed completion debit transaction with tran details: {JsonConvert.SerializeObject(iSOResult)} is: {JsonConvert.SerializeObject(walletCompReLockResp)}");

                                    walletTranRespJson = new WalletTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else
                            {
                                rsp = "06";
                                logger.Error($"{authType} - completion unlock response from Wallet is {rsp} for account - {iSOResult.debitAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");

                                walletTranRespJson = new WalletTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                        }
                        else
                        {
                            rsp = "06";
                            logger.Error($"{authType} - completion unlock response from WalletAPI is null for account - {iSOResult.debitAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)}, rsp = 06");
                            walletTranRespJson = new WalletTranRespJson()
                            {
                                code = rsp,
                                data = null,
                                message = ""
                            };
                        }
                        break;
                    default:
                        walletTranRespJson = null;
                        break;
                }

            }
            catch (Exception ex)
            {
                logger.Error($"{authType} - Exception at method VogueSendTransaction for trantype {authType}: {ex}");
                walletTranRespJson = null;
            }
            return walletTranRespJson;
        }
        public WalletPayResp DoTransfer(ISODetails iSORest, string creditAcct)
        {
            WalletApis walletApis = new WalletApis();
            WalletPayResp walletPayResp = new WalletPayResp();
            try
            {
                double charge = 0;
                if (Convert.ToDouble(iSORest.isoCharge) > 0)
                {
                    charge = Convert.ToDouble(iSORest.isoCharge) / 100;
                }

                var payload = new WalletToWalletReq()
                {
                    amt = ((Convert.ToDouble(iSORest.amt) / 100) + charge).ToString(),
                    channelID = 1,
                    CURRENCYCODE = "NGN",
                    frmacct = "0" + iSORest.debitAcct,
                    toacct = creditAcct,
                    paymentRef = iSORest.uniqueID,
                    remarks = iSORest.terminalLocation,
                    TransferType = 1
                };
                //call the getaccountfullinfo endpoint
                string isoBalance = string.Empty;

                string walletEndPoint = ConfigurationManager.AppSettings["walletPayEPoint"];
                string walletFTResp = walletApis.WalletPost(payload, walletEndPoint);

                logger.Error($"Raw FT Response from DoTransferPost method for account: 0{iSORest.debitAcct} is: {walletFTResp}");
                walletPayResp = string.IsNullOrEmpty(walletFTResp) ? null : JsonConvert.DeserializeObject<WalletPayResp>(walletFTResp);
                logger.Error($"Deserialized FT Response from DoTransferPost method for account: {iSORest.debitAcct} is: {walletPayResp}");
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method DoTransfer: {ex}");
                walletPayResp = null;
            }
            return walletPayResp;
        }
        private void UpdateTransactionDet(SterlingRevDTO input)
        {
            int row = 0;
            try
            {
                var query = $"UPDATE OneWallet_Transactions set IsReversed = @IsReversed, DateUpdated = @DateUpdated, ReversalFTID = @ReversalFTID, Balance = @Balance, RequestPIN = @RequestPIN where FTID = @FTID and ID = @ID";
                var _configuration = ConfigurationManager.AppSettings["con"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@IsReversed", input.IsReversed);
                        cmd.Parameters.AddWithValue("@DateUpdated", input.DateUpdated);
                        cmd.Parameters.AddWithValue("@ReversalFTID", input.ReversalFTID);
                        cmd.Parameters.AddWithValue("@RequestPIN", input.RequestPIN);
                        cmd.Parameters.AddWithValue("@FTID", input.FTID);
                        cmd.Parameters.AddWithValue("@ID", input.ID);
                        cmd.Parameters.AddWithValue("@Balance", input.Balance);
                        row = cmd.ExecuteNonQuery();
                        connect.Dispose();
                        connect.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method UpdateVogueConfig: {ex}");
            }
        }
        public void InsertTransactionDet(WalletPayResp trfResult,ISODetails iSOResult, string bal,double charge)
        {
            var input = new SterlingDTO()
            {
                Account = "0" + iSOResult.debitAcct,
                Acctcurrency = iSOResult.currency,
                Balance = bal,
                ChargeAMT = charge.ToString(),
                CommAMT = "",
                DateInserted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                DateUpdated = "",
                FTID = iSOResult.uniqueID,
                IsReversed = false,
                ReferenceID = iSOResult.uniqueID,
                RequestPIN = "",
                ResponseCode = trfResult.response,
                ResponseText = trfResult.message,
                ReversalFTID = "",
                TerminalID = iSOResult.terminalId,
                TranAmount = iSOResult.amt,
                TranSurcharge = iSOResult.isoCharge,
                UniqueID = iSOResult.uniqueID,
                TranType = iSOResult.procCode.Substring(0, 2),
                ATMTillAccount = walletPoolAcct
            };

            int i = 0;
            try
            {
                var query = $"Insert into OneWallet_Transactions values (@UniqueID,@Account,@TranAmount,@TranSurcharge,@IsReversed,@TerminalID,@RequestPIN,@ReferenceID,@ResponseCode,@ResponseText,@Balance,@Acctcurrency,@CommAMT,@FTID,@ReversalFTID,@ChargeAMT,@DateInserted,@DateUpdated,@TranType,@ATMTillAccount,@Code,@Message,@Currency,@AuthorizationCode,@AvailableBalance,@LedgerBalance);";
                var _configuration = ConfigurationManager.AppSettings["con"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@UniqueID", input.UniqueID);
                        cmd.Parameters.AddWithValue("@Account", input.Account);
                        cmd.Parameters.AddWithValue("@TranAmount", input.TranAmount);
                        cmd.Parameters.AddWithValue("@TranSurcharge", input.TranSurcharge);
                        cmd.Parameters.AddWithValue("@IsReversed", input.IsReversed);
                        cmd.Parameters.AddWithValue("@TerminalID", input.TerminalID);
                        cmd.Parameters.AddWithValue("@RequestPIN", input.RequestPIN);
                        cmd.Parameters.AddWithValue("@ReferenceID", input.ReferenceID);
                        cmd.Parameters.AddWithValue("@ResponseCode", input.ResponseCode);
                        cmd.Parameters.AddWithValue("@ResponseText", input.ResponseText);
                        cmd.Parameters.AddWithValue("@Balance", input.Balance);
                        cmd.Parameters.AddWithValue("@Acctcurrency", input.Acctcurrency);
                        cmd.Parameters.AddWithValue("@CommAMT", input.CommAMT);
                        cmd.Parameters.AddWithValue("@FTID", input.FTID);
                        cmd.Parameters.AddWithValue("@ReversalFTID", input.ReversalFTID);
                        cmd.Parameters.AddWithValue("@ChargeAMT", input.ChargeAMT);
                        cmd.Parameters.AddWithValue("@DateInserted", input.DateInserted);
                        cmd.Parameters.AddWithValue("@DateUpdated", input.DateUpdated);
                        cmd.Parameters.AddWithValue("@TranType", input.TranType);
                        cmd.Parameters.AddWithValue("@ATMTillAccount", input.ATMTillAccount);
                        cmd.Parameters.AddWithValue("@Code", "");
                        cmd.Parameters.AddWithValue("@Message", "");
                        cmd.Parameters.AddWithValue("@Currency", "");
                        cmd.Parameters.AddWithValue("@AuthorizationCode", "");
                        cmd.Parameters.AddWithValue("@AvailableBalance", "");
                        cmd.Parameters.AddWithValue("@LedgerBalance", "");
                        i = cmd.ExecuteNonQuery();
                        connect.Dispose();
                        connect.Close();
                        logger.Error($"Record with token: {input.RequestPIN} has update status: {i}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method InsertTransactionDet: {ex}");
            }
        }
        public SterlingRevDTO GetTransactionDet(SterlingGetDTO input)
        {
            string query = $"select ID, ReversalFTID, FTID, RequestPIN FROM OneWallet_Transactions where TerminalID = '{input.TerminalID}' and UniqueID = '{input.UniqueID}'";
            SterlingRevDTO sterlingRevDTO = new SterlingRevDTO();
            SqlDataReader sdr;
            int count;
            try
            {
                var _configuration = ConfigurationManager.AppSettings["con"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        sdr = cmd.ExecuteReader();
                        count = sdr.FieldCount;
                        while (sdr.Read())
                        {
                            sterlingRevDTO.ID = Convert.ToInt32(sdr["ID"]);
                            sterlingRevDTO.FTID = sdr["FTID"].ToString();
                            sterlingRevDTO.ReversalFTID = sdr["ReversalFTID"].ToString();
                            sterlingRevDTO.RequestPIN = sdr["RequestPIN"].ToString();
                            sterlingRevDTO.IsReversed = false;
                            sterlingRevDTO.DateUpdated = "";
                            sterlingRevDTO.Balance = "";
                        }
                        cmd.Dispose();
                    }
                    connect.Dispose();
                    connect.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method GetVogueConfig: {ex}");
            }
            return sterlingRevDTO;
        }
    }
}
