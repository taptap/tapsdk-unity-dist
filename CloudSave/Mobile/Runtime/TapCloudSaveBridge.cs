using System.Collections.Generic;
using TapSDK.Core;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.CloudSave.Internal;
using TapSDK.Core.Internal.Log;

namespace TapSDK.CloudSave.Mobile
{
    public class ErrorResponse
    {
        [JsonProperty("errorCode")] public int ErrorCode { get; set; }
        [JsonProperty("errorMessage")] public string ErrorMessage { get; set; }
    }

    public class TapCloudSaveBridge : ITapCloudSaveBridge
    {
        public static string TAP_CLOUDSAVE_SERVICE = "BridgeCloudSaveService";

        public static string TDS_CLOUDSAVE_SERVICE_CLZ = "com.taptap.sdk.cloudsave.unity.BridgeCloudSaveService";

        public static string TDS_CLOUDSAVE_SERVICE_IMPL = "com.taptap.sdk.cloudsave.unity.BridgeCloudSaveServiceImpl";

        // 全局callback管理
        private readonly List<ITapCloudSaveCallback> callbacks = new List<ITapCloudSaveCallback>();
        private bool hasRegisteredNativeCallback = false;

        public TapCloudSaveBridge()
        {
            EngineBridge.GetInstance().Register(TDS_CLOUDSAVE_SERVICE_CLZ, TDS_CLOUDSAVE_SERVICE_IMPL);
        }

        public void Init(TapTapSdkOptions options)
        {
            // 原生由原生内部实现
        }

        public void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            TapLog.Log("[TapCloudSaveBridge] RegisterCloudSaveCallback called with global callback management");
            
            if (callback != null)
            {
                // 添加到全局callback列表
                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                    TapLog.Log($"[TapCloudSaveBridge] Added callback. Total callbacks: {callbacks.Count}");
                }
                else
                {
                    TapLog.Log("[TapCloudSaveBridge] Callback already registered");
                }

