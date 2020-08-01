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

namespace SterlingWalletISOBridge.WalletPay
{
    public class WalletApis
    {
        private static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string WalletGet(string endPoint, string param)
        {
            //{param}={value}
            var response = string.Empty;
            string jsonResult = string.Empty;
            string fullUri = string.Empty;
            try
            {
                string walletRoot = ConfigurationManager.AppSettings["walletBaseUrl"];
                fullUri = $"{walletRoot}/{endPoint}?{param}";

                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(walletRoot);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"{token}");

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;
                    logger.Error($"Vogue get raw response - {response}");
                    logger.Error($"Vogue get serialized response: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - VoguePayGet: {ex.Message}");
            }
            return response;
        }
        //https://pass.sterling.ng/OneWallet/api/Wallet/LockFunds?req.mobile=08063805960&req.amount=10&req.lockedBy=card
        public string WalletPost(object payload, string endPoint)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["walletBaseUrl"];
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
                    logger.Error($"response from Imal inquiry directly: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
    }
}
