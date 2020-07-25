using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using java.lang;
using org.jpos.iso;
using org.jpos.iso.channel;
using org.jpos.iso.packager;
using org.jpos.util;
using Console = System.Console;
using Exception = System.Exception;
using System.Configuration;

namespace SterlingWalletISOBridge
{
    class Program
    {
        private static readonly ILog logger =
                 LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
 
        static void Main(string[] args)
        {
            string appname = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";

            if (CheckIfAppIsRunning(appname))
            {
                Console.WriteLine(string.Format("{0} is already running...\r\nExiting", appname));
            }
            else
            {
                logger.Info(string.Format("Starting {0}...", appname));
                Console.WriteLine(string.Format("Starting {0}...", appname));
                try
                {
                    StartISO_Processor();
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
        }
        public static void StartISO_Processor()
        {
            var ISO8583Parser = @ConfigurationManager.AppSettings["xmlpath"];
            var bridgePort = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
            Console.WriteLine("ISO Bridge started @ " + DateTime.Now.ToString("D") + " on port " + bridgePort);
            Console.WriteLine("Please ensure that the ISO Parser file " + ISO8583Parser + " exists as this will be used to breakdown the ISO8583 message...");
            Console.WriteLine("********************************************************************");
            
            try
            {
                //Set the inbuilt ISO Logger
                Logger isoLogger = new Logger();
                
                //Set the protected Listener which implements PCIDSS compliance
                var pLog = new ProtectedLogListener();
                pLog.setConfiguration(new ISOLog());
                isoLogger.addListener(pLog);
                //Set how log is maanged, size e.t.c
                var rLog = new RotateLogListener();
                rLog.setConfiguration(new ISOLog());
                isoLogger.addListener(rLog);

                //Set the packager that will be used to break down the ISO8583 message...
                GenericPackager p;
                p = new GenericPackager(ISO8583Parser);
                ServerChannel channel = new PostChannel(p);
                ((LogSource)channel).setLogger(isoLogger, "ISO_Channel");
                ISOServer server = new ISOServer(bridgePort, channel, null);
                server.setLogger(isoLogger, "ISO_Server");
                var a = isoLogger.ToString();
                Console.WriteLine("Display what i am sending to Server ==>"+a);
                server.addISORequestListener(new ISOMessageProcessor());
                new Thread(server).start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }
        public static bool CheckIfAppIsRunning(string appname)
        {
            string query = "Select * from Win32_Process Where Name = '" + appname + "'";
            ManagementObjectCollection processList = (new ManagementObjectSearcher(query)).Get();
            return processList.Count > 1;
        }
    }
} 
