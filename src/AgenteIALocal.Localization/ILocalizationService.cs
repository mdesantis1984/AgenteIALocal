// NUEVA INTERFAZ ILocalizationService - ID: 20260123_121800
using System;
using System.Collections.Generic;

namespace AgenteIALocal.Localization
{
    public interface ILocalizationService
    {
        string CurrentLanguageCode { get; }
        bool IsLanguageAvailable(string code);
        IEnumerable<LanguageInfo> GetAvailableLanguages();
        string GetString(string key);
        void SetLanguage(string code);
        event EventHandler LanguageChanged;
    }
}
