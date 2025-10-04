using System;
using System.IO;

namespace MacroMR
{
    public static class LicenseManager
    {
        private static readonly string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarwanMacro"
        );

        private static readonly string activatedFilePath = Path.Combine(appFolder, "activated.dat");
        private static readonly string usedKeysFilePath = Path.Combine(appFolder, "used_keys.txt");
        private static readonly string validKeysFilePath = Path.Combine(appFolder, "valid_keys.txt");

        public static bool IsLicenseValid(string key)
        {
            if (!File.Exists(validKeysFilePath))
                return false;

            string[] validKeys = File.ReadAllLines(validKeysFilePath);
            string[] usedKeys = File.Exists(usedKeysFilePath) ? File.ReadAllLines(usedKeysFilePath) : Array.Empty<string>();

            bool isUsed = Array.Exists(usedKeys, k => k.Trim() == key);
            bool isValid = Array.Exists(validKeys, k => k.Trim() == key);

            return isValid && !isUsed;
        }

        public static void SaveActivation()
        {
            Directory.CreateDirectory(appFolder);
            File.WriteAllText(activatedFilePath, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        public static bool IsAlreadyActivated()
        {
            return File.Exists(activatedFilePath);
        }

        public static void SaveUsedKey(string key)
        {
            Directory.CreateDirectory(appFolder);
            File.AppendAllText(usedKeysFilePath, key + Environment.NewLine);
        }
    }
}