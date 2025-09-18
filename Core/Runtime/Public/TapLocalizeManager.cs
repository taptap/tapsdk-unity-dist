using UnityEngine;

namespace TapSDK.Core
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

        private TapTapLanguageType _language = TapTapLanguageType.Auto;
        private bool _regionIsCn;

        public static void SetCurrentRegion(bool isCn)
        {
            Instance._regionIsCn = isCn;
        }

        public static void SetCurrentLanguage(TapTapLanguageType language)
        {
            Instance._language = language;
        }

        public static TapTapLanguageType GetCurrentLanguage()
        {
            if (Instance._language != TapTapLanguageType.Auto) return Instance._language;
            Instance._language = GetSystemLanguage();
            if (Instance._language == TapTapLanguageType.Auto)
            {
                Instance._language = Instance._regionIsCn ? TapTapLanguageType.zh_Hans : TapTapLanguageType.en;
            }

            return Instance._language;
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
                default:
                    return null;
            }
        }

        public static string GetCurrentLanguageString2() {
            TapTapLanguageType lang = GetCurrentLanguage();
            switch (lang) {
                case TapTapLanguageType.zh_Hans:
                    return "zh-CN";
                case TapTapLanguageType.en:
                    return "en-US";
                case TapTapLanguageType.zh_Hant:
                    return "zh-TW";
                case TapTapLanguageType.ja:
                    return "ja-JP";
                case TapTapLanguageType.ko:
                    return "ko-KR";
                case TapTapLanguageType.th:
                    return "th-TH";
                case TapTapLanguageType.id:
                    return "id-ID";
                default:
                    return null;
            }
        }

        private static TapTapLanguageType GetSystemLanguage()
        {
            var lang = TapTapLanguageType.Auto;
            var sysLanguage = Application.systemLanguage;
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
            }

            return lang;
        }
    }
}