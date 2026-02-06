using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.OnlineBattle
{
    /// <summary>
    /// 房间配置
    /// </summary>
    public class RoomConfig
    {
        // 房间最大支持人数，取值范围为[1, 20]，必填
        [JsonProperty]
        public int maxPlayerCount = 0;

        // 房间类型，最大32字节，必填
        [JsonProperty]
        public string type = "";

        // 房间名称，最大64个字节，选填，当匹配房间时，不需要该参数
        [JsonProperty]
        public string name = "";

        // 自定义房间属性，最大2048字节，选填
        [JsonProperty]
        public string customProperties = "";

        // 房间匹配参数，选填，最大支持3个K/V对, K/V取值由开发者决定，K最大32字节，V最大64字节
        [JsonProperty]
        public Dictionary<string, string> matchParams = null;
    }

    /// <summary>
    /// 玩家配置
    /// </summary>
    public class PlayerConfig
    {
        // 自定义玩家状态，整形，选填。开发者可随意设置任何值，意义由开发者自行判断
        [JsonProperty]
        public int customStatus = 0;

        // 自定义玩家属性，选填。开发者可以随意设置任何数据，最大长度为2048字节
        [JsonProperty]
        public string customProperties = "";
    }

    /// <summary>
    /// 创建房间请求配置类
    /// </summary>
    public class CreateRoomConfig
    {
        // 房间配置，必填
        [JsonProperty("roomCfg")]
        public RoomConfig roomConfig = null;

        // 玩家配置，选填
        [JsonProperty("playerCfg")]
        public PlayerConfig playerConfig = null;
    }

    /// <summary>
    /// 匹配房间请求配置类
    /// </summary>
    public class MatchRoomConfig
    {
        // 房间配置，必填
        [JsonProperty("roomCfg")]
        public RoomConfig roomConfig = null;

        // 玩家配置，选填
        [JsonProperty("playerCfg")]
        public PlayerConfig playerConfig = null;
    }
}
