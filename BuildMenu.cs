using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Build {
    public class BuildMenu {
        public const string MENU_ROOT = "Build/";

        public const int PACKAGE_IDX_START = 1001;
        public const int UAB_IDX_START = 2001;
        public const int OTHER_IDX_START = 3001;

        [MenuItem(MENU_ROOT + "打开 player settings", false, 1)]
        static void openPlayerSettings() {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        const string MENU_SWITCH_RAIDTEST = MENU_ROOT + "切换环境/项目组内部测试";
        [MenuItem(MENU_SWITCH_RAIDTEST, false, OTHER_IDX_START)]
        static void switchEnvironment_RaidTest() {
            BuildUtil.SwitchPackageUsage(PackageUsage.RaidTest);
            AssetDatabase.Refresh();
        }
        [MenuItem(MENU_SWITCH_RAIDTEST, true, OTHER_IDX_START)]
        static bool switchEnvironment_RaidTest_valid() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            bool flag = cfg.pkgusage == PackageUsage.RaidTest;
            Menu.SetChecked(MENU_SWITCH_RAIDTEST, flag);
            return true;
        }

        const string MENU_SWITCH_RELEASE = MENU_ROOT + "切换环境/对外正式";
        [MenuItem(MENU_SWITCH_RELEASE, false, OTHER_IDX_START + 1)]
        static void switchEnvironment_Release() {
            BuildUtil.SwitchPackageUsage(PackageUsage.Product);
            AssetDatabase.Refresh();
        }
        [MenuItem(MENU_SWITCH_RELEASE, true, OTHER_IDX_START + 1)]
        static bool switchEnvironment_Release_valid() {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            bool flag = cfg.pkgusage == PackageUsage.Product;
            Menu.SetChecked(MENU_SWITCH_RELEASE, flag);
            return true;
        }
    }
}