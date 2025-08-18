using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TapSDK.Core.Standalone.Internal.Http;

namespace TapSDK.Compliance.Internal.Http {
    public class ComplianceHttpClient {

        static readonly int INTERNAL_SERVER_ERROR_LIMIT = 3;

        internal readonly string serverUrl;

        private static readonly TapHttp tapHttp = TapHttp
            .NewBuilder("TapCompliance", TapTapCompliance.Version)
            .Build();


        private readonly Dictionary<string, string> additionalHeaders = new Dictionary<string, string>();

        public ComplianceHttpClient(string serverUrl) {
            this.serverUrl = serverUrl;
        }


        public void ChangeAddtionalHeader(string key, string value) {
            if (string.IsNullOrEmpty(key)) {
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                if (additionalHeaders.ContainsKey(key))
                    additionalHeaders.Remove(key);
            }
            else
            {
                additionalHeaders[key] = value;
            }
        }

        public Task<T> Get<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> queryParams = null) {
            return Request<T>(path, HttpMethod.Get, headers, null, queryParams);
        }

        public Task<T> Post<T>(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {
            return Request<T>(path, HttpMethod.Post, headers, data, queryParams);
        }


        async Task<T> Request<T>(string path,
            HttpMethod method,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {

            string apiServer = serverUrl;
            StringBuilder urlSB = new StringBuilder(apiServer.TrimEnd('/'));
            urlSB.Append($"/{path}");
            string url = urlSB.ToString();

            Dictionary<string, string> newHeaders = null;
            if(headers != null){
                newHeaders = new Dictionary<string, string>();
               foreach (KeyValuePair<string, object> kv in headers) {
                    newHeaders.Add(kv.Key, kv.Value.ToString());
                }
            }

            Dictionary<string, string> newQueryParams = null;
            if(queryParams != null){
                newQueryParams = new Dictionary<string, string>();
                foreach (KeyValuePair<string, object> kv in queryParams) {
                    newQueryParams.Add(kv.Key, kv.Value.ToString());
                }
            }

            TapHttpResult<T> result;
            if(method == HttpMethod.Get)
            {
             result =  await tapHttp.GetAsync<T>(url:url, headers:newHeaders, queryParams: newQueryParams);
            } else {
              result = await tapHttp.PostJsonAsync<T>(path:url, headers: newHeaders, queryParams:newQueryParams,
              json: data, retryStrategy: TapHttpRetryStrategy.CreateDefault(TapHttpBackoffStrategy.CreateFixed(INTERNAL_SERVER_ERROR_LIMIT)));
            }
            if (result.IsSuccess){
                return result.Data;
            }else{
                 AbsTapHttpException  exception = result.HttpException;
                 if(exception is TapHttpServerException  serverException) {
                     ComplianceException complianceException;
                    if(serverException.StatusCode >= HttpStatusCode.InternalServerError){
                        complianceException = new ComplianceException((int)serverException.StatusCode, serverException.Message);
                    }else {
                        complianceException = new ComplianceException((int)serverException.StatusCode, serverException.Message) {
                            Error = serverException.ErrorData.Error,
                            Description = serverException.ErrorData.ErrorDescription,
                            Now = serverException.TapHttpResponse.Now,
                            ErrorCode = serverException.ErrorData.Code
                        };
                    }
                    throw complianceException;
                 }else {
                    throw exception;
                 }
                
            }
        }

    }
}
