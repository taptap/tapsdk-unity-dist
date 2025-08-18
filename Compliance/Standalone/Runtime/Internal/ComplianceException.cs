using TapSDK.Core;

namespace TapSDK.Compliance 
{
    public class ComplianceException : TapException 
    {
        public string Error { get; internal set; }

        public string Description { get; internal set; }

        public long Now { get; internal set; }

        public long ErrorCode {get; internal set;}

        public string ErrorMessage  { get; internal set; }

        public ComplianceException(int code, string message) : base(code, message) { }

        public bool IsTokenExpired(){
            return Error != null && Error.Equals("business_code_error") && ErrorCode == 200000;
        }

    }
}
