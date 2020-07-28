using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge
{
    class SterlingPayRevDTO
    {
    }
    public class FTReversalReq
    {
        public FT_Request FT_Request { get; set; }
    }
    public class FT_Request
    {
        public string TransactionBranch { get; set; }
        public string FTReference { get; set; }
    }
}
