using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.CloudSave.Internal
{
    internal static class TapTapCloudSaveInternal
    {
        private static readonly ITapCloudSaveBridge Bridge;

        static TapTapCloudSaveInternal()
        {
            Bridge = BridgeUtils.CreateBridgeImplementation(typeof(ITapCloudSaveBridge), "TapSDK.CloudSave")
                as ITapCloudSaveBridge;
        }

        internal static void Init(TapTapSdkOptions options)
        {
            Bridge?.Init(options);
        }

        internal static void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            Bridge?.RegisterCloudSaveCallback(callback);
        }

        internal static void UnregisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            Bridge?.UnregisterCloudSaveCallback(callback);
        }

        internal static Task<ArchiveData> CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath) =>
            Bridge?.CreateArchive(metadata, archiveFilePath, archiveCoverPath);

        internal static Task<ArchiveData> UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath) =>
            Bridge?.UpdateArchive(archiveUuid, metadata, archiveFilePath, archiveCoverPath);

        internal static Task<ArchiveData> DeleteArchive(string archiveUuid) =>
            Bridge?.DeleteArchive(archiveUuid);

        internal static Task<List<ArchiveData>> GetArchiveList() =>
            Bridge?.GetArchiveList();

        internal static Task<byte[]> GetArchiveData(string archiveUuid, string archiveFileId) =>
            Bridge?.GetArchiveData(archiveUuid, archiveFileId);

        internal static Task<byte[]> GetArchiveCover(string archiveUuid, string archiveFileId) =>
            Bridge?.GetArchiveCover(archiveUuid, archiveFileId);
    }
}