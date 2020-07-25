using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.VoguePay
{
    public class VogueRespCodes
    {
        public string Response (string code)
        {
            string rsp = string.Empty;
            switch (code)
            {
                case "AUTH_OK":
                    rsp = "AUTH_OK";
                    break;
                case "AUTH_FAIL":
                    rsp = "AUTH_FAIL";
                    break;
                case "RS_400":
                    rsp = "RS_400";
                    break;
                case "RS_401":
                    rsp = "RS_401";
                    break;
                case "OK":
                    rsp = "00";
                    break;
                case "CB02":
                    rsp = "01";
                    break;
                case "CB03":
                    rsp = "54";
                    break;
                case "CB01":
                    rsp = "02";
                    break;
                case "CB04":
                    rsp = "43";
                    break;
                case "CB05":
                    rsp = "06";
                    break;
                case "CB06":
                    rsp = "51";
                    break;
                case "CB07":
                    rsp = "06";
                    break;
                case "CB08":
                    rsp = "01";
                    break;
                case "CB09":
                    rsp = "61";
                    break;
                case "CB10":
                    rsp = "25";
                    break;
                case "CB11":
                    rsp = "06";
                    break;
                case "CB12":
                    rsp = "25";
                    break;
                case "CB13":
                    rsp = "06";
                    break;
                case "CB14":
                    rsp = "25";
                    break;
                case "CB15":
                    rsp = "06";
                    break;
            }
            return rsp;
        }
    }
}
