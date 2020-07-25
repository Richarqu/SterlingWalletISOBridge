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

namespace SBPGenericISOBridge.Qashless
{
    public class QashlessApis
    {
        private static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string QashlessPost(object payload, string endPoint)
        {
            string response = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["baseUrl"];
                string fullUri = $"{rootUrl}/{endPoint}";
                string json = JsonConvert.SerializeObject(payload);
                HttpContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes("cards:74782nfbf#728727");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                     response = httpClient.PostAsync(fullUri, httpContent).Result.Content.ReadAsStringAsync().Result;
                    logger.Error($"response from Imal inquiry directly: {JsonConvert.SerializeObject(response)}");
                    //var rawResponse = response;
                    //logger.Error($"Imal inquiry raw response - {rawResponse}");
                    //jsonResult = JsonConvert.DeserializeObject<string>(response);
                    //_acctTypeResp = JsonConvert.DeserializeObject<InquiryDto>(jsonResult);
                    //logger.Error($"Imal inquiry deserializedObject response - {jsonResult}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return response;
        }
        public string QashlessGet(string endPoint)
        {
            string jsonResult = string.Empty;
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["baseUrl"];
                string fullUri = $"{rootUrl}/{endPoint}";
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"response from Imal inquiry directly: {JsonConvert.SerializeObject(response)}");
                    var rawResponse = response;
                    logger.Error($"Imal inquiry raw response - {rawResponse}");
                    jsonResult = JsonConvert.DeserializeObject<string>(response);
                    //_acctTypeResp = JsonConvert.DeserializeObject<InquiryDto>(jsonResult);
                    logger.Error($"Imal inquiry deserializedObject response - {jsonResult}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return jsonResult;
        }
        public string QashlessGetLoad(object payload, string endPoint)
        {
            string jsonResult = string.Empty;
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
                    var response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    logger.Error($"response from Imal inquiry directly: {JsonConvert.SerializeObject(response)}");
                    var rawResponse = response;
                    logger.Error($"Imal inquiry raw response - {rawResponse}");
                    jsonResult = JsonConvert.DeserializeObject<string>(response);
                    //_acctTypeResp = JsonConvert.DeserializeObject<InquiryDto>(jsonResult);
                    logger.Error($"Imal inquiry deserializedObject response - {jsonResult}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error occurred in method - CheckImalAcctType: {ex.Message}");
            }
            return jsonResult;
        }
    }
}
