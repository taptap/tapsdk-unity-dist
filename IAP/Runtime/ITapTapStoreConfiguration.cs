using UnityEngine.Purchasing.Extension;

namespace TapSDK.IAP
{
    public interface ITapTapStoreConfiguration : IStoreConfiguration
    {
        void SetClientId(string clientId);

        void SetClientToken(string clientToken);

        void SetRegionCode(int regionCode);

        void SetEnableLog(bool isRNDMode);

        void SetIsRNDMode(bool isRNDMode);
    }
}