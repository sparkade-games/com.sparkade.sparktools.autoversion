namespace Sparkade.SparkTools.AutoVersion.Editor
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

        /// <inheritdoc/>
        public int callbackOrder => 0;

        /// <summary>
        /// Sets the build version to a version derived from the git repo the project is located in.
        /// </summary>
        public static void SetBuildVersion()
        {
            prevVersion = PlayerSettings.bundleVersion;
            string buildVersion = GetGitVersion();
            PlayerSettings.bundleVersion = buildVersion;
            Debug.Log($"Build version set to '{buildVersion}'.");
        }

        /// <summary>
        /// Resets the version to what it was before SetBuildVersion was last called.
        /// </summary>
        public static void ResetEditorVersion()
        {
            if (prevVersion != null)
            {
                PlayerSettings.bundleVersion = prevVersion;
                prevVersion = null;
                AssetDatabase.SaveAssets();
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

        /// <summary>
        /// Stamps the build with a version.
        /// </summary>
        /// <param name="buildReport">This parameter is unused and should be ignored.</param>
        public void OnPreprocessBuild(BuildReport buildReport = default)
        {
            Application.logMessageReceived += this.OnBuildError;
            SetBuildVersion();
        }

        /// <summary>
        /// Resets the version for the Editor.
        /// </summary>
        /// <param name="buildReport">This parameter is unused and should be ignored.</param>
        public void OnPostprocessBuild(BuildReport buildReport = default)
        {
            Application.logMessageReceived -= this.OnBuildError;
            ResetEditorVersion();
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