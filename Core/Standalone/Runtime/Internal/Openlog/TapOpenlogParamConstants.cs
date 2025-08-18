namespace TapSDK.Core.Standalone.Internal.Openlog
{
    internal class TapOpenlogParamConstants
    {
        public const string PARAM_ACTION = "action";
        // 该条日志的唯一标识 d
        public const string PARAM_T_LOG_ID = "t_log_id";

        // 客户端时区，eg：Asia/Shanghai d
        public const string PARAM_TIMEZONE = "timezone";

        // 客户端生成的时间戳，毫秒级 d
        public const string PARAM_TIMESTAMP = "timestamp";

        // 应用包名 g
        public const string PARAM_APP_PACKAGE_NAME = "app_package_name";

        // 应用版本字符串 g
        public const string PARAM_APP_VERSION = "app_version";

        // 应用版本（数字） g
        public const string PARAM_APP_VERSION_CODE = "app_version_code";

        // 固定一个枚举值: TapSDK g
        public const string PARAM_PN = "pn";

        // SDK接入项目具体模块枚举值 d
        public const string PARAM_TAPSDK_PROJECT = "tapsdk_project";

        // SDK 模块版本号 d
        public const string PARAM_TAPSDK_VERSION = "tapsdk_version";

        // SDK 产物类型 d
        public const string PARAM_TAPSDK_ARTIFACT = "tapsdk_artifact";

        // SDK 运行平台 g
        public const string PARAM_PLATFORM = "platform";

        // SDK设置的地区，例如 zh_CN d
        public const string PARAM_SDK_LOCALE = "sdk_locale";

        // 游戏账号 ID（非角色 ID）d
        public const string PARAM_GAME_USER_ID = "game_user_id";

        // SDK生成的设备全局唯一标识 d
        public const string PARAM_GID = "gid";

        // SDK生成的设备唯一标识 d
        public const string PARAM_DEVICE_ID = "device_id";

        // SDK生成的设备一次安装的唯一标识 d
        public const string PARAM_INSTALL_UUID = "install_uuid";

        // 设备品牌，eg: Xiaomi d
        public const string PARAM_DV = "dv";

        // 设备品牌型号，eg：21051182C d
        public const string PARAM_MD = "md";

        // 设备CPU型号，eg：arm64-v8a d
        public const string PARAM_CPU = "cpu";

        // 支持 CPU 架构，eg：arm64-v8a d
        public const string PARAM_CPU_ABIS = "cpu_abis";

        // 设备操作系统 d
        public const string PARAM_OS = "os";

        // 设备操作系统版本 d
        public const string PARAM_SV = "sv";

        // 物理设备真实屏幕分辨率宽 g
        public const string PARAM_WIDTH = "width";

        // 物理设备真实屏幕分辨率高 g
        public const string PARAM_HEIGHT = "height";

        // 设备可用存储空间（磁盘），单位B d
        public const string PARAM_ROM = "rom";

        // 设备可用内存，单位B d
        public const string PARAM_RAM = "ram";

        // 设备总存储空间（磁盘），单位B g
        public const string PARAM_TOTAL_ROM = "total_rom";

        // 设备总内存，单位B g
        public const string PARAM_TOTAL_RAM = "total_ram";

        // 芯片型号，eg：Qualcomm Technologies, Inc SM7250 g
        public const string PARAM_HARDWARE = "hardware";

        // taptap的用户ID的外显ID（加密）d
        public const string PARAM_OPEN_ID = "open_id";

        // 网络类型，eg：wifi, mobile
        public const string PARAM_NETWORK_TYPE = "network_type";

        // SDK进程粒度的本地日志 session_id
        public const string PARAM_P_SESSION_ID = "p_session_id";
    }
}
