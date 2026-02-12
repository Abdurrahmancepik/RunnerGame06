#if UNITY_EDITOR
using System.IO;
using Reactional.Core;
using UnityEditor;
using UnityEngine;

namespace Reactional
{
    [InitializeOnLoad]
    internal class ReactionalMenu : MonoBehaviour
    {
        private const string dontAskAgainKey = "Reactional.DontShowAgain";
        private const string firstTimeKey = "Reactional.FirstTime";

        static ReactionalMenu()
        {
            var reactionalAssetsPath = $"{Application.streamingAssetsPath}/Reactional";
            if (!Directory.Exists(reactionalAssetsPath)) Directory.CreateDirectory(reactionalAssetsPath);
        }

        /// <summary>
        /// Create a name for the GameObject you get from the MenuItem.
        /// </summary>
        /// <param name="gameObjectName"></param>
        /// <param name="disallowMultipleComponent">Set to false if you want to allow the user to add more than one of the gameObjects with the component.</param>
        /// <typeparam name="T"></typeparam>
        private static void CreateMenuItem<T>(string gameObjectName, bool disallowMultipleComponent = true)
            where T : Component
        {
            var existingComponent = AssetHelper.GetFirstOfType<T>();
            if (existingComponent != null && disallowMultipleComponent)
            {
                if (Setup.IsValid)
                {
                    Setup.ReactionalLog($"{typeof(T).Name} Component already exists in the scene.", LogLevel.Warning);
                    return;
                }
            }

            var gameObject = new GameObject(gameObjectName);
            gameObject.AddComponent<T>();
        }

        [MenuItem("Tools/Reactional/Add Reactional Manager", false, 21)]
        internal static void AddReactionalManager()
        {
            var rm = AssetHelper.GetFirstOfType<ReactionalManager>();
            if (rm != null)
            {
                if (Setup.IsValid)
                {
                    Setup.ReactionalLog("Reactional Manager already exists in the scene.", LogLevel.Warning);
                    return;
                }
            }

            // Create the Reactional Music GameObject and add the ReactionalManager and Playback components
            var reactionalMusic = new GameObject("Reactional Music");
            reactionalMusic.AddComponent<ReactionalManager>();
            reactionalMusic.AddComponent<BasicPlayback>();

            // Create the Reactional Engine child GameObject and add the ReactionalEngine script
            var reactionalEngine = new GameObject("Reactional Engine");
            reactionalEngine.AddComponent<ReactionalEngine>();
            reactionalEngine.transform.SetParent(reactionalMusic.transform);

            reactionalEngine.AddComponent<ProfilerController>();
            reactionalEngine.transform.SetParent(reactionalMusic.transform);
        }

        [MenuItem("Tools/Reactional/Components/Add Reactional Profile Controller", false, 45)]
        private static void AddProfileController() =>
            CreateMenuItem<ProfilerController>("Reactional Profiler Controller", true);

        [MenuItem("Tools/Reactional/Visit Platform", false, 1)]
        private static void OpenReactionalPlatform() => Application.OpenURL("https://app.reactionalmusic.com/");

        [MenuItem("Tools/Reactional/Documentation", false, 2)]
        private static void OpenReactionalDocumentation() =>
            Application.OpenURL("https://docs.reactionalmusic.com/Unity/");

        [MenuItem("Tools/Reactional/Discord", false, 3)]
        private static void OpenReactionalDiscord() => Application.OpenURL("https://discord.gg/bAJNRdXq4c");

        [MenuItem("Tools/Reactional/Forum Support", false, 4)]
        private static void OpenReactionalSupport() => Application.OpenURL("https://forum.reactionalmusic.com/");

        [MenuItem("Tools/Reactional/Reactional Website", false, 5)]
        private static void OpenReactionalWebsite() => Application.OpenURL("https://reactionalmusic.com/");

        private static void ShowDownloadMessage(string assetName, string url)
        {
            var confirm = EditorUtility.DisplayDialog(
                title: "Downloading " + assetName,
                message: assetName +
                         "will be downloaded in your browser.\n\nOnce it's done, open it as a separate Unity project.",
                ok: "OK"
            );
            if (confirm) Application.OpenURL(url);
        }

        [MenuItem("Tools/Reactional/Demos & Games/Download Reactional Demo Scene", false, 104)]
        private static void DownloadDemoScene()
        {
            const string DemoSceneUrl =
                "https://storage.googleapis.com/rm-cdn/demo-scenes/unity/ReactionalShooterDemo.zip";
            ShowDownloadMessage("Reactional Demo Scene", DemoSceneUrl);
        }

        [MenuItem("Tools/Reactional/Demos & Games/Download Reactional Disco Bot Scene", false, 105)]
        private static void DownloadDiscoBot()
        {
            const string DiscoBotUrl = "https://storage.googleapis.com/rm-cdn/demo-scenes/unity/ReactionalDiscoBot.zip";
            ShowDownloadMessage("Reactional Disco Bot", DiscoBotUrl);
        }

        [MenuItem("Tools/Reactional/Open Readme", false, 106)]
        private static void OpenReadMe() => OpenFile("README.md");

        [MenuItem("Tools/Reactional/Open Changelog", false, 107)]
        private static void OpenChangelog() => OpenFile("CHANGELOG.md");

        [MenuItem("Tools/Reactional/Version: " + Reactional.Setup.PluginVersion, false, 999)]
        private static void Version()
        {
        }

        [MenuItem("Tools/Reactional/Version: " + Reactional.Setup.PluginVersion, true, 999)]
        private static bool DisableVersion()
        {
            return false;
        }

        private static void OpenFile(string fileName)
        {
            const string PluginFolder = "ReactionalMusic";
            const string Resources = "Resources";
            var filePath = Path.Combine(Application.dataPath, PluginFolder, Resources, fileName);

            if (File.Exists(filePath))
            {
                EditorUtility.OpenWithDefaultApp(filePath);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    title: "File Not Found",
                    message: $"Could not find {fileName} in {PluginFolder}.",
                    ok: "OK"
                );
            }
        }
    }
}

#endif