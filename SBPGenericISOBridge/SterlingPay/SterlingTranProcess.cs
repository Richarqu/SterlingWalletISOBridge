using log4net;
using Newtonsoft.Json;
using org.jpos.iso;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.SterlingPay
{
    public class SterlingTranProcess
    {
        //create balance method
        //create transfer method
        //create reversal method
        //create auth method
        //create reversal method

        private static readonly ILog logger =
        LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FTReversalResp DoFTReversal(ISOMsg m)
        {
            string tranBranch = string.Empty;
            ProcessISO _processISO = new ProcessISO();
            FTReversalResp fTRevResp = new FTReversalResp();
            SterlingPayApis sterlingApis = new SterlingPayApis();
            FTReversalReq fTRevReq = new FTReversalReq();
            //ValidationProcess vPro = new ValidationProcess();
            try
            {
                var iSOResult = _processISO.GetISODetails(m);

                //get ATM till details
                string tillBranch = string.Empty;
                string tillEndPoint = ConfigurationManager.AppSettings["atmTill"];
                string tillbranch = ConfigurationManager.AppSettings["tillBranch"];
                ATMTillDetails tillAcctRaw = new ATMTillDetails();

                string terminalID = iSOResult.terminalId;
                string tran_type = iSOResult.procCode.Substring(0, 2);

                if (terminalID.Substring(0, 4) == "1232" && tran_type == "01")
                {
                    string tillResp = sterlingApis.GetATMTillAcct(tillEndPoint, iSOResult.terminalId.Trim());
                    logger.Error($"ATM till account response for terminal id: {iSOResult.terminalId} is: {tillResp}");
                    tillAcctRaw = string.IsNullOrEmpty(tillResp) ? null : JsonConvert.DeserializeObject<ATMTillDetails>(tillResp);
                    tillBranch = tillAcctRaw.ATM_DETAILS.Record.COMPANYCODE;
                }
                else { tillBranch = tillbranch; }

                var input = new SterlingGetDTO()
                {
                    TerminalID = iSOResult.terminalId,
                    UniqueID = iSOResult.uniqueID
                };
                //use fiorano to get FT ID by unique id -- works better than below
                var origTranDet = GetTransactionDet(input);
                if (origTranDet != null && !string.IsNullOrEmpty(origTranDet.FTID))
                {
                    var load = new SterlingWalletISOBridge.FT_Request() { FTReference = origTranDet.FTID, TransactionBranch = tillBranch };
                    var payload = new FTReversalReq
                    {
                        FT_Request = load
                    };

                    string fTRevUrl = ConfigurationManager.AppSettings["ftRev"];
                    string rootUrl = ConfigurationManager.AppSettings["fioranoFTRevUrl"];
                    string fTRevResponse = sterlingApis.DoTransferPost(payload, fTRevUrl, rootUrl);
                    logger.Error($"Raw FT Response from DoFTReversal method for FT Reference: {origTranDet.FTID} is: {fTRevResponse}");
                    fTRevResp = string.IsNullOrEmpty(fTRevResponse) ? null : JsonConvert.DeserializeObject<FTReversalResp>(fTRevResponse);
                    logger.Error($"Deserialized Response from DoFTReversal method for FT Reference: {origTranDet.FTID} is: {fTRevResp}");
                    if (fTRevResp.FTResponseExt.ResponseCode == "00")
                    {
                        var updateInput = new SterlingRevDTO()
                        {
                            DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            FTID = origTranDet.FTID,
                            ID = origTranDet.ID,
                            IsReversed = true,
                            RequestPIN = iSOResult.debitAcct,
                            ReversalFTID = fTRevResp.FTResponseExt.FTID,
                            Balance = fTRevResp.FTResponseExt.Balance
                        };
                        UpdateTransactionDet(updateInput);
                    }
                }
                else { logger.Error($"Error in method DoFTReversal, could not get FT details for transaction with record: {JsonConvert.SerializeObject(iSOResult)}"); fTRevResp = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method DoFTReversal: {ex}");
                fTRevResp = null;
            }
            return fTRevResp;
        }
        public LockTranResp UnlockAmount(ISOMsg m, string account, ISODetails iSORest, string lockID, string msgType)
        {
            LockTranResp lockTranResp = new LockTranResp();
            SterlingPayApis sterlingApis = new SterlingPayApis();
            try
            {
                string endPoint = ConfigurationManager.AppSettings["unLockUrl"];
                string rootURL = ConfigurationManager.AppSettings["fioranoLockBase"];
                var amtLoad = new Unlockamount() { LockID = lockID };
                var payload = new UnlockTranDTO() { UnlockAmount = amtLoad };
                string rawUnlockResp = sterlingApis.DoFioranoPost(payload, endPoint, rootURL);
                logger.Error($"raw response for unlock transaction with tran details: {JsonConvert.SerializeObject(iSORest)} is: {rawUnlockResp}");
                lockTranResp = string.IsNullOrEmpty(rawUnlockResp) ? null : JsonConvert.DeserializeObject<LockTranResp>(rawUnlockResp);
                string tillAcct = ConfigurationManager.AppSettings["vogueSettlementAcct"];
                //update DB if successful
                if (lockTranResp.LockAmountResponse.Responsecode == "1")
                {
                    var updateInput = new VogueRecordDTO()
                    {
                        AuthorizationCode = iSORest.auth_id,
                        AvailableBalance = iSORest.amt,
                        Code = msgType,
                        Currency = iSORest.currency,
                        FTID = lockID,
                        LedgerBalance = iSORest.amt,
                        Message = lockTranResp.LockAmountResponse.ResponseDescription,
                        UniqueID = iSORest.uniqueID,
                        ATMTillAccount = tillAcct
                    };
                    UpdateVogueProcessedTransDet(updateInput);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception: {ex} in method UnlockAmount for unlock transaction details: {JsonConvert.SerializeObject(iSORest)} on account: {account}");
                lockTranResp = null;
            }
            return lockTranResp;
        }
        public LockTranResp LockAmount(ISOMsg m, string account, ISODetails iSORest)
        {
            LockTranResp lockTranResp = new LockTranResp();
            SterlingPayApis sterlingApis = new SterlingPayApis();
            try
            {
                string startDate = DateTime.Now.ToString("yyyy-MM-dd");
                string endDate = DateTime.Now.AddDays(365).ToString("yyyy-MM-dd");
                string lockEndpoint = ConfigurationManager.AppSettings["lockUrl"];
                string lockRootURL = ConfigurationManager.AppSettings["fioranoLockBase"];
                double charge = 0;
                if (Convert.ToDouble(iSORest.isoCharge) > 0)
                {
                    charge = Convert.ToDouble(iSORest.isoCharge) / 100;
                }
                string conAmt = ((Convert.ToDouble(iSORest.amt) / 100) + charge).ToString();
                var lockAmt = new Lockamount()
                {
                    account = account,
                    amount = conAmt,
                    description = iSORest.terminalLocation.Substring(0,30),
                    enddate = endDate,
                    startdate = startDate
                };

                var payload = new LockTranDTO
                {
                    LockAmount = lockAmt
                };
                string rawLockResp = sterlingApis.DoFioranoPost(payload, lockEndpoint, lockRootURL);
                logger.Error($"raw response for lock transaction with tran details: {JsonConvert.SerializeObject(iSORest)} is: {rawLockResp}");
                lockTranResp = string.IsNullOrEmpty(rawLockResp) ? null : JsonConvert.DeserializeObject<LockTranResp>(rawLockResp);
                if (lockTranResp.LockAmountResponse.Responsecode == "1")
                {
                    //insert tran record
                    var input = new SterlingDTO()
                    {
                        Account = account,
                        Acctcurrency = iSORest.currency,
                        Balance = iSORest.creditAcct,
                        ChargeAMT = iSORest.isoCharge,
                        CommAMT = "",
                        DateInserted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        DateUpdated = "",
                        FTID = lockTranResp.LockAmountResponse.LockID,
                        IsReversed = false,
                        ReferenceID = lockTranResp.LockAmountResponse.LockID,
                        RequestPIN = iSORest.debitAcct,
                        ResponseCode = "00",
                        ResponseText = lockTranResp.LockAmountResponse.ResponseDescription,
                        ReversalFTID = "",
                        TerminalID = iSORest.terminalId,
                        TranAmount = iSORest.amt,
                        TranSurcharge = iSORest.isoCharge,
                        UniqueID = iSORest.procCode.Substring(0, 2) + iSORest.stan + iSORest.rrn + iSORest.terminalId,
                        TranType = iSORest.procCode.Substring(0, 2),
                        ATMTillAccount = ""
                    };
                    //insert record to DB
                    InsertTransactionDet(input);
                }
            }
            catch(Exception e)
            {
                lockTranResp = null;
                logger.Error($"Exception in method LockAmount for transaction with details: {JsonConvert.SerializeObject(iSORest)} is: {e}");
            }
            return lockTranResp;
        }
        public FundsTransResp DoTransfer(ISOMsg m, string account, ISODetails iSORest, string type)
        {
            FundsTransResp fundsTransResp = new FundsTransResp();
            SterlingPayApis sterlingApis = new SterlingPayApis();
            //get ATM till account
            string terminalID = iSORest.terminalId;
            string tran_type = iSORest.procCode.Substring(0,2);
            string tillAcct = ConfigurationManager.AppSettings["vogueSettlementAcct"];
            string tillAcctCur = ConfigurationManager.AppSettings["tillAcctCur"];
            string tillBranch = ConfigurationManager.AppSettings["tillBranch"];
            try
            {
                string tillEndPoint = ConfigurationManager.AppSettings["atmTill"];
                ATMTillDetails tillAcctRaw = new ATMTillDetails();
                //only when terminal id is for Sterling ATM call the ATM till account else credit settlement account
                if (terminalID.Substring(0, 4) == "1232" && tran_type == "01")
                {
                    string tillResp = sterlingApis.GetATMTillAcct(tillEndPoint, iSORest.terminalId.Trim());
                    logger.Error($"ATM till account response for terminal id: {iSORest.terminalId} is: {tillResp}");
                    tillAcctRaw = string.IsNullOrEmpty(tillResp) ? null : JsonConvert.DeserializeObject<ATMTillDetails>(tillResp);

                    tillAcct = tillAcctRaw.ATM_DETAILS.Record.CRACCT;
                    tillAcctCur = tillAcctRaw.ATM_DETAILS.Record.CRCCY;
                    tillBranch = tillAcctRaw.ATM_DETAILS.Record.COMPANYCODE;
                }
                //else { tillAcct = "0068607442"; tillAcctCur = "566"; tillBranch = "NG0020039"; }

                if ((tillAcctRaw != null && !string.IsNullOrEmpty(tillAcct)) || !string.IsNullOrEmpty(tillAcct))
                {

                    string commCode = ConfigurationManager.AppSettings["commissionCode"];
                    string vTellerAppID = ConfigurationManager.AppSettings["vTellerAppID"];
                    string tranType = ConfigurationManager.AppSettings["transactionType"];

                    ProcessISO _processISO = new ProcessISO();

                    //var iSORest = iSORest;
                    //string[] bufferInfo = vPro.GetAccparams(iSORest.structuredData);
                    //build the transfer payload
                    double charge = 0;
                    if (Convert.ToDouble(iSORest.isoCharge) > 0)
                    {
                        charge = Convert.ToDouble(iSORest.isoCharge) / 100;
                    }

                    var load = new FT_Request() { 
                        CommissionCode = commCode, 
                        CreditAccountNo = tillAcct, 
                        CreditCurrency = tillAcctCur, 
                        DebitAcctNo = account, 
                        DebitAmount = ((Convert.ToDouble(iSORest.amt) / 100) + charge).ToString(), 
                        DebitCurrency = "566", 
                        VtellerAppID = vTellerAppID, 
                        narrations = iSORest.terminalLocation, 
                        TransactionBranch = tillBranch, 
                        TransactionType = tranType, 
                        SessionId = iSORest.procCode.Substring(0, 2) + iSORest.stan + iSORest.rrn + iSORest.terminalId, 
                        TrxnLocation = iSORest.terminalLocation };

                    var payload = new FTRequest
                    {
                        FT_Request = load
                    };
                    //call the getaccountfullinfo endpoint
                    string isoBalance = string.Empty;

                    string fTUrl = ConfigurationManager.AppSettings["ftUrl"];
                    string rootUrl = ConfigurationManager.AppSettings["fioranoFTBaseUrl"];
                    string fTResponse = sterlingApis.DoTransferPost(payload, fTUrl, rootUrl);

                    logger.Error($"Raw FT Response from DoTransferPost method for account: {account} is: {fTResponse}");
                    fundsTransResp = string.IsNullOrEmpty(fTResponse) ? null : JsonConvert.DeserializeObject<FundsTransResp>(fTResponse);
                    logger.Error($"Deserialized FT Response from DoTransferPost method for account: {account} is: {fundsTransResp}");
                    if (fundsTransResp != null)
                    {
                        if (fundsTransResp.FTResponse.ResponseCode == "00" && type != "completion")
                        {
                            //For completion, do no insert new record, identify the pre-auth in the database and update record...with completion details...
                            //build input
                            var input = new SterlingDTO()
                            {
                                Account = account,
                                Acctcurrency = iSORest.currency,
                                Balance = fundsTransResp.FTResponse.Balance,
                                ChargeAMT = fundsTransResp.FTResponse.CHARGEAMT,
                                CommAMT = fundsTransResp.FTResponse.COMMAMT,
                                DateInserted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                DateUpdated = "",
                                FTID = fundsTransResp.FTResponse.FTID,
                                IsReversed = false,
                                ReferenceID = fundsTransResp.FTResponse.ReferenceID,
                                RequestPIN = "",
                                ResponseCode = fundsTransResp.FTResponse.ResponseCode,
                                ResponseText = fundsTransResp.FTResponse.ResponseText,
                                ReversalFTID = "",
                                TerminalID = iSORest.terminalId,
                                TranAmount = iSORest.amt,
                                TranSurcharge = iSORest.isoCharge,
                                UniqueID = iSORest.procCode.Substring(0, 2) + iSORest.stan + iSORest.rrn + iSORest.terminalId,
                                TranType = iSORest.procCode.Substring(0, 2),
                                ATMTillAccount = tillAcct
                            };
                            //insert record to DB
                            InsertTransactionDet(input);
                        }
                    }
                    //else if(type == "completion") {
                    //    //update the pre-auth record...
                    //    var updateInput = new SterlingRevDTO()
                    //    {
                    //        Balance = fundsTransResp.FTResponse.Balance,
                    //        DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    //        FTID = iSORest.tranRef,
                    //        ID = Convert.ToInt32(iSORest.uniqueID),
                    //        IsReversed = false,
                    //        RequestPIN = iSORest.debitAcct,
                    //        ReversalFTID = fundsTransResp.FTResponse.FTID
                    //    };
                    //    UpdateTransactionDet(updateInput);
                    //}
                }
                else { fundsTransResp = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method DoTransfer: {ex}");
                fundsTransResp = null;
            }
            return fundsTransResp;
        }
        public AccountDetails GetAccountBal(string account, ISODetails iSORest, int id)
        {
            ProcessISO _processISO = new ProcessISO();
            AccountDetails acctDet = new AccountDetails();
            SterlingPayApis qashlessApis = new SterlingPayApis();
            string isoBalance = string.Empty;
            try
            {
                string balUrl = ConfigurationManager.AppSettings["balanceUrl"];
                string balance = qashlessApis.GetBalance(balUrl, account);
                logger.Error($"Raw balance from GetBalance method for account: {account} is: {balance}");
                var accountResp = string.IsNullOrEmpty(balance) ? null : JsonConvert.DeserializeObject<AccountFullInfo>(balance);
                logger.Error($"Deserialized balance from GetBalance method for account: {account} is: {accountResp}");
                //form the isobalance
                if (accountResp != null && accountResp.BankAccountFullInfo.NUBAN.Length == 10)
                {
                    string useableBal = (Convert.ToDouble(accountResp.BankAccountFullInfo.UsableBal)).ToString();
                    string cleBal = (Convert.ToDouble(accountResp.BankAccountFullInfo.CLE_BAL)).ToString();
                    useableBal = useableBal.Replace(".", "");
                    cleBal = cleBal.Replace(".", "");
                    isoBalance = accountResp == null ? isoBalance : GetISOBalanceFormat(accountResp.BankAccountFullInfo.T24_CUR_CODE, useableBal, cleBal);

                    acctDet = new AccountDetails { account = accountResp.BankAccountFullInfo.ACCT_NO, isoBalance = isoBalance, acctcurrency = accountResp.BankAccountFullInfo.T24_CUR_CODE, useableBal = accountResp.BankAccountFullInfo.UsableBal, phoneNo = accountResp.BankAccountFullInfo.MOB_NUM };
                    //build input but check id is 1 before processing
                    if (id == 1)
                    {
                        var input = new SterlingDTO()
                        {
                            Account = account,
                            Acctcurrency = iSORest.currency,
                            Balance = accountResp.BankAccountFullInfo.UsableBal,
                            ChargeAMT = "",
                            CommAMT = "",
                            DateInserted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            DateUpdated = "",
                            FTID = "",
                            IsReversed = false,
                            ReferenceID = "",
                            RequestPIN = "",
                            ResponseCode = "00",
                            ResponseText = "",
                            ReversalFTID = "",
                            TerminalID = iSORest.terminalId,
                            TranAmount = iSORest.amt,
                            TranSurcharge = "",
                            UniqueID = iSORest.procCode.Substring(0, 2) + iSORest.stan + iSORest.rrn + iSORest.terminalId,
                            TranType = iSORest.procCode.Substring(0, 2),
                            ATMTillAccount = ""
                        };
                        //insert record to DB
                        InsertTransactionDet(input);
                    }
                    logger.Error($"isoBalance returned for account: {account} is: {isoBalance}");
                }
                else { acctDet = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method GetAccountBal: {ex}");
                acctDet = null;
            }
            return acctDet;
        }
        public AccountDetails GetTranDetByUniqueID(string account, ISODetails iSORest, int id)
        {
            //how do i make this method, it is very important!!!!!
            ProcessISO _processISO = new ProcessISO();
            AccountDetails acctDet = new AccountDetails();
            SterlingPayApis qashlessApis = new SterlingPayApis();
            string isoBalance = string.Empty;
            try
            {
                string balUrl = ConfigurationManager.AppSettings["balanceUrl"];
                string balance = qashlessApis.GetBalance(balUrl, account);
                logger.Error($"Raw balance from GetBalance method for account: {account} is: {balance}");
                var accountResp = string.IsNullOrEmpty(balance) ? null : JsonConvert.DeserializeObject<AccountFullInfo>(balance);
                logger.Error($"Deserialized balance from GetBalance method for account: {account} is: {accountResp}");
                //form the isobalance
                if (accountResp != null && accountResp.BankAccountFullInfo.NUBAN.Length == 10)
                {
                    string useableBal = (Convert.ToDouble(accountResp.BankAccountFullInfo.UsableBal)).ToString();
                    string cleBal = (Convert.ToDouble(accountResp.BankAccountFullInfo.CLE_BAL)).ToString();
                    useableBal = useableBal.Replace(".", "");
                    cleBal = cleBal.Replace(".", "");
                    isoBalance = accountResp == null ? isoBalance : GetISOBalanceFormat(accountResp.BankAccountFullInfo.T24_CUR_CODE, useableBal, cleBal);

                    acctDet = new AccountDetails { account = accountResp.BankAccountFullInfo.ACCT_NO, isoBalance = isoBalance, acctcurrency = accountResp.BankAccountFullInfo.T24_CUR_CODE, useableBal = accountResp.BankAccountFullInfo.UsableBal, phoneNo = accountResp.BankAccountFullInfo.MOB_NUM };
                    //build input but check id is 1 before processing
                    if (id == 1)
                    {
                        var input = new SterlingDTO()
                        {
                            Account = account,
                            Acctcurrency = iSORest.currency,
                            Balance = accountResp.BankAccountFullInfo.UsableBal,
                            ChargeAMT = "",
                            CommAMT = "",
                            DateInserted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            DateUpdated = "",
                            FTID = "",
                            IsReversed = false,
                            ReferenceID = "",
                            RequestPIN = "",
                            ResponseCode = "00",
                            ResponseText = "",
                            ReversalFTID = "",
                            TerminalID = iSORest.terminalId,
                            TranAmount = iSORest.amt,
                            TranSurcharge = "",
                            UniqueID = iSORest.procCode.Substring(0, 2) + iSORest.stan + iSORest.rrn + iSORest.terminalId,
                            TranType = iSORest.procCode.Substring(0, 2),
                            ATMTillAccount = ""
                        };
                        //insert record to DB
                        InsertTransactionDet(input);
                    }
                    logger.Error($"isoBalance returned for account: {account} is: {isoBalance}");
                }
                else { acctDet = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method GetAccountBal: {ex}");
                acctDet = null;
            }
            return acctDet;
        }
        private string GetISOBalanceFormat(string currency, string availBalance, string ledgerBalance)
        {
            var isobal = string.Empty;
            try
            {
                var bal = new StringBuilder("1002" + currency);
                if (availBalance.Substring(0, 1) == "-")
                {
                    bal.Append("D" + availBalance.PadLeft(12, '0'));
                }
                else
                {
                    bal.Append("C" + availBalance.PadLeft(12, '0'));
                }

                bal.Append("1001" + currency);

                if (ledgerBalance.Substring(0, 1) == "-")
                {
                    bal.Append("D" + ledgerBalance.PadLeft(12, '0'));
                }
                else
                {
                    bal.Append("C" + ledgerBalance.PadLeft(12, '0'));
                }
                isobal = bal.ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return isobal;
        }
        private void UpdateTransactionDet(SterlingRevDTO input)
        {
            int row = 0;
            try
            {
                var query = $"UPDATE Vogue_Transactions set IsReversed = @IsReversed, DateUpdated = @DateUpdated, ReversalFTID = @ReversalFTID, Balance = @Balance, RequestPIN = @RequestPIN where FTID = @FTID and ID = @ID";
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
        public void UpdateVogueProcessedTransDet(VogueRecordDTO input)
        {
            int row = 0;
            try
            {
                var query = $"UPDATE Vogue_Transactions set Code = @Code, Message = @Message, ATMTillAccount = @ATMTillAccount, Currency = @Currency, AuthorizationCode = @AuthorizationCode,AvailableBalance = @AvailableBalance,LedgerBalance=@LedgerBalance where UniqueID = @UniqueID and FTID = @FTID";
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
                        cmd.Parameters.AddWithValue("@Code", input.Code);
                        cmd.Parameters.AddWithValue("@Message", input.Message);
                        cmd.Parameters.AddWithValue("@ATMTillAccount", input.ATMTillAccount);
                        cmd.Parameters.AddWithValue("@Currency", input.Currency);
                        cmd.Parameters.AddWithValue("@AuthorizationCode", input.AuthorizationCode);
                        cmd.Parameters.AddWithValue("@AvailableBalance", input.AvailableBalance);
                        cmd.Parameters.AddWithValue("@LedgerBalance", input.LedgerBalance);
                        cmd.Parameters.AddWithValue("@UniqueID", input.UniqueID);
                        cmd.Parameters.AddWithValue("@FTID", input.FTID);
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
        public void InsertTransactionDet(SterlingDTO input)
        {
            int i = 0;
            try
            {
                var query = $"Insert into Vogue_Transactions values (@UniqueID,@Account,@TranAmount,@TranSurcharge,@IsReversed,@TerminalID,@RequestPIN,@ReferenceID,@ResponseCode,@ResponseText,@Balance,@Acctcurrency,@CommAMT,@FTID,@ReversalFTID,@ChargeAMT,@DateInserted,@DateUpdated,@TranType,@ATMTillAccount,@Code,@Message,@Currency,@AuthorizationCode,@AvailableBalance,@LedgerBalance);";
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
            string query = $"select ID, ReversalFTID, FTID, RequestPIN FROM Vogue_Transactions where TerminalID = '{input.TerminalID}' and UniqueID = '{input.UniqueID}'";
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
        public class AccountFullInfo
        {
            public Bankaccountfullinfo BankAccountFullInfo { get; set; }
        }

        public class Bankaccountfullinfo
        {
            public string NUBAN { get; set; }
            public string BRA_CODE { get; set; }
            public string DES_ENG { get; set; }
            public string CUS_NUM { get; set; }
            public string CUR_CODE { get; set; }
            public string LED_CODE { get; set; }
            public string CUS_SHO_NAME { get; set; }
            public string AccountGroup { get; set; }
            public string CustomerStatus { get; set; }
            public string ADD_LINE1 { get; set; }
            public string ADD_LINE2 { get; set; }
            public string MOB_NUM { get; set; }
            public string email { get; set; }
            public string ACCT_NO { get; set; }
            public string MAP_ACC_NO { get; set; }
            public string ACCT_TYPE { get; set; }
            public string ISO_ACCT_TYPE { get; set; }
            public string TEL_NUM { get; set; }
            public string DATE_OPEN { get; set; }
            public string STA_CODE { get; set; }
            public string CLE_BAL { get; set; }
            public string CRNT_BAL { get; set; }
            public string TOT_BLO_FUND { get; set; }
            public object INTRODUCER { get; set; }
            public string DATE_BAL_CHA { get; set; }
            public string NAME_LINE1 { get; set; }
            public string NAME_LINE2 { get; set; }
            public string BVN { get; set; }
            public string REST_FLAG { get; set; }
            public RESTRICTION[] RESTRICTION { get; set; }
            public string IsSMSSubscriber { get; set; }
            public string Alt_Currency { get; set; }
            public string Currency_Code { get; set; }
            public string T24_BRA_CODE { get; set; }
            public string T24_CUS_NUM { get; set; }
            public string T24_CUR_CODE { get; set; }
            public string T24_LED_CODE { get; set; }
            public string OnlineActualBalance { get; set; }
            public string OnlineClearedBalance { get; set; }
            public string OpenActualBalance { get; set; }
            public string OpenClearedBalance { get; set; }
            public string WorkingBalance { get; set; }
            public string CustomerStatusCode { get; set; }
            public string CustomerStatusDeecp { get; set; }
            public object LimitID { get; set; }
            public string LimitAmt { get; set; }
            public string MinimumBal { get; set; }
            public string UsableBal { get; set; }
            public string AccountDescp { get; set; }
            public string CourtesyTitle { get; set; }
            public string AccountTitle { get; set; }
        }

        public class RESTRICTION
        {
            public object RestrictionCode { get; set; }
            public object RestrictionDescription { get; set; }
        }
    }
}