                // 初始化注册原生callback（只注册一次）
                InitRegisterNativeCallback();
            }
        }

        public void UnregisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            TapLog.Log("[TapCloudSaveBridge] UnregisterCloudSaveCallback called");
            
            if (callback != null)
            {
                if (callbacks.Contains(callback))
                {
                    callbacks.Remove(callback);
                    TapLog.Log($"[TapCloudSaveBridge] Removed callback. Remaining callbacks: {callbacks.Count}");
                    
                    // 当没有callback时，取消注册原生callback
                    if (callbacks.Count == 0 && hasRegisteredNativeCallback)
                    {
                        var command = new Command.Builder()
                            .Service(TAP_CLOUDSAVE_SERVICE)
                            .Method("unregisterCloudSaveCallback")
                            .Callback(false)
                            .OnceTime(false);
                        EngineBridge.GetInstance().CallHandler(command.CommandBuilder());
                        hasRegisteredNativeCallback = false;
                        TapLog.Log("[TapCloudSaveBridge] Unregistered native callback");
                    }
                }
                else
                {
                    TapLog.Log("[TapCloudSaveBridge] Callback not found");
                }
            }
        }

        private void InitRegisterNativeCallback()
        {
            if (hasRegisteredNativeCallback)
            {
                return;
            }
            hasRegisteredNativeCallback = true;

            var command = new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("registerCloudSaveCallback")
                .Callback(true)
                .OnceTime(false);
            
            EngineBridge.GetInstance().CallHandler(command.CommandBuilder(), (response) =>
            {
                TapLog.Log($"[TapCloudSaveBridge] Native callback received: code={response.code}, content={response.content}");

                if (callbacks.Count == 0) return;

                try
                {
                    int resultCode = -1;

                    // 修复：始终尝试解析content中的resultCode，不要因为response.code失败就跳过
                    // iOS端会在TapSDKResult.content中传递真实的错误码（如300001 NEED_LOGIN）
                    if (!string.IsNullOrEmpty(response.content))
                    {
                        try
                        {
                            var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                            if (result != null && !string.IsNullOrEmpty(result.content))
                            {
                                // 尝试解析为字典（iOS格式：{"resultCode": 300001}）
                                try
                                {
                                    var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.content);
                                    if (dataDict != null && dataDict.ContainsKey("resultCode"))
                                    {
                                        resultCode = SafeDictionary.GetValue<int>(dataDict, "resultCode", -1);
                                        TapLog.Log($"[TapCloudSaveBridge] Parsed resultCode from dictionary: {resultCode}");
                                    }
                                    else
                                    {
                                        // 尝试直接解析为int（兼容旧格式）
                                        resultCode = JsonConvert.DeserializeObject<int>(result.content);
                                        TapLog.Log($"[TapCloudSaveBridge] Parsed resultCode directly: {resultCode}");
                                    }
                                }
                                catch
                                {
                                    // 如果无法解析为字典，尝试直接解析为int
                                    resultCode = JsonConvert.DeserializeObject<int>(result.content);
                                    TapLog.Log($"[TapCloudSaveBridge] Parsed resultCode as int: {resultCode}");
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            TapLog.Error($"[TapCloudSaveBridge] Failed to parse TapEngineBridgeResult: {parseEx.Message}, using -1");
                            resultCode = -1;
                        }
                    }
                    else
                    {
                        TapLog.Error("[TapCloudSaveBridge] Response content is null or empty, using -1");
                    }

                    TapLog.Log($"[TapCloudSaveBridge] Final resultCode: {resultCode}");

                    // 通知所有已注册的callback
                    foreach (var callback in callbacks)
                    {
                        try
                        {
                            callback?.OnResult(resultCode);
                        }
                        catch (Exception e)
                        {
                            TapLog.Log($"[TapCloudSaveBridge] Error in callback execution: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    TapLog.Log($"[TapCloudSaveBridge] Error processing native callback: {e.Message}");

                    // 即使解析错误，也要通知所有callback
                    foreach (var callback in callbacks)
                    {
                        try
                        {
                            callback?.OnResult(-1);
                        }
                        catch (Exception callbackException)
                        {
                            TapLog.Log($"[TapCloudSaveBridge] Error in callback execution during error handling: {callbackException.Message}");
                        }
                    }
                }
            });
            
            TapLog.Log("[TapCloudSaveBridge] Initialized native callback registration");
        }

        public Task<ArchiveData> CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath)
        {
            TapLog.Log("[TapCloudSaveBridge] CreateArchive called with Task<ArchiveData> return type (NEW API)");
            var taskSource = new TaskCompletionSource<ArchiveData>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("createArchive")
                .Args("archiveMetadata",  JsonConvert.SerializeObject(metadata))
                .Args("archiveFilePath",  archiveFilePath)
                .Args("archiveCoverPath",  archiveCoverPath)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), 
                response =>
            {
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to create archive: code="+response.code + " content="+response.content));
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            taskSource.TrySetResult(archive);
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "Failed to create archive: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to create archive: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to create archive: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }
        
        public Task<ArchiveData> UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath)
        {
            TapLog.Log("[TapCloudSaveBridge] UpdateArchive called with Task<ArchiveData> return type (NEW API)");
            var taskSource = new TaskCompletionSource<ArchiveData>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("updateArchive")
                .Args("archiveUUIDForUpdate",  archiveUuid)
                .Args("archiveMetadataForUpdate",  JsonConvert.SerializeObject(metadata))
                .Args("archiveFilePathForUpdate",  archiveFilePath)
                .Args("archiveCoverPathForUpdate",  archiveCoverPath)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to update archive: code="+response.code + " content="+response.content));
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            taskSource.TrySetResult(archive);
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "Failed to update archive: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to update archive: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to update archive: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }
        
        public Task<ArchiveData> DeleteArchive(string archiveUuid)
        {
            TapLog.Log("[TapCloudSaveBridge] DeleteArchive called with Task<ArchiveData> return type (NEW API)");
            var taskSource = new TaskCompletionSource<ArchiveData>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("deleteArchive")
                .Args("archiveUUID",  archiveUuid)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to delete archive: code="+response.code + " content="+response.content));
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archive = JsonConvert.DeserializeObject<ArchiveData>(result.content);
                        if (archive != null)
                        {
                            taskSource.TrySetResult(archive);
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "Failed to delete archive: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to delete archive: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to delete archive: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }

        public Task<List<ArchiveData>> GetArchiveList()
        {
            TapLog.Log("[TapCloudSaveBridge] 🚀 GetArchiveList called - Starting archive list request");
            TapLog.Log($"[TapCloudSaveBridge] 📋 Service: {TAP_CLOUDSAVE_SERVICE}, Method: getArchiveList");
            
            var taskSource = new TaskCompletionSource<List<ArchiveData>>();
            var command = new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveList")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
                
            TapLog.Log($"[TapCloudSaveBridge] 📤 Sending command to iOS: {command.ToJSON()}");
            
            EngineBridge.GetInstance().CallHandler(command, (response) =>
            {
                TapLog.Log($"[TapCloudSaveBridge] 📥 Received response from iOS - Code: {response.code}, Content length: {response.content?.Length ?? 0}");
                
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        TapLog.Log($"[TapCloudSaveBridge] ❌ Response failed - Code: {response.code}, Content: {response.content}");
                        taskSource.TrySetException(new TapException(-1, "Failed to get archive list: code="+response.code + " content="+response.content));
                        return;
                    }
                    
                    TapLog.Log($"[TapCloudSaveBridge] 🔍 Parsing response content: {response.content.Substring(0, Math.Min(200, response.content.Length))}...");
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        TapLog.Log($"[TapCloudSaveBridge] ✅ Bridge result successful, parsing archive list content: {result.content?.Substring(0, Math.Min(100, result.content?.Length ?? 0))}...");
                        
                        var archiveList = JsonConvert.DeserializeObject<List<ArchiveData>>(result.content);
                        if (archiveList != null)
                        {
                            TapLog.Log($"[TapCloudSaveBridge] 🎉 Successfully parsed {archiveList.Count} archives, completing task");
                            taskSource.TrySetResult(archiveList);
                        }
                        else
                        {
                            TapLog.Log($"[TapCloudSaveBridge] ❌ Failed to deserialize archive list from content: {result.content}");
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        TapLog.Log($"[TapCloudSaveBridge] ❌ Bridge result failed - Code: {result?.code}, Content: {result?.content}");
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                TapLog.Log($"[TapCloudSaveBridge] 📋 Parsed error response - Code: {errorResponse.ErrorCode}, Message: {errorResponse.ErrorMessage}");
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                TapLog.Log("[TapCloudSaveBridge] ❌ Failed to parse error response, raw content: " + response.content);
                                taskSource.TrySetException(new TapException(-1, "Failed to get archive list: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            TapLog.Log($"[TapCloudSaveBridge] ❌ Exception while parsing error response: {e.Message}");
                            taskSource.TrySetException(new TapException(-1, "Failed to get archive list: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    TapLog.Log($"[TapCloudSaveBridge] ❌ Top-level exception while processing response: {e.Message}");
                    taskSource.TrySetException(new TapException(-1, "Failed to get archive list: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }
        
        public Task<byte[]> GetArchiveData(string archiveUuid, string archiveFileId)
        {
            TapLog.Log("[TapCloudSaveBridge] GetArchiveData called with Task<byte[]> return type (NEW API)");
            var taskSource = new TaskCompletionSource<byte[]>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveData")
                .Args("archiveUUID",  archiveUuid)
                .Args("archiveFileID",  archiveFileId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to get archive data: code=" + response.code + " content="+response.content));
                        return;
                    }

                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var archiveData = Convert.FromBase64String(result.content);
                        if (archiveData != null)
                        {
                            taskSource.TrySetResult(archiveData);
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "Failed to get archive data: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to get archive data: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to get archive data: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }
        
        public Task<byte[]> GetArchiveCover(string archiveUuid, string archiveFileId)
        {
            TapLog.Log("[TapCloudSaveBridge] GetArchiveCover called with Task<byte[]> return type (NEW API)");
            var taskSource = new TaskCompletionSource<byte[]>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_CLOUDSAVE_SERVICE)
                .Method("getArchiveCover")
                .Args("archiveUUIDForCover",  archiveUuid)
                .Args("archiveFileIDForCover",  archiveFileId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (response) =>
            {
                try
                {
                    if (response.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(response.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to get archive cover: code="+response.code + " content="+response.content));
                        return;
                    }
                    
                    var result = JsonConvert.DeserializeObject<TapEngineBridgeResult>(response.content);
                    if (result != null && result.code == TapEngineBridgeResult.RESULT_SUCCESS)
                    {
                        var coverData = Convert.FromBase64String(result.content);
                        if (coverData != null)
                        {
                            taskSource.TrySetResult(coverData);
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "json convert failed: content="+result.content));
                        }
                    }
                    else
                    {
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(result.content);
                            if (errorResponse != null)
                            {
                                taskSource.TrySetException(new TapException(errorResponse.ErrorCode, errorResponse.ErrorMessage));
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "Failed to get archive cover: content="+response.content));
                            }
                        }
                        catch (Exception e)
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to get archive cover: content="+response.content));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to get archive cover: error=" + e.Message + ", content=" + response.content));
                }
            });
            return taskSource.Task;
        }
    }
}