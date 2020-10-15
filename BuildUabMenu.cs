using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;
using System.Linq;

namespace Build {
    public class BuildUabMenu {
        const string MENU_ROOT = BuildMenu.MENU_ROOT + "Uab/";

        const string BUILTIN_CFG_PATH = "Assets/Resources/Builtin/builtin_cfg.txt";

        #region uab build
        static string getUabcfgPath() {
            return "Assets/Standard Assets/UAB/Editor/cfg.txt";
        }


        internal class fcfg
        {
            public fcfg(string ext,bool res)
            {
                this.extensions = ext;
                this.resourceasset = res;
            }
            public string extensions;
            public bool resourceasset;
        }

        //用来刷新cfg的按文件夹打包的接口
        static Dictionary<string, fcfg> getFolderPackAB()
        {
            Dictionary<string, fcfg> l = new Dictionary<string, fcfg>();
            l.Add("Assets/ResourcesAB/UI/Icon", new fcfg(".png,.asset", false));
            l.Add("Assets/ResourcesAB/UI/Textures", new fcfg(".png", false));
            l.Add("Assets/ResourcesAB/UI", new fcfg(".*", false));
            l.Add("Assets/ResourcesAB/UI/UIAtlas", new fcfg(".png", true));
            l.Add("Assets/Arts/heros", new fcfg(".*", true));
            l.Add("Assets/Arts/Scenes", new fcfg(".*", true));

            return l;
        }


        static List<string> defaultVar()
        {
            List<string> def = new List<string>();
            def.Add("\"onlytop\":false,");
            def.Add("\"dirsearchlayer\":0,");
            def.Add("\"uabcount\":1,");
            def.Add("\"enabled\":true,");
            def.Add("\"builtin\":false,");
            def.Add("\"isasset\":true,");
            def.Add("\"isbinary\":false,");
            def.Add("\"noencrypt\":false,");
            def.Add("\"forceextract\":false,");
            def.Add("\"usetmp\":false,");
            def.Add("\"resourceasset\":false");

            return def;
        }

        static void optimizeJsonFile()
        {
            string[] nl = File.ReadAllLines(getUabcfgPath(), Encoding.UTF8);
            List<string> nsl = new List<string>();

            var defau = defaultVar();

            for (int i = 0;i<nl.Length;i++)
            {
                var str = nl[i];
                var strT = str.Trim().Replace(" ","");
                //Debug.LogError("str is :" + strT + " is contain :" + defau.Contains(strT));
                if (!defau.Contains(strT))
                    nsl.Add(str);
            }

            for (int i = 0;i<nsl.Count;i++)
            {
                var str = nsl[i];
                
                if ((i + 1) < nsl.Count)
                {
                    var next_str = nsl[i + 1];
                    var strT = next_str.Trim().Replace(" ", "");
                    Debug.LogError("strT is :" + strT);
                    Debug.LogError(" | is true : " +  (strT == "},"));
                    if (strT == "},")
                    {
                        str = str.Replace(",","");
                        nsl[i] = str;
                        Debug.LogError("NEW IS " + nsl[i]);
                    }                    
                }                
            }

            File.WriteAllLines(getUabcfgPath(), nsl.ToArray());

            AssetDatabase.Refresh();
        }


