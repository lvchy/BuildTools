using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using BuildReportTool.Window.Screen;
using Funplus;
using System.Security;
using System.Text;
using DigitalRuby.LightningBolt;

namespace Build {
    public class BuildPackage {
        const string COMPANY_NAME = "Jade Interactive";
        const string APP_NAME = "4elements";
        const string BUNDLE_NAME = "fourelements";
        const string PRODUCT_NAME = "Chaos Magic";

        const string PACKAGE_ROOT = "_package";

        const string SCENE_ROOT = "Assets/Scenes";
        static List<string> defaultScenes = new List<string>() { "Launcher.unity" };
        static HashSet<string> disableScenes;

        static BuildOptions buildOp = BuildOptions.None;
        static string packagePath;
        static string obbPath;

        static string usage;
        static PackageUsage pkgusage;

        static bool oriUseUab;
        static bool oriUpdateUab;
        static void setPackageName(PackageType pkgType, bool updateUab, bool isdebug, string svnver) {
            BuildTargetGroup currTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string root = ensurePackageRoot(pkgusage.name, currTargetGroup.ToString());
            string filename = "";
            if (currTargetGroup == BuildTargetGroup.Android) {
                filename = $"{APP_NAME}_" +
                            $"{BuildUtil.GetBuiltinVer()}_"+
                            $"{DateTime.Now.ToString("yyMMddHHmm")}_" +
                            $"{svnver}_" +
                            $"{(pkgType == PackageType.Full ? "f" : "m")}_" +
                            $"{(isdebug ? "d" : "r")}_" +
                            $"{(updateUab ? 1 : 0)}_" +
                            $"{(int)pkgusage.chtype}_" +
                            $"{(pkgusage.sdkenv == SdkEnv.Production ? "p" : "s")}_" +
                            $"{pkgusage.tag}.apk";
            }
            Debug.Log($"pkgname: {filename}");
            packagePath = Path.Combine(root, filename);

            if (pkgusage.useobb) {
                obbPath = Path.Combine(root, $"main.{svnver}.com.raid.fourelements.obb");
            }
        }

        public static void Build(PackageType pkgType,
                                bool updateUab = true,
                                bool isdebug = false,
                                string svnver = "",
                                string strusage = "",
                                bool sdkpay = false,
                                bool internalDebug = false) {
            Debug.Log("start package build...");

            if (string.IsNullOrEmpty(svnver)) {
                svnver = SvnEditor.GetVersion().ToString();
            }

            usage = strusage.ToLower();
            pkgusage = BuildUtil.SwitchPackageUsage(usage);
            BuildUtil.SaveSvnversion(svnver);
            BuildUtil.SaveSdkPaySwitch(sdkpay);
            BuildUtil.SaveInternalDebug(internalDebug);
            setPackageName(pkgType, updateUab, isdebug, svnver);

            Debug.Log($"loginURL: {pkgusage.loginurl}, sdkenv: {pkgusage.sdkenv}, chtype: {pkgusage.chtype}, useobb: {pkgusage.useobb}, tag: {pkgusage.tag}");

            BuildUabMenu.BuildAll();

            preProcess(pkgType, updateUab);
            buildPackage(isdebug, svnver);
            postProcess();

            Debug.Log("end package build...");
        }

        private static void preProcess(PackageType pkgType, bool updateUab) {
            Debug.Log("start pre process...");

            BuildUtil.SavePkgType(pkgType);

            // uab
            BuildUabMenu.Copy2stream(pkgType == PackageType.Mini);
            oriUseUab = BuildUabMenu.GetUseUab();
            oriUpdateUab = BuildUabMenu.GetUpdateuab();
            BuildUabMenu.SetUseUab(true);
            BuildUabMenu.SetUpdateUab(updateUab);

            // xlua
            CSObjectWrapEditor.Generator.ClearAll();
            CSObjectWrapEditor.Generator.GenAll();

            preProcessScenesSetting();

#if UNITY_ANDROID
            // 删除 manifest temp
            string[] files = new string[] {
                "Assets/Plugins/Android/AndroidManifest.xml",
                "Assets/Plugins/Android/AndroidManifest.xml.meta",
                "Assets/Plugins/Android/AndroidManifestTemp.xml",
                "Assets/Plugins/Android/AndroidManifestTemp.xml.meta",
                "Assets/Plugins/Android/mainTemplate.gradle",
                "Assets/Plugins/Android/mainTemplate.gradle.meta",
                "Assets/Plugins/Android/mainTemplateA.gradle",
                "Assets/Plugins/Android/mainTemplateA.gradle.meta",
            };
            foreach (string file in files) {
                if (File.Exists(file)) {
                    File.Delete(file);
                }
            }
#endif

            AssetDatabase.Refresh();

            Debug.Log("end pre process...");
        }

