﻿namespace Sparkade.SparkTools.AutoVersion.Editor
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    /// <summary>
    /// Automatically stamps builds with a version derived from the git repo the project is located in.
    /// </summary>
    public class AutoVersion : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static string prevVersion = null;

        /// <summary>
        /// Gets a value indicating whether the version has been set.
        /// </summary>
        public static bool VersionSet => prevVersion != null;

        /// <inheritdoc/>
        public int callbackOrder => 0;

        /// <summary>
        /// Sets the build version to a version derived from the git repo the project is located in.
        /// </summary>
        public static void SetVersion()
        {
            if (!VersionSet)
            {
                prevVersion = PlayerSettings.bundleVersion;
                string buildVersion = GetGitVersion();
                PlayerSettings.bundleVersion = buildVersion;
                Debug.Log($"Build version set to '{buildVersion}'.");
            }
            else
            {
                Debug.LogError("Build version is already set.");
            }
        }

        /// <summary>
        /// Resets the version to what it was before SetBuildVersion was last called.
        /// </summary>
        public static void ResetVersion()
        {
            if (VersionSet)
            {
                PlayerSettings.bundleVersion = prevVersion;
                prevVersion = null;
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError("Build version has not been set.");
            }
        }

        /// <summary>
        /// Gets a version derived from the git repo the project is located in.
        /// </summary>
        /// <returns>The git version as a string.</returns>
        public static string GetGitVersion()
        {
            string result;

            string cmdArguments = "/c git update-index --refresh > nul & git describe --always --broken";
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", cmdArguments)
            {
                WorkingDirectory = Application.dataPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();
                result = process.StandardOutput.ReadToEnd().Split('\n')[0];
            }

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogWarning("Unable to get git hash, please ensure git is installed and your project is in a git repo.");
                result = "version-error";
            }

            return result;
        }

        /// <inheritdoc/>
        public void OnPreprocessBuild(BuildReport buildReport = default)
        {
            Application.logMessageReceived += this.OnBuildError;
            SetVersion();
        }

        /// <inheritdoc/>
        public void OnPostprocessBuild(BuildReport buildReport = default)
        {
            Application.logMessageReceived -= this.OnBuildError;
            ResetVersion();
        }

        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (BuildPipeline.isBuildingPlayer && type == LogType.Error)
            {
                this.OnPostprocessBuild();
            }
        }
    }
}