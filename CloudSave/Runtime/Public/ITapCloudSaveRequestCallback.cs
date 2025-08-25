using System.Collections.Generic;

namespace TapSDK.CloudSave
{
    public interface ITapCloudSaveRequestCallback
    {
        void OnArchiveCreated(ArchiveData archive);
        void OnArchiveUpdated(ArchiveData archive);
        void OnArchiveDeleted(ArchiveData archive);
        void OnArchiveListResult(List<ArchiveData> archiveList);
        void OnArchiveDataResult(byte[] archiveData);
        void OnArchiveCoverResult(byte[] coverData);
        void OnRequestError(int errorCode, string errorMessage);
    }
}