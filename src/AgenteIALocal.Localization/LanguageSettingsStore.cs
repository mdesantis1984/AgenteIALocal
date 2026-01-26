// NUEVA CLASE LanguageSettingsStore - ID: 20260123_121915
// MODIFICADO - ID: 20260123_210200 - ROLLBACK System.Text.Json → Newtonsoft.Json
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AgenteIALocal.Localization
{
    public class LanguageSettingsStore
    {
        private readonly string _filePath;

        public LanguageSettingsStore(string filePath)
        {
            _filePath = filePath;
        }

        // MODIFICADO METODO Load - ID: 20260125_003900
        // FIX: Current vacío en defaults para forzar detección OS (no hardcodear en-US)
        public LanguageSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                var def = new LanguageSettings { Current = "", AutoDetect = true };
                Save(def);
                return def;
            }

            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            try
            {
                return JsonConvert.DeserializeObject<LanguageSettings>(json) ?? new LanguageSettings { Current = "", AutoDetect = true };
            }
            catch
            {
                // Si no se puede leer, fallback a valores por defecto
                return new LanguageSettings { Current = "", AutoDetect = true };
            }
        }

        public void Save(LanguageSettings settings)
        {
            var tmp = _filePath + ".tmp";
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(tmp, json, Encoding.UTF8);
            File.Copy(tmp, _filePath, true);
            File.Delete(tmp);
        }
    }
}
