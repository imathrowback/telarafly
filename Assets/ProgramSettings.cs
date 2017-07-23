using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public class ProgramSettings
    {
        private string configFile = "telarafly.cfg";
        private string oldconfigFile = "nif2obj.properties";
        public Properties properties { get; }

        private static ProgramSettings inst;

        private ProgramSettings()
        {
            // get rid of the old config file if it exists
            if (File.Exists(oldconfigFile))
            {
                File.Copy(oldconfigFile, configFile);
                File.Delete(oldconfigFile);
            }
            properties = new Properties(configFile);
        }
        public static string get(string key)
        {
            return getInstance().properties.get(key);
        }

        public static string get(string key, string defaultVal)
        {

            return getInstance().properties.get(key, defaultVal);
        }

        public static int get(string key, int defaultVal)
        {
            return int.Parse(getInstance().properties.get(key, "" + defaultVal));
        }

        public static float get(string key, float defaultVal)
        {
            return float.Parse(getInstance().properties.get(key, "" + defaultVal));
        }

        static private ProgramSettings getInstance()
        {
            if (inst == null)
                inst = new ProgramSettings();
            return inst;
        }
    }
}
