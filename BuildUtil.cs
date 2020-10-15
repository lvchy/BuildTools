using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Text;
using System;
using XUPorterJSON;
using Debug = UnityEngine.Debug;

namespace Build {
    public class BuildUtil {
        const string PACKAGE_PATH = "Assets/Editor/Build/package_cfg.json";

        public const string LOGINURL_RAIDTEST = "http://raid-gs.diandian.info:7200/id_inter/id/v1/";
        public const string LOGINURL_INNERTEST = "http://10.1.70.66:8080/id_inter/id/v1/";
        public const string LOGINURL_PRODUCT = "https://raid-test-idinter.centurygame.com/id_inter/id/v1/";

        public static string ExecuteCommand(string app, string args, string workDir = null) {
            List<string> outputs = new List<string>();

            var psi = new ProcessStartInfo(app, args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            if (workDir != null && Directory.Exists(workDir)) {
                psi.WorkingDirectory = workDir;
            }
            using (var p = Process.Start(psi)) {
                while (!p.StandardOutput.EndOfStream) {
                    outputs.Add(p.StandardOutput.ReadLine() + "\n");
                }
                while (!p.StandardError.EndOfStream) {
                    outputs.Add(p.StandardError.ReadLine() + "\n");
                }
                p.WaitForExit(1000 * 10);
            }

            string output = string.Concat(outputs.ToArray());
            Debug.Log(output);

            return output;
        }

        public static void SaveSvnversion(string ver) {
            if (string.IsNullOrEmpty(ver)) {
                ver = SvnEditor.GetVersion().ToString();
            }
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.svnversion = ver;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SaveSdkPaySwitch(bool flag) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.sdkpaySwitch = flag;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SaveInternalDebug(bool flag) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.internalDebug = flag;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SaveLoginURL(string url) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.loginUrl= url;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SaveChannelType(ChannelType chtype) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.channelType = chtype.ToString();
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SavePkgUsage(string usage) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.pkgusage = usage.ToLower();
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static void SavePkgType(PackageType type) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.ismini = type == PackageType.Mini;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static PackageUsage SwitchPackageUsage(string usage) {
            string content = File.ReadAllText(PACKAGE_PATH, Encoding.UTF8);

            Hashtable ht = MiniJSON.jsonDecode(content) as Hashtable;
            Hashtable htUsage = ht["pkgusage"] as Hashtable;
            Hashtable htPkgUsage = htUsage[usage] as Hashtable;
            PackageUsage pkgusage = new PackageUsage();
            pkgusage.name = usage;

            if (htPkgUsage.ContainsKey("loginurl")) {
                pkgusage.loginurl = htPkgUsage["loginurl"] as string;
            }

            if (htPkgUsage.ContainsKey("sdkenv")) {
                pkgusage.sdkenv = (SdkEnv)Enum.Parse(typeof(SdkEnv), htPkgUsage["sdkenv"] as string);
            }

            if (htPkgUsage.ContainsKey("channelType")) {
                pkgusage.chtype = (ChannelType)Enum.Parse(typeof(ChannelType), htPkgUsage["channelType"] as string);
            }

            if (htPkgUsage.ContainsKey("useobb")) {
                pkgusage.useobb = (bool)htPkgUsage["useobb"];
            }

            if (htPkgUsage.ContainsKey("useX86")) {
                pkgusage.useX86 = (bool)htPkgUsage["useX86"];
            }

            if (htPkgUsage.ContainsKey("pkgtag")) {
                pkgusage.tag = htPkgUsage["pkgtag"] as string;
            }

            SaveLoginURL(pkgusage.loginurl);
            SaveChannelType(pkgusage.chtype);
            SavePkgUsage(usage);
            Funplus.Internal.FunplusSettings.Environment = pkgusage.sdkenv == SdkEnv.Production;

            return pkgusage;
        }

        public static string GetBuiltinVer() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            return cfg.version;
        }
    }
}