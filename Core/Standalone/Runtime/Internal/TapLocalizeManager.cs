using System.Threading;
using TapSDK.Core.Internal.Utils;
using UnityEngine;

namespace TapSDK.Core.Standalone
{
    public class TapLocalizeManager
    {
        private static volatile TapLocalizeManager _instance;
        private static readonly object ObjLock = new object();

        public static TapLocalizeManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (ObjLock)
                {
                    if (_instance == null)
                    {
                        _instance = new TapLocalizeManager();
                    }
                }

                return _instance;
            }
        }

        private bool _regionIsCn;

        public static void SetCurrentRegion(bool isCn)
        {
            Instance._regionIsCn = isCn;
        }

        private TapTapLanguageType _language = TapTapLanguageType.Auto;

        public static void SetCurrentLanguage(TapTapLanguageType language)
        {
            Instance._language = language;
        }

        public static TapTapLanguageType GetCurrentLanguage()
        {
            return Instance._language != TapTapLanguageType.Auto ? Instance._language : GetSystemLanguage();
        }

        public static string GetCurrentLanguageString() {
            TapTapLanguageType lang = GetCurrentLanguage();
            switch (lang) {
                case TapTapLanguageType.zh_Hans:
                    return "zh_CN";
                case TapTapLanguageType.en:
                    return "en_US";
                case TapTapLanguageType.zh_Hant:
                    return "zh_TW";
                case TapTapLanguageType.ja:
                    return "ja_JP";
                case TapTapLanguageType.ko:
                    return "ko_KR";
                case TapTapLanguageType.th:
                    return "th_TH";
                case TapTapLanguageType.id:
                    return "id_ID";
                case TapTapLanguageType.de:
                    return "de";
                case TapTapLanguageType.es:
                    return "es_ES";
                case TapTapLanguageType.fr:
                    return "fr";
                case TapTapLanguageType.pt:
                    return "pt_PT";
                case TapTapLanguageType.ru:
                    return "ru";
                case TapTapLanguageType.tr:
                    return "tr";
                case TapTapLanguageType.vi:
                    return "vi_VN";
                default:
                    return Instance._regionIsCn ? "zh_CN" : "en_US";
            }
        }

        public static string GetCurrentLanguageString2() {
            return GetCurrentLanguageString().Replace("_", "-");
        }

        private static TapTapLanguageType GetSystemLanguage()
        {
            var lang = TapTapLanguageType.Auto;
            // Application.systemLanguage 必须在主线程访问，所以这里需要使用 TapLoom 确保调用线程
            var defaultSystemLanguage = Instance._regionIsCn ? SystemLanguage.ChineseSimplified : SystemLanguage.English;
            var sysLanguage = TapLoom.RunOnMainThreadSync(
                () => Application.systemLanguage,
                defaultSystemLanguage
            );
            switch (sysLanguage)
            {
                case SystemLanguage.ChineseSimplified:
                    lang = TapTapLanguageType.zh_Hans;
                    break;
                case SystemLanguage.English:
                    lang = TapTapLanguageType.en;
                    break;
                case SystemLanguage.ChineseTraditional:
                    lang = TapTapLanguageType.zh_Hant;
                    break;
                case SystemLanguage.Japanese:
                    lang = TapTapLanguageType.ja;
                    break;
                case SystemLanguage.Korean:
                    lang = TapTapLanguageType.ko;
                    break;
                case SystemLanguage.Thai:
                    lang = TapTapLanguageType.th;
                    break;
                case SystemLanguage.Indonesian:
                    lang = TapTapLanguageType.id;
                    break;
                default:
                    lang = Instance._regionIsCn ? TapTapLanguageType.zh_Hans : TapTapLanguageType.en;
                    break;
            }

            return lang;
        }
    }
}