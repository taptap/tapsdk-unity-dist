
namespace TapSDK.Core.Standalone.Internal.Http
{
    /// <summary>
    /// HTTP 错误常量类。
    /// </summary>
    public static class TapHttpErrorConstants
    {

        // --------------------------------------------------------------------------------------------
        // 下面的错误信息是服务器端 [data.error] 字段的枚举
        // --------------------------------------------------------------------------------------------

        /// <summary>
        /// 400 请阅读：[通用 API 人机验证协议](https://xindong.atlassian.net/wiki/spaces/TAP/pages/66032081)。
        /// </summary>
        public const string ERROR_CAPTCHA_NEEDS = "captcha.needs";

        /// <summary>
        /// 400 人机验证未通过。
        /// </summary>
        public const string ERROR_CAPTCHA_FAILED = "captcha.failed";

        /// <summary>
        /// 403 用户冻结。
        /// </summary>
        public const string ERROR_USER_IS_DEACTIVATED = "user_is_deactivated";

        /// <summary>
        /// 400 请求缺少某个必需参数，包含一个不支持的参数或参数值，或者格式不正确。
        /// </summary>
        public const string ERROR_INVALID_REQUEST = "invalid_request";

        /// <summary>
        /// 404 请求失败，请求所希望得到的资源未被在服务器上发现。在参数相同的情况下，不应该重复请求。
        /// </summary>
        public const string ERROR_NOT_FOUND = "not_found";

        /// <summary>
        /// 403 用户没有对当前动作的权限，引导重新身份验证并不能提供任何帮助，而且这个请求也不应该被重复提交。
        /// </summary>
        public const string ERROR_FORBIDDEN = "forbidden";

        /// <summary>
        /// 500 服务器出现异常情况，可稍等后重新尝试请求，但需有尝试上限，建议最多 3 次，如一直失败，则中断并告知用户。
        /// </summary>
        public const string ERROR_SERVER_ERROR = "server_error";

        /// <summary>
        /// 400 客户端时间不正确，应请求服务器时间重新构造。
        /// </summary>
        public const string ERROR_INVALID_TIME = "invalid_time";

        /// <summary>
        /// 400 请求是重放的。
        /// </summary>
        public const string ERROR_REPLAY_ATTACKS = "replay_attacks";

        /// <summary>
        /// 401 client_id、client_secret 参数无效。
        /// </summary>
        public const string ERROR_INVALID_CLIENT = "invalid_client";

        /// <summary>
        /// 400 提供的 Access Grant 是无效的、过期的或已撤销的，例如: Device Code 无效（一个设备授权码只能使用一次）等。
        /// </summary>
        public const string ERROR_INVALID_GRANT = "invalid_grant";

        /// <summary>
        /// 400 服务器不支持 grant_type 参数的值。
        /// </summary>
        public const string ERROR_UNSUPPORTED_GRANT_TYPE = "unsupported_grant_type";

        /// <summary>
        /// 400 服务器不支持 response_type 参数的值。
        /// </summary>
        public const string ERROR_UNSUPPORTED_RESPONSE_TYPE = "unsupported_response_type";

        /// <summary>
        /// 400 服务器不支持 secret_type 参数的值。
        /// </summary>
        public const string ERROR_UNSUPPORTED_SECRET_TYPE = "unsupported_secret_type";

        /// <summary>
        /// 400 Device Flow 中，设备通过 Device Code 换取 Access Token 的接口过于频繁。
        /// </summary>
        public const string ERROR_SLOW_DOWN = "slow_down";

        /// <summary>
        /// 429 登录尝试次数过多，请稍后重试，用于 password 模式下出错次数过多。
        /// </summary>
        public const string ERROR_TOO_MANY_LOGIN_ATTEMPTS = "too_many_login_attempts";

        /// <summary>
        /// 401 授权服务器拒绝请求，这个状态出现在拿着 token 请求用户资源时，如出现，客户端应退出本地的用户登录信息，引导用户重新登录。
        /// </summary>
        public const string ERROR_ACCESS_DENIED = "access_denied";

        /// <summary>
        /// 401 认证内容无效 grant_type 为 password 的模式下，用户名或密码错误。
        /// </summary>
        public const string ERROR_INVALID_CREDENTIALS = "invalid_credentials";

        /// <summary>
        /// 400 Device Flow 中，用户还没有对 Device Code 完成授权操作，按 interval 要求频率继续轮询，直到 expires_in 过期。
        /// </summary>
        public const string ERROR_AUTHORIZATION_PENDING = "authorization_pending";

        /// <summary>
        /// 服务端业务异常，如：防沉迷 token 失效（code=20000）。
        /// </summary>
        public const string ERROR_BUSINESS_ERROR = "business_code_error";
    }
}
