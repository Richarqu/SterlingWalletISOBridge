using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.util;
using Configuration = org.jpos.core.Configuration;

namespace SterlingWalletISOBridge
{
    class ISOLog : Configuration
    {
        public string get(string str)
        {
            return ConfigurationManager.AppSettings[str];
        }

        public bool getBoolean(string str)
        {
            throw new NotImplementedException();
        }

        public string get(string str1, string str2)
        {
            return ConfigurationManager.AppSettings[str1];
        }

        public string[] getAll(string str)
        {
            throw new NotImplementedException();
        }

        public int getInt(string str)
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings[str]);
        }

        public int getInt(string str, int i)
        {
            throw new NotImplementedException();
        }

        public bool getBoolean(string str, bool b)
        {
            throw new NotImplementedException();
        }

        public int[] getInts(string str)
        {
            throw new NotImplementedException();
        }

        public long getLong(string str, long l)
        {
            return Convert.ToInt64(ConfigurationManager.AppSettings[str]);
        }

        public long[] getLongs(string str)
        {
            throw new NotImplementedException();
        }

        public double[] getDoubles(string str)
        {
            throw new NotImplementedException();
        }

        public bool[] getBooleans(string str)
        {
            throw new NotImplementedException();
        }

        public long getLong(string str)
        {
            return Convert.ToInt64(ConfigurationManager.AppSettings[str]);
        }

        public double getDouble(string str)
        {
            throw new NotImplementedException();
        }

        public double getDouble(string str, double d)
        {
            throw new NotImplementedException();
        }

        public void put(string str, object obj)
        {
            throw new NotImplementedException();
        }

        public Set keySet()
        {
            throw new NotImplementedException();
        }
    }
}
