using org.jpos.iso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge
{
    public class ISODetails
    {
        public string tranRef { get; set; }
        public string procCode { get; set; }
        public string debitAcct { get; set; }
        public string creditAcct { get; set; }
        public string amt { get; set; }
        public string isoCharge { get; set; }
        public string terminalId { get; set; }
        public string terminalLocation { get; set; }
        public string rrn { get; set; }
        public string stan { get; set; }
        public string currency { get; set; }
        public string auth_id { get; set; }
        public string revTranDet { get; set; }
        public string uniqueID { get; set; }
        public string currencyIntl { get; set; }
        public string amtIntl { get; set; }
    }
    public class ProcessISO
    {
        // charge = iSOResult.isoCharge.Substring(1,iSOResult.isoCharge.Length - 1);
        public ISODetails GetISODetails (ISOMsg m)
        {
            ISODetails _iSODetails = new ISODetails();
            string chargeAmt = "00000000";
            string charge = m.getString(28);
            if (!string.IsNullOrEmpty(charge))
            {
                chargeAmt = !(charge.Substring(0, 1) == "D") ? "-" + charge.Substring(1, charge.Length - 1) : charge.Substring(1, charge.Length - 1);
            }
            _iSODetails = new ISODetails
            {
                procCode = m.getString(3),
                tranRef = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41),
                debitAcct = m.getString(102),
                creditAcct = m.getString(103),
                amt = m.getString(4),
                amtIntl = m.getString(5),
                isoCharge = chargeAmt,
                terminalId = m.getString(41),
                rrn = m.getString(37),
                stan = m.getString(11),
                terminalLocation = m.getString(43),
                currency = m.getString(49),
                currencyIntl = m.getString(50),
                auth_id = m.getString(38),
                revTranDet = m.getString(90),
                uniqueID = m.getString(3).Substring(0, 2) + m.getString(11) + m.getString(37) + m.getString(41)
            };
            return _iSODetails;
        }
    }
}
