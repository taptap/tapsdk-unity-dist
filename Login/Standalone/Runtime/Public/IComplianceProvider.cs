namespace TapSDK.Login.Standalone
{
    public interface IComplianceProvider
    {
        string GetAgeRangeScope(bool isCN);
    }
}