using log4net;
using Newtonsoft.Json;
using org.jpos.iso;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SterlingWalletISOBridge.SterlingPay;
using SterlingWalletISOBridge.Validation;

namespace SterlingWalletISOBridge.VoguePay
{
    public class VogueTranProcess
    {
        private static readonly ILog logger =
               LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string iv = ConfigurationManager.AppSettings["iv"];
        private static string key = ConfigurationManager.AppSettings["key"];
        private string text = ConfigurationManager.AppSettings["text"];
        private byte[] ivByte = System.Text.Encoding.UTF8.GetBytes(iv);
        private byte[] keyByte = System.Text.Encoding.UTF8.GetBytes(key);
        private string jsonAuth;
        private VogueResp authResp;
        private string custID = ConfigurationManager.AppSettings["custID"];
        private string authKey = ConfigurationManager.AppSettings["authKey"];
        private string vogueAuth = ConfigurationManager.AppSettings["vogueAuth"];
        private string vogueBal = ConfigurationManager.AppSettings["vogueBal"];
        private string vogueSterlingAcct = ConfigurationManager.AppSettings["vogueAcct"];

        private void UpdateVogueConfig (VogueConfigData input)
        {
            int row = 0;
            try
            {
                var query = $"UPDATE VoguePayConfigs set Token = '{input.Token}', DateUpdated = '{input.DateUpdated.ToString("yyyy-MM-dd HH:mm:ss.fff")}' where ID = 1";
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
        private void InsertVogueConfig(VogueConfigData input)
        {
            try
            {
                var query = $"Insert into VoguePayConfigs values (@Token,@DateUpdated);";
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
                        cmd.Parameters.AddWithValue("@Token", input.Token);
                        cmd.Parameters.AddWithValue("@DateUpdated", input.DateUpdated.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        cmd.ExecuteNonQuery();
                        connect.Dispose();
                        connect.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method InsertVogueConfig: {ex}");
            }
        }
        private VogueConfigData GetVogueConfig()
        {
            string query = "select Token, DateUpdated FROM VoguePayConfigs";
            VogueConfigData configData = new VogueConfigData();
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
                            configData.Token = sdr["Token"].ToString();
                            configData.DateUpdated = Convert.ToDateTime(sdr["DateUpdated"]);
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
            return configData;
        }
        private string VogueAuth (string vogueAuth, string custID, string authKey)
        {
            string vogueToken = string.Empty;
            var authConfig = GetVogueConfig();
            double dateDiff = DateTime.Now.Subtract(authConfig.DateUpdated).TotalHours;
           if (authConfig.Token == null || dateDiff > 23)
            {
                VoguePayApis voguePayApis = new VoguePayApis();
                AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
                VogueAuthDecryptResp vogueDecrypt = new VogueAuthDecryptResp();

                jsonAuth = voguePayApis.VoguePayPostAuth(vogueAuth, custID, authKey);
                logger.Error($"Token raw response for withdrawal call: {jsonAuth}");
                authResp = string.IsNullOrEmpty(jsonAuth) ? null : JsonConvert.DeserializeObject<VogueResp>(jsonAuth);
                if (!string.IsNullOrEmpty(authResp.data) || authResp != null)
                {
                    //decrypt the auth data gotten from auth api
                    var byteArrayAuthResp = aESEncDec.StringToByteArray(authResp.data);
                    string getTokenDecrypt = aESEncDec.DecryptStringFromBytes_Aes(byteArrayAuthResp, keyByte, ivByte);
                    vogueDecrypt = JsonConvert.DeserializeObject<VogueAuthDecryptResp>(getTokenDecrypt);
                    var input = new VogueConfigData
                    {
                        Token = vogueDecrypt.data.token,
                        DateUpdated = DateTime.Now
                    };
                    //save the tokens rather than update them
                    if (authConfig.Token == null) { InsertVogueConfig(input); }
                    else { UpdateVogueConfig(input); }
                    vogueToken = vogueDecrypt.data.token;
                }
                else { vogueToken = string.Empty; logger.Error($"Invalid response gotten for token call in method VogueAuth"); }
            }
            else
            {
                vogueToken = authConfig.Token;
            }
            return vogueToken;
        }
        //tran_type 37 would do balance inquiry
        public VogueBalResp VogueBalance(ISOMsg m)
        {
            VoguePayApis voguePayApis = new VoguePayApis();
            ProcessISO processISO = new ProcessISO();
            VogueBalResp vogueBalResp = new VogueBalResp();
            AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
            var iSOResult = processISO.GetISODetails(m);
            //string debitAcct = m.getString(102);
            string debitAcct = iSOResult.debitAcct;
            try
            {
                var tokenResp = VogueAuth(vogueAuth, custID, authKey);
                if (!string.IsNullOrEmpty(tokenResp))
                {
                    //pass the decrypted token as part of info for account bal.
                    var jsonResponse = voguePayApis.VoguePayGet(vogueBal, "Account", debitAcct, tokenResp, iSOResult);
                    logger.Error($"Balance jsonResponse received is: {jsonResponse}");
                    var balRespRaw = string.IsNullOrEmpty(jsonResponse) ? null : JsonConvert.DeserializeObject<VogueResp>(jsonResponse);
                    logger.Error($"Balance raw response received is: {balRespRaw.data}");
                    if (!string.IsNullOrEmpty(balRespRaw.data) || balRespRaw != null)
                    {
                        //decrypt the response from the balance call
                        var byteArrayBalResp = aESEncDec.StringToByteArray(balRespRaw.data);
                        string getBalDecrypt = aESEncDec.DecryptStringFromBytes_Aes(byteArrayBalResp, keyByte, ivByte);
                        vogueBalResp = JsonConvert.DeserializeObject<VogueBalResp>(getBalDecrypt);
                        logger.Error($"Balance decrypted response received is: {getBalDecrypt}");
                    }
                    else { vogueBalResp = null; }
                }
                else { vogueBalResp = null; }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method VogueBalance: {ex}");
                vogueBalResp = null;
            }
            return vogueBalResp;
        }

        public VogueTranRespJson VogueSendTransaction(ISOMsg m, VogueTranDet payload, string tranType, string vogueTranUrl)
        {
            VogueTranRespJson vogueTranRespJson = new VogueTranRespJson();
            string availBal = string.Empty;
            string tranAmt = string.Empty;
            string rsp = string.Empty;
            //string vogueSterlingAcct = ConfigurationManager.AppSettings["vogueAcct"];
            try
            {
                ResponseCode respCode = new ResponseCode();
                ProcessISO processISO = new ProcessISO();
                SterlingTranProcess sterlingTranPro = new SterlingTranProcess();
                VogueTranProcess vogueTranProcess = new VogueTranProcess();
                VogueRespCodes vogueRespCodes = new VogueRespCodes();
                //VogueTranResp voguePurResp = new VogueTranResp();
                //VoguePayApis voguePayApis = new VoguePayApis();
                //AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
                var iSOResult = processISO.GetISODetails(m);

                var tokenResp = VogueAuth(vogueAuth, custID, authKey);
                if (!string.IsNullOrEmpty(tokenResp))
                {
                    //switch the tranType to know when it is reversal
                    switch (tranType)
                    {
                        case "purchase":
                        case "withdrawal":
                            //get customer's bal from Vogue...
                            var balResult = vogueTranProcess.VogueBalance(m);
                            if (balResult != null && balResult.data != null)
                            {
                                //get available bal from Vogue Customer Account
                                availBal = balResult.data.AvailableBalance;
                                tranAmt = payload.Amount;
                                double charge = 0;
                                if (Convert.ToDouble(iSOResult.isoCharge) > 0)
                                {
                                    charge = Convert.ToDouble(iSOResult.isoCharge) / 100;
                                }
                                if ((Convert.ToDouble(availBal)) > (Convert.ToDouble(tranAmt) + charge))
                                {
                                    //get bal from Vogue Account with Sterling...
                                    var isoBalResp = sterlingTranPro.GetAccountBal(vogueSterlingAcct, iSOResult, 1);
                                    if (isoBalResp != null && isoBalResp.acctcurrency == "566")
                                    {
                                        if ((Convert.ToDouble(isoBalResp.useableBal)) > ((Convert.ToDouble(iSOResult.amt) / 100) + charge))
                                        {
                                            //the debit account would be Vogue Sterling Debit account --0067188797
                                            //debit Vogue account with Sterling
                                            var wdrResult = sterlingTranPro.DoTransfer(m, vogueSterlingAcct, iSOResult, tranType);
                                            if (wdrResult != null)
                                            {
                                                rsp = respCode.Response(wdrResult.FTResponse.ResponseCode);
                                                if (rsp == "00")
                                                {
                                                    string uniqueID = iSOResult.procCode.Substring(0, 2) + iSOResult.stan + iSOResult.rrn + iSOResult.terminalId;
                                                    //then call Vogue to debit their customer's account...and do not wait for response
                                                    Task.Factory.StartNew(() => DoVogueTransfer(payload, tranType, tokenResp, vogueTranUrl, wdrResult, uniqueID));
                                                    uniqueID = string.Empty;
                                                    //DoVogueTransfer(payload, tranType, tokenResp, vogueTranUrl, wdrResult, uniqueID);
                                                    rsp = "00";
                                                    var dataInfo = new Datas()
                                                    {
                                                        AvailableBalance = availBal,
                                                        Currency = balResult.data.Currency,
                                                        LedgerBalance = balResult.data.LedgerBalance,
                                                        AuthorizationCode = wdrResult.FTResponse.FTID
                                                    };
                                                    vogueTranRespJson = new VogueTranRespJson()
                                                    {
                                                        code = rsp,
                                                        data = dataInfo,
                                                        message = "Successful"
                                                    };
                                                }
                                                else
                                                {
                                                    //return response sterling do transfer is not successful
                                                    logger.Error($"Transfer response from Fiorano for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)} is {rsp}: rsp = 51. 51 will be changed to 57 for Vogue Sterling Insufficient Balance");
                                                    rsp = rsp == "51" ? "57" : rsp;
                                                    vogueTranRespJson = new VogueTranRespJson()
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
                                                logger.Error($"Transfer response from Fiorano is null for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                                vogueTranRespJson = new VogueTranRespJson()
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
                                            rsp = "57";
                                            logger.Error($"Account balance of Vogue Sterling account - {vogueSterlingAcct}, {isoBalResp.useableBal} is less than transaction amount - {Convert.ToDouble(iSOResult.amt) / 100} for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 57");
                                            vogueTranRespJson = new VogueTranRespJson()
                                            {
                                                code = rsp,
                                                data = null,
                                                message = ""
                                            };
                                        }
                                    }
                                    else
                                    {
                                        //return response that unable to get balance from Vogue account with Sterling
                                        rsp = "06";
                                        logger.Error($"GetBalance response from Fiorano is null for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                                        vogueTranRespJson = new VogueTranRespJson()
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
                                    logger.Error($"Vogue customer's balance for account - {iSOResult.debitAcct} and iso transaction {JsonConvert.SerializeObject(iSOResult)}, {Convert.ToDouble(availBal)} is less than transaction amount {Convert.ToDouble(tranAmt)}: rsp = 51");
                                    vogueTranRespJson = new VogueTranRespJson()
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
                                logger.Error($"GetBalance response from Vogue is null for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                                vogueTranRespJson = new VogueTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                            break;
                        case "purchaseReversal":
                        case "withdrawalReversal":
                            //first make reversal call to Vogue account 
                            //if reversal call is successful then
                            //for reversal, identify the FT in transaction table and doftreversal on the Vogue sterling account
                            var vogueTranResp = DoVogueTransfer(payload, tranType, tokenResp, vogueTranUrl, null, "");
                            if(vogueTranResp != null && vogueTranResp.code == "OK")
                            {
                                //perform FT reversal
                                var tranReversed = sterlingTranPro.DoFTReversal(m);
                                var dataResp = new Datas()
                                {
                                    AvailableBalance = tranReversed.FTResponseExt.Balance,
                                    Currency = "566",
                                    LedgerBalance = tranReversed.FTResponseExt.Balance,
                                    //AuthorizationCode = tranReversed.FTResponseExt.FTID
                                    AuthorizationCode = vogueTranResp.data.AuthorizationCode
                                };
                                vogueTranRespJson = new VogueTranRespJson()
                                {
                                    code = tranReversed.FTResponseExt.ResponseCode,
                                    message = tranReversed.FTResponseExt.FTID,
                                    data = dataResp
                                };
                            }
                            else { logger.Error($"Vogue reversal for transaction details: {JsonConvert.SerializeObject(payload)} was not successful, response received is: {JsonConvert.SerializeObject(vogueTranResp)}"); vogueTranRespJson = null; }
                            break;
                        default:
                            vogueTranRespJson = null;
                            break;
                    }
                }
                else { vogueTranRespJson = null; logger.Error($"Vogue auth returned null for transaction details: {JsonConvert.SerializeObject(payload)}"); }
            }
            catch (Exception ex)
            {
                logger.Error($"Exception at method VogueSendTransaction for trantype {tranType}: {ex}");
                vogueTranRespJson = null;
            }
            return vogueTranRespJson;
        }
        private VogueTranRespJson DoVogueTransfer(VogueTranDet payload, string tranType, string tokenResp, string vogueTranUrl,FundsTransResp tranResp,string uniqueID)
        {
            //store response from DoVogueTransfer for reference
            SterlingTranProcess sTranPro = new SterlingTranProcess();
            VogueTranRespJson vogueTranRespJson = new VogueTranRespJson();
            VogueTranResp voguePurResp = new VogueTranResp();
            VoguePayApis voguePayApis = new VoguePayApis();
            AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
            try
            {
                var jsonTranPayload = JsonConvert.SerializeObject(payload);
                logger.Error($"{tranType} payload sent is: {jsonTranPayload}");
                var payloadEncrypt = aESEncDec.EncryptStringToBytes_Aes(jsonTranPayload, keyByte, ivByte);
                logger.Error($"Encrypted {tranType} payload sent is: {payloadEncrypt}");
                //convert the encrypted byte array to hexadecimal string
                string hexPayload = aESEncDec.ByteArrayToString(payloadEncrypt);
                logger.Error($"Hex {tranType} payload sent is: {hexPayload} with token: {tokenResp}");
                //the hex string is sent as a formdata post with parameter data
                var jsonTranResp = voguePayApis.VoguePayFormPost(vogueTranUrl, hexPayload, tokenResp);
                logger.Error($"Json {tranType} response received is: {jsonTranResp}");
                voguePurResp = string.IsNullOrEmpty(jsonTranResp) ? null : JsonConvert.DeserializeObject<VogueTranResp>(jsonTranResp);
                if (!string.IsNullOrEmpty(voguePurResp.data) || voguePurResp != null)
                {
                    var byteArrayPurResp = aESEncDec.StringToByteArray(voguePurResp.data);
                    string getPurDecrypt = aESEncDec.DecryptStringFromBytes_Aes(byteArrayPurResp, keyByte, ivByte);
                    logger.Error($"Decrypted {tranType} response received is: {getPurDecrypt}");
                    vogueTranRespJson = JsonConvert.DeserializeObject<VogueTranRespJson>(getPurDecrypt, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                    logger.Error($"About to update Vogue Tran table with funds transfer response {JsonConvert.SerializeObject(tranResp)} and vogue funds transfer response {JsonConvert.SerializeObject(vogueTranRespJson)} of payload {JsonConvert.SerializeObject(payload)}");
                    //update vogue tran table...
                    string tillAcct = payload.Terminal.Substring(0,4) == "1232" ? payload.Terminal : ConfigurationManager.AppSettings["vogueSettlementAcct"];
                    if (tranResp != null)
                    {
                        var upInput = new VogueRecordDTO()
                        {
                            AuthorizationCode = vogueTranRespJson.data.AuthorizationCode,
                            AvailableBalance = vogueTranRespJson.data.AvailableBalance,
                            Code = vogueTranRespJson.code,
                            Currency = vogueTranRespJson.data.Currency,
                            LedgerBalance = vogueTranRespJson.data.LedgerBalance,
                            Message = vogueTranRespJson.message,
                            FTID = tranResp.FTResponse.FTID,
                            UniqueID = uniqueID,
                            ATMTillAccount = tillAcct
                        };
                        sTranPro.UpdateVogueProcessedTransDet(upInput);
                    }
                }
                else
                {
                    //vogueTranRespJson = null;
                    logger.Error($"Vogue transfer returned null response for funds transfer response {JsonConvert.SerializeObject(tranResp)} of payload {JsonConvert.SerializeObject(payload)}");
                    //update empty records in the Vogue Tran table for reconciliation...
                    var upInput = new VogueRecordDTO()
                    {
                        AuthorizationCode = "",
                        AvailableBalance = "",
                        Code = "",
                        Currency = "",
                        LedgerBalance = "",
                        Message = "",
                        FTID = tranResp.FTResponse.FTID,
                        UniqueID = uniqueID
                    };
                    sTranPro.UpdateVogueProcessedTransDet(upInput);
                    vogueTranRespJson = null;
                }
            }
            catch(Exception ex)
            {
                logger.Error($"Exception at method DoVogueTransfer with: {ex}");
                vogueTranRespJson = null;
            }
            return vogueTranRespJson;
        }
        private VogueTranRespJson DoVogueAuth(ISOMsg m, VogueTranAuth payload, string tranType, string vogueAuthUrl, string tokenResp)
        {
            VogueTranResp vogueAuthResp = new VogueTranResp();
            VoguePayApis voguePayApis = new VoguePayApis();
            AESEncryptDecrypt aESEncDec = new AESEncryptDecrypt();
            VogueTranRespJson vogueTranRespJson = new VogueTranRespJson();
            try
            {
                var jsonTranPayload = JsonConvert.SerializeObject(payload);
                logger.Error($"{tranType} payload sent is: {jsonTranPayload}");
                var payloadEncrypt = aESEncDec.EncryptStringToBytes_Aes(jsonTranPayload, keyByte, ivByte);
                logger.Error($"Encrypted {tranType} payload sent is: {payloadEncrypt}");
                //convert the encrypted byte array to hexadecimal string
                string hexPayload = aESEncDec.ByteArrayToString(payloadEncrypt);
                logger.Error($"Hex {tranType} payload sent is: {hexPayload} with token: {tokenResp}");
                //the hex string is sent as a formdata post with parameter data
                var jsonTranResp = voguePayApis.VoguePayFormPost(vogueAuthUrl, hexPayload, tokenResp);
                logger.Error($"Json {tranType} response received is: {jsonTranResp}");
                vogueAuthResp = string.IsNullOrEmpty(jsonTranResp) ? null : JsonConvert.DeserializeObject<VogueTranResp>(jsonTranResp);
                if (!string.IsNullOrEmpty(vogueAuthResp.data) || vogueAuthResp != null)
                {
                    var byteArrayPurResp = aESEncDec.StringToByteArray(vogueAuthResp.data);
                    string getPurDecrypt = aESEncDec.DecryptStringFromBytes_Aes(byteArrayPurResp, keyByte, ivByte);
                    logger.Error($"Decrypted {tranType} response received is: {getPurDecrypt}");
                    //if response is successful, lock vogue account with Steling for tranasaction amount
                    vogueTranRespJson = JsonConvert.DeserializeObject<VogueTranRespJson>(getPurDecrypt);
                }
                else { vogueTranRespJson = null; }
            }
            catch(Exception ex)
            {
                logger.Error($"Exception in method DoVogueAuth: {ex}");
                vogueTranRespJson = null;
            }
            return vogueTranRespJson;
        }
        public VogueTranRespJson VogueSendAuthTransaction(ISOMsg m, VogueTranAuth payload, string tranType, string vogueAuthUrl)
        {
            //send pre-auth to vogue
            //for successful auth response, lock transaction amount in vogue account with sterling
            //respond to customer after successful lock
            VogueTranRespJson vogueTranRespJson = new VogueTranRespJson();
            try
            {
                ResponseCode respCode = new ResponseCode();
                VogueRespCodes vogueRespCodes = new VogueRespCodes();
                ProcessISO processISO = new ProcessISO();
                SterlingTranProcess sterlingTranPro = new SterlingTranProcess();
                string rsp = string.Empty;
                var iSOResult = processISO.GetISODetails(m);

                var tokenResp = VogueAuth(vogueAuth, custID, authKey);
                if (!string.IsNullOrEmpty(tokenResp))
                {
                    switch (tranType)
                    {
                        case "pre-auth":
                            vogueTranRespJson = DoVogueAuth(m, payload, tranType, vogueAuthUrl, tokenResp);
                            if (vogueRespCodes.Response(vogueTranRespJson.code) == "00")
                            {
                                //check bal on Vogue Account with Sterling
                                var isoBalResp = sterlingTranPro.GetAccountBal(vogueSterlingAcct, iSOResult, 0);
                                if (isoBalResp != null && isoBalResp.acctcurrency == "566")
                                {
                                    double charge = 0;
                                    if (Convert.ToDouble(iSOResult.isoCharge) > 0)
                                    {
                                        charge = Convert.ToDouble(iSOResult.isoCharge) / 100;
                                    }
                                    if ((Convert.ToDouble(isoBalResp.useableBal)) > ((Convert.ToDouble(iSOResult.amt) / 100) + charge))
                                    {
                                        //lock tran amount on Vogue Account
                                        iSOResult.creditAcct = vogueTranRespJson.data.AvailableBalance;  //isoBalResp.useableBal;
                                        var sterlingLockResp = sterlingTranPro.LockAmount(m, vogueSterlingAcct, iSOResult);
                                        //if lock is successful, return success response
                                        if (sterlingLockResp != null)
                                        {
                                            rsp = sterlingLockResp.LockAmountResponse.Responsecode == "1" ? "00" : "06";
                                            if (rsp == "00")
                                            {
                                                var datas = new Datas()
                                                {
                                                    AuthorizationCode = vogueTranRespJson.data.AuthorizationCode,
                                                    AvailableBalance = sterlingLockResp.LockAmountResponse.LockID,
                                                    Currency = "",
                                                    LedgerBalance = ""
                                                };
                                                vogueTranRespJson = new VogueTranRespJson()
                                                {
                                                    code = rsp,
                                                    data = datas,
                                                    message = sterlingLockResp.LockAmountResponse.ResponseDescription
                                                };
                                            }
                                            else
                                            {
                                                logger.Error($"{tranType} - LockAmount response from Fiorano is {rsp} for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); vogueTranRespJson = new VogueTranRespJson()
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
                                            logger.Error($"{tranType} - LockAmount response from Fiorano is {rsp} for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                            vogueTranRespJson = new VogueTranRespJson()
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
                                        rsp = "57";
                                        logger.Error($"{tranType} - Account balance of Vogue Sterling account - {vogueSterlingAcct}, {isoBalResp.useableBal} is less than transaction amount - {Convert.ToDouble(iSOResult.amt) / 100} for account - {vogueSterlingAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)}: rsp = 57");
                                        vogueTranRespJson = new VogueTranRespJson()
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
                                    logger.Error($"{tranType} - GetBalance response from Fiorano is null for account - {vogueSterlingAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                                    vogueTranRespJson = new VogueTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else
                            {
                                logger.Error($"{tranType} - DoVogueAuth was not successful with response details: {JsonConvert.SerializeObject(vogueTranRespJson)} for iso transaction: {JsonConvert.SerializeObject(iSOResult)}");
                                vogueTranRespJson = new VogueTranRespJson()
                                {
                                    code = vogueRespCodes.Response(vogueTranRespJson.code),
                                    data = null,
                                    message = ""
                                };
                            }
                            break;
                        case "completion":
                            //for completion
                            //get vogue bal
                            //debit vogue account
                            //release lock after successful debit of vogue account with sterling
                            //send by background to vogue to release lock on customer's acccount and debit him
                            string vogueTranUrl = ConfigurationManager.AppSettings["vogueProcessPreAuth"];
                            //check bal on Vogue Account with Sterling
                            var isoBalResp2 = sterlingTranPro.GetAccountBal(vogueSterlingAcct, iSOResult, 0);
                            if (isoBalResp2 != null && isoBalResp2.acctcurrency == "566")
                            {
                                double charge = 0;
                                if (Convert.ToDouble(iSOResult.isoCharge) > 0)
                                {
                                    charge = Convert.ToDouble(iSOResult.isoCharge) / 100;
                                }
                                if ((Convert.ToDouble(isoBalResp2.useableBal)) > ((Convert.ToDouble(iSOResult.amt) / 100) + charge))
                                {
                                    string uniqueID = iSOResult.procCode.Substring(0, 2) + iSOResult.revTranDet.Substring(4, 6) + iSOResult.rrn + iSOResult.terminalId;
                                    //find the earlier transaction
                                    //be sure about the uniqueid
                                    var input = new SterlingGetDTO()
                                    {
                                        UniqueID = uniqueID,
                                        TerminalID = iSOResult.terminalId
                                    };
                                    var origTranDet = sterlingTranPro.GetTransactionDet(input);
                                    //iSOResult.tranRef = origTranDet.FTID; iSOResult.uniqueID = origTranDet.ID.ToString(); 
                                    var debitResult = sterlingTranPro.DoTransfer(m, vogueSterlingAcct, iSOResult, "completion");
                                    if (debitResult != null)
                                    {
                                        rsp = respCode.Response(debitResult.FTResponse.ResponseCode);
                                        if (rsp == "00")
                                        {
                                            //call the sterling unlock method to release transaction lock...
                                            if (origTranDet != null)
                                            {
                                                //iSOResult.auth_id = vogueTranRespJson.data.AuthorizationCode;
                                                iSOResult.amt = debitResult.FTResponse.Balance;
                                                iSOResult.tranRef = debitResult.FTResponse.FTID;
                                                iSOResult.uniqueID = uniqueID;
                                                var unlockResp = sterlingTranPro.UnlockAmount(m, vogueSterlingAcct, iSOResult, origTranDet.FTID, "0220");
                                                if (unlockResp != null)
                                                {
                                                    rsp = unlockResp.LockAmountResponse.Responsecode == "1" ? "00" : "06";
                                                    if (rsp == "00")
                                                    {
                                                        //then call Vogue to debit their customer's account...and do not wait for response
                                                        Task.Factory.StartNew(() => DoVogueAuth(m, payload, tranType, vogueTranUrl, tokenResp));
                                                        var datas = new Datas()
                                                        {
                                                            AuthorizationCode = unlockResp.LockAmountResponse.LockID,
                                                            AvailableBalance = "",
                                                            Currency = "",
                                                            LedgerBalance = ""
                                                        };
                                                        vogueTranRespJson = new VogueTranRespJson()
                                                        {
                                                            code = rsp,
                                                            data = datas,
                                                            message = unlockResp.LockAmountResponse.ResponseDescription
                                                        };
                                                    }
                                                    else
                                                    {
                                                        logger.Error($"{tranType} - Unlock response from Fiorano is {rsp} for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); vogueTranRespJson = new VogueTranRespJson()
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
                                                    logger.Error($"{tranType} - Unlock response from Fiorano is null for account - {vogueSterlingAcct} completion and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                                    vogueTranRespJson = new VogueTranRespJson()
                                                    {
                                                        code = rsp,
                                                        data = null,
                                                        message = ""
                                                    };
                                                }
                                            }
                                            else
                                            {
                                                logger.Error($"{tranType} - Cannot locate original transaction on account - {vogueSterlingAcct} for completion - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); rsp = "06"; vogueTranRespJson = new VogueTranRespJson()
                                                {
                                                    code = rsp,
                                                    data = null,
                                                    message = ""
                                                };
                                            }
                                        }
                                        else
                                        {
                                            //return response sterling do transfer is not successful
                                            logger.Error($"{tranType} - Transfer response for completion from Fiorano for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)} is {rsp}");
                                            rsp = rsp == "51" ? "57" : rsp;
                                            vogueTranRespJson = new VogueTranRespJson()
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
                                        logger.Error($"{tranType} - Transfer response from Fiorano is null for account - {vogueSterlingAcct} and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                        vogueTranRespJson = new VogueTranRespJson()
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
                                    rsp = "57";
                                    logger.Error($"{tranType} - Account balance of Vogue Sterling account - {vogueSterlingAcct}, {isoBalResp2.useableBal} is less than transaction amount - {Convert.ToDouble(iSOResult.amt) / 100} for account - {vogueSterlingAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)}: rsp = 57");
                                    vogueTranRespJson = new VogueTranRespJson()
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
                                logger.Error($"{tranType} - GetBalance response from Fiorano is null for account - {vogueSterlingAcct} and iso transaction - {JsonConvert.SerializeObject(iSOResult)} or account currency is not Naira: rsp = 06");
                                vogueTranRespJson = new VogueTranRespJson()
                                {
                                    code = rsp,
                                    data = null,
                                    message = ""
                                };
                            }
                            break;
                        case "pre-authReversal":
                            //reverse on Vogue account
                            vogueTranRespJson = DoVogueAuth(m, payload, tranType, vogueAuthUrl, tokenResp);
                            if (vogueTranRespJson != null && vogueTranRespJson.code == "00")
                            {
                                //perform unlock for lockid as reversal
                                string uniqueId = iSOResult.procCode.Substring(0, 2) + iSOResult.stan + iSOResult.rrn + iSOResult.terminalId;
                                //find the earlier transaction
                                var input = new SterlingGetDTO()
                                {
                                    UniqueID = uniqueId,
                                    TerminalID = iSOResult.terminalId
                                };
                                //find another means to retrieve lockid and transaction FT by uniqueID of transaction...Use EACBS directly if necessary....
                                var origTranDets = sterlingTranPro.GetTransactionDet(input);
                                if (origTranDets != null)
                                {
                                    //call the sterling unlock method to release transaction lock...
                                    var unlockResp = sterlingTranPro.UnlockAmount(m, vogueSterlingAcct, iSOResult, origTranDets.FTID, "0420");
                                    if (unlockResp != null)
                                    {
                                        rsp = unlockResp.LockAmountResponse.Responsecode == "1" ? "00" : "06";
                                        if (rsp == "00")
                                        {
                                            var datas = new Datas()
                                            {
                                                AuthorizationCode = vogueTranRespJson.data.AuthorizationCode,
                                                AvailableBalance = unlockResp.LockAmountResponse.LockID,
                                                Currency = "",
                                                LedgerBalance = ""
                                            };
                                            vogueTranRespJson = new VogueTranRespJson()
                                            {
                                                code = rsp,
                                                data = datas,
                                                message = unlockResp.LockAmountResponse.ResponseDescription
                                            };
                                        }
                                        else
                                        {
                                            logger.Error($"{tranType} - Unlock response from Fiorano is {rsp} for account - {vogueSterlingAcct} pre-authreversal and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); vogueTranRespJson = new VogueTranRespJson()
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
                                        logger.Error($"{tranType} - Unlock response from Fiorano is null for account - {vogueSterlingAcct} pre-authreversal and isoresult - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06");
                                        vogueTranRespJson = new VogueTranRespJson()
                                        {
                                            code = rsp,
                                            data = null,
                                            message = ""
                                        };
                                    }
                                }
                                else
                                {
                                    logger.Error($"{tranType} - Cannot locate original transaction on account - {vogueSterlingAcct} for pre-authReversal - {JsonConvert.SerializeObject(iSOResult)}: rsp = 06"); rsp = "06"; vogueTranRespJson = new VogueTranRespJson()
                                    {
                                        code = rsp,
                                        data = null,
                                        message = ""
                                    };
                                }
                            }
                            else { logger.Error($"{tranType} - Vogue reversal for transaction details: {JsonConvert.SerializeObject(payload)} was not successful, response received is: {JsonConvert.SerializeObject(vogueTranRespJson)}"); vogueTranRespJson = null; }
                            break;
                        default:
                            vogueTranRespJson = null;
                            break;
                    }
                }
                else { vogueTranRespJson = null; logger.Error($"{tranType} - Token response for DoVogueAuth was null for iso transaction: {JsonConvert.SerializeObject(iSOResult)}"); }
            }
            catch (Exception ex)
            {
                logger.Error($"{tranType} - Exception at method VogueSendTransaction for trantype {tranType}: {ex}");
                vogueTranRespJson = null;
            }
            return vogueTranRespJson;
        }
    }
    public class VogueConfigData
    {
        public string Token { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
