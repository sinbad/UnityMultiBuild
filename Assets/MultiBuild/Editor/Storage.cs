using System.IO;
using UnityEditor;
using UnityEngine;

namespace MultiBuild {

    public static class Storage {

        static string SettingsFilePath = "Assets/MultiBuild/MultiBuildSettings.asset";

        /// <summary>
        /// Try to load saved settings
        /// </summary>
        /// <returns>Settings instance if present, or null if not</returns>
        public static Settings LoadSettings() {
            return AssetDatabase.LoadAssetAtPath(SettingsFilePath, typeof(Settings)) as Settings;
        }

        /// <summary>
        /// Try to load settings, and if they do not exist, create new instance
        /// </summary>
        /// <returns>Loaded settings, or new instance. From this point will be saved directly.</returns>
        public static Settings LoadOrCreateSettings() {

            // try to load first
            Settings s = LoadSettings();

            if (s == null) {
                // Create new
                s = ScriptableObject.CreateInstance<Settings>();
                s.Reset();
                // Should not save during play, probably won't happen but check
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    EditorApplication.delayCall += () => CreateNewSettingsAsset(s);
                } else {
                    CreateNewSettingsAsset(s);
                }
            }
            return s;
        }


        private static void CreateNewSettingsAsset(Settings s) {
            string f = SettingsFilePath;
            string dir = Path.GetDirectoryName(f);
            if(!Directory.Exists(dir)){
                Directory.CreateDirectory(dir);
            }
            AssetDatabase.CreateAsset(s, f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // After this the settings asset is saved with other assets
        }
    }
}