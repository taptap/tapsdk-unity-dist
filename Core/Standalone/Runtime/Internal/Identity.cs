using System;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal {
    public class Identity {
        public static readonly string DEVICE_ID_KEY = "tapdb_unique_id";
        public static readonly string PERSISTENT_ID_KEY = "tapdb_persist_id";
        public static readonly string INSTALLATION_ID_KEY = "tapdb_install_id";

        public static string DeviceId {
            get {
                string deviceId = TapCoreStandalone.Prefs.Get<string>(DEVICE_ID_KEY);
                if (string.IsNullOrWhiteSpace(deviceId)) {
                    deviceId = SystemInfo.deviceUniqueIdentifier;
                    TapCoreStandalone.Prefs.Set(DEVICE_ID_KEY, deviceId);
                }
                return deviceId;
            }
        }

        public static string PersistentId {
            get {
                string persistentId = TapCoreStandalone.Prefs.Get<string>(PERSISTENT_ID_KEY);
                if (string.IsNullOrWhiteSpace(persistentId)) {
                    persistentId = Guid.NewGuid().ToString();
                    TapCoreStandalone.Prefs.Set(PERSISTENT_ID_KEY, persistentId);
                }
                return persistentId;
            }
        }

        public static string InstallationId {
            get {
                string installationId = TapCoreStandalone.Prefs.Get<string>(INSTALLATION_ID_KEY);
                if (string.IsNullOrWhiteSpace(installationId)) {
                    installationId = Guid.NewGuid().ToString();
                    TapCoreStandalone.Prefs.Set(INSTALLATION_ID_KEY, installationId);
                }
                return installationId;
            }
        }
    }
}