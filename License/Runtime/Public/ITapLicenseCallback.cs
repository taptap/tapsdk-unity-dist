namespace TapSDK.License {
    public interface ITapLicenseCallback
    {
        void OnLicenseSuccess();

        void OnLicenseFailed();
    }
}
