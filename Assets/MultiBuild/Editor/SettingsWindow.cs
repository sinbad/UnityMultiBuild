using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/* Easiest way to check if we're running 5.4 or lower. */
#if UNITY_5_5_OR_NEWER
#else
namespace UnityEditor
{
	public struct BuildPlayerOptions
	{
		public string[] scenes { get; set; }
		public string locationPathName { get; set; }
		public string assetBundleManifestPath { get; set; }
		public BuildTargetGroup targetGroup { get; set; }
		public BuildTarget target { get; set; }
		public BuildOptions options { get; set; }
	}
}
#endif

namespace MultiBuild {
    public class SettingsWindow : EditorWindow {

        Settings _settings;
        Settings Settings {
            get {
                if (_settings == null) {
                    _settings = LoadOrCreateSettings();
                }
                return _settings;
            }
        }
        SerializedObject _serializedSettings;
        SerializedObject SerializedSettings {
            get {
                if (_serializedSettings == null) {
                    _serializedSettings = new SerializedObject(Settings);
                }
                return _serializedSettings;
            }
        }

        // Manually format the descriptive names
        // Simpler than DescriptionAttribute style IMO
        Dictionary<Target, string> _targetNames;
        Dictionary<Target, string> TargetNames {
            get {
                if (_targetNames == null) {
                    _targetNames = new Dictionary<Target, string> {
                        {Target.Android, "Android"},
                        {Target.iOS, "iOS"},
                        {Target.Linux32, "Linux 32-bit"},
                        {Target.Linux64, "Linux 64-bit"},
                        {Target.Mac32, "Mac 32-bit"},
                        {Target.Mac64, "Mac 64-bit"},
                        {Target.MacUniversal, "Mac Universal"},
                        {Target.WebGL, "WebGL"},
                        {Target.Windows32, "Windows 32-bit"},
                        {Target.Windows64, "Windows 64-bit"},
                        {Target.WindowsStore, "Windows Store App"},
                        {Target.Tizen, "Tizen"},
                        {Target.PS4, "Playstation 4"},
                        {Target.XboxOne, "Xbox One"},
                        {Target.SamsungTV, "Samsung TV"},
                        {Target.WiiU, "Nintendo WiiU"},
                        {Target.tvOS, "tvOS"},
#if UNITY_5_5_OR_NEWER
                        {Target.Nintendo3DS, "Nintendo 3DS"},
#endif
#if UNITY_5_6_OR_NEWER
                        {Target.Switch, "Nintendo Switch"},
#endif
                    };
                }
                return _targetNames;
            }
        }
        // Because we need to sort and Unity Popup doesn't have a data tag
        Dictionary<string, Target> _targetNameToValue;
        Dictionary<string, Target> TargetNameToValue {
            get {
                if (_targetNameToValue == null) {
                    _targetNameToValue = new Dictionary<string, Target>();
                    foreach (var target in TargetNames.Keys) {
                        _targetNameToValue[TargetNames[target]] = target;
                    }
                }
                return _targetNameToValue;
            }
        }



        Target[] _targets;
        Target[] Targets {
            get {
                if (_targets == null) {
                    _targets = (Target[])Enum.GetValues(typeof(Target));
                }
                return _targets;
            }
        }

        GUIStyle _actionButtonStyle;
        GUIStyle ActionButtonStyle {
            get {
                if (_actionButtonStyle == null) {
                    _actionButtonStyle = new GUIStyle(GUI.skin.button);
                    _actionButtonStyle.fontStyle = FontStyle.Bold;
                    _actionButtonStyle.normal.textColor = Color.white;
                }
                return _actionButtonStyle;
            }
        }
        GUIStyle _labelMarginStyle;
        GUIStyle LabelMarginStyle {
            get {
                if (_labelMarginStyle == null) {
                    _labelMarginStyle = new GUIStyle();
                    _labelMarginStyle.margin.left = GUI.skin.label.margin.left;
                }
                return _labelMarginStyle;
            }
        }
        GUIStyle _removeButtonContainerStyle;
        GUIStyle RemoveButtonContainerStyle {
            get {
                if (_removeButtonContainerStyle == null) {
                    _removeButtonContainerStyle = new GUIStyle();
                    _removeButtonContainerStyle.margin.left = 30;
                }
                return _removeButtonContainerStyle;
            }
        }
        List<string> _targetNamesNotAdded;
        bool _targetsDirty = true;
        int _targetToAddIndex;

