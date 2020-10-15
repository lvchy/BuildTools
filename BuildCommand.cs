using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace Build {
    public class BuildCommand {
        const string ARG_PLATFORM = "-platform";
        const string ARG_PKGTYPE = "-pkgtype";
        const string ARG_UPDATEUAB = "-updateuab";
        const string ARG_LOGINURL = "-loginurl";
        const string ARG_SDKENV = "-sdkenv";
        const string ARG_CHANNELTYPE = "-channeltype";
        const string ARG_SVNVER = "-svnver";
        const string ARG_BUILDTYPE = "-buildtype";
        const string ARG_PKGTAG = "-pkgtag";

        const string ARG_PKGUSAGE = "-pkgusage";
        const string ARG_SDKPAY = "-sdkpay";

        const string ARG_INTERNAL_DEBUG = "-interdebug";

        static void switchPlatform(string platform) {
            if (platform == "Android") {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }
            } else if (platform == "iOS") {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS) {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                }
            } else {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64) {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                }
            }
        }

        public static void CmdSwitchPlatform() {
            string[] args = Environment.GetCommandLineArgs();
            string platform = "";
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == ARG_PLATFORM) {
                    platform = args[i + 1];
                    break;
                }
            }
            Debug.Log($"switch platform to {platform}");
            switchPlatform(platform);
        }

        public static void CmdBuildUab() {
            BuildUabMenu.BuildAsset();
        }

        public static void CmdBuildPackage() {
            HashSet<string> hs = new HashSet<string>() {
            ARG_PKGTYPE,
            ARG_UPDATEUAB,
            ARG_LOGINURL,
            ARG_SDKENV,
            ARG_CHANNELTYPE,
            ARG_SVNVER,
            ARG_BUILDTYPE,
            ARG_PKGTAG,
            ARG_PKGUSAGE,
            ARG_SDKPAY,
            ARG_INTERNAL_DEBUG
        };

            PackageType pkgtype = PackageType.Full;
            bool updateUab = true;
            string loginurl = "";
            string svnver = "";
            SdkEnv sdkenv = SdkEnv.Production;
            ChannelType chtype = ChannelType.Raw;
            bool isdebug = false;
            string pkgtag = "";

            string pkgusage = "";
            bool sdkpay = false;

            bool internalDebug = false;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) {
                if (!hs.Contains(args[i])) {
                    Logger.D("unknown arg", args[i]);
                    continue;
                }

                Logger.D("arg", args[i], args[i + 1]);
                switch (args[i]) {
                    case ARG_PKGTYPE:
                        pkgtype = (PackageType)Enum.Parse(typeof(PackageType), args[i + 1]);
                        break;
                    case ARG_UPDATEUAB:
                        updateUab = bool.Parse(args[i + 1]);
                        break;
                    case ARG_LOGINURL:
                        loginurl = args[i + 1];
                        break;
                    case ARG_SDKENV:
                        sdkenv = (SdkEnv)Enum.Parse(typeof(SdkEnv), args[i + 1]);
                        break;
                    case ARG_CHANNELTYPE:
                        chtype = (ChannelType)Enum.Parse(typeof(ChannelType), args[i + 1]);
                        break;
                    case ARG_SVNVER:
                        svnver = args[i + 1];
                        break;
                    case ARG_BUILDTYPE:
                        isdebug = (args[i + 1] == "Debug" ? true : false);
                        break;
                    case ARG_PKGTAG:
                        pkgtag = (args[i + 1]).ToLower();
                        break;
                    case ARG_PKGUSAGE:
                        pkgusage = (args[i + 1]).ToLower();
                        break;
                    case ARG_SDKPAY:
                        sdkpay = bool.Parse(args[i + 1]);
                        break;
                    case ARG_INTERNAL_DEBUG:
                        internalDebug = bool.Parse(args[i + 1]);
                        break;
                    default:
                        continue;
                }
                i++;
            }

            Logger.D("pkgtype", pkgtype, "updateuab", updateUab, "svnver", svnver, "debug", isdebug, "pkgusage", pkgusage, "sdkpay", sdkpay, "internalDebug", internalDebug);

            BuildPackage.Build(pkgtype, updateUab, isdebug, svnver, pkgusage, sdkpay, internalDebug);
        }
    }
}