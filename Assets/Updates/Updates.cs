using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using UnityEngine;
using UnityEditor;

namespace Assets.Updates
{
    public class Version
    {
        public int major { get; }
        public int minor { get; }
        public int patch { get; }

        public Version(string ver)
        {
            string[] parts = ver.Split('.');
            major = int.Parse(parts[0]);
            minor = int.Parse(parts[1]);
            patch = int.Parse(parts[2]);
        }

        
        override public string ToString()
        {
            return major + "." + minor + "." + patch;
        }
    }

    public class Updates
    {
        public static bool upgradeExists(String versionToCheck)
        {
            Version latestRemote = getLatestRemoteVersion();
            Version current = new Version(versionToCheck);

            List<Version> versions = new List<Version>();
            versions.Add(latestRemote);
            versions.Add(current);
            var sorted = versions.OrderBy(a => a.major).ThenBy(a => a.minor).ThenBy(a => a.patch);
            
            Version highest = sorted.Last();
            if (current.ToString().Equals(highest.ToString()))
                return false;
            return true;
        }
        public static Version getLatestRemoteVersion()
        {
            string latestURL = "https://api.github.com/repos/imathrowback/telarafly/releases/latest";

            ServicePointManager.ServerCertificateValidationCallback = delegate (System.Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
            {
                return (true);
            };

            using (WebClient client = new WebClient())
            {
                try
                {

                    client.Headers.Add("user-agent", "telarafly-" + PlayerSettings.bundleVersion);
                    string json = client.DownloadString(latestURL);

                    var definition = new { tag_name = "" };
                    var stuff = JsonConvert.DeserializeAnonymousType(json, definition);
                    return new Assets.Updates.Version(stuff.tag_name);
                }
                catch (WebException ex)
                {
                    Debug.LogError(ex);
                    WebResponse response = ex.Response;
                    WebHeaderCollection headers = response.Headers;
                    for (int i = 0; i < headers.Count; ++i)
                    {
                        string header = headers.GetKey(i);
                        foreach (string value in headers.GetValues(i))
                        {
                            Debug.Log(header + ":" +  value);
                        }
                    }
                    throw ex;
                }
            }
        }
    }
}
