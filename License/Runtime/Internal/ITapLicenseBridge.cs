
namespace TapSDK.License.Internal {
    public interface ITapLicenseBridge {

        void Init();
        void RegisterLicencesCallback(ITapLicenseCallback callback);

        void RegisterDLCCallback(ITapDlcCallback callback);

        void CheckLicense();

        void CheckLicenseForcibly();

        void QueryDLC(string[] skus);

        void PurchaseDLC(string sku);

        void SetTestEnvironment(bool isTest);

    }
}