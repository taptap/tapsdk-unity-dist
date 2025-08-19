using System.Collections.Generic;

namespace TapSDK.CloudSave
{
    public interface ITapCloudSaveCallback
    {
        void OnResult(int resultCode);
    }
}