        public static void ResetABCfg()
        {
            var addL = getFolderPackAB();
            var newL = new Dictionary<string,UabConfigGroup>();
            UabConfig cfg = UabCommon.LoadUabConfigFromFile(getUabcfgPath());
            for (int i = 0;i<cfg.G.Length;i++)
            {
                newL.Add(cfg.G[i].name.ToLower(), cfg.G[i]);
            }

            foreach (var n in addL)
            {
                string[] abdirs = UabCollect.collectDirPath(n.Key,1);
                for (int i = 0;i<abdirs.Length;i++)
                {
                    var dir = abdirs[i].Replace("\\","/");
                    abdirs[i] = dir;
                }

                foreach (var dir in abdirs)
                {                    
                    if (addL.ContainsKey(dir))//子目录如果按照文件夹打包的话，这里就过滤掉
                    {
                        continue;
                    }

                    var ndir = dir.Split('/');
                    UabConfigGroup ncfg = new UabConfigGroup();
                    if (ndir.Length < 2)
                    {
                        Debug.LogError("不太可能吧，怎么能在Assets下直接放资源呢");
                        return;
                    }
                    ncfg.name = string.Format("{0}{1}", ndir[ndir.Length - 2],ndir[ndir.Length-1]);
                    ncfg.name = ncfg.name.ToLower();
                    ncfg.assetpath = dir;
                    ncfg.loadpath = dir.Replace("Assets/ResourcesAB/","");

                    ncfg.extensions = n.Value.extensions;
                    ncfg.resourceasset = n.Value.resourceasset;
                    ncfg.uabcount = 1;
                    if (!newL.ContainsKey(ncfg.name))
                    {
                        newL.Add(ncfg.name, ncfg);
                    }
                    else
                    {
                        newL[ncfg.name] = ncfg;
                    }
                }                
            }

            cfg.G = newL.Values.ToArray<UabConfigGroup>();
            string js = JsonUtility.ToJson(cfg,true);

            File.WriteAllBytes(getUabcfgPath(), Encoding.UTF8.GetBytes(js));
            AssetDatabase.Refresh();

            optimizeJsonFile();
        }

        static string getPythonCmdPath() {
            string path = "";
            if (Application.platform == RuntimePlatform.OSXEditor) {
                path = "/usr/local/bin/python3";
            } else {
                path = Path.Combine(Application.dataPath, "../../Tools/Python38-32/python.exe");
                path = Path.GetFullPath(path).Replace('\\', '/');
            }
            Debug.Log("python path: " + path);
            return path;
        }

        static void buildBinary() {
            string cmd = "cmd_buildbinary";
            string workroot = Path.GetFullPath(UabFileUtil.PathCombine(Application.dataPath, "..")).Replace('\\', '/');
            string platform = UabCommon.GetPlatformName();
            string pypath = Path.GetFullPath(UabFileUtil.PathCombine(Application.dataPath, "../../Tools/UabTools/runner.py"));
            string args = string.Format("{0} --cmd={1} --workroot={2} --platform={3}", pypath, cmd, workroot, platform);
            string workdir = @"C:\work\repository\projectrain_clean\Tools\UabTools";
            BuildUtil.ExecuteCommand(getPythonCmdPath(), args, workdir);
        }

        public static void Copy2stream(bool onlyBuiltin = false) {
            Debug.Log("start copy 2 stream... onlyBuiltin: " + onlyBuiltin);

            string cmd = "cmd_copy2stream";
            string workroot = Path.GetFullPath(UabFileUtil.PathCombine(Application.dataPath, "..")).Replace('\\', '/');
            string platform = UabCommon.GetPlatformName();
            string pypath = Path.GetFullPath(UabFileUtil.PathCombine(Application.dataPath, "../../Tools/UabTools/runner.py"));
            string args = string.Format("{0} --cmd={1} --workroot={2} --platform={3} --onlybuiltin={4} --pkgusage={5}", pypath, cmd, workroot, platform, onlyBuiltin, "raid-test");
            string workdir = @"C:\work\repository\projectrain_clean\Tools\UabTools";
            BuildUtil.ExecuteCommand(getPythonCmdPath(), args, workdir);

            AssetDatabase.Refresh();

            Debug.Log("end copy 2 stream...");
        }

        public static void BuildAsset() {
            Debug.Log("start build all uab...");
            try {
                UabBuild.Build(getUabcfgPath());
            } catch (Exception e) {
                Debug.LogError(e.Message +" stack trace is " + e.StackTrace);
            }
            Debug.Log("end build all uab...");
        }

        [MenuItem(MENU_ROOT + "rebuild cfg", false, BuildMenu.UAB_IDX_START)]
        public static void RebuildABCfg()
        {
            ResetABCfg();
        }

        [MenuItem(MENU_ROOT + "build binary only", false, BuildMenu.UAB_IDX_START + 1)]
        public static void BuildBinary() {
            Debug.Log("start build binary uab...");
            try {
                buildBinary();
            } catch (Exception e) {
                Debug.LogError(e.Message);
            }
            Debug.Log("end build binary uab...");
        }

