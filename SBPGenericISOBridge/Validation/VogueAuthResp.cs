using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge.Validation
{
    public class VogueResp
    {
        public string data { get; set; }
    }
    public class VogueAuthDecryptResp
    {
        public string code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public string token { get; set; }
        public string expires { get; set; }
    }
}
