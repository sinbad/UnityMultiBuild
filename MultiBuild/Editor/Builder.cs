using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.Build.Reporting;

namespace MultiBuild {

    public static class Builder {

        /// <summary>
        /// Build with default saved options
        /// </summary>
        public static bool Build() {
            var settings = Storage.LoadSettings();
            if (settings == null) {
                throw new InvalidOperationException("No saved settings found, cannot build");
            }
            return Build(settings, null);
        }

        /// <summary>
        /// Build using command line arguments, i.e. via the -executeMethod argument to Unity.
        /// Unlike Build() this does not load any saved settings. Always uses the product name.
        /// Arguments must all be after the executeMethod call:
        ///  Unity -quit -batchmode -executeMethod MultiBuild.Builder.BuildCommandLine <outputFolder> <is_dev> <targetName> [targetName...]
        /// Products are created in <outputFolder>/targetName/
        /// targetName must match the enum MultiBuild.Target
        /// No other arguments must be after that
        /// </summary>
        /// <returns></returns>
        public static void BuildCommandLine() {
            // We get all the args, including UNity.exe, -quit -batchmode etc
            // read everything after our execute call
            var args = System.Environment.GetCommandLineArgs();
            // 0 = looking for args
            // 1 = expecting output folder
            // 2 = expecting dev boolean
            // 3 = expecting target
            int stage = 0;
            Settings settings = new Settings();
            settings.Reset();

            string usage = "\nUsage:\n  Unity <args> -executeMethod MultiBuild.Builder.BuildCommandLine <outputFolder> <is_dev> <targetName> [targetName...]\n";

            for (int i = 0; i < args.Length; ++i) {
                switch (stage) {
                case 0:
                    // Skipping over all args until we see ours
                    if (args[i].Equals("MultiBuild.Builder.BuildCommandLine")) {
                        stage++;
                    }
                    break;
                case 1:
                    // next arg is output
                    settings.outputFolder = args[i];
                    stage++;
                    break;
                case 2:
                    // next arg is dev flag
                    try {
                        settings.developmentBuild = Boolean.Parse(args[i]);
                        stage++;
                    } catch (FormatException) {
                        throw new ArgumentException("Development build argument was not a valid boolean" + usage);
                    }
                    break;
                default:
                case 3:
                    // all subsequent args should be targets
                    try {
                        settings.targets.Add((Target)Enum.Parse(typeof(Target), args[i]));
                    } catch (ArgumentException) {
                        throw new ArgumentException(string.Format("Invalid target '{0}'", args[i]));
                    }
                    break;
                }
            }
            if (stage != 3 || settings.targets.Count == 0) {
                throw new ArgumentException("Not enough arguments." + usage);
            }

            Build(settings, null);

        }

        /// <summary>
        /// Build with given settings, call back if required
        /// </summary>
        /// <param name="settings">Settings to build with</param>
        /// <param name="callback">Callback which is called before and after a
        /// given build target, being passed the build options, a float from
        /// 0..1 indicating how far through the process we are, and a bool which
        /// is false for the pre-call and true for the post-call. Return true to
        /// continue or false to abort.</param>
        /// <returns>True if the process completed fully or false if was cancelled by callback</returns>
        public static bool Build(Settings settings, System.Func<BuildPlayerOptions, float, bool, bool> callback) {

            var buildSteps = SelectedBuildOptions(settings);
            int i = 1;
            foreach (var opts in buildSteps) {
                if (callback != null &&
                    !callback(opts, (float)(i / (float)buildSteps.Count), false)) {
                        return false; // cancelled
                }
#if UNITY_2018_1_OR_NEWER
                var report = BuildPipeline.BuildPlayer(opts);
                string err = report.summary.result == BuildResult.Succeeded ? string.Empty : "See log";
#elif UNITY_5_5_OR_NEWER
                var err = BuildPipeline.BuildPlayer(opts);
#else
                string err = BuildPipeline.BuildPlayer(
                        opts.scenes,
                        opts.locationPathName,
                        opts.target,
                        opts.options);
#endif
                if (!string.IsNullOrEmpty(err)) {
                    throw new InvalidOperationException(string.Format("Build error: {0}", err));
                }
                ++i;
                if (callback != null &&
                    !callback(opts, (float)(i / (float)buildSteps.Count), true)) {
                        return false; // cancelled
                }
            }
            return true;
        }

        public static BuildTargetGroup GroupForTarget(BuildTarget t) {
            // Can't believe Unity doesn't have a method for this already
            switch (t) {
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
            case BuildTarget.StandaloneOSX:
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
            case BuildTarget.WiiU:
                return BuildTargetGroup.WiiU;
            case BuildTarget.tvOS:
                return BuildTargetGroup.tvOS;
#if UNITY_5_5_OR_NEWER
            case BuildTarget.N3DS:
                return BuildTargetGroup.N3DS;
#else
            case BuildTarget.Nintendo3DS:
                return BuildTargetGroup.Nintendo3DS;
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

        static BuildTarget UnityTarget(Target t) {
            switch (t) {
            case Target.Win32:
                return BuildTarget.StandaloneWindows;
            case Target.Win64:
                return BuildTarget.StandaloneWindows64;
            case Target.Mac:
            case Target.Mac32:
                return BuildTarget.StandaloneOSX;
            case Target.MacUniversal:
                return BuildTarget.StandaloneOSX;
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
            case Target.WinStore:
                return BuildTarget.WSAPlayer;
            case Target.Tizen:
                return BuildTarget.Tizen;
            case Target.PS4:
                return BuildTarget.PS4;
            case Target.XboxOne:
                return BuildTarget.XboxOne;
            case Target.WiiU:
                return BuildTarget.WiiU;
            case Target.tvOS:
                return BuildTarget.tvOS;
#if UNITY_5_5_OR_NEWER
            case Target.Nintendo3DS:
                return BuildTarget.N3DS;
#else
            case Target.Nintendo3DS:
                return BuildTarget.Nintendo3DS;
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

        static public List<BuildPlayerOptions> SelectedBuildOptions(Settings settings) {
            var ret = new List<BuildPlayerOptions>();
            foreach (var target in settings.targets) {
                ret.Add(BuildOpts(settings, target));
            }
            return ret;
        }

        static public BuildPlayerOptions BuildOpts(Settings settings, Target target) {
            BuildPlayerOptions o = new BuildPlayerOptions();
            // Build all the scenes selected in build settings
            o.scenes = EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();
            string subfolder = target.ToString();
            o.locationPathName = Path.Combine(settings.outputFolder, subfolder);
            // location needs to include the output name too
            if (settings.useProductName)
                o.locationPathName = Path.Combine(o.locationPathName, PlayerSettings.productName);
            else
                o.locationPathName = Path.Combine(o.locationPathName, settings.overrideName);
            // Need to append exe in Windows, isn't added by default
            // Weirdly .app is added automatically for Mac
            if (target == Target.Win32 || target == Target.Win64)
                o.locationPathName += ".exe";

            o.target = UnityTarget(target);
            BuildOptions opts = BuildOptions.None;
            if (settings.developmentBuild)
                opts |= BuildOptions.Development;
            o.options = opts;

            return o;
        }

    }

}