        string SettingsFilePath {
            get {
                // Assets always use forward slashes even on Windows
                return "Assets/MultiBuild/MultiBuildSettings.asset";
            }
        }

        [MenuItem ("Tools/MultiBuild...")]
        public static void  ShowWindow () {
            EditorWindow.GetWindow(typeof(SettingsWindow), false, "MultiBuild");
        }

        void OnGUI () {
            if (_targetsDirty) {
                UpdateTargetsNotAdded();
            }

            // Use SerializedObject as a proxy to get dirty flags, undo & asset save
            GUILayout.Label ("Output Settings", EditorStyles.boldLabel);

            // Need to use nested verticals and flexible space to align text & button vertically
            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(GUI.skin.button.CalcHeight(new GUIContent("..."), 30)));
            // Also need a style on label to make it line up on the left edge
            EditorGUILayout.BeginVertical(LabelMarginStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("outputFolder"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false))) {
                var prop = SerializedSettings.FindProperty("outputFolder");
                var fld = EditorUtility.OpenFolderPanel("Pick build folder", prop.stringValue, PlayerSettings.productName);
                if (!string.IsNullOrEmpty(fld)) {
                    prop.stringValue = fld;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("useProductName"));
            if (!Settings.useProductName) {
                EditorGUILayout.PropertyField(SerializedSettings.FindProperty("overrideName"));
            }
            EditorGUILayout.Space();


            GUILayout.Label ("Platforms To Build", EditorStyles.boldLabel);

            bool removeTargetAtEnd = false;
            Target targetToRemove = Target.iOS;
            foreach (var target in Settings.targets) {
                EditorGUILayout.BeginHorizontal(RemoveButtonContainerStyle, GUILayout.MaxHeight(23));
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.MinWidth(30));
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.6f,0, 0,1);
                if (GUILayout.Button(" X ", ActionButtonStyle)) {
                    // Don't do this now, breaks iteration
                    targetToRemove = target;
                    removeTargetAtEnd = true;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(TargetNames[target]);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            if (Settings.targets.Count == 0) {
                GUILayout.Label("No platforms selected! Add one below.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(GUI.skin.button.CalcHeight(new GUIContent("..."), 30)));
            EditorGUILayout.BeginVertical(LabelMarginStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Add platform");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            _targetToAddIndex = EditorGUILayout.Popup(_targetToAddIndex, _targetNamesNotAdded.ToArray());
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add")) {
                // Ugh dealing with arrays in SerializedObject is awful
                string newTargetName = _targetNamesNotAdded[_targetToAddIndex];
                Target newTarget = TargetNameToValue[newTargetName];
                // Insert in order
                var proplist = SerializedSettings.FindProperty("targets");
                int insertIndex;
                for (insertIndex = 0; insertIndex < proplist.arraySize; ++insertIndex) {
                    string name = TargetNames[(Target)proplist.GetArrayElementAtIndex(insertIndex).enumValueIndex];
                    if (string.Compare(newTargetName, name, true) < 0) {
                        break;
                    }
                }
                proplist.arraySize++;
                // Move all existing items forward to make room for insert in order
                for (int i = proplist.arraySize-1; i > insertIndex; --i) {
                    proplist.GetArrayElementAtIndex(i).enumValueIndex =
                        proplist.GetArrayElementAtIndex(i-1).enumValueIndex;
                }
                proplist.GetArrayElementAtIndex(insertIndex).enumValueIndex = (int)newTarget;
                _targetsDirty = true;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Label ("Additional options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("developmentBuild"));

            if (removeTargetAtEnd) {
                int index = Settings.targets.IndexOf(targetToRemove);
                var proplist = SerializedSettings.FindProperty("targets");
                proplist.DeleteArrayElementAtIndex(index);
                _targetsDirty = true;
            }

            // This applies any changes to the underlying asset and marks dirty if needed
            // this is what ensures the asset gets saved
            SerializedSettings.ApplyModifiedProperties();
            if (_targetsDirty) {
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0,0.6f,0,1);
            if (GUILayout.Button("Build Selected Platforms", ActionButtonStyle, GUILayout.MinHeight(30))) {
                // do eet
                Build();
            }
        }

        void UpdateTargetsNotAdded() {
            _targetNamesNotAdded = new List<string>();
            for (int i = 0, n = (int)Target.Max; i < n; ++i) {
                Target target = Targets[i];
                if (!Settings.targets.Contains(target))
                    _targetNamesNotAdded.Add(TargetNames[target]);
            }
            _targetNamesNotAdded.Sort();
            _targetsDirty = false;
        }

        Settings LoadOrCreateSettings() {

            // try to load first
            Settings s = AssetDatabase.LoadAssetAtPath(SettingsFilePath, typeof(Settings)) as Settings;

            if (s == null) {
                // Create new
                s = ScriptableObject.CreateInstance<Settings>();
                s.Reset();
                // Should not save during play, probably won't happen but check
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    EditorApplication.delayCall += () => SaveNewSettingsAsset(s);
                } else {
                    SaveNewSettingsAsset(s);
                }
            }
            return s;
        }

        void SaveNewSettingsAsset(Settings s) {
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

        void Build() {

            var savedTarget = EditorUserBuildSettings.activeBuildTarget;

            var buildSteps = SelectedBuildOptions();
            int i = 1;
            bool cancelled = false;
            foreach (var opts in buildSteps) {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Building platforms...",
                    string.Format("Building {0}...", opts.target.ToString()),
                    (float)(i / (float)buildSteps.Count))) {
                        cancelled = true;
                        break;
                }
#if UNITY_5_5_OR_NEWER
                string err = BuildPipeline.BuildPlayer(opts);
#else
                string err = BuildPipeline.BuildPlayer(
                        opts.scenes,
                        opts.locationPathName,
                        opts.target,
                        opts.options);
#endif
                if (!string.IsNullOrEmpty(err)) {
                    EditorUtility.DisplayDialog("Build error", err, "Close");
                    break;
                }
                ++i;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Building platforms...",
                    string.Format("Building {0} Done", opts.target.ToString()),
                    (float)(i / (float)buildSteps.Count))) {
                        cancelled = true;
                        break;
                }
            }
            if (cancelled) {
                EditorUtility.DisplayDialog("Cancelled", "Build cancelled before finishing.", "Close");
            }

            // Building can change the active target, can cause warnings or odd behaviour
            // Put it back to how it was
            if (EditorUserBuildSettings.activeBuildTarget != savedTarget) {
#if UNITY_5_6_OR_NEWER
                EditorUserBuildSettings.SwitchActiveBuildTarget(GroupForTarget(savedTarget), savedTarget);
#else
                EditorUserBuildSettings.SwitchActiveBuildTarget(savedTarget);
#endif
            }
        }

