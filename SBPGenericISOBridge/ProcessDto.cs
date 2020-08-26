using com.sun.istack.@internal.logging;
using log4net;
using SterlingWalletISOBridge.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge
{
    public class ProcessDto
    {
        private static readonly ILog logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ValidationResp ValidateReq(ValidationReq request)
        {
            ValidationResp _valResp = new ValidationResp();
            try
            {

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return _valResp;
        }
    }
}
