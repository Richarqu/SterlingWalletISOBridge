using com.sun.security.ntlm;
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

namespace SterlingWalletISOBridge.VoguePay
{
    public class VoguePayApis
    {
        private static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string VoguePayPost(object payload, string endPoint, string token)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["vogueRoot"];
                string fullUri = $"{rootUrl}/{endPoint}";
                string json = JsonConvert.SerializeObject(payload);
                HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    //var byteArray = Encoding.ASCII.GetBytes("cards:74782nfbf#728727");
                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"{token}");
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
        public string VoguePayFormPost(string endPoint, string data, string token)
        {
            string resp = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["vogueRoot"];
                string fullUri = $"{rootUrl}/{endPoint}";

                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"{token}");
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    //create a form content with the custID and Key
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("data", data)
                    });
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var clientResponse = httpClient.PostAsync(new Uri(fullUri), formContent);
                    var stringContent = clientResponse.Result.Content.ReadAsStringAsync();
                    resp = stringContent.Result;
                    logger.Error($"response from Vogue token call: {JsonConvert.SerializeObject(resp)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - VoguePayPostAuth: {ex.Message}");
            }
            //decrypt the token before using it.
            return resp;
        }
        public string VoguePayPostAuth(string endPoint, string custID, string key)
        {
            string token = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["vogueRoot"];
                string fullUri = $"{rootUrl}/{endPoint}";

                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    //create a form content with the custID and Key
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("CustID", custID),
                        new KeyValuePair<string, string>("Key", key)
                    });
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var clientResponse = httpClient.PostAsync(new Uri(fullUri), formContent);
                    var stringContent = clientResponse.Result.Content.ReadAsStringAsync();
                    token = stringContent.Result;
                    logger.Error($"response from Vogue token call: {JsonConvert.SerializeObject(token)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - VoguePayPostAuth: {ex.Message}");
            }
            //decrypt the token before using it.
            return token;
        }
        public string VoguePayGet(string endPoint,string param, string value, string token, ISODetails iSODetails)
        {
            var response = string.Empty;
            string jsonResult = string.Empty;
            string fullUri = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["vogueRoot"];
                if (!string.IsNullOrEmpty(iSODetails.currencyIntl) && !string.IsNullOrEmpty(iSODetails.amtIntl))
                {
                     fullUri = $"{rootUrl}/{endPoint}?{param}={value}?TranRef={iSODetails.currencyIntl}?Amount={iSODetails.amtIntl}";
                }
                else
                {
                     fullUri = $"{rootUrl}/{endPoint}?{param}={value}";
                }
                using (HttpClient httpClient = new HttpClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"{token}");

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;
                    logger.Error($"Vogue get raw response - {response}");
                    logger.Error($"Vogue get serialized response: {JsonConvert.SerializeObject(response)}");                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - VoguePayGet: {ex.Message}");
            }
            return response;
        }
        public string VoguePayGetLoad(object payload, string endPoint)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["rootURL"];
                string fullUri = $"{rootUrl}/{endPoint}";
                string json = JsonConvert.SerializeObject(payload);
                HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"Vogue Getload serialized response: {JsonConvert.SerializeObject(response)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - VoguePayGetLoad: {ex.Message}");
            }
            return response;
        }
    }
}
