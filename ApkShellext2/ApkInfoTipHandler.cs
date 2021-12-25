﻿using ApkQuickReader;
using Microsoft.Win32;
using SharpShell.Attributes;
using SharpShell.Diagnostics;
using SharpShell.Extensions;
using SharpShell.ServerRegistration;
using SharpShell.SharpInfoTipHandler;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ApkShellext2.Properties;

namespace ApkShellext2 {

    [Guid("946435a5-fe96-416d-99db-e94ee9fb46c8")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".ipa")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appxbundle")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appx")]
    public class ApkInfoTipHandler : SharpInfoTipHandler {
        /// <summary>
        /// Gets info for the selected item (SelectedItemPath).
        /// </summary>
        /// <param name="infoType">Type of info to return.</param>
        /// <param name="singleLine">if set to <c>true</c>, put the info in a single line.</param>
        /// <returns>
        /// Specified info for the selected file.
        /// </returns>
        protected override string GetInfo(RequestedInfoType infoType, bool singleLine) {
            try {
                Utility.Localize();
                string TipPattern = Utility.GetSetting("ToolTipPattern", NonLocalizeResources.strInfoTipDefault);
                bool isapk = SelectedItemPath.EndsWith(".apk");
                bool isipa = SelectedItemPath.EndsWith(".ipa");
                
                using (AppPackageReader reader = AppPackageReader.Read(SelectedItemPath)) {
                    return ApkContextMenu.ReplaceVariables(TipPattern, reader);
                }
            } catch (Exception ex) {
                Log("Error happend during GetInfo : " + ex.Message);
                return Properties.Resources.strReadFileFailed;
            }
        }

        [CustomRegisterFunction]
        public static void postDoRegister(Type type, RegistrationType registrationType) {
            Console.WriteLine("Registering " + type.FullName);

            #region Clean up older versions registry
            try {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"\CLSID\" +
                    type.GUID.ToRegistryString() + @"\InprocServer32")) {
                    if (key != null && key.GetSubKeyNames().Count() != 0) {
                        Console.WriteLine("Found old version in registry, cleaning up ...");
                        foreach (var k in key.GetSubKeyNames()) {
                            if (k != type.Assembly.GetName().Version.ToString()) {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"\CLSID\" +
                        type.GUID.ToRegistryString() + @"\InprocServer32\" + k);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Logging.Error("Cleaning up older version but see exception. "
                     + e.Message);
            }
            #endregion
        }

        protected override void Log(string message){
            Utility.Log(this, Path.GetFileName(SelectedItemPath), message);
        }
    }
}
