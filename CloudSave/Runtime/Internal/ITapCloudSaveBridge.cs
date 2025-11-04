using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapSDK.Core;

namespace TapSDK.CloudSave.Internal
{
    public interface ITapCloudSaveBridge
    {
        void Init(TapTapSdkOptions options);

        void RegisterCloudSaveCallback(ITapCloudSaveCallback callback);

        void UnregisterCloudSaveCallback(ITapCloudSaveCallback callback);

        Task<ArchiveData> CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath);

        Task<ArchiveData> UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath);

        Task<ArchiveData> DeleteArchive(string archiveUuid);
        Task<List<ArchiveData>> GetArchiveList();
        Task<byte[]> GetArchiveData(string archiveUuid, string archiveFileId);
        Task<byte[]> GetArchiveCover(string archiveUuid, string archiveFileId);
    }
}