        [MenuItem(MENU_ROOT + "build all", false, BuildMenu.UAB_IDX_START)]
        public static void BuildAll() {
            BuildAsset();
            buildBinary();
        }

        [MenuItem(MENU_ROOT + "copy2stream all", false, BuildMenu.UAB_IDX_START + 2)]
        static void copy2stream_all() {
            Copy2stream(false);
        }

        [MenuItem(MENU_ROOT + "copy2stream builtin", false, BuildMenu.UAB_IDX_START + 3)]
        static void copy2stream_builtin() {
            Copy2stream(true);
        }
        #endregion

        #region uab switch
        const string MENU_USE_UAB = MENU_ROOT + "使用uab";
        [MenuItem(MENU_USE_UAB, false, BuildMenu.UAB_IDX_START + 101)]
        static void menuUseUab() {
            bool flag = Menu.GetChecked(MENU_USE_UAB);
            SetUseUab(!flag);
            Menu.SetChecked(MENU_USE_UAB, !flag);
        }
        [MenuItem(MENU_USE_UAB, true, BuildMenu.UAB_IDX_START + 101)]
        static bool menuUseUabValid() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            Menu.SetChecked(MENU_USE_UAB, cfg.useUab);
            return true;
        }

        const string MENU_UPDATE_UAB = MENU_ROOT + "更新uab";
        [MenuItem(MENU_UPDATE_UAB, false, BuildMenu.UAB_IDX_START + 102)]
        static void menuUpdateUab() {
            bool flag = Menu.GetChecked(MENU_UPDATE_UAB);
            SetUpdateUab(!flag);
            Menu.SetChecked(MENU_UPDATE_UAB, !flag);
        }
        [MenuItem(MENU_UPDATE_UAB, true, BuildMenu.UAB_IDX_START + 102)]
        static bool menuUpdateUabValid() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            Menu.SetChecked(MENU_UPDATE_UAB, cfg.updateUab);
            return true;
        }

        public static void SetUseUab(bool flag) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.useUab = flag;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }
        public static void SetUpdateUab(bool flag) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            cfg.updateUab = flag;
            BuiltinData.WriteBuiltinConfig(cfg);
            AssetDatabase.Refresh();
        }

        public static bool GetUpdateuab() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            return cfg.updateUab;
        }

        public static bool GetUseUab() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            return cfg.useUab;
        }
        #endregion

        [MenuItem(MENU_ROOT + "清除解压标志", false, BuildMenu.UAB_IDX_START + 201)]
        static void clearExtractFlag() {
            PlayerPrefs.SetInt(UabDef.UAB_EXTRACT_FLAG, -1);
            Debug.Log("done...");

            string root = UabCommon.GetPersistUabRoot();
            if (Directory.Exists(root)) {
                Directory.Delete(root, true);
            }
        }

        [MenuItem(MENU_ROOT + "读取 uab 中的 asset", false, BuildMenu.UAB_IDX_START + 202)]
        static void loadUabAssetNames() {
            UnityEngine.Object o = Selection.activeObject;
            if (o == null) {
                return;
            }
            string path = AssetDatabase.GetAssetPath(o);
            Debug.Log(path);
            AssetBundle ab = AssetBundle.LoadFromFile(path);
            if (ab == null) {
                return;
            }
            Debug.Log($"----------------------- {System.IO.Path.GetFileNameWithoutExtension(path)} -------------------------");
            string[] names = ab.GetAllAssetNames();
            foreach (string n in names) {
                Debug.Log(n);
            }
            Debug.Log("------------------------------------------------");
            ab.Unload(true);
        }

        [MenuItem(MENU_ROOT + "解压 uab", false, BuildMenu.UAB_IDX_START + 202)]
        static void extractUab() {
            string path = BattleEditor.SelectBattleReplay("u");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            byte[] bt = File.ReadAllBytes(path);
            UabEncrypt.DecryptBytes(bt);
            File.WriteAllBytes(path, bt);

            Debug.Log("done...");
        }
    }
}