        private static void postProcess() {
            Debug.Log("start post process...");

            // xlua
            CSObjectWrapEditor.Generator.ClearAll();

            postProcessScenesSetting();

            // uab
            BuildUabMenu.SetUseUab(oriUseUab);
            BuildUabMenu.SetUpdateUab(oriUpdateUab);

            AssetDatabase.Refresh();

#if UNITY_ANDROID
            // 删除symbols.zip
            string root = Path.GetDirectoryName(packagePath);
            string[] zips = Directory.GetFiles(root, "*.zip", SearchOption.TopDirectoryOnly);
            foreach (string zipfile in zips) {
                File.Delete(zipfile);
            }

            // 重命名 obb
            if (pkgusage.useobb) {
                string oldobb = Path.ChangeExtension(packagePath, ".main.obb");
                UabFileUtil.CopyFile(oldobb, obbPath);
                Debug.Log($"rename obb. src[ {oldobb} ], dst[ {obbPath} ]");
                File.Delete(oldobb);
            }

            // copy 2 nas
            string nasroot = $@"\\nas.centurygamesh.io\Public\deployment\release\package\{usage}\{UabCommon.GetPlatformName()}\";

            string nasPkgPath = nasroot + Path.GetFileName(packagePath);
            Debug.Log($"--------------------------------------- package path ----------------------------------------{nasPkgPath}");
            UabFileUtil.CopyFile(packagePath, nasPkgPath);
            string pkgtmp = nasPkgPath;
            if (pkgusage.useobb) {
                string nasObbPath = nasroot + Path.GetFileName(obbPath);
                UabFileUtil.CopyFile(obbPath, nasObbPath);
                Debug.Log($"copy obb to nas. src[ {obbPath} ], dst[ {nasObbPath} ]");
                pkgtmp += $"\n{nasObbPath}";
            }

            string pkgtmpPath = Path.Combine(ensurePackageRoot(), "pkgtmp.txt");
            Debug.Log($"pkgtmpPath: {pkgtmpPath}. pkgtmp: {pkgtmp}");
            File.WriteAllBytes(pkgtmpPath, Encoding.UTF8.GetBytes(pkgtmp));
            
#endif
            Debug.Log("end post process...");
        }

        private static void preProcessScenesSetting() {
            disableScenes = new HashSet<string>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in scenes) {
                string scenename = scene.path.Substring(SCENE_ROOT.Length + 1);
                if (scene.enabled && !defaultScenes.Contains(scenename)) {
                    scene.enabled = false;
                    disableScenes.Add(scene.path);
                }
            }
            EditorBuildSettings.scenes = scenes;
        }

        private static void postProcessScenesSetting() {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in scenes) {
                if (scene.enabled) {
                    continue;
                }
                if (disableScenes.Contains(scene.path)) {
                    scene.enabled = true;
                }
            }
            EditorBuildSettings.scenes = scenes;
        }

        private static void settingBuildEnv(bool isdebug = false, string svnver = "0") {
            if (isdebug) {
                buildOp = BuildOptions.CompressWithLz4 | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler;
            } else {
                buildOp = BuildOptions.CompressWithLz4 | BuildOptions.SymlinkLibraries;
            }

            BuildTargetGroup currTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            if (currTargetGroup == BuildTargetGroup.iOS) {
                EditorUserBuildSettings.iOSBuildConfigType = isdebug ? iOSBuildType.Debug : iOSBuildType.Release;
            } else if (currTargetGroup == BuildTargetGroup.Android) {
                EditorUserBuildSettings.androidBuildType = isdebug ? AndroidBuildType.Debug : AndroidBuildType.Release;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                // 连接电脑调试
                EditorUserBuildSettings.allowDebugging = isdebug;
                EditorUserBuildSettings.connectProfiler = isdebug;
                EditorUserBuildSettings.development = isdebug;

                if (pkgusage.useIL2CPP) {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                } else {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
                }
                AndroidArchitecture aac = AndroidArchitecture.ARMv7;
                if (pkgusage.useX86) {
                    aac |= AndroidArchitecture.X86;
                }
                if (pkgusage.useIL2CPP) {
                    aac |= AndroidArchitecture.ARM64;
                }
                PlayerSettings.Android.targetArchitectures = aac;

                PlayerSettings.stripEngineCode = false;
                PlayerSettings.Android.blitType = AndroidBlitType.Never;

                PlayerSettings.Android.keystoreName = Path.GetFullPath($"{Application.dataPath}/../AndroidKeyStore/raid.keystore");
                PlayerSettings.Android.keystorePass = "diandian";
                PlayerSettings.Android.keyaliasName = "raid";
                PlayerSettings.Android.keyaliasPass = "diandian";
                PlayerSettings.keystorePass = "diandian";
                PlayerSettings.keyaliasPass = "diandian";

                PlayerSettings.Android.bundleVersionCode = int.Parse(svnver);

                PlayerSettings.Android.useAPKExpansionFiles = pkgusage.useobb;
            }

            PlayerSettings.productName = PRODUCT_NAME;
            //PlayerSettings.applicationIdentifier = string.Format("com.{0}.{1}", COMPANY_NAME, BUNDLE_NAME);
        }

        private static void buildPackage(bool isdebug, string svnver = "0") {
            Debug.Log("start build package..." + ", isdebug: " + isdebug.ToString());
            Debug.Log($"start build package... isdebug: {isdebug}");
            settingBuildEnv(isdebug, svnver);

            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, packagePath,
                EditorUserBuildSettings.activeBuildTarget, buildOp);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
                Debug.Log("build package success. " + packagePath);
            } else {
                throw new Exception(string.Format("build package fail. [ {0} ]", report.summary.result));
            }

            Debug.Log("end build package...");
        }

        private static string ensurePackageRoot(params string[] args) {
            string dir = UabFileUtil.PathCombine(Application.dataPath, "..", PACKAGE_ROOT);
            UabFileUtil.EnsureDir(dir);
            for (int i = 0; i < args.Length; i++) {
                dir = UabFileUtil.PathCombine(dir, args[i]);
                UabFileUtil.EnsureDir(dir);
            }
            return dir;
        }
    }
}