        BuildTargetGroup GroupForTarget(BuildTarget t) {
            // Can't believe Unity doesn't have a method for this already
            switch (t) {
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSXUniversal:
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return BuildTargetGroup.Standalone;
            case BuildTarget.iOS:
                return BuildTargetGroup.iOS;
            case BuildTarget.Android:
                return BuildTargetGroup.Android;
            case BuildTarget.WebGL:
                return BuildTargetGroup.WebGL;
            case BuildTarget.WSAPlayer:
                return BuildTargetGroup.WSA;
            case BuildTarget.Tizen:
                return BuildTargetGroup.Tizen;
            case BuildTarget.PS4:
                return BuildTargetGroup.PS4;
            case BuildTarget.XboxOne:
                return BuildTargetGroup.XboxOne;
            case BuildTarget.SamsungTV:
                return BuildTargetGroup.SamsungTV;
            case BuildTarget.WiiU:
                return BuildTargetGroup.WiiU;
            case BuildTarget.tvOS:
                return BuildTargetGroup.tvOS;
#if UNITY_5_5_OR_NEWER
            case BuildTarget.N3DS:
                return BuildTargetGroup.N3DS;
#endif
#if UNITY_5_6_OR_NEWER
            case BuildTarget.Switch:
                return BuildTargetGroup.Switch;
#endif
                // TODO more platforms?
            default:
                return BuildTargetGroup.Unknown;
            }
        }

