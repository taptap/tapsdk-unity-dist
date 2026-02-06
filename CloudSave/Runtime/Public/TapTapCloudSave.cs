using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapSDK.CloudSave.Internal;

namespace TapSDK.CloudSave
{
    public class TapTapCloudSave
    {
        public static readonly string Version = "4.10.0-beta.4";

        public static void RegisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            TapTapCloudSaveInternal.RegisterCloudSaveCallback(callback);
        }

        public static void UnregisterCloudSaveCallback(ITapCloudSaveCallback callback)
        {
            TapTapCloudSaveInternal.UnregisterCloudSaveCallback(callback);
        }

        public static Task<ArchiveData> CreateArchive(ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath) =>
            TapTapCloudSaveInternal.CreateArchive(metadata, archiveFilePath, archiveCoverPath);

        public static Task<ArchiveData> UpdateArchive(string archiveUuid, ArchiveMetadata metadata, string archiveFilePath, string archiveCoverPath) =>
            TapTapCloudSaveInternal.UpdateArchive(archiveUuid, metadata, archiveFilePath, archiveCoverPath);

        public static Task<ArchiveData> DeleteArchive(string archiveUuid) =>
            TapTapCloudSaveInternal.DeleteArchive(archiveUuid);

        public static Task<List<ArchiveData>> GetArchiveList() =>
            TapTapCloudSaveInternal.GetArchiveList();

        public static Task<byte[]> GetArchiveData(string archiveUuid, string archiveFileId) =>
            TapTapCloudSaveInternal.GetArchiveData(archiveUuid, archiveFileId);

        public static Task<byte[]> GetArchiveCover(string archiveUuid, string archiveFileId) =>
            TapTapCloudSaveInternal.GetArchiveCover(archiveUuid, archiveFileId);
    }
}