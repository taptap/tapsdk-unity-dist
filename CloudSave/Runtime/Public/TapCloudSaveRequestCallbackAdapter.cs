using System.Collections.Generic;

namespace TapSDK.CloudSave
{
    public abstract class TapCloudSaveRequestCallbackAdapter : ITapCloudSaveRequestCallback
    {
        public void OnArchiveCreated(ArchiveData archive)
        {
        }

        public void OnArchiveUpdated(ArchiveData archive)
        {
        }

        public void OnArchiveDeleted(ArchiveData archive)
        {
        }

        public void OnArchiveListResult(List<ArchiveData> archiveList)
        {
        }

        public void OnArchiveDataResult(byte[] archiveData)
        {
        }

        public void OnArchiveCoverResult(byte[] coverData)
        {
        }

        public void OnRequestError(int errorCode, string errorMessage)
        {
        }
    }
}