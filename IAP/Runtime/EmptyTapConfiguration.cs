namespace TapSDK.IAP
{
    public class EmptyTapConfiguration : ITapTapStoreConfiguration
    {
        public void SetClientId(string clientId)
        {
        }

        public void SetClientToken(string clientToken)
        {
        }

        public void SetRegionCode(int regionCode)
        {
            throw new System.NotImplementedException();
        }

        public void SetEnableLog(bool isRNDMode)
        {
            
        }

        public void SetIsRNDMode(bool isRNDMode)
        {
            
        }
    }
}