        BuildTarget UnityTarget(Target t) {
            switch (t) {
            case Target.Windows32:
                return BuildTarget.StandaloneWindows;
            case Target.Windows64:
                return BuildTarget.StandaloneWindows64;
            case Target.Mac32:
                return BuildTarget.StandaloneOSXIntel;
            case Target.Mac64:
                return BuildTarget.StandaloneOSXIntel64;
            case Target.MacUniversal:
                return BuildTarget.StandaloneOSXUniversal;
            case Target.Linux32:
                return BuildTarget.StandaloneLinux;
            case Target.Linux64:
                return BuildTarget.StandaloneLinux64;
            case Target.iOS:
                return BuildTarget.iOS;
            case Target.Android:
                return BuildTarget.Android;
            case Target.WebGL:
                return BuildTarget.WebGL;
            case Target.WindowsStore:
                return BuildTarget.WSAPlayer;
            case Target.Tizen:
                return BuildTarget.Tizen;
            case Target.PS4:
                return BuildTarget.PS4;
            case Target.XboxOne:
                return BuildTarget.XboxOne;
            case Target.SamsungTV:
                return BuildTarget.SamsungTV;
            case Target.WiiU:
                return BuildTarget.WiiU;
            case Target.tvOS:
                return BuildTarget.tvOS;
#if UNITY_5_5_OR_NEWER
            case Target.Nintendo3DS:
                return BuildTarget.N3DS;
#endif
#if UNITY_5_6_OR_NEWER
            case Target.Switch:
                return BuildTarget.Switch;
#endif
                // TODO more platforms?
            default:
                throw new NotImplementedException("Target not supported");
            }
        }

        public List<BuildPlayerOptions> SelectedBuildOptions() {
            var ret = new List<BuildPlayerOptions>();
            foreach (var target in Settings.targets) {
                ret.Add(BuildOpts(target));
            }
            return ret;
        }

        public BuildPlayerOptions BuildOpts(Target target) {
            BuildPlayerOptions o = new BuildPlayerOptions();
            // Build all the scenes selected in build settings
            o.scenes = EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();
            string subfolder = TargetNames[target];
            o.locationPathName = Path.Combine(Settings.outputFolder, subfolder);
            // location needs to include the output name too
            if (Settings.useProductName)
                o.locationPathName = Path.Combine(o.locationPathName, PlayerSettings.productName);
            else
                o.locationPathName = Path.Combine(o.locationPathName, Settings.overrideName);
            // Need to append exe in Windows, isn't added by default
            // Weirdly .app is added automatically for Mac
            if (target == Target.Windows32 || target == Target.Windows64)
                o.locationPathName += ".exe";

            o.target = UnityTarget(target);
            BuildOptions opts = BuildOptions.None;
            if (Settings.developmentBuild)
                opts |= BuildOptions.Development;
            o.options = opts;

            return o;
        }

    }

}
