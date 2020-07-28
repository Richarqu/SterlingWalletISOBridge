using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.SterlingPay
{
    public class SterlingPayApis
    {
        private static readonly ILog logger =
               LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string DoTransferPost(object payload, string endPoint, string rootUrl)
        {
            string response = string.Empty;
            try
            {
                string fullUri = $"{rootUrl}/{endPoint}";
                string json = JsonConvert.SerializeObject(payload);
                HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.PostAsync(fullUri, httpContent).Result.Content.ReadAsStringAsync().Result;
                    logger.Error($"response from QashlessPost method: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        public string DoFioranoPost(object payload, string endPoint, string rootUrl)
        {
            string response = string.Empty;
            try
            {
                string fullUri = $"{rootUrl}/{endPoint}";
                string json = JsonConvert.SerializeObject(payload);
                HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.PostAsync(fullUri, httpContent).Result.Content.ReadAsStringAsync().Result;
                    logger.Error($"response from QashlessPost method: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        public string GetATMTillAcct(string endPoint, string terminalID)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["fioranoBaseUrlATM"];
                string fullUri = $"{rootUrl}/{endPoint}/{terminalID}";
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"response from Imal inquiry directly: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        public string GetBalance(string endPoint, string account)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["fioranoBaseUrl"];
                string fullUri = $"{rootUrl}/{endPoint}/{account}";
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"response from GetBalance inquiry directly: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        public string GetFiorano(string rootUrl,string endPoint, string uniqueId)
        {
            //atuniqueUrl
            string response = string.Empty;
            try
            {
                //string rootUrl = ConfigurationManager.AppSettings["fioranoBaseUrlATM"];
                string fullUri = $"{rootUrl}/{endPoint}/{uniqueId}";
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"response from GetBalance inquiry directly: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        /*
         * 
        {{base_url}}/EacbsUpdate1/UnlockAmount
    

        {

        "UnlockAmount": {

        "LockID": "LockID"

        }

        }

        {{base_url}}/EacbsUpdate1/LockAmount
        {

        "LockAmount": {

        "account": "account",

        "amount": "amount",

        "description": "description",

        "startdate": "startdate",

        "enddate": "enddate"

        }

        }
    
        {{base_url}}/EacbsEnquiry3/GetFT_Details/:FTReference(T24 FtReference)/:HistoryCheck(True if reading from history else false)/:HistNum(History Number)


        {{base_url}}/EacbsEnquiry3/GetATMTransactionById/:At_Unique_Id(The Unique id of the ATM transaction; Can be gotten from the concatenation of field3(0,2)+field11+field37+field41 of the ISO Message)
         * */
    }
}
