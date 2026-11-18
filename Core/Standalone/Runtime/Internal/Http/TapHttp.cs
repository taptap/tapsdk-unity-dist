using System.Net.Http;

namespace TapSDK.Core.Standalone.Internal.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TapSDK.Core.Internal.Log;
    using UnityEngine;

    public class TapHttp
    {

        private static readonly string TAG = "Http";

        private static readonly int MAX_GET_RETRY_COUNT = 3;

        internal static readonly long CONNECT_TIMEOUT_MILLIS = 10 * 1000L;
        internal static readonly long READ_TIMEOUT_MILLIS = 5 * 1000L;
        internal static readonly long WRITE_TIMEOUT_MILLIS = 5 * 1000L;

        /// <summary>
        /// 当前 region 生效的通用 API 域名。跨程序集的调用方（如 Achievement 的连通性探测）
        /// 应该用这个，而不是自己在 HOST_CN / HOST_IO 之间挑一个——真正解析 region 的
        /// TapSDKHost 是 internal，别的程序集看不到它。
        /// </summary>
        public static string ApiHost => TapSDKHost.GetApiHost();

        // 保持 public static 字段原样，不改成属性：本 PR 面向补丁分支，把字段改成属性会破坏
        // 公开 CLR 成员——已编译的 dll 抛 MissingFieldException，反射读写也一并失效。
        //
        // 它们的语义是"CN / IO 区的默认域名常量"，是类型初始化时的快照，<b>不反映</b>当前
        // region，也不保证反映 RND 覆写（覆写发生在本类型静态初始化之前才会被带上）。
        // 需要"当前实际生效的域名"请一律用上面的 ApiHost —— 本次修复前成就模块的连通性探测
        // 读的就是这里，结果在海外包里去解析了 CN 域名。
        public static readonly string HOST_CN = TapSDKHost.HOST_CN;
        public static readonly string HOST_IO = TapSDKHost.HOST_IO;

        private static HttpClient client = GetHttpClient();

        private readonly TapHttpConfig httpConfig;

        private readonly TapLog log = new TapLog(TAG);

        private TapHttp() { }

        internal TapHttp(TapHttpConfig httpConfig)
        {
            this.httpConfig = httpConfig;
        }

        public static TapHttpBuilder NewBuilder(string moduleName, string moduleVersion)
        {
            return new TapHttpBuilder(moduleName, moduleVersion);
        }

        private static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler { UseCookies = false };
            HttpClient client = new HttpClient(handler){
                // 默认超时 10 秒
                Timeout = TimeSpan.FromMilliseconds(CONNECT_TIMEOUT_MILLIS)
            };
            return client;
        }

        public async void PostJson<T>(
            string url,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, object> json = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null,
            Action<T> onSuccess = null,
            Action<AbsTapHttpException> onFailure = null)
        {
            TapHttpResult<T> tapHttpResult = await PostJsonAsync<T>(
                path: url,
                headers: headers,
                queryParams: queryParams,
                json: json,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy
            );
            if (tapHttpResult.IsSuccess)
            {
                onSuccess?.Invoke(tapHttpResult.Data);
            }
            else
            {
                onFailure?.Invoke(tapHttpResult.HttpException);
            }
        }

        public async void PostForm<T>(
            string url,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> form = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null,
            Action<T> onSuccess = null,
            Action<AbsTapHttpException> onFailure = null)
        {
            TapHttpResult<T> tapHttpResult = await PostFormAsync<T>(
                url: url,
                headers: headers,
                queryParams: queryParams,
                form: form,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy
            );
            if (tapHttpResult.IsSuccess)
            {
                onSuccess?.Invoke(tapHttpResult.Data);
            }
            else
            {
                onFailure?.Invoke(tapHttpResult.HttpException);
            }
        }

        public async void Get<T>(
            string url,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null,
            Action<T> onSuccess = null,
            Action<Exception> onFailure = null
        )
        {
            TapHttpResult<T> tapHttpResult = await GetAsync<T>(
                url: url,
                headers: headers,
                queryParams: queryParams,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy
           );
            if (tapHttpResult.IsSuccess)
            {
                onSuccess?.Invoke(tapHttpResult.Data);
            }
            else
            {
                onFailure?.Invoke(tapHttpResult.HttpException);
            }
        }

        public async Task<TapHttpResult<T>> GetAsync<T>(
            string url,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null
        )
        {
            if (retryStrategy == null)
            {
                retryStrategy = TapHttpRetryStrategy.CreateDefault(TapHttpBackoffStrategy.CreateFixed(MAX_GET_RETRY_COUNT));
            }
            return await Request<T>(
                url: url,
                method: HttpMethod.Get,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy,
                headers: headers,
                queryParams: queryParams
            );
        }

        public async Task<TapHttpResult<T>> PostJsonAsync<T>(
            string path,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            object json = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null
        )
        {
            if (retryStrategy == null)
            {
                retryStrategy = TapHttpRetryStrategy.CreateDefault(TapHttpBackoffStrategy.CreateNone());
            }
            string jsonStr = null;
            if (json != null)
            {
                jsonStr = await Task.Run(() => JsonConvert.SerializeObject(json));
            }
            return await Request<T>(
                url: path,
                method: HttpMethod.Post,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy,
                headers: headers,
                queryParams: queryParams,
                body: jsonStr
            );
        }

        public async Task<TapHttpResult<T>> PostFormAsync<T>(
            string url,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> form = null,
            bool enableAuthorization = false,
            ITapHttpRetryStrategy retryStrategy = null
        )
        {
            if (retryStrategy == null)
            {
                retryStrategy = TapHttpRetryStrategy.CreateDefault(TapHttpBackoffStrategy.CreateNone());
            }
            return await Request<T>(
                url: url,
                method: HttpMethod.Post,
                enableAuthorization: enableAuthorization,
                retryStrategy: retryStrategy,
                headers: headers,
                queryParams: queryParams,
                body: form
            );
        }

        private async Task<TapHttpResult<T>> Request<T>(
            string url,
            HttpMethod method,
            bool enableAuthorization,
            ITapHttpRetryStrategy retryStrategy,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            object body = null
        )
        {
            TapHttpResult<T> tapHttpResult;
            long nextRetryMillis;
            do
            {
                tapHttpResult = await RequestInner<T>(url, method, enableAuthorization, headers, queryParams, body);

                if (tapHttpResult.IsSuccess)
                {
                    return tapHttpResult;
                }

                nextRetryMillis = retryStrategy.NextRetryMillis(tapHttpResult.HttpException);
                if (nextRetryMillis > 0)
                {
                    log.Log($"Request failed, retry after {nextRetryMillis} ms");
                    await Task.Delay(TimeSpan.FromMilliseconds(nextRetryMillis));
                }

            } while (nextRetryMillis >= 0L);

            return tapHttpResult;
        }

        private async Task<TapHttpResult<T>> RequestInner<T>(
            string path,
            HttpMethod method,
            bool enableAuthorization,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> queryParams = null,
            object body = null
        )
        {
            if(!CheckNetworkConnection()){
                return TapHttpResult<T>.NetworkError(new TapHttpNetworkErrorException("network error"));
            }else{
                TapLog.Log("network interface is available");
            }
            // 处理查询参数 
            Dictionary<string, string> allQueryParams = new Dictionary<string, string>();
            if (queryParams != null)
            {
                foreach (var param in queryParams)
                {
                    allQueryParams[param.Key] = param.Value;
                }
            }
            var fixedQueryParams = httpConfig.Sign.GetFixedQueryParams();
            if (fixedQueryParams != null)
            {
                foreach (var param in fixedQueryParams)
                {
                    allQueryParams[param.Key] = param.Value;
                }
            }
            string host = httpConfig.Domain != null ? httpConfig.Domain : TapSDKHost.GetApiHost();
            // 拼接查询参数
            UriBuilder uriBuilder;
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                uriBuilder = new UriBuilder(path);
            }
            else
            {
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }
                uriBuilder = new UriBuilder(uri: $"{host}{path}");
            }

            if (allQueryParams.Count > 0)
            {
                var queryBuilder = new StringBuilder();
                foreach (var param in allQueryParams)
                {
                    // 使用 Uri.EscapeDataString 来编码参数的键和值
                    queryBuilder.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}&");
                }
                // 移除末尾的 '&'
                queryBuilder.Length -= 1;

                uriBuilder.Query = queryBuilder.ToString();
            }

            var requestUri = uriBuilder.Uri;

            // 创建 HttpRequestMessage
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = requestUri
            };

            // 处理请求头
            Dictionary<string, string> allHeaders = new Dictionary<string, string>();
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    allHeaders[header.Key] = header.Value;
                }
            }
            Dictionary<string, string> fixedHeaders = httpConfig.Sign.GetFixedHeaders(requestUri.ToString(), method, httpConfig.ModuleName, httpConfig.ModuleVersion, enableAuthorization);
            if (fixedHeaders != null)
            {
                foreach (var header in fixedHeaders)
                {
                    allHeaders[header.Key] = header.Value;
                }
            }
            // 添加请求头
            if (allHeaders != null)
            {
                foreach (var header in allHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }
            }

            // 根据请求类型设置请求体
            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                if (body != null)
                {
                    if (body is string jsonBody) // 处理 JSON 数据
                    {
                        StringContent requestContent = new StringContent(jsonBody);
                        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        request.Content = requestContent;
                    }
                    else if (body is Dictionary<string, string> formData) // 处理 Form 数据
                    {
                        request.Content = new FormUrlEncodedContent(formData);
                    }
                }
            }
            // 签算
            httpConfig.Sign.Sign(request);
            try
            {
                if (TapTapSDK.taptapSdkOptions.enableLog)
                {
                    TapHttpUtils.PrintRequest(client, request);
                }
                // 发送请求
                HttpResponseMessage response = await client.SendAsync(request);
                if (TapTapSDK.taptapSdkOptions.enableLog)
                {
                    TapHttpUtils.PrintResponse(response);
                }
                // 解析响应
                return await httpConfig.Parser.Parse<T>(response);
            }
            catch (Exception ex)
            {
                // 捕获并处理请求异常
                return TapHttpResult<T>.UnknownFailure(new TapHttpUnknownException(ex));
            }
        }

        // 判断网络连接状态
        // Application.internetReachability 只能在主线程调用；非主线程（如 Timer 回调）调用会抛 UnityException。
        // 捕获异常时 fallback 为"已连接"，让后续 HTTP 请求自然失败并走正常错误处理路径。
        private bool CheckNetworkConnection()
        {
            try
            {
                // 判断当前设备是否存在可用的网络接口，不保证目标服务或公网可达
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                // 检查能力不可用时，不让预检查阻断真实请求
                log.Log($"Check network connection failed, continue request. {ex.Message}");
                return true;
            }
        }
    }
}