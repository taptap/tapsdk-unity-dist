using System;
using System.Net;

namespace TapSDK.Core.Standalone.Internal.Http
{
    public abstract class AbsTapHttpException : Exception
    {
        public AbsTapHttpException(string message) : base(message)
        {
        }

        protected AbsTapHttpException(string message, Exception e) : base(message, e)
        {
        }
    }

    public class TapHttpUnknownException : AbsTapHttpException
    {
        public TapHttpUnknownException(Exception e) : base("Unknown error", e)
        {
        }
    }

    public class TapHttpNetworkErrorException : AbsTapHttpException
    {
        public TapHttpNetworkErrorException(string msg) : base("network error")
        {
            
        }
    }

    public class TapHttpInvalidResponseException : AbsTapHttpException
    {
        public HttpStatusCode StatusCode { get; }

        public TapHttpInvalidResponseException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// 表示 TapSDK 中与 Http 相关的服务端返回的错误信息。
    /// </summary>
    public class TapHttpServerException : AbsTapHttpException
    {
        /// <summary>
        ///  获取服务器返回的 HTTP 状态码。
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// 获取服务器返回的 Response。
        /// </summary>
        public TapHttpResponse TapHttpResponse { get; }

        /// <summary>
        /// 获取服务器返回的 Response Error Data。
        /// </summary>
        public TapHttpErrorData ErrorData { get; }

        /// <summary>
        /// 初始化 <see cref="TapHttpServerException"/> 类的新实例。
        /// </summary>
        /// <param name="statusCode">服务器返回的 HTTP 状态码。</param>
        /// <param name="code">与异常相关的自定义错误代码。</param>
        /// <param name="msg">错误消息。</param>
        /// <param name="error">服务器返回的错误标识符。</param>
        /// <param name="errorDescription">错误的详细描述。</param>
        public TapHttpServerException(HttpStatusCode statusCode, TapHttpResponse tapHttpResponse, TapHttpErrorData tapHttpErrorData)
            : base(tapHttpErrorData.Msg)
        {
            StatusCode = statusCode;
            TapHttpResponse = tapHttpResponse ?? throw new ArgumentNullException(nameof(tapHttpResponse));
            ErrorData = tapHttpErrorData ?? throw new ArgumentNullException(nameof(tapHttpErrorData));
        }
    }
}