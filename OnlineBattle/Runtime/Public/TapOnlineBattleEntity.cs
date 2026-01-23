using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.OnlineBattle
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    public class PlayerInfo
    {
        // 玩家ID
        [JsonProperty]
        public string id = "";

        // 玩家状态：0-离线、1-在线
        [JsonProperty]
        public int status = 0;

        // 自定义玩家状态，开发者可随意设置任何值，意义由开发者自行判断
        [JsonProperty]
        public int customStatus = 0;

        // 自定义玩家属性
        [JsonProperty]
        public string customProperties = "";
    }

    /// <summary>
    /// 房间信息，包含房间内玩家信息
    /// </summary>
    public class RoomInfo
    {
        // 房间ID
        [JsonProperty]
        public string id = "";

        // 房间名称
        [JsonProperty]
        public string name = "";

        // 房间类型
        [JsonProperty]
        public string type = "";

        // 房主ID
        [JsonProperty]
        public string ownerId = "";

        // 0-未开始帧同步，1-帧同步中
        [JsonProperty]
        public int status = 0;

        // 自定义房间属性
        [JsonProperty]
        public string customProperties = "";

        // 房间最大支持人数
        [JsonProperty]
        public int maxPlayerCount = 0;

        // 房间内当前玩家列表
        [JsonProperty]
        public List<PlayerInfo> players = null;

        // 房间创建时间，1970年开始的秒数
        [JsonProperty]
        public string createTime = "";
    }

    /// <summary>
    /// Native 返回的房间信息结构
    /// </summary>
    public class RoomInfoResult
    {
        [JsonProperty]
        public RoomInfo roomInfo = null;
    }

    /// <summary>
    /// 房间基本信息，不包含玩家信息
    /// </summary>
    public class RoomBasicInfo
    {
        // 房间ID
        [JsonProperty]
        public string id = "";

        // 房间名称
        [JsonProperty]
        public string name = "";

        // 房间类型
        [JsonProperty]
        public string type = "";

        // 自定义房间属性
        [JsonProperty]
        public string customProperties = "";

        // 房间最大支持人数
        [JsonProperty]
        public int maxPlayerCount = 0;

        // 房间当前人数
        [JsonProperty]
        public int playerCount = 0;

        // 房间创建时间，1970年开始的秒数
        [JsonProperty]
        public string createTime = "";
    }

    /// <summary>
    /// 查询房间返回结果
    /// </summary>
    public class RoomListData
    {
        // // 房间列表
        [JsonProperty]
        public List<RoomBasicInfo> rooms = null;

        // 用于请求下一页的偏移量
        [JsonProperty]
        public int offset = 0;

        // 是否还有更多房间可以拉取
        [JsonProperty]
        public bool hasMore = false;
    }

    /// <summary>
    /// 用户操作信息
    /// </summary>
    public class PlayerInputInfo
    {
        // 玩家ID
        [JsonProperty]
        public string playerId = "";

        // 玩家操作数据，utf8字符串格式
        [JsonProperty]
        public string data = "";

        // 服务器收到该操作数据的时间，1970年开始的毫秒数
        [JsonProperty("serverTms")]
        public string serverTime = "";
    }

    /// <summary>
    /// 帧同步数据
    /// </summary>
    public class FrameData
    {
        public int id = 0;

        public List<PlayerInputInfo> inputs = null;
    }

    /// <summary>
    /// 开始对战时返回的对战信息
    /// </summary>
    public class FrameSyncInfo
    {
        [JsonProperty]
        public RoomInfo roomInfo = null;

        // 帧同步ID，房间内唯一
        [JsonProperty]
        public int frameSyncId = 0;

        // 用于初始化线性同余伪随机数生成器的随机数种子
        [JsonProperty]
        public int seed = 0;

        // 对战开始的服务端时间，1970年开始的毫秒数
        [JsonProperty("serverTms")]
        public string serverTime = "";
    }
}
