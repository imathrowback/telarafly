using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
//using log4net;

namespace DotNet.Config
{
    /// <summary>
    /// This class uses a java style .properties file to load, apply ('glue'), and save settings. 
    /// 
    /// I've found this approach and helper class is far easier than using app.config for several reasons:
    /// 
    /// 1) Non-technical folks have no problem editing .ini "name=value" style files, but they struggle with XML
    /// 2) Values with quotes in xml are a headache - you have to use '&quot;' which gets messy
    /// 3) If you're writing a packaged dll, it needs to use the entry point's app.config which we don't control. 
    ///    Using this approach, you can ship a 'MyDll.config' 
    /// 4) The 'glueOn' approach is really handy. It'll just stick values from the .properties file onto your class,
    ///    regardless of whether they're public, private, or static. If you use a 'private static string _name' convention,
    ///    it'll even take 'name=value' from the properties file (no underscore) and apply it
    /// 5) Saving settings back to app.config is a royal headache. Here, you just call Save(name, value) and it's done
    /// 
    /// This class also takes into account loading the properties file from the same directory as the executing assembly,
    /// even when installed as a service, so you don't run into the issue of the current path being sys32 or whatever
    /// execution location services are started from.
    /// </summary>

    public class AppSettings
    {
        //The default config file name is 'config.properties'. 
        private static string defaultConfigFileName = "telarafly.cfg";
        private static string defaultConfigFileFullPath = Path.Combine(GetAssemblyDirectory(), defaultConfigFileName);

        private static Dictionary<string, string> appSettings;
        //private static ILog log = LogManager.GetLogger(typeof(AppSettings));

        //used for unit testing to confirm we only do the work once
        public static int CacheCount = 0;

        

        public static Dictionary<string, string> Retrieve()
        {
            return Retrieve(defaultConfigFileFullPath);
        }

        public static Dictionary<string, string> Retrieve(string configFile)
        {

            try
            {
                return _Retrieve(configFile);
            }
            catch (ArgumentException ex)
            {
                string message = "Key already exists: check " + configFile + " for duplicate settings. You may have accidentally commented/uncommented one so it appears twice";
                throw new ArgumentException(message);
            }
        }

        private static Dictionary<string, string> _Retrieve(string configFile)
        {
            //NOTE that if we need to use different config files at once this will need to be updated to 
            //check it's the same config file
            if (appSettings != null)
                return appSettings;

            ++CacheCount;

            if (!File.Exists(configFile))
            {
                configFile = Path.Combine(GetAssemblyDirectory(), configFile);
                if (!File.Exists(configFile))
                {
                    string message = "> Failed to locate config file " + configFile;
                    //log.Error(message);
                    throw new FileNotFoundException(message);
                }
            }

            var lines = File.ReadAllLines(configFile)
                .Where(line => !line.TrimStart().StartsWith("#"))
                .ToList();

            var temp = new Dictionary<string, string>();
            string n = null;
            string v = null;

            const string nameValueSettingLinePattern = @"^[^=\s]+\s*=\s*[^\s]+";
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Replace("\t", "    ");

                if (Regex.IsMatch(line, nameValueSettingLinePattern))
                {
                    var s = line.Split('=');
                    n = s[0].Trim();
                    v = line.Substring(line.IndexOf('=') + 1);

                    //allow for comments on the same line following the value like name=value #this is a comment
                    //2015-10-01: don't allow comments on the same line because it interferes with 
                    //cases like colors.one = #FF0000;
                    //v = Regex.Replace(v, @"\s+#.*$", "");
                }
                else if (n != null && v != null && Regex.IsMatch(line, @"^\s{3,}[^ ]"))//multi-liners must indent at least 3 spaces
                {
                    /*  a multi-line value like:
                     * name=hello there
                     *      how are you today ?
                     */
                    //v += Environment.NewLine + line.Trim(); //preserve line breaks!
                    v += " " + line.Trim(); //do NOT preserve line breaks!
                }

                if ((n != null && v != null) &&
                    (i == lines.Count - 1  //we're at the end
                        || Regex.IsMatch(lines[i + 1], nameValueSettingLinePattern) //or the next line marks a new pair 
                        || String.IsNullOrEmpty(lines[i + 1]) //or the next line is empty 
                    ))
                {
                    if (!temp.ContainsKey(n))
                    {
                        temp.Add(n, v);
                    }
                    else
                    {
                        throw new ArgumentException("Your config file contains multiple entires for " + n);
                    }

                    n = null;
                    v = null;
                }

            }

            var nameValuePairs = new Dictionary<string, string>();

            foreach (var pair in temp)
            {
                string name = pair.Key;
                string value = pair.Value;

                if (value.Contains("$"))
                {
                    //do we have some setting that can resolve this ?
                    foreach (var pair2 in temp)
                    {
                        if (value.Contains("$" + pair2.Key))
                        {
                            value = value.Replace("$" + pair2.Key, pair2.Value);
                        }
                    };
                }

                if (value.Contains("$"))
                {
                    value = value.Replace("$PATH", GetAssemblyDirectory())
                                 .Replace("$TIMESTAMP", DateTime.Now.ToString("yyyyMMdd"));
                }

                //cleanup multiline stuff
                value = Regex.Replace(value, "[\n\r\t]", " ");

                nameValuePairs.Add(name, value.Trim());
            }

            appSettings = nameValuePairs;

            return nameValuePairs;
        }

        public static void saveFrom(object o, string configFile)
        {
            BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Public
                            | BindingFlags.GetField | BindingFlags.Static
                            | BindingFlags.Instance | BindingFlags.GetProperty
                            | BindingFlags.NonPublic;

            Type objType = o.GetType();
            foreach (var field in objType.GetFields(flags))
            {
                var name = field.Name;
                var value = field.GetValue(o);
                Console.WriteLine("writing " + name + "=" + value);
                Save(name, value.ToString(), configFile);
            }
        }


        public static void saveFrom(Dictionary<string, string> settings, string configFile)
        {
            foreach (string key in settings.Keys) 
            {
                string value = settings[key];
                Save(key, value, configFile);
            }
        }
        /// <summary>
        /// NOT WELL TESTED.
        /// </summary>
        public static void Save(String name, String value, string configFile)
        {
            //NOTE: Loading up the file each time we want to save one setting isn't the most efficient thing in the world,
            //but since we'll only ever be saving a couple settings, it's ok for the time being

            bool wasUpdated = false;

            if (!File.Exists(configFile))
            {
                File.WriteAllLines(configFile, new string[] { name + "=" + value });
            }
            else
            {
                List<string> lines = File.ReadAllLines(configFile).ToList();

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(name + "="))
                    {
                        Console.WriteLine("updating config setting:" + lines[i] + " to " + name + "=" + value);
                        lines[i] = name + "=" + value;
                        wasUpdated = true;
                        break;
                    }
                }
                if (!wasUpdated)
                {
                    lines.Add(name + "=" + value);
                    Console.WriteLine("add config setting: " + name + "=" + value);
                }
                File.WriteAllLines(configFile, lines.ToArray());
            }

        }

        public static string GetAssemblyDirectory()
        {
            //TODO: AppDomain.CurrentDomain.BaseDirectory;?
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

    }
}

