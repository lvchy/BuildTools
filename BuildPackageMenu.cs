using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;

namespace Build {
    public class BuildPackageMenu {
        const string MENU_ROOT = BuildMenu.MENU_ROOT + "Package/";

        static void build(PackageType pkdtype, bool debug) {
            BuiltinConfig cfg = BuiltinData.LoadBuiltinConfig();
            BuildPackage.Build(pkdtype, false, debug, "", PackageUsage.RaidTest, cfg.sdkpaySwitch, true);
        }

        [MenuItem(MENU_ROOT + "Develop/Build Full", false, BuildMenu.PACKAGE_IDX_START + 1)]
        static void buildDevelopFull() {
            build(PackageType.Full, true);
        }
        [MenuItem(MENU_ROOT + "Develop/Build Mini", false, BuildMenu.PACKAGE_IDX_START + 1)]
        static void buildDevelopMini() {
            build(PackageType.Mini, true);
        }

        [MenuItem(MENU_ROOT + "Release/Build Full", false, BuildMenu.PACKAGE_IDX_START + 2)]
        static void buildReleaseFull() {
            build(PackageType.Full, false);
        }
        [MenuItem(MENU_ROOT + "Release/Build Mini", false, BuildMenu.PACKAGE_IDX_START + 2)]
        static void buildReleaseMini() {
            build(PackageType.Mini, false);
        }
    }
}