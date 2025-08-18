using System;
using System.Net;

namespace TapSDK.Core.Standalone.Internal.Http
{
    /// <summary>
    /// 表示 HTTP 错误类型的枚举。
    /// </summary>

    /// <summary>
    /// 表示 TapSDK 中 HTTP 请求的结果。
    /// </summary>
    public class TapHttpResult<T>
    {
        /// <summary>
        /// 指示请求是否成功。
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// HTTP 请求的响应内容。
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// 错误类型，区分网络错误、客户端错误、服务器错误等。
        /// </summary>
        public AbsTapHttpException HttpException { get; private set; }

        /// <summary>
        /// 私有构造函数，防止直接实例化。
        /// </summary>
        private TapHttpResult()
        {
        }

        /// <summary>
        /// 创建一个成功的 HTTP 请求结果。
        /// </summary>
        /// <param name="response">HTTP 响应的内容。</param>
        /// <returns>TapHttpResult 对象，表示成功的请求。</returns>
        public static TapHttpResult<T> Success(T data)
        {
            return new TapHttpResult<T>
            {
                IsSuccess = true,
                Data = data,
            };
        }

        /// <summary>
        /// 创建一个失败的 HTTP 请求结果，通常是网络错误或客户端错误。
        /// </summary>
        /// <param name="errorType">错误类型，例如网络错误或客户端错误。</param>
        /// <param name="exception">异常对象，用于传递错误详情（可选）。</param>
        /// <returns>TapHttpResult 对象，表示失败的请求。</returns>
        public static TapHttpResult<T> InvalidResponseFailure(TapHttpInvalidResponseException exception)
        {
            return new TapHttpResult<T>
            {
                IsSuccess = false,
                HttpException = exception
            };
        }

        /// <summary>
        /// 创建一个服务端返回的错误结果。
        /// </summary>
        /// <param name="httpException">包含详细服务端错误信息的异常对象。</param>
        /// <returns>TapHttpResult 对象，表示服务端错误的请求。</returns>
        public static TapHttpResult<T> ServerFailure(TapHttpServerException httpException)
        {
            return new TapHttpResult<T>
            {
                IsSuccess = false,
                HttpException = httpException,
            };
        }

        public static TapHttpResult<T> UnknownFailure(TapHttpUnknownException httpException)
        {
            return new TapHttpResult<T>
            {
                IsSuccess = false,
                HttpException = httpException,
            };
        }

        public static TapHttpResult<T> NetworkError(TapHttpNetworkErrorException httpException)
        {
            return new TapHttpResult<T>
            {
                IsSuccess = false,
                HttpException = httpException,
            };
        }

    }
}
