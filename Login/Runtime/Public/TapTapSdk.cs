using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal;
using UnityEngine;

namespace TapSDK.Login.Internal
{
    public static class TapTapSdk
    {
        public static string ClientId { get; private set; }

        public static Region CurrentRegion { get; private set; }
        
        public static void SDKInitialize(string clientId, bool isCn) {
            ClientId = clientId;
            CurrentRegion = isCn ? (Region)new RegionCN() : new RegionIO();
            TapLocalizeManager.SetCurrentRegion(isCn);
        }
    }
}