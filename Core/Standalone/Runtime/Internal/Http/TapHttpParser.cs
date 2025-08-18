using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TapSDK.Core.Standalone.Internal.Http
{
    /// <summary>
    /// 表示一个 HTTP 解析器的接口。
    /// Represents an HTTP parser interface.
    /// </summary>
    public interface ITapHttpParser
    {
        /// <summary>
        /// 解析 HTTP 响应。
        /// Parses the HTTP response.
        /// </summary>
        /// <typeparam name="T">解析后返回的对象类型。The type of the object to return after parsing.</typeparam>
        /// <param name="response">HTTP 响应消息。The HTTP response message.</param>
        /// <returns>解析结果。The parsing result.</returns>
        Task<TapHttpResult<T>> Parse<T>(HttpResponseMessage response);
    }

    /// <summary>
    /// 提供 HTTP 解析功能的类。
    /// Class that provides HTTP parsing functionality.
    /// </summary>
    public class TapHttpParser
    {
        /// <summary>
        /// 创建默认的 HTTP 解析器。
        /// Creates a default HTTP parser.
        /// </summary>
        /// <returns>返回一个实现了 ITapHttpParser 接口的解析器。Returns a parser that implements the ITapHttpParser interface.</returns>
        public static ITapHttpParser CreateDefaultParser()
        {
            return new Default();
        }

        public static ITapHttpParser CreateEventParser()
        {
            return new Event();
        }

        private class Event : ITapHttpParser
        {
            public Task<TapHttpResult<T>> Parse<T>(HttpResponseMessage response)
            {
                _ = response ?? throw new ArgumentNullException(nameof(response));
                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.MultipleChoices)
                {
                    return Task.FromResult(TapHttpResult<T>.Success(default));
                }
                else
                {
                    return Task.FromResult(TapHttpResult<T>.UnknownFailure(default));
                }
            }

        }

        private class Default : ITapHttpParser
        {
            /// <summary>
            /// 解析 HTTP 响应。
            /// Parses the HTTP response.
            /// </summary>
            /// <typeparam name="T">解析后返回的对象类型。The type of the object to return after parsing.</typeparam>
            /// <param name="response">HTTP 响应消息。The HTTP response message.</param>
            /// <returns>解析结果。The parsing result.</returns>
            public async Task<TapHttpResult<T>> Parse<T>(HttpResponseMessage response)
            {
                _ = response ?? throw new ArgumentNullException(nameof(response));
                HttpStatusCode statusCode = response.StatusCode;
                // 读取响应内容
                // Read the response content
                string content = await response.Content.ReadAsStringAsync();

                // 处理响应
                // Handle the response
                TapHttpResponse httpResponse;
                try
                {
                    httpResponse = JsonConvert.DeserializeObject<TapHttpResponse>(content);
                    // 设置当前服务端返回的事件戳
                    if (httpResponse.Now > 0){
                        TapHttpTime.ResetLastServerTime(httpResponse.Now);
                    }
                }
                catch (Exception)
                {
                    return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "Failed to parse TapHttpResponse"));
                }
                if (httpResponse == null)
                {
                    return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "TapHttpResponse is null"));
                }

                if (httpResponse.Success)
                {
                    // 修正时间
                    // Fix the time
                    TapHttpTime.FixTime(httpResponse.Now);
                    if (httpResponse.Data == null)
                    {
                        return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "TapHttpResponse.Data is null"));
                    }
                    try
                    {
                        T data = httpResponse.Data.ToObject<T>();
                        if (data == null)
                        {
                            return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "TapHttpResponse.Data is null"));
                        }
                        return TapHttpResult<T>.Success(data);
                    }
                    catch (Exception)
                    {
                        return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "Failed to parse TapHttpResponse.Data"));
                    }
                }
                else
                {
                    TapHttpErrorData httpErrorData = httpResponse.Data?.ToObject<TapHttpErrorData>();
                    if (httpErrorData == null)
                    {
                        return TapHttpResult<T>.InvalidResponseFailure(new TapHttpInvalidResponseException(statusCode, "TapHttpErrorData is null"));
                    }
                    return TapHttpResult<T>.ServerFailure(new TapHttpServerException((HttpStatusCode)statusCode, httpResponse, httpErrorData));
                }
            }
        }
    }
}
