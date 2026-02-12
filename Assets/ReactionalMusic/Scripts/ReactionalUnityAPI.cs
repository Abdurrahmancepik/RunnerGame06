using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Reactional.Core;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Unity.Profiling;

namespace Reactional
{
    [DefaultExecutionOrder(550)]
    public static class Setup
    {
        public const string PluginVersion = "1.0.3";

        /// <summary>
        /// Get or Set the sample rate of the engine
        /// </summary>
        public static int Samplerate
        {
            get
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "sample_rate");
                return (int)engine.GetParameterInt(-1, pindex);
            }
            set
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "sample_rate");
                engine.SetParameterInt(-1, pindex, value);
            }
        }

        /// <summary>
        /// Get or Set the block size of the engine.
        /// Adjusting the block size may affect the engine's processing capabilities.
        /// </summary>
        /// <example>
        /// <code>
        /// // Setting the BlockSize
        /// Reactional.Setup.BlockSize = 512;
        /// </code>
        /// <code>
        /// // Getting the BlockSize
        /// int blockSize = Reactional.Setup.BlockSize;
        /// </code>
        /// </example>
        public static int BlockSize
        {
            get
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "block_size");
                return (int)engine.GetParameterInt(-1, pindex);
            }
            set
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "block_size");
                engine.SetParameterInt(-1, pindex, value);
            }
        }

        /// <summary>
        /// Get or Set the lookahead parameter of the engine.
        /// Adjusting the lookahead value can influence the audio processing latency.
        /// </summary>
        /// <example>
        /// <code>
        /// // Setting the Lookahead
        /// Reactional.Setup.Lookahead = 1;
        /// </code>
        /// <code>
        /// // Getting the Lookahead
        /// int lookahead = Reactional.Setup.Lookahead;
        /// </code>
        /// </example>
        public static int Lookahead
        {
            get
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "lookahead");
                return (int)engine.GetParameterInt(-1, pindex);
            }
            set
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(-1, "lookahead");
                engine.SetParameterInt(-1, pindex, value);
            }
        }

        /// <summary>
        /// Initialize audio settings with specified buffer size and sample rate.
        /// </summary>
        /// <param name="bufferSize">The buffer size to set, defaults to -1 for auto detection.</param>
        /// <param name="sampleRate">The sample rate to set, defaults to -1 for auto detection.</param>
        /// <example>
        /// <code>
        /// Reactional.Setup.InitAudio();
        /// </code>
        /// <code>
        /// Reactional.Setup.InitAudio(512, 44100);
        /// </code>
        /// </example>
        public static void InitAudio(int bufferSize = -1, int sampleRate = -1)
        {
            if (bufferSize == -1)
            {
                AudioSettings.GetDSPBufferSize(out int bufsize, out int numbufs);
                bufferSize = bufsize;
            }
            if (sampleRate == -1)
            {
                sampleRate = AudioSettings.outputSampleRate;
            }

            Reactional.Setup.BlockSize = bufferSize;
            Reactional.Setup.Samplerate = sampleRate;
            Reactional.Setup.Lookahead = ReactionalManager.Instance.lookahead;
        }

        /// <summary>
        /// Checks the validity of the ReactionalManager instance in the scene.
        /// Returns true if a valid instance exists; otherwise, logs an error and returns false.
        /// </summary>
        /// <example>
        /// <code>
        /// bool valid = Reactional.Setup.IsValid;
        /// </code>
        /// </example>
        public static bool IsValid
        {
            get
            {
                var rM = ReactionalManager.Instance;
                if (!rM)
                {
                    Setup.ReactionalLog("Reactional: No ReactionalManager found in scene", LogLevel.Warning);
                    return false;
                }
                bool isValid = rM.bundles.Count > 0;
                return isValid;
            }
        }

        /// <summary>
        /// Log a message with a specified severity level.
        /// Enabled on the Reactional Engine
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="level">The severity level.</param>
        /// <example>
        /// <code>
        /// Reactional.Setup.ReactionalLog("Initialization complete.");
        /// Reactional.Setup.ReactionalLog("Warning: Potential issue detected.", LogLevel.Warning);
        /// </code>
        /// </example>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR"), HideInCallstack]
        public static void ReactionalLog(string msg, LogLevel level = LogLevel.Log)
        {
            var activeLogLevel = ReactionalEngine.LogLevel;
            if (activeLogLevel == LogLevel.Disabled || (activeLogLevel & level) == 0) { return; }
            switch (level)
            {
                case LogLevel.Log:
                    Debug.Log($"Reactional: {msg}");
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"Reactional: {msg}");
                    break;
                case LogLevel.Error:
                    Debug.LogError($"Reactional: {msg}");
                    break;
            }
        }
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR"), HideInCallstack]
        public static void ReactionalLog(IntPtr msg, LogLevel level = LogLevel.Log) => ReactionalLog(Native.StringifyPointer(msg), level);
        
        /// <summary>
        /// Updates the bundles in the ReactionalManager.
        /// This method will check the StreamingAssets/Reactional folder for content.
        /// </summary>
        /// <example>
        /// <code>
        /// Reactional.Setup.UpdateBundles();
        /// </code>
        /// </example>
        public static void UpdateBundles()
        {
            ReactionalManager rM = ReactionalManager.Instance;
            rM.UpdateBundles();
        }

        /// <summary>
        /// Load all bundles asynchronously.
        /// </summary>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadBundles();
        /// </code>
        /// </example>
        public static async Task LoadBundles()
        {
            var rM = ReactionalManager.Instance;

            foreach (var bundle in rM.bundles)
            {
                await LoadBundle(bundle.name);
            }
        }

        /// <summary>
        /// Load a specific bundle asynchronously by its name.
        /// </summary>
        /// <param name="bundleName">The name of the bundle to load.</param>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadBundle("MyMusicProject_12030");
        /// </code>
        /// </example>
        public static async Task LoadBundle(string bundleName)
        {
            var rM = ReactionalManager.Instance;

            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));
            if (bundle == null)
            {
                ReactionalLog("Bundle not found: " + bundleName, LogLevel.Warning);
                return;
            }

            ReactionalLog($"Loaded Bundle: {bundleName}");
            foreach (var section in bundle.sections)
            {
                await LoadSection(bundleName, section.name);
            }
        }

        /// <summary>
        /// Load a specific section of a bundle asynchronously.
        /// </summary>
        /// <param name="bundleName">The name of the bundle containing the section.</param>
        /// <param name="sectionName">The name of the section to load.</param>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadSection("MyMusicProject_12030", "Level2");
        /// </code>
        /// </example>
        public static async Task LoadSection(string bundleName, string sectionName)
        {
            var rM = ReactionalManager.Instance;
            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));
            if (bundle == null)
            {
                ReactionalLog("Bundle not found: " + bundleName, LogLevel.Warning);
                return;
            }
            var section = bundle.sections.Find(x => x.name.Equals(sectionName));
            if (section == null)
            {
                ReactionalLog("Section not found: " + sectionName, LogLevel.Warning);
                return;
            }
            
            ReactionalLog($"Loaded Section: {sectionName}");

            if (section.themes.Count > 0)
            {
                await LoadTheme(bundleName, sectionName, section.themes[0].name);
            }

            foreach (var playlist in section.playlists)
            {
                await LoadPlaylist(bundleName, sectionName, playlist.name);
            }
        }

        /// <summary>
        /// Load the default section asynchronously.
        /// </summary>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadSection();
        /// </code>
        /// </example>
        public static async Task LoadSection()
        {
            var rM = ReactionalManager.Instance;
            string defaultSection = rM.defaultSection;
            if (defaultSection == "")
            {
                defaultSection = rM.bundles[0].sections[0].name;
            }

            // find section in bundles
            foreach (var bundle in rM.bundles)
            {
                foreach (var section in bundle.sections)
                {
                    if (section.name == defaultSection)
                    {
                        await LoadSection(bundle.name, section.name);
                        return;
                    }
                }
            }

            await Setup.LoadTheme();
            await LoadPlaylist();
        }

        /// <summary>
        /// Load a specific playlist asynchronously.
        /// </summary>
        /// <param name="bundleName">The name of the bundle containing the section.</param>
        /// <param name="sectionName">The name of the section containing the playlist.</param>
        /// <param name="playlistName">The name of the playlist to load.</param>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadPlaylist("MyMusicProject_12030", "Level2", "DefaultPlaylist");
        /// </code>
        /// </example>
        public static async Task LoadPlaylist(string bundleName, string sectionName, string playlistName)
        {
            var rM = ReactionalManager.Instance;
            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));
            var section = bundle.sections.Find(x => x.name.Equals(sectionName));
            var playlist = section.playlists.Find(x => x.name.Equals(playlistName));
            if (playlist == null)
            {
                ReactionalLog("Playlist not found: " + playlistName, LogLevel.Warning);
                return;
            }

            foreach (var track in playlist.tracks)
            {
                await LoadTrack(bundleName, track.name);
            }
        }

        /// <summary>
        /// Load a playlist asynchronously by its name.
        /// </summary>
        /// <param name="bundleName">The name of the bundle containing the playlist.</param>
        /// <param name="playlistName">The name of the playlist to load.</param>
        /// <example>
        /// <code>
        /// await Reactional.Setup.LoadPlaylist("MyMusicProject_12030", "DefaultPlaylist");
        /// </code>
        /// </example>
        public static async Task LoadPlaylist(string bundleName, string playlistName)
        {
            bool foundPlaylist = false;

            ReactionalManager rM = ReactionalManager.Instance;
            Reactional.Core.Engine engine = ReactionalEngine.Instance.engine;

            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));
            foreach (var section in bundle.sections)
            {
                var playlist = section.playlists.Find(y => y.name.Equals(playlistName));
                if (playlist != null)
                {
                    foundPlaylist = true;
                    ReactionalLog("Found Playlist \"" + playlistName + "\". Loading.");
                    foreach (var track in playlist.tracks)
                    {
                        await LoadTrack(bundleName, track.name);
                    }
                    ReactionalLog("Load finished.");
                }
            }
            if (!foundPlaylist)
                ReactionalLog("No playlist found.", LogLevel.Warning);
        }

        /// <summary>
        /// Loads a playlist by its index in the loaded bundles list.
        /// </summary>
        /// <param name="bundleIndex">The index of the bundle containing the playlist.</param>
        /// <param name="playlistName">The name of the playlist to load.</param>
        public static async Task LoadPlaylist(int bundleIndex, string playlistName)
        {
            ReactionalManager rM = ReactionalManager.Instance;
            var bundles = rM.bundles;
            if (bundleIndex >= bundles.Count)
            {
                Setup.ReactionalLog("Reactional: Bundle index out of range", LogLevel.Warning);
                return;
            }
            var bundle = bundles[bundleIndex];
            await LoadPlaylist(bundle.name, playlistName);
        }

        /// <summary>
        /// Loads a playlist by its name, optionally using the default section if no name is provided.
        /// </summary>
        /// <param name="playlistName">The name of the playlist to load, defaults to an empty string to use the default section.</param>
        public static async Task LoadPlaylist(string playlistName = "")
        {
            ReactionalManager rM = ReactionalManager.Instance;
            var bundles = rM.bundles;

            string bundleName = "";
            if (playlistName == "" && rM.defaultSection == "")
            {
                playlistName = bundles?[0]?.sections?[0]?.playlists?[0]?.name ?? "";
                bundleName = bundles[0].name;
            }
            else if (playlistName == "")
            {
                foreach (var bundle in bundles)
                {
                    foreach (var section in bundle.sections)
                    {
                        if (section.name == rM.defaultSection)
                        {
                            playlistName = section.playlists?.Count > 0 ? section.playlists[0]?.name : "";
                            bundleName = bundle.name;
                        }
                    }
                }
            }
            else
            {
                foreach (var bundle in bundles)
                {
                    foreach (var s in bundle.sections)
                    {
                        foreach (var p in s.playlists)
                        {
                            if (p.name == playlistName)
                            {
                                playlistName = p.name;
                                bundleName = bundle.name;
                            }
                        }
                    }
                }
            }

            if (bundleName == "")
            {
                Setup.ReactionalLog("Reactional: Default Section not found, falling back to first bundle", LogLevel.Warning);
                bundleName = bundles[0].name;
                playlistName = bundles[0].sections[0].playlists[0].name;
            }

            await LoadPlaylist(bundleName, playlistName);
        }

        private static readonly object trackLock = new object();

        /// <summary>
        /// Loads a specific track by its name from a given bundle.
        /// </summary>
        /// <param name="bundleName">The name of the bundle containing the track.</param>
        /// <param name="trackName">The name of the track to load.</param>
        public static async Task LoadTrack(string bundleName, string trackName)
        {
            ReactionalManager rM = ReactionalManager.Instance;
            Reactional.Core.Engine engine = ReactionalEngine.Instance.engine;

            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));

            TrackInfo track = null;
            foreach (var section in bundle.sections)
            {
                foreach (var playlist in section.playlists)
                {
                    track = playlist.tracks.Find(y => y.name.Equals(trackName));
                    if (track != null)
                    {
                        ReactionalLog("Found Track \"" + trackName + "\". Loading.");

                        // Check if track exists in loaded tracks
                        if (rM._loadedTracks.Find(x => x.name == trackName) != null)
                        {
                            ReactionalLog("Track already loaded: " + trackName, 0);
                            return;
                        }
                        string path;
                        if (bundle.path.Contains("StreamingAssets"))
                            path = Application.streamingAssetsPath + "/Reactional/" + track.bundleID;
                        else
                            path = bundle.path;
                        track.ID = AssetHelper.AddTrackFromPath(engine, System.IO.Path.Combine(path, track.hash));
                        if (track.ID >= 0)
                        {
                            await AssetHelper.LoadTrackAssets(
                                engine, 
                                track.ID,
                                Application.streamingAssetsPath + "/Reactional/" + track.bundleID, 
#if !UNITY_ANDROID
                                loadAsync: rM.loadType == Setup.LoadType.LoadInBackground ? true : false
#else
                                loadAsync: false
#endif
                            );
                        }
                        lock (trackLock)
                        {
                            rM._loadedTracks.Add(track);
                        }
                        ReactionalLog("Load finished.");
                        return;
                    }
                }
            }
            if (track == null)
                ReactionalLog("No track found:" + trackName, LogLevel.Warning);
        }

        /// <summary>
        /// Unload a track by its name
        /// </summary>
        /// <param name="trackName">The name of the theme to Unload</param>
        public static void UnloadTrack(string trackName)
        {
            var rM = ReactionalManager.Instance;
            if (rM == null)
            {
                ReactionalLog("manager not found", LogLevel.Warning);
                return;
            }

            var track = rM._loadedTracks.Find(x => x.name == trackName);

            if (track != null)
            {
                ReactionalEngine.Instance.engine.RemoveTrack(track.ID);
                ReactionalLog($"Track '{trackName}' unloaded");
            }
            else
            {
                ReactionalLog($" Track '{trackName}' not loaded and could not be removed", LogLevel.Warning);
            }
        }
        
        public static Task<TrackInfo> LoadTrack(byte[] metadata, byte[] audioData)
        {
            var rM = ReactionalManager.Instance;
            var trackDict = AssetHelper.ReadTrackMetadataFromData(metadata);
            
            int ENCRYPTION_KEY_SIZE = 256;
            int HASH_SIZE = 64;

            var size = audioData.Length;
            var keyOffset = size - ENCRYPTION_KEY_SIZE;
            var hashOffset = keyOffset - HASH_SIZE;
    
            var data = audioData[..hashOffset];
            var trackHash = audioData[hashOffset..keyOffset];
            var hash = System.Text.Encoding.UTF8.GetString(trackHash);
            var key = audioData[keyOffset..];
    
            var engine = ReactionalEngine.Instance.engine;
            var trackId = engine.AddTrackFromBytes(metadata);
            if (trackId < 0)
            {
                throw new EngineErrorException(trackId);
            }
            TrackInfo ti = AssetHelper.ParseTrack(trackDict, hash);
            if (rM._loadedTracks.Find(x => x.name == ti.name) != null)
            {
                ReactionalLog("Track already loaded: " + ti.name);
                return null;
            }
            ReactionalLog("Loading track \"" + ti.name + "\"");
            ti.ID = trackId;
            for (int i = 0; i < engine.GetNumAssets(trackId); i++)
            {
                string id = engine.GetAssetId(trackId, i);
                string type = engine.GetAssetType(trackId, i);
                engine.SetAssetData(trackId, id, type, data, key);
            }
            ReactionalManager.Instance._loadedTracks.Add(ti);
            return Task.FromResult(ti);
        }

        public static async Task<TrackInfo> LoadTrack(Services.Remote.TrackDownloadData data)
        {
            return await LoadTrack(data.metadata, data.trackData);
        }

        /// <summary>
        /// Loads a specific theme by its name from a given section and bundle.
        /// </summary>
        /// <param name="bundleName">The name of the bundle containing the theme.</param>
        /// <param name="sectionName">The name of the section containing the theme.</param>
        /// <param name="themeName">The name of the theme to load.</param>
        public static async Task LoadTheme(string bundleName, string sectionName, string themeName)
        {
            ReactionalManager rM = ReactionalManager.Instance;
            Reactional.Core.Engine engine = ReactionalEngine.Instance.engine;

            var bundle = rM.bundles.Find(x => x.name.Equals(bundleName));
            var section = bundle.sections.Find(x => x.name.Equals(sectionName));
            var theme = section.themes.Find(x => x.name.Equals(themeName));

            if (theme == null)
            {
                ReactionalLog("Theme not found: " + themeName, LogLevel.Warning);
                return;
            }
            
            // check if theme exists in loaded themes
            if (rM._loadedThemes.Find(x => x.name == themeName) != null)
            {
                ReactionalLog("Theme already loaded: " + themeName, 0);
                return;
            }

            string path;
            if (bundle.path.Contains("StreamingAssets"))
                path = Application.streamingAssetsPath + "/Reactional/" + theme.bundleID;
            else
                path = bundle.path;
            theme.ID = AssetHelper.AddTrackFromPath(engine, System.IO.Path.Combine(path, theme.hash));
            await AssetHelper.LoadTrackAssets(
                engine, 
                theme.ID, 
                path, 
#if !UNITY_ANDROID
                loadAsync: rM.loadType == Setup.LoadType.LoadInBackground ? true : false
#else
                loadAsync: false
#endif
            );
            ReactionalLog("Theme loaded: " + themeName);
            if (Playback.Theme.GetState() == Playback.MusicSystem.PlaybackState.Stopped)
                engine.SetTheme(theme.ID);
            rM._loadedThemes.Add(theme);
        }

        /// <summary>
        /// Loads a theme by its name, optionally using the default section if no name is provided.
        /// </summary>
        /// <param name="themeName">The name of the theme to load, defaults to an empty string to use the default section.</param>
        
        public static async Task LoadTheme(string themeName = "")
        {
            if (themeName == "" && ReactionalManager.Instance.defaultSection == "")
            {
                themeName = ReactionalManager.Instance.bundles[0].sections[0].themes[0].name;
            }

            foreach (var bundle in ReactionalManager.Instance.bundles)
            {
                foreach (var section in bundle.sections)
                {
                    if (themeName == "" && section.name == ReactionalManager.Instance.defaultSection)
                        themeName = section.themes[0].name;

                    foreach (var theme in section.themes)
                    {
                        if (theme.name == themeName)
                        {
                            await LoadTheme(bundle.name, section.name, themeName);
                            return;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Unload a theme by its name
        /// </summary>
        /// <param name="themeName">The name of the theme to Unload</param>
        public static void UnloadTheme(string themeName)
        {
            var rM = ReactionalManager.Instance;
            if (rM == null)
            {
                ReactionalLog("manager not found", LogLevel.Warning);
                return;
            }

            var theme = rM._loadedThemes.Find(x => x.name == themeName);

            if (theme != null)
            {
                ReactionalEngine.Instance.engine.RemoveTrack(theme.ID);
                ReactionalLog($"Theme '{themeName}' Unloaded");
            }
            else
            {
                ReactionalLog($" Theme '{themeName}' Not Loaded and could not be removed", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Gets or sets the permission to play.
        /// </summary>
        public static bool AllowPlay
        {
            get => ReactionalEngine.Instance && ReactionalEngine.Instance._allowPlay;
            set => ReactionalEngine.Instance._allowPlay = value;
        }

        public enum LoadType
        {
#if !UNITY_ANDROID
            LoadInBackground = 0,
#endif
            Synchronous = 1,
        }

        public enum UpdateMode
        {
            Main = 0,
            Threaded = 1,
            AudioThread = 2
        }

        public enum AudioOutputMode
        {
            Unity = 0,
            Custom = 1
        }

        public enum PlaybackMode
        {
            [Tooltip("Randomly selects the next track")]
            Random,
            [Tooltip("Plays track in sequence")] 
            Sequential,
            [Tooltip("Repeats the current track")] 
            Repeat,
            [Tooltip("Plays a single track once")] 
            Single
        }

        /// <summary>
        /// Types of theme controls available
        /// </summary>
        public enum ThemeControlType
        {
            [Tooltip("Controls a macro parameter")]
            Macro,
            [Tooltip("Controls Part Changes")]
            Part,
            [Tooltip("Controls triggering of Stingers")]
            Stinger,
            [Tooltip("Controls a overridable instrument")]
            OverridableInstrument
        }
        
        public enum TrackControlType   // TODO should Bundle and Sctions really be here. 
        { 
            [Tooltip("Choose what bundle you are using")]
            Bundle,
            [Tooltip("Choose Sections from your bundle")]
            Section,
            [Tooltip("Choose a Playlist with tracks from your Section")]
            Playlist,
            [Tooltip("Choose a specific Track from your Playlist")]
            Track
        }
        
        public enum EditorControlType
        {
            [Tooltip("Control a Theme (Parts, Macros, Stingers, Overridable Instruments)")]
            Theme,

            [Tooltip("Control a Track (Playlist, Tracks)")]
            Track,
        }

        public class ParameterRecorder
        {
            [System.Serializable]
            public class ParameterRecord
            {
                public float time;
                public float value;
            }

            [System.Serializable]
            public class ParameterRecordParent
            {
                public string name;
                public int id;
                public List<ParameterRecord> records = new List<ParameterRecord>();
            }

            [System.Serializable]
            public class ParameterRecordList
            {
                public List<ParameterRecordParent> records = new List<ParameterRecordParent>();
            }

            public ParameterRecordList records = new ParameterRecordList();

            public void Record(string name, int id, float time, float value)
            {
                if (records.records.Find(x => x.name == name) == null)
                {
                    records.records.Add(new ParameterRecordParent() { name = name });
                }
                var parent = records.records.Find(x => x.name == name);
                parent.records.Add(new ParameterRecord() { time = time, value = value });
            }

            public void Clear()
            {
                records.records.Clear();
            }

            public void Save(string path)
            {
                var json = JsonUtility.ToJson(records);
                System.IO.File.WriteAllText(path, json);
            }
        }
    }

    namespace Playback
    {
        [DefaultExecutionOrder(200)]
        public static class Theme
        {
            /// <summary>
            /// Gets the ID of the current theme.
            /// </summary>
            /// <returns>The theme ID, or -1 if no theme is loaded.</returns>
            public static int GetThemeID()
            {
                if (!ReactionalEngine.Instance) return -1;
                var theme = ReactionalEngine.Instance.engine.GetTheme();
                return theme;
            }

            /// <summary>
            /// Checks if a theme is loaded.
            /// </summary>
            /// <returns>True if a theme is loaded, false otherwise.</returns>
            public static bool IsLoaded()
            {
                var theme = ReactionalEngine.Instance.engine.GetTheme();
                bool loaded = theme >= 0;
                return loaded;
            }
            
            /// <summary>
            /// Gets the metadata of a theme.
            /// </summary>
            /// <param name="id">The theme ID.</param>
            /// <returns>The metadata of the theme as a string.</returns>
            [Obsolete("Deprecated as of v.1.0.2, use GetThemeInfo")]
            public static string GetThemeMetadata(int id)
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(id, "metadata");
                return (string)engine.GetParameterValue(id, pindex);
            }

            /// <summary>
            /// Gets the metadata of the current theme.
            /// </summary>
            /// <returns>The metadata of the theme as a string.</returns>
            [Obsolete("Deprecated as of v.1.0.2, use GetCurrentThemeInfo")]
            public static string GetCurrentThemeMetadata()
            {
                var theme = ReactionalEngine.Instance.engine.GetTheme();
                return GetThemeMetadata(theme);
            }

            /// <summary>
            /// Gets the information of the current theme.
            /// </summary>
            /// <returns>The <see cref="ThemeInfo"/> object containing the details of the current theme.</returns>
            public static ThemeInfo GetCurrentThemeInfo()
            {
                var theme = ReactionalEngine.Instance.engine.GetTheme();
                var ti = ReactionalManager.Instance._loadedThemes.Find(x => x.ID == theme);
                return ti;
            }

            /// <summary>
            /// Plays the current theme.
            /// </summary>
            public static void Play()
            {
                Engine engine = ReactionalEngine.Instance.engine;
                var ti = GetCurrentThemeInfo();
                int trackID = Reactional.Playback.Theme.GetThemeID();
                if (trackID < 0)
                {
                    Setup.ReactionalLog("No theme loaded.", LogLevel.Warning);
                    return;
                }

                if (Theme.GetState() == MusicSystem.PlaybackState.Playing)
                { 
                    if (CurrentBeat() < 0f || Theme.GetCurrentPart() == -1 || Theme.GetPartName(Theme.GetCurrentPart()) == "part: silence")
                    {
                        Theme.SetControl(GetPartName(0), 1f);
                    }
                    return;
                }

                int trk = Reactional.Playback.Playlist.GetTrackID();
                if (trk >= 0)
                {
                    Reactional.Playback.Playlist.CopyPrerollToTheme(trk);
                }

                int pindex = engine.FindParameter(trackID, "status");
                engine.SetParameterInt(trackID, pindex, 1);
                Setup.ReactionalLog($"Theme {ti.name} Started ");
            }

            /// <summary>
            /// Resets the current theme to its default state
            /// Flush scheduled events.
            /// </summary>
            public static void Reset()
            {
                int trackID = Reactional.Playback.Theme.GetThemeID();
                if (trackID < 0) return;
                ReactionalEngine.Instance.engine.ResetTrack(trackID);
                Setup.ReactionalLog($"Theme Reset");
                
            }

            /// <summary>
            /// Stops the current theme.
            /// </summary>
            public static void Stop(bool forceStop = false)
            {
                if (!Playlist.IsPlaying || forceStop)
                {
                    var trackID = GetThemeID();
                    if(trackID < 0) return;
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(trackID, "status");
                    engine.SetParameterInt(trackID, pindex, 0);
                    Setup.ReactionalLog("Theme stopped");
                }
                else
                {
                    Theme.SetControl("part: silence", 0f);
                }
            }
            
            /// <summary>
            /// Checks if a theme is playing.
            /// </summary>
            /// <returns>True if a theme is playing, false otherwise.</returns>
            public static bool IsPlaying
            {
                get
                {
                    if (!IsLoaded()) return false;
                    var themeID = Reactional.Playback.Theme.GetThemeID();
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(themeID, "status");
                    if ((int)engine.GetParameterInt(themeID, pindex) == 1)
                        return true;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Gets the state of the current theme.
            /// </summary>
            /// <returns>The state of the theme.</returns>
            private static int State
            {
                get
                {
                    if (!ReactionalEngine.Instance) return 0;
                    var trackID = Reactional.Playback.Theme.GetThemeID();
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(trackID, "status");
                    if (pindex < 0)
                        return 0;
                    else
                        return (int)engine.GetParameterInt(trackID, pindex);
                }
            }

            /// <summary>
            /// Gets the state of the current theme.
            /// </summary>
            /// <returns>The state of the theme.</returns>
            public static MusicSystem.PlaybackState GetState()
            {
                return (MusicSystem.PlaybackState)State;
            }

            /// <summary>
            /// Overrides the instrument settings for the theme.
            /// </summary>
            /// <param name="instrumentName">Name of the instrument.</param>
            /// <param name="pulseRate">Pulse rate for the instrument.</param>
            /// <param name="pitch">Pitch of the instrument.</param>
            /// <param name="velocity">Velocity of the instrument.</param>
            /// <param name="active">Whether the instrument is active.</param>
            /// <param name="legato">Legato value for the instrument.</param>
            public static void InstrumentOverride(string instrumentName, float pulseRate, float pitch, float velocity, bool active, float legato = 1f)
            {
                string name = "Playable Performer: " + instrumentName;

                var engine = ReactionalEngine.Instance.engine;
                int id = engine.GetTheme();
                if (id < 0)
                {
                    Setup.ReactionalLog("No loaded theme found.");
                    return;
                }
                int control = engine.FindControl(id, name);

                if (control < 0)
                {
                    Setup.ReactionalLog("Reactional Control not found: " + name);
                    return;
                }

                double[] values = new double[5];
                values[0] = active ? 1 : 0;
                values[1] = pulseRate;
                values[2] = pitch;
                values[3] = velocity;
                values[4] = legato;

                engine.SetControlValue(id, control, values);
            }

            /// <summary>
            /// Gets a list of overridable instruments in the theme.
            /// </summary>
            /// <returns>A list of overridable instruments.</returns>
            public static List<string> GetOverridableInstruments()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                List<string> ctrls = new List<string>();
                for (int i = 0; i < engine.GetNumControls(trackID); i++)
                {
                    string c = engine.GetControlName(trackID, i);
                    if (c.StartsWith("Playable Performer: "))
                    {
                        c = c.Substring(20);
                        ctrls.Add(c);
                    }
                }
                return ctrls;
            }

            /// <summary>
            /// Triggers a stinger in the theme.
            /// </summary>
            /// <param name="stingerName">The name of the stinger.</param>
            /// <param name="quant">The musically quantized value corresponding to when to trigger the stinger.</param>
            public static void TriggerStinger(string stingerName, MusicSystem.Quant quant = MusicSystem.Quant.Sixteenth)
            {
                var value = MusicSystem.QuantToFloat(quant) / 4;
                SetControl(stingerName, value);
            }

            /// <summary>
            /// Triggers a stinger in the theme with a specific quantization value.
            /// </summary>
            /// <param name="stingerName">The name of the stinger.</param>
            /// <param name="value">The musically quantized value corresponding to when to trigger the stinger.</param>
            public static void TriggerStinger(string stingerName, float value)
            {
                SetControl(stingerName, value);
            }

            /// <summary>
            /// Triggers a state change in the theme.
            /// </summary>
            /// <param name="stateName">The name of the state.</param>
            public static void TriggerState(string stateName)
            {
                SetControl(stateName);
            }
            
            /// <summary>
            /// Triggers a part change in the theme with a specific quantization value.
            /// </summary>
            /// <param name="partName">The name of the part.</param>
            /// <param name="quant">The musically quantized value corresponding to when to trigger the Part.</param>
            public static void TriggerPart(string partName, MusicSystem.Quant quant = MusicSystem.Quant.Sixteenth)
            {
                var value = MusicSystem.QuantToFloat(quant) / 4;
                SetControl("part: " + partName, value);
            }

            /// <summary>
            /// Triggers a part change in the theme with a specific quantization value.
            /// </summary>
            /// <param name="partName">The name of the part.</param>
            /// <param name="value">The musically quantized value corresponding to when to trigger the Part.</param>
            public static void TriggerPart(string partName, float value)
            {
                SetControl("part: " + partName, value);
            }

            /// <summary>
            /// Gets a dictionary of controls for the theme.
            /// </summary>
            /// <returns>A dictionary of controls with their values and types.</returns>
            public static Dictionary<string, (float controlValue, string controlType)> GetControls(bool istheme = true)
            {
                var ctrls = new Dictionary<string, (float controlValue, string controlType)>();

                var engine = ReactionalEngine.Instance.engine;
                if(engine == null) return ctrls;

                int id = istheme ? engine.GetTheme() : engine.GetTrack();
                if(id < 0) { return ctrls; }
                
                for (int i = 0; i < engine.GetNumControls(id); i++)
                {
                    var level = engine.GetControlLevel(id, i);
                    if (level != "custom") continue;
                    string controlName = engine.GetControlName(id, i);
                    float controlValue = (float)engine.GetControlValue(id, i);
                    string controlType = engine.GetControlType(id, i);
                    ctrls.Add(controlName, (controlValue, controlType));
                }
                return ctrls;
            }

            /// <summary>
            /// Gets the current beat of the theme.
            /// </summary>
            /// <returns>The current beat.</returns>
            public static float CurrentBeat()
            {
                return ReactionalEngine.Instance.CurrentBeat;
            }

            /// <summary>
            /// Gets the current tempo of the theme in BPM.
            /// </summary>
            /// <returns>The current tempo in BPM.</returns>
            public static float TempoBpm()
            {
                return ReactionalEngine.Instance.TempoBpm;
            }

            /// <summary>
            /// Sets a control value in the theme or track.
            /// </summary>
            /// <param name="controlName">The name of the control.</param>
            /// <param name="value">The value to set.</param>
            /// <param name="istheme">Whether to set the control for the theme or track.</param>
            public static void SetControl(string controlName, float value = 0.5f, bool istheme = true)
            {
                var engine = ReactionalEngine.Instance.engine;

                int t;
                if (istheme)
                    t = engine.GetTheme();
                else
                    return;

                int control = engine.FindControl(t, controlName);
                if (control < 0)
                {
                    Setup.ReactionalLog("Reactional Control not found: " + controlName, 0);
                    return;
                }

                value = Mathf.Clamp(value, 0f, 1f);
                engine.SetControlValue(t, control, value);
            }

            /// <summary>
            /// Gets a control value from the theme or track.
            /// </summary>
            /// <param name="controlName">The name of the control.</param>
            /// <param name="istheme">Whether to get the control from the theme or track.</param>
            /// <returns>The value of the control.</returns>
            public static double GetControl(string controlName, bool istheme = true)
            {
                var engine = ReactionalEngine.Instance.engine;

                int t;
                if (istheme)
                    t = engine.GetTheme();
                else
                    t = engine.GetTrack();

                int control = engine.FindControl(t, controlName);
                if (control < 0)
                {                    
                    return -1;
                }

                return engine.GetControlValue(t, control);
            }

            /// <summary>
            /// Gets or sets the volume of the theme.
            /// </summary>
            public static float Volume
            {
                get
                {
                    var eng = MusicSystem.GetEngine();
                    return (float)eng.GetParameterFloat(-1, eng.FindParameter(-1, "theme_gain"));
                }
                set
                {
                    var eng = MusicSystem.GetEngine();
                    int gain = eng.FindParameter(-1, "theme_gain");
                    if (gain < 0) return;
                    eng.SetParameterFloat(-1, gain, value);
                }
            }

            /// <summary>
            /// Gets the number of parts in the theme.
            /// </summary>
            /// <returns>The number of parts.</returns>
            public static int GetNumParts()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return engine.GetNumParts(trackID);
            }

            /// <summary>
            /// Gets the current part index in the theme.
            /// </summary>
            /// <returns>The current part index.</returns>
            public static int GetCurrentPart()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return engine.GetCurrentPart(trackID);
            }


            /// <summary>
            /// Gets the name of a specific part of the theme.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The name of the part.</returns>
            public static string GetPartName(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return engine.GetPartName(trackID, part);
            }

            /// <summary>
            /// Gets the offset of a specific part of the theme in seconds.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The offset of the part in seconds.</returns>
            public static float GetPartOffset(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return (float)(engine.GetPartOffset(trackID, part) / 1_000_000f);
            }

            /// <summary>
            /// Gets the duration of a specific part of the theme in seconds.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The duration of the part in seconds.</returns>
            public static float GetPartDuration(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return (float)(engine.GetPartDuration(trackID, part) / 1_000_000f);
            }

            /// <summary>
            /// Gets the number of bars in the theme.
            /// </summary>
            /// <returns>The number of bars.</returns>
            public static int GetNumBars()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return engine.GetNumBars(trackID);
            }

            /// <summary>
            /// Gets the current bar index in the theme.
            /// </summary>
            /// <returns>The current bar index.</returns>
            public static int GetCurrentBar()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                if(trackID < 0) return -1;
                return engine.GetCurrentBar(trackID);
            }

            /// <summary>
            /// Gets the offset of a specific bar in the theme in seconds.
            /// </summary>
            /// <param name="bar">The bar index.</param>
            /// <returns>The offset of the bar in seconds.</returns>
            public static float GetBarOffset(int bar)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return (float)(engine.GetBarOffset(trackID, bar) / 1_000_000f);
            }

            /// <summary>
            /// Gets the duration of a specific bar in the theme in seconds.
            /// </summary>
            /// <param name="bar">The bar index.</param>
            /// <returns>The duration of the bar in seconds.</returns>
            public static float GetBarDuration(int bar)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTheme();
                return (float)(engine.GetBarDuration(trackID, bar) / 1_000_000f);
            }
        }
        [DefaultExecutionOrder(550)]
        public static class Playlist
        {
            /// <summary>
            /// Gets the current beat of the playlist.
            /// </summary>
            /// <returns>The current beat.</returns>
            public static float CurrentBeat()
            {
                int trackID = Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(trackID, "current_beat");
                return (float)engine.GetParameterFloat(trackID, pindex) / 1_000_000f;
            }

            /// <summary>
            /// Gets the tempo of the playlist in BPM.
            /// </summary>
            /// <returns>The tempo in BPM.</returns>
            public static float TempoBpm()
            {
                int trackID = Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(trackID, "bpm");
                return (float)engine.GetParameterFloat(trackID, pindex);
            }

            /// <summary>
            /// Gets the ID of the current track.
            /// </summary>
            /// <returns>The track ID.</returns>
            public static int GetTrackID()
            {
                var track = ReactionalEngine.Instance.engine.GetTrack();
                return track;
            }

            /// <summary>
            /// Deprecated.
            /// Gets the metadata of the current track.
            /// </summary>
            /// <returns>The metadata of the track as a string.</returns>
            [Obsolete("Deprecated as of v.1.0.2, use GetCurrentTrackInfo")]
            public static string GetCurrentTrackMetadata()
            {
                var track = ReactionalEngine.Instance.engine.GetTrack();
                return GetTrackMetadata(track);
            }

            /// <summary>
            /// Gets the information of the current track.
            /// </summary>
            /// <returns>The <see cref="TrackInfo"/> object containing the details of the current track.</returns>
            public static TrackInfo GetCurrentTrackInfo()
            {
                var track = ReactionalEngine.Instance.engine.GetTrack();
                var ti = ReactionalManager.Instance._loadedTracks.Find(x => x.ID == track);
                return ti;
            }

            /// <summary>
            /// Gets the playlist index of the track currently selected/playing.
            /// </summary>
            /// <returns>The index of the selected track.</returns>
            public static int GetSelectedTrackIndex()
            {
                var track = ReactionalManager.Instance.selectedTrack;
                return track;
            }

            /// <summary>
            /// Plays a specified track.
            /// </summary>
            /// <param name="ti">The TrackInfo object of the track to play.</param>
            /// <param name="fadeintime">The fade-in time in seconds. Defaults to 0.</param>
            /// <param name="fadeouttime">The fade-out time in seconds. Defaults to 0.1.</param>
            public static void PlayTrack(TrackInfo ti, float fadeintime = 0, float fadeouttime = 0.1f)
            {
                var trackID = ti.ID;

                // ReactionalManager.Instance.selectedTrack = ReactionalManager.Instance._loadedTracks.IndexOf(ti);
                PlayTrackByID(trackID, fadeintime: fadeintime, fadeouttime: fadeouttime);
            }

            /// <summary>
            /// Plays a track by its index in the loaded tracks list.
            /// </summary>
            /// <param name="loadedTrackIndex">The index of the track to play.</param>
            /// <param name="fadeintime">The fade-in time in seconds. Defaults to 0.</param>
            /// <param name="fadeouttime">The fade-out time in seconds. Defaults to 0.1.</param>
            public static void PlayTrack(int loadedTrackIndex, float fadeintime = 0, float fadeouttime = 0.1f)
            {
                // ReactionalManager.Instance.selectedTrack = loadedTrackIndex;
                var trackID = ReactionalManager.Instance._loadedTracks[loadedTrackIndex].ID;
                PlayTrackByID(trackID, fadeintime: fadeintime, fadeouttime: fadeouttime);
            }

            /// <summary>
            /// Plays a track by its name.
            /// </summary>
            /// <param name="trackName">The name of the track to play.</param>
            /// <param name="fadeintime">The fade-in time in seconds. Defaults to 0.</param>
            /// <param name="fadeouttime">The fade-out time in seconds. Defaults to 0.1.</param>
            public static void PlayTrack(string trackName, float fadeintime = 0, float fadeouttime = 0.1f)
            {
                Reactional.Core.TrackInfo ti = ReactionalManager.Instance._loadedTracks.Find(x => x.name.Equals(trackName));
                if (ti == null)
                {
                    Setup.ReactionalLog("Track not found: " + trackName, LogLevel.Warning);
                    return;
                }
                var trackID = ti.ID;

                // ReactionalManager.Instance.selectedTrack = ReactionalManager.Instance._loadedTracks.IndexOf(ti);
                PlayTrackByID(trackID, fadeintime: fadeintime, fadeouttime: fadeouttime);
            }

            /// <summary>
            /// Plays a track by its ID asynchronously.
            /// </summary>
            /// <param name="trackID">The ID of the track to play.</param>
            /// <param name="fadeintime">The fade-in time in seconds.</param>
            /// <param name="fadeouttime">The fade-out time in seconds.</param>
            public static async void PlayTrackByID(int trackID, float fadeintime = 0, float fadeouttime = 0.1f)
            {
                if (Playlist.State == 1)
                {
                    await FadeOut(fadeouttime);
                }
                else
                {
                    await Task.Delay((int)(0.01f * 1000)); // safeguard if theme.play was called after track.play
                }
                bool themePaused = Theme.GetState() != MusicSystem.PlaybackState.Playing || Theme.GetPartName(Theme.GetCurrentPart()) == "part: silence";
                var currentPart = Theme.GetPartName(Theme.GetCurrentPart());
                if(ReactionalManager.Instance._loadedThemes.Count > 0)
                    Theme.Stop(true);

                Engine engine = ReactionalEngine.Instance.engine;

                Reactional.Setup.AllowPlay = false;
                engine.ResetTrack(trackID);
                engine.SetTrack(trackID);
                ReactionalManager.Instance.selectedTrack = ReactionalManager.Instance._loadedTracks.FindIndex(x => x.ID == trackID);

                var themeId = Theme.GetThemeID();
                if (themeId >= 0)
                {
                    engine.ResetTrack(themeId);
                    engine.SetTheme(themeId);
                    CopyPrerollToTheme(trackID);
                }
                Playlist.Start();

                if (themeId >= 0)
                {
                    bool stingerOnlyTheme = Theme.GetNumParts() == 1 && Theme.GetPartName(0) == "part: silence";
                    if (themePaused && !stingerOnlyTheme)
                        Theme.SetControl("part: silence", 1f);
                    else if (!stingerOnlyTheme)
                        Theme.SetControl(currentPart, 1f);
                    Theme.Play();
                }
                
                Reactional.Setup.AllowPlay = true;
                if (fadeintime > 0)
                    await FadeIn(fadeintime);

                engine.SetParameterInt(trackID, engine.FindParameter(trackID, "resample_quality"), 2);
            }

            /// <summary>
            /// Copies the preroll data to the theme.
            /// </summary>
            /// <param name="trackID">The ID of the track.</param>
            public static void CopyPrerollToTheme(int trackID)
            {
                Engine engine = ReactionalEngine.Instance.engine;
                int preroll = engine.FindParameter(trackID, "pre_roll");
                long prerolldata = engine.GetParameterInt(trackID, preroll);
                engine.SetParameterInt(Reactional.Playback.Theme.GetThemeID(), preroll, prerolldata);
            }

            /// <summary>
            /// Gets the metadata of a track.
            /// </summary>
            /// <param name="id">The track ID.</param>
            /// <returns>The metadata of the track as a string.</returns>
            [Obsolete("Deprecated as of v.1.0.2, use GetCurrentThemeInfo")]
            private static string GetTrackMetadata(int id)
            {
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(id, "metadata");
                return (string)engine.GetParameterValue(id, pindex);
            }

            /// <summary>
            /// Loads a streamed track asynchronously.
            /// </summary>
            /// <param name="ti">The track information.</param>
            private static async Task LoadStreamedTrack(TrackInfo ti)
            {
                Engine engine = ReactionalEngine.Instance.engine;
                int trackID = engine.AddTrackFromPath(Application.streamingAssetsPath + "/Reactional/" + ti.bundleID + "/" + ti.hash, null);
                engine.SetParameterBool(trackID, engine.FindParameter(trackID, "disk_streaming"), true);
                engine.SetParameterValue(trackID, engine.FindParameter(trackID, "asset_base_path"), Application.streamingAssetsPath + "/Reactional/" + ti.bundleID + "/");
                await AssetHelper.LoadTrackAssets(engine, trackID, Application.streamingAssetsPath + "/Reactional/" + ti.bundleID, streaming: true);
                engine.SetTrack(trackID);
            }

            /// <summary>
            /// Checks if a track is loaded.
            /// </summary>
            /// <returns>True if a track is loaded, false otherwise.</returns>
            public static bool IsLoaded()
            {
                if (!ReactionalEngine.Instance)
                    return false;
                var track = ReactionalEngine.Instance.engine.GetTrack();
                bool loaded = track >= 0;
                return loaded;
            }

            /// <summary>
            /// Plays the next track in the playlist.
            /// </summary>
            /// <param name="fadeouttime">The fade-out time in seconds.</param>
            /// <param name="fadeintime">The fade-in time in seconds.</param>
            public static void Next(float fadeouttime = 0.1f, float fadeintime = 0f)
            {
                if (!ReactionalEngine.Instance)
                    return;
                var rM = ReactionalManager.Instance;
                rM.selectedTrack = (rM.selectedTrack + 1) % (rM._loadedTracks.Count);
                int trackNum = rM.selectedTrack;
                Playback.Playlist.PlayTrack(trackNum, fadeouttime: fadeouttime, fadeintime: fadeintime);
            }

            /// <summary>
            /// Plays the previous track in the playlist.
            /// </summary>
            /// <param name="fadeouttime">The fade-out time in seconds.</param>
            /// <param name="fadeintime">The fade-in time in seconds.</param>
            public static void Prev(float fadeouttime = 0.1f, float fadeintime = 0f)
            {
                if (ReactionalEngine.Instance == null)
                    return;
                var rM = ReactionalManager.Instance;
                rM.selectedTrack = (rM.selectedTrack - 1) < 0 ? rM._loadedTracks.Count - 1 : rM.selectedTrack - 1;
                int trackNum = rM.selectedTrack;
                Playback.Playlist.PlayTrack(trackNum, fadeouttime: fadeouttime, fadeintime: fadeintime);
            }

            /// <summary>
            /// Plays a random track from the playlist.
            /// </summary>
            /// <param name="fadeouttime">The fade-out time in seconds.</param>
            /// <param name="fadeintime">The fade-in time in seconds.</param>
            public static void Random(float fadeouttime = 0.1f, float fadeintime = 0f)
            {
                if (!ReactionalEngine.Instance)
                    return;
                var rM = ReactionalManager.Instance;
                rM.selectedTrack = UnityEngine.Random.Range(0, rM._loadedTracks.Count);
                int trackNum = rM.selectedTrack;
                Playback.Playlist.PlayTrack(trackNum, fadeouttime: fadeouttime, fadeintime: fadeintime);
            }

            /// <summary>
            /// Skips to the closest bar offset based on a percentage of the track length.
            /// </summary>
            /// <param name="percentage"> The percentage of the track length to skip to (0-100).</param>
            public static void SkipToPercentage(float percentage)
            {
                if (!IsLoaded()) return;
                var trackID = Reactional.Playback.Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var trackInfo = Reactional.Playback.Playlist.GetCurrentTrackInfo();

                float length = trackInfo.duration;
                float offset = length * percentage / 100f;
                int target = 0;
                var numBars = Reactional.Playback.Playlist.GetNumBars();
                for (int bar = 0; bar < numBars; bar++)
                {
                    if (bar < 0) continue;
                    var barOffset = Reactional.Playback.Playlist.GetBarOffset(bar) * 1_000_000f; // Convert to microbeats            
                    if (barOffset <= offset && barOffset + Reactional.Playback.Playlist.GetBarDuration(bar) * 1_000_000f > offset)
                    {
                        target = bar;
                        break;
                    }
                }
                SkipToBar(target);
            }

            /// <summary>
            /// Skips to a specific bar in the track.
            /// </summary>
            public static void SkipToBar(int bar)
            {
                if (!IsLoaded()) return;
                var trackID = Reactional.Playback.Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var offset = Reactional.Playback.Playlist.GetBarOffset(bar) * 1_000_000f; // Convert to microbeats
                engine.SetParameterInt(-1, engine.FindParameter(-1, "skip"), (int)offset);
            }

            /// <summary>
            /// Skips to a specific part in the track if available.
            /// </summary>
            public static void SkipToPart(int part)
            {
                if (!IsLoaded()) return;
                var trackID = Reactional.Playback.Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var offset = Reactional.Playback.Playlist.GetPartOffset(part) * 1_000_000f; // Convert to microbeats
                engine.SetParameterInt(-1, engine.FindParameter(-1, "skip"), (int)offset);
            }


            /// <summary>
            /// Gets the state of the current track.
            /// </summary>
            /// <returns>The state of the track.</returns>
            private static int State
            {
                get
                {
                    if (!IsLoaded()) return 0;
                    var trackID = Reactional.Playback.Playlist.GetTrackID();
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(trackID, "status");
                    return (int)engine.GetParameterInt(trackID, pindex);
                }
            }

            /// <summary>
            /// Gets the playback state of the current track.
            /// </summary>
            /// <returns>The playback state of the track.</returns>
            public static MusicSystem.PlaybackState GetState()
            {
                if (!IsLoaded()) return MusicSystem.PlaybackState.Stopped;
                var trackID = Reactional.Playback.Playlist.GetTrackID();
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(trackID, "status");
                return (MusicSystem.PlaybackState)engine.GetParameterInt(trackID, pindex);
            }

            /// <summary>
            /// Checks if a track is playing.
            /// </summary>
            /// <returns>True if a track is playing, false otherwise.</returns>
            public static bool IsPlaying
            {
                get
                {
                    if (!IsLoaded()) return false;
                    var trackID = Reactional.Playback.Playlist.GetTrackID();
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(trackID, "status");
                    if ((int)engine.GetParameterInt(trackID, pindex) == 1)
                        return true;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Stops the current track with an optional fade-out time.
            /// </summary>
            /// <param name="fadeout">The fade-out time in seconds.</param>
            public static void Stop(float fadeout = 0)
            {
                if (fadeout > 0)
                {
                    _ = FadeOut(fadeout);
                }
                else
                {
                    var trackID = Reactional.Playback.Playlist.GetTrackID();
                    var engine = ReactionalEngine.Instance.engine;
                    var pindex = engine.FindParameter(trackID, "status");
                    engine.SetParameterInt(trackID, pindex, 0);
                }
            }

            /// <summary>
            /// Fades out the current track.
            /// </summary>
            /// <param name="fadeout">The fade-out time in seconds.</param>
            private static async Task FadeOut(float fadeout)
            {
                if (!ReactionalEngine.Instance)
                    return;
                ReactionalEngine.Instance.engine.TrackFade(0f, 0, (long)(fadeout * 1000000), true);

                while (Playback.Playlist.State == 1) await Task.Delay(100);
            }

            /// <summary>
            /// Fades in the current track.
            /// </summary>
            /// <param name="fadein">The fade-in time in seconds.</param>
            private static async Task FadeIn(float fadein)
            {
                if (!ReactionalEngine.Instance)
                    return;
                float amp = (float)ReactionalEngine.Instance.engine.GetTrackChannelAmp(0);
                ReactionalEngine.Instance.engine.TrackFade(0f, 0, 1);
                await Task.Delay(100);
                ReactionalEngine.Instance.engine.TrackFade(amp, 1, (long)(fadein * 1000000));
            }

            /// <summary>
            /// Starts playing the current track.
            /// </summary>
            private static void Start()
            {
                Engine engine = ReactionalEngine.Instance.engine;
                int trackID = Reactional.Playback.Playlist.GetTrackID();
                var ti = Reactional.Playback.Playlist.GetCurrentTrackInfo();
                if (trackID < 0)
                {
                    trackID = ReactionalManager.Instance._loadedTracks[0].ID;
                    engine.SetTrack(trackID);
                }

                

                if (trackID >= 0)
                {
                    int pindex = engine.FindParameter(trackID, "status");
                    engine.SetParameterInt(trackID, pindex, 1);
                }
                Setup.ReactionalLog($"Track {ti.name} started");
               
            }

            /// <summary>
            /// Plays the current track.
            /// </summary>
            public static void Play()
            {
                Engine engine = ReactionalEngine.Instance.engine;

                int trackID = Reactional.Playback.Playlist.GetTrackID();

                if (trackID < 0 && ReactionalManager.Instance._loadedTracks.Count > 0)
                {
                    trackID = ReactionalManager.Instance._loadedTracks[0].ID;
                }

                if (trackID < 0)
                {
                    Setup.ReactionalLog("No track loaded", LogLevel.Warning);
                    return;
                }

                if (Reactional.Playback.Playlist.State == 1)
                {
                    Reactional.Playback.Playlist.Stop();
                    PlayTrackByID(trackID, 0f, 0.5f);
                }
                else
                {
                    PlayTrackByID(trackID, 0f, 0f);
                }
            }

            /// <summary>
            /// Gets or sets the volume of the playlist.
            /// </summary>
            public static float Volume
            {
                get
                {
                    var eng = MusicSystem.GetEngine();
                    return (float)eng.GetParameterFloat(-1, eng.FindParameter(-1, "track_gain"));
                }
                set
                {
                    var eng = MusicSystem.GetEngine();
                    int gain = eng.FindParameter(-1, "track_gain");
                    if (gain < 0) return;
                    eng.SetParameterFloat(-1, gain, value);
                }
            }

            /// <summary>
            /// Gets the number of parts in the current track.
            /// </summary>
            /// <returns>The number of parts.</returns>
            private static int GetNumParts()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return engine.GetNumParts(trackID);
            }

            /// <summary>
            /// Gets the current part index in the current track.
            /// </summary>
            /// <returns>The current part index.</returns>
            private static int GetCurrentPart()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return engine.GetCurrentPart(trackID);
            }

            /// <summary>
            /// Gets the name of a specific part of the current track.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The name of the part.</returns>
            private static string GetPartName(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return engine.GetPartName(trackID, part);
            }

            /// <summary>
            /// Gets the offset of a specific part of the current track in seconds.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The offset of the part in seconds.</returns>
            private static float GetPartOffset(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return (float)(engine.GetPartOffset(trackID, part) / 1_000_000f);
            }

            /// <summary>
            /// Gets the duration of a specific part of the current track in seconds.
            /// </summary>
            /// <param name="part">The part index.</param>
            /// <returns>The duration of the part in seconds.</returns>
            private static float GetPartDuration(int part)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return (float)(engine.GetPartDuration(trackID, part) / 1_000_000f);
            }

            /// <summary>
            /// Gets the number of bars in the current track.
            /// </summary>
            /// <returns>The number of bars.</returns>
            public static int GetNumBars()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return engine.GetNumBars(trackID);
            }

            /// <summary>
            /// Gets the current bar index in the current track.
            /// </summary>
            /// <returns>The current bar index.</returns>
            public static int GetCurrentBar()
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                if(trackID < 0) return -1;
                return engine.GetCurrentBar(trackID);
            }

            /// <summary>
            /// Gets the offset of a specific bar in the current track in seconds.
            /// </summary>
            /// <param name="bar">The bar index.</param>
            /// <returns>The offset of the bar in seconds.</returns>
            public static float GetBarOffset(int bar)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return (float)(engine.GetBarOffset(trackID, bar) / 1_000_000f);
            }

            /// <summary>
            /// Gets the duration of a specific bar in the current track in seconds.
            /// </summary>
            /// <param name="bar">The bar index.</param>
            /// <returns>The duration of the bar in seconds.</returns>
            public static float GetBarDuration(int bar)
            {
                var engine = ReactionalEngine.Instance.engine;
                int trackID = engine.GetTrack();
                return (float)(engine.GetBarDuration(trackID, bar) / 1_000_000f);
            }
        }

        public static class MusicSystem
        {
            /// <summary>
            /// Schedules audio playback at a specified quantized time.
            /// </summary>
            /// <param name="audioSource">The audio source to play.</param>
            /// <param name="quant">The quantization value.</param>
            /// <param name="timeOffset">The time offset in seconds.</param>
            public static void ScheduleAudio(AudioSource audioSource, float quant = 1, float timeOffset = 0)
            {
                var time = GetRealTimeToBeat(quant);
                audioSource.PlayScheduled(AudioSettings.dspTime + time + timeOffset);
            }
            
            /// <summary>
            /// Binds a function to a musically reoccurring event with their corresponding musically quantized value.
            /// </summary>
            /// <example>
            /// Using method:
            /// <code>
            /// SubscribeToQuantizationEvent(Quant.Whole, MethodName);
            /// </code>
            /// Using lambda:
            /// <code>
            /// SubscribeToQuantizationEvent(Quant.Half, () => {
            ///     /* logic */
            /// });
            /// </code>
            /// </example>
            /// <param name="Quant">The quantization value you want to subscribe to.</param>
            /// <param name="Func">The function or lambda that will execute when the quantization events are fired from the engine.</param>
            public static void SubscribeToQuantizationEvent(Quant Quant, ReactionalEngine.QuantizationEvent Func)
            {
                var engine = ReactionalEngine.Instance;
                if (!engine)
                {
                    Setup.ReactionalLog("Engine instance not yet set up, ensure creation before calling SubscribeToQuantizationEvent.", 0);
                    return;
                }
                ref var quantization_event_data = ref engine.quantization_events[(int)Quant];
                quantization_event_data.Event += Func;
            }
            
            /// <summary>
            /// Unbinds a function previously bound by the SubscribeToQuantizationEvent method.
            /// </summary>
            /// <param name="Quant">The quantization value you want to unsubscribe to.</param>
            /// <param name="Func">The function to be removed from the event dispatch.</param>
            public static void UnsubscribeToQuantizationEvent(Quant Quant, ReactionalEngine.QuantizationEvent Func)
            {
                var engine = ReactionalEngine.Instance;
                ref var quantization_event_data = ref engine.quantization_events[(int)Quant];
                quantization_event_data.Event -= Func;
            }

            /// <summary>
            /// Quantization values for musical timing.
            /// </summary>
            public enum Quant
            {
                None,
                
                Whole,
                Half,
                Quarter,
                Eighth,
                Sixteenth,
	
                DottedWhole,
                DottedHalf,
                DottedQuarter,
                DottedEighth,
                DottedSixteenth,

                WholeTriplet,
                HalfTriplet,
                QuarterTriplet,
                EighthTriplet,
                SixteenthTriplet,
            };
            
            /// <summary>
            /// Converts Quant enum values to their corresponding value as a double.
            /// </summary>
            public static double QuantToDouble(Quant Quant)
            {
                switch (Quant) {
                    case Quant.Whole:            { return 4d;     }
                    case Quant.Half:             { return 2d;     }
                    case Quant.Quarter:			 { return 1d;	  }
                    case Quant.Eighth:			 { return 0.5d;	  }
                    case Quant.Sixteenth:		 { return 0.25d;  }
	
                    case Quant.DottedWhole:		 { return 6d;	  } 
                    case Quant.DottedHalf:		 { return 3d;	  } 
                    case Quant.DottedQuarter:	 { return 1.5d;	  }
                    case Quant.DottedEighth:	 { return 0.75d;  }
                    case Quant.DottedSixteenth:  { return 0.375d; }

                    case Quant.WholeTriplet:	 { return 5.33333333333d; }
                    case Quant.HalfTriplet:		 { return 2.66666666666d; }
                    case Quant.QuarterTriplet:	 { return 1.33333333333d; }
                    case Quant.EighthTriplet:	 { return 0.66666666666d; }
                    case Quant.SixteenthTriplet: { return 0.33333333333d; }
	
                    case Quant.None: default: { return -1; }
                }
            }

            /// <summary>
            /// Converts Quant enum values to their corresponding value as a float.
            /// </summary>
            public static float QuantToFloat(Quant Quant)
            {
                return (float)QuantToDouble(Quant);
            }

            /// <summary>
            /// Playback states for the music system.
            /// </summary>
            public enum PlaybackState
            {
                Stopped = 0,
                Playing = 1,
                Paused  = 2,
            }

            /// <summary>
            /// Gets or sets the playback permission.
            /// </summary>
            public static bool PlaybackAllowed
            {
                get => Reactional.Setup.AllowPlay;
                set => Reactional.Setup.AllowPlay = value;
            }

            /// <summary>
            /// Gets the engine instance.
            /// </summary>
            /// <returns>The engine instance.</returns>
            public static Engine GetEngine()
            {
                return ReactionalEngine.Instance.engine;
            }

            /// <summary>
            /// Get the ammount of beats left until the next quantized beat.
            /// Same as doing GetNextBeat(quant) - GetCurrentBeat()
            /// </summary>
            /// <param name="quant">The next beat multiple to find.</param>
            /// <param name="offset">A shift for when you don't want rounded beats.</param>
            /// <param name="theme"></param>
            /// <returns>The number of beats left until next quant</returns>
            public static float GetTimeToBeat(float quant, float offset = 0, bool theme = true)
            {
                int trackID = theme ? Playback.Theme.GetThemeID() : Playback.Playlist.GetTrackID();

                long nextQuant = GetEngine().GetNextQuantBeat(trackID, (long)(quant * 1_000_000), (long)(offset * 1_000_000));
                double currBeat = GetCurrentMicroBeat();

                return (float)(nextQuant - currBeat) / 1_000_000f;
            }

            /// <summary>
            /// Gets the real-time to the next quantized beat.
            /// </summary>
            /// <param name="quant">The quantization value.</param>
            /// <returns>The time in seconds to the next beat.</returns>
            public static float GetRealTimeToBeat(float quant)
            {
                return BeatsToSeconds(GetTimeToBeat(quant));
            }
            
            /// <summary>
            /// Converts seconds to beats based on the current tempo.
            /// </summary>
            /// <param name="seconds"></param>
            /// <returns></returns>
            public static float SecondsToBeats(float seconds)
            {
                return (GetTempoBpm() / 60f) * seconds;
            }

            /// <summary>
            /// Get the beat position of the next quantized beat.
            /// i.e. if current beat is 5.32, and quant=4 is supplied, it will return beat 8.
            /// </summary>
            /// <param name="quant">The next beat multiple to find.</param>
            /// <param name="offset">A shift for when you don't want rounded beats.</param>
            /// <param name="theme"></param>
            /// <returns>A beat value in the future.</returns>
            public static float GetNextBeat(float quant, float offset = 0)
            {
                var id = Theme.GetThemeID() < 0 ? Playlist.GetTrackID() : Theme.GetThemeID();
                if (id < 0) { return -1; }
                
                long nextQuant = GetEngine().GetNextQuantBeat(id, (long)(quant * 1_000_000), (long)(offset * 1_000_000));
                return nextQuant / 1_000_000f;
            }

            /// <summary>
            /// Get the beat position of the next quantized beat in microseconds.
            /// Get the beat position of the next quantized beat in microbeats, i.e. 1/1.000.000th of a beat.
            /// </summary>
            /// <param name="quant"></param>
            /// <param name="offset"></param>
            /// <param name="theme"></param>
            /// <returns>A microbeat value in the future.</returns>
            public static long GetNextBeatAbsolute(float quant, float offset = 0)
            {
                int trackID = Theme.GetThemeID() < 0 ? Playlist.GetTrackID() : Theme.GetThemeID();

                long nextQuant = GetEngine().GetNextQuantBeat(trackID, (long)(quant * 1_000_000), (long)(offset * 1_000_000));
                return nextQuant;
            }

            /// <summary>
            /// Gets the current beat position.
            /// </summary>
            /// <returns>The current beat position.</returns>
            public static float GetCurrentBeat()
            {
                if (!ReactionalEngine.Instance) return 0f;
                return ReactionalEngine.Instance.CurrentBeat;
            }

            /// <summary>
            /// Gets the current beat position in microbeats.
            /// </summary>
            /// <returns>The current beat position in microbeats.</returns>
            public static double GetCurrentMicroBeat()
            {
                if (PlaybackAllowed == false) return 0f;
                int trackID = Theme.GetThemeID() == -1
                    ? Playlist.GetTrackID()
                    : Theme.GetThemeID();
                if (trackID < 0) return -1f;
                var engine = ReactionalEngine.Instance.engine;
                var pindex = engine.FindParameter(trackID, "current_beat");
                return engine.GetParameterFloat(trackID, pindex);
            }

            /// <summary>
            /// Gets the tempo in beats per minute (BPM).
            /// </summary>
            /// <returns>The tempo in BPM.</returns>
            public static float GetTempoBpm()
            {
                if (PlaybackAllowed == false) return 0f;
                return ReactionalEngine.Instance.TempoBpm;
            }
        
           /// <summary>
           /// Gets the current musical scale or chord.
           /// </summary>
           /// <typeparam name="T"></typeparam>
           /// <code>
           /// Reactional.Playback.MusicSystem.GetCurrentScale&lt;string&gt;()
           /// Reactional.Playback.MusicSystem.GetCurrentScale&lt;float[]&gt;()
           /// </code>
           /// <returns>The current scale as an float[] or string</returns>
           /// <exception cref="Exception"></exception>
            public static T GetCurrentScale<T>()
            {
                return typeof(T) switch
                {
                    { } t when t == typeof(string) => ((T)(object)string.Join(", ", ReactionalEngine.Instance.CurrentScale)),
                    { } t when t == typeof(float[]) => ((T)(object)ReactionalEngine.Instance.CurrentScale),
                    _ => throw new Exception($"Unsupported response type: {typeof(T)}")
                };
            }
           
            /// <summary> 
            /// Gets the current root note of the current scale or chord.
            /// </summary> 
            /// <returns>The current root note as an integer.</returns>
            public static int GetCurrentRoot()
            {
                return ReactionalEngine.Instance.CurrentRootNote;
            }

            /// <summary>
            /// Converts beats to seconds based on the current tempo.
            /// </summary>
            /// <param name="beats">The number of beats to convert.</param>
            /// <returns>The equivalent time in seconds.</returns>
            public static float BeatsToSeconds(float beats)
            {
                return (60f / GetTempoBpm()) * beats;
            }

            /// <summary>
            /// Coroutine to duck the music volume.
            /// </summary>
            /// <param name="amp">The target amplitude for ducking.</param>
            /// <returns>An IEnumerator for the coroutine.</returns>
            public static IEnumerator DuckMusic(float amp)
            {
                float startVolume = ReactionalManager.Instance.trackGain;
                var FadeTime = 0.75f;

                if (amp < 1 && !ReactionalManager.Instance.isDucked)
                {
                    ReactionalManager.Instance.isDucked = true;
                    while (Reactional.Playback.Playlist.Volume > amp && ReactionalManager.Instance.isDucked)
                    {
                        Reactional.Playback.Playlist.Volume -= startVolume * Time.deltaTime / FadeTime;

                        yield return null;
                    }
                }
                else if (Mathf.Approximately(amp, 1) && ReactionalManager.Instance.isDucked)
                {
                    ReactionalManager.Instance.isDucked = false;
                    while (Reactional.Playback.Playlist.Volume < startVolume && !ReactionalManager.Instance.isDucked)
                    {
                        Reactional.Playback.Playlist.Volume += startVolume * Time.deltaTime / FadeTime;

                        yield return null;
                    }
                }
            }

            /// <summary>
            /// Toggle pause of system playback.
            /// </summary>
            public static void TogglePause()
            {
                Setup.AllowPlay = !Setup.AllowPlay;
            }

            /// <summary>
            /// Custom yield instruction that waits for the next beat.
            /// </summary>
            public class WaitForNextBeat : CustomYieldInstruction
            {
                float beat;
                float quantBeat;
                float offset;

                /// <summary>
                /// Determines whether to keep waiting.
                /// </summary>
                public override bool keepWaiting
                {
                    get
                    {
                        double mb = Reactional.Playback.MusicSystem.GetCurrentMicroBeat();
                        if ((quantBeat - mb) > (beat + 1)) quantBeat = GetNextBeatAbsolute(beat) + (offset * 1_000_000f);
                        bool state = (mb < quantBeat);
                        return state;
                    }
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="WaitForNextBeat"/> class.
                /// </summary>
                /// <param name="beat">The beat duration to wait for.</param>
                /// <param name="offset">The offset in beats.</param>
                public WaitForNextBeat(float beat, float offset = 0f)
                {
                    this.beat = beat;
                    this.offset = offset;
                    this.quantBeat = GetNextBeatAbsolute(beat) + (offset * 1_000_000f);
                }
            }
        }
    }
    namespace Services
    {
        public static class Remote
        {
            [Serializable]
            public class TrackDownloadData
            {
                public byte[] metadata;
                public byte[] trackData;

                public TrackDownloadData(byte[] metadata, byte[] trackData)
                {
                    this.metadata = metadata;
                    this.trackData = trackData;
                }
            }
            
            /// <summary>
            /// Initializes the client to the backend, creating new client id and secret if not already registered.
            /// </summary>
            public static async Task<(string id, string secret)> Initialize(string clientId = "", string clientSecret = "")
            {
                return await Backend.Initialize(clientId, clientSecret);
            }

            /// <summary>
            /// Fetches playlists from the backend service.
            /// </summary>
            /// <param name="projectId"></param>
            /// <param name="limit"></param>
            /// <param name="offset"></param>
            /// <param name="countryCode"></param>
            /// <returns> 
            ///     A <see cref="Backend.PlaylistsPage"/> object containing the fetched playlists.
            ///     Returns null if the backend is not initialized.
            ///  </returns>
            public static async Task<Backend.PlaylistsPage> GetPlaylists(string projectId, int limit = 10, int offset = 0, string countryCode = null)
            {
                if (!Backend.IsInitialized)
                {
                    Setup.ReactionalLog("Backend not initialized, ensure initialization before trying to retrieve playlists.");
                    return null;
                }
                var url = $"/dev/projects/{projectId}/playlists?offset={offset}&limit={limit}";
                var page = await Backend.FetchPlaylistPage(url, countryCode);
                return page;
            }
            
            /// <summary>
            /// Fetches a specific playlist by its ID from the backend service.
            /// </summary>
            /// <param name="playlistId"></param>¨
            /// <param name="countryCode"></param>¨
            /// <returns>
            ///     A <see cref="Backend.PlaylistPayload"/> object containing the playlist data.
            ///     Returns null if the backend is not initialized.
            /// </returns>
            public static async Task<Backend.PlaylistPayload> GetPlaylist(string playlistId, string countryCode = null)
            {
                if (!Backend.IsInitialized)
                {
                    Setup.ReactionalLog("Backend not initialized, ensure initialization before trying to retrieve playlists.");
                    return null;
                }

                var playlist = await Backend.FetchPlaylist(playlistId, null, countryCode);
                return playlist;
            }

            /// <summary>
            /// Gets the info of a track to display in ex. a store page for the user prior to track download.
            /// </summary>
            /// <param name="trackId">Backend ID for track</param>
            /// <param name="countryCode">When not providing a users `countryCode` only tracks with worldwide access will be parsed.</param>
            /// <returns>TrackPayload</returns>
            public static async Task<Backend.TrackPayload> GetTrackMetadata(string trackId, string countryCode = null)
            {
                return await Backend.FetchTrackPayload(trackId, countryCode, false);
            }

            /// <summary>
            /// Gets the info of requested tracks to display in ex. a store page for the user prior to track download.
            /// </summary>
            /// <param name="trackIds">Backend ID's for tracks</param>
            /// <param name="countryCode">When not providing a users `countryCode` only tracks with worldwide access will be parsed.</param>
            /// <returns>TrackPayload[]</returns>
            public static async Task<Backend.TrackPayload[]> GetTracksMetadata(string[] trackIds, string countryCode = null)
            {
                return await Backend.FetchTracksPayload(trackIds, countryCode, false);
            }

            /// <summary>
            /// Gets the track data used for playback.
            /// </summary>
            /// <param name="track">Payload received from GetTrack/sMetadata</param>
            /// <param name="countryCode">When not providing a users `countryCode` only tracks with worldwide access will be parsed.</param>
            /// <returns>TrackDownloadData</returns>
            public static async Task<TrackDownloadData> GetTrack(Backend.TrackPayload track, string countryCode = null)
            {
                if (track.id == null)
                {
                    Setup.ReactionalLog("Cannot find tracks to download", LogLevel.Error);
                    return null;
                }
                var md = await Backend.Retry(
                    operation: () => Backend.FetchTrackMetadata(track.id, countryCode),
                    maxRetries: 3,
                    initialDelay: 1000
                );
                var td = await Backend.Retry(
                    operation: () => Backend.FetchTrackData(track.id, countryCode),
                    maxRetries: 3,
                    initialDelay: 2000 // Longer delay for audio data as it's typically larger
                );
                var data = new TrackDownloadData(md, td);
                Setup.ReactionalLog($"Successfully downloaded track: {track.title}");
                return data;
            }

            /// <summary>
            /// Fetches all of a client's previous purchases as an array of `EntitlementsPayload`.
            /// </summary>
            /// <returns>EntitlementsPayload[]</returns>
            public static async Task<Backend.EntitlementsPayload[]> GetClientEntitlements()
            {
                if (!Backend.IsInitialized)
                {
                    Setup.ReactionalLog("Backend not initialized, ensure initialization before trying to retrieve entitlements.");
                    return null;
                }
                var entitlements = await Backend.FetchClientEntitlements();
                return entitlements;
            }
        }

        public static class Preview
        {
            /// <summary>
            /// Fetches album artwork from given `url`.
            /// </summary>
            public static async Task<Texture2D> DownloadAlbumArtwork(string url, string fallbackIcon = "ReactionalIcon", int timeoutInSeconds = 20)
            {
                var req = UnityWebRequestTexture.GetTexture(url);
                var op = req.SendWebRequest();
                float timer = 0;
                while (!op.isDone)
                {
                    timer += Time.deltaTime;
                    if (timer > timeoutInSeconds)
                    {
                        Debug.LogError("DownloadImage: Timeout");
                        return Resources.Load<Texture2D>(fallbackIcon);
                    }
                    await Task.Yield();
                }
                if (req.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"DownloadImage Error: {req.error}");
                    return Resources.Load<Texture2D>(fallbackIcon);
                }
                return DownloadHandlerTexture.GetContent(req);
            }

            /// <summary>
            /// Starts playback of AAC audio file from the web.<br/>
            /// Note: Does not work on desktop platforms due to Unity AudioSource format limitations.
            /// </summary>
            /// <warning></warning>
            /// <param name="audioSource">Source to use for audio playback.</param>
            /// <param name="url">URL to the specified audio file.</param>
            /// <returns></returns>
            public static IEnumerator PreviewAudio(AudioSource audioSource, string url)
            {
                using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.ACC);
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.ConnectionError)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                    yield break;
                }
                Debug.Log(www.error);
            }
        }
    }
}
