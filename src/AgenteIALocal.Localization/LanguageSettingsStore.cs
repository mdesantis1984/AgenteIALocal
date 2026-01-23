// NUEVA CLASE LanguageSettingsStore - ID: 20260123_121915
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

        public LanguageSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                var def = new LanguageSettings { Current = "en-US", AutoDetect = true };
                Save(def);
                return def;
            }

            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            try
            {
                return JsonConvert.DeserializeObject<LanguageSettings>(json) ?? new LanguageSettings { Current = "en-US", AutoDetect = true };
            }
            catch
            {
                // Si no se puede leer, fallback a valores por defecto
                return new LanguageSettings { Current = "en-US", AutoDetect = true };
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
