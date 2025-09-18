using System;
using System.Collections.Generic;

namespace TapSDK.RelationLite
{
    public class RelationLiteUserResult
    {
        public List<RelationLiteUserItem> list;

        public string nextPageToken;

        public RelationLiteUserResult(List<RelationLiteUserItem> data, string token)
        {
            this.list = data;
            this.nextPageToken = token;
        }
    }
}