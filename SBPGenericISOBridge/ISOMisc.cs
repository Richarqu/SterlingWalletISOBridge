using log4net;
using org.jpos.iso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SterlingWalletISOBridge
{
    class ISOMisc
    {
        private static readonly ILog logger =
               LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //Break Down Message Fields
        public  StringBuilder BreakMsg(ISOMsg msg)
        {
            StringBuilder breakdown = new StringBuilder();

            try
            {
                breakdown.Append(msg.getMTI() + ":\n");

                for (int i = 1; i <= msg.getMaxField(); i++)
                {
                    if (msg.hasField(i))
                    {
                        if (i == 127)
                        {
                            breakdown.Append("Field " + i + "\n");
                            for (int j = 0; j <= 47; j++)
                            {
                                string f = "127." + j;
                                if (msg.hasField(f))
                                {
                                    breakdown.Append("      " + f.PadRight(8, ' ') + "==> [" + msg.getString(f) + "]\n");
                                }
                            }
                        }
                        else if (i == 2)
                        {
                            var pan = msg.getString(i);
                            breakdown.Append("Field " + (i.ToString()).PadRight(8, ' ') + "==> [" + MaskPan(pan) + "]\n");
                        }
                        else if (i == 35)
                        {
                            var track2 = msg.getString(i);
                            var dd = track2.Split('D');
                            if (dd.Length < 2)
                            {
                                dd = track2.Split('=');
                                if (dd.Length >= 2)
                                {
                                    track2 = dd[0].Substring(0, 6) + "****".PadLeft(dd[0].Length - 10, '*') + dd[0].Substring(dd[0].Length - 4, 4) + "=" + dd[1];
                                }
                            }
                            else
                            {
                                track2 = dd[0].Substring(0, 6) + "****".PadLeft(dd[0].Length - 10, '*') + dd[0].Substring(dd[0].Length - 4, 4) + "D" + dd[1];
                            }
                            breakdown.Append("Field " + (i.ToString()).PadRight(8, ' ') + "==> [" + track2 + "]\n");
                        }
                        else if ((i == 45) || (i == 52))
                        {
                            //Do not display at all
                        }
                        else if ((i == 53) || (i == 64) || (i == 65) || (i == 96))
                        {
                            var data = msg.getBytes(i);
                            breakdown.Append("Field " + (i.ToString()).PadRight(8, ' ') + "==> [" + ISOUtil.hexString(data) + "]\n");
                        }
                        else
                        {
                            breakdown.Append("Field " + (i.ToString()).PadRight(8, ' ') + "==> [" + msg.getString(i) + "]\n");
                        }
                    }
                }
            }
            catch (ISOException e)
            {
                breakdown.Append(e.ToString());
            }

            return breakdown;
        }

        //Mask Pan
        public static string MaskPan(string pan)
        {
            if (pan != "" || pan.Length > 10)
            {
                pan = pan.Substring(0, 6) + "****".PadLeft(pan.Length - 10, '*') + pan.Substring(pan.Length - 4, 4);
            }
            return pan;
        }

        //Form the ISO Balance format
        public string GetISOBalanceFormat(string currency,string availBalance,string ledgerBalance)
        {
            var isobal = string.Empty;
            try
            {
                var bal = new StringBuilder("1002" + currency);
                if (availBalance.Substring(0, 1) == "-")
                {
                    bal.Append("D" + availBalance.PadRight(12, '0'));
                }
                else
                {
                    bal.Append("C" + availBalance.PadRight(12, '0'));
                }

                bal.Append("1001" + currency);

                if (ledgerBalance.Substring(0, 1) == "-")
                {
                    bal.Append("D" + ledgerBalance.PadRight(12, '0'));
                }
                else
                {
                    bal.Append("C" + ledgerBalance.PadRight(12, '0'));
                }
                isobal = bal.ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return isobal;
        }

        public string ISO_Response(string appResponse)
        {
            string rsp = "06";

            switch (appResponse)
            {
                case "":
                    rsp = "06";
                    break;
            }

            return rsp;
        }
    }
}
