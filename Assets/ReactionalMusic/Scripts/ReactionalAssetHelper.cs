using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Reactional.Core
{
    internal class AssetHelper : MonoBehaviour
    {
        List<string> themes = new List<string>();
        List<string> tracks = new List<string>();

        // TODO: Make async?
        public static int AddTrackFromPath(Engine engine, string path)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);
                www.SendWebRequest();
                while (!www.isDone)
                {
                }
                return CheckTrackID(engine.AddTrackFromBytes(www.downloadHandler.data));
            }
            else
                return CheckTrackID(engine.AddTrackFromPath(path));
        }

        static Dictionary<string, object> ReadTrackMetadataFromPath(string path, string trackpath)
        {
            var trackPath = path + "/" + trackpath;
            string jsonText;
            if (Application.platform == RuntimePlatform.Android)
            {
                UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(trackPath);
                www.SendWebRequest();
                while (!www.isDone)
                {
                }

                jsonText = Reactional.Core.Engine.GetTrackMetadata(www.downloadHandler.data);
            }
            else
            {
                byte[] data = File.ReadAllBytes(trackPath);
                jsonText = Reactional.Core.Engine.GetTrackMetadata(data);
            }
            return DeserializeTrack(jsonText);
        }
        
        public static Dictionary<string, object> ReadTrackMetadataFromData(byte[] data)
        {
            string jsonText = Reactional.Core.Engine.GetTrackMetadata(data);
            return DeserializeTrack(jsonText);
        }

        static Dictionary<string, object> DeserializeTrack(string jsonText) {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(jsonText))
            {
                var meta = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
                
                dict.Add("name",       meta.ContainsKey("title")      ? meta["title"]?.ToString()      : " ");
                dict.Add("artists",    meta.ContainsKey("artists")    ? meta["artists"]?.ToString()    : " ");
                dict.Add("title",      meta.ContainsKey("title")      ? meta["title"]?.ToString()      : " ");
                dict.Add("album",      meta.ContainsKey("album")      ? meta["album"]?.ToString()      : " ");
                dict.Add("genre",      meta.ContainsKey("genre")      ? meta["genre"]?.ToString()      : " ");
                dict.Add("bpm",        meta.ContainsKey("bpm")        ? meta["bpm"]?.ToString()        : " ");
                dict.Add("time_signature", meta.ContainsKey("time_signature") ? meta["time_signature"] : new int[] { 4, 4 });
                dict.Add("duration",   meta.ContainsKey("duration")   ? meta["duration"]               : 0);
                dict.Add("time",       meta.ContainsKey("time")       ? meta["time"]                   : 0f);
                dict.Add("cover",      meta.ContainsKey("cover")      ? meta["cover"]?.ToString()      : " ");
                dict.Add("controls",   meta.ContainsKey("controls")   ? meta["controls"]               : null);
                dict.Add("performers", meta.ContainsKey("performers") ? meta["performers"]             : null);
            } else
                dict.Add("name", ""); // fallback

            return dict;
        }

        static int CheckTrackID(int id)
        {            
            if (id >= 0) return id;
            try
            {   
                throw new EngineErrorException(id);
            }
            catch (EngineErrorException e)
            {
                if (e.Error == -70)
                    Setup.ReactionalLog("Track timestamp has expired; please download a new version of the track. \nUpgrade your project for longer timestamp validity. License this track to remove timestamp restrictions.", LogLevel.Warning);
                else
                    Setup.ReactionalLog("Track validation failed: " + e.Message, LogLevel.Error);
            }
            return -1;
        }

        public static (List<Section> sections, string bundleName) ParseBundleFromPath(string path)
        {
            using var _ = ProfilerMarkers.k_ParseBundle.Auto();

            string jsonText;
            if (Application.platform == RuntimePlatform.Android)
            {
                UnityEngine.Networking.UnityWebRequest www =
                    UnityEngine.Networking.UnityWebRequest.Get(path + "/manifest.json");
                www.SendWebRequest();
                while (!www.isDone)
                {
                }

                jsonText = www.downloadHandler.text;
            }
            else
            {
                jsonText = File.ReadAllText(path + "/manifest.json");
            }

            List<ThemeInfo> themes = new List<ThemeInfo>();
            List<TrackInfo> tracks = new List<TrackInfo>();
            List<Section> sectionsList = new List<Section>();

            var json = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;

            Dictionary<string, object> sections = (Dictionary<string, object>)json["sections"];
            foreach (KeyValuePair<string, object> sectionPair in sections)
            {
                Section s = new Section();
                s.name = sectionPair.Key;
                Dictionary<string, object> sectionData = sectionPair.Value as Dictionary<string, object>;

                int.TryParse(sectionData.GetValueOrDefault("order", 0).ToString(), out int sectionOrder);
                s.order = sectionOrder;

                List<object> themes_list = sectionData["themes"] as List<object>;
                for (int i = 0; i < themes_list.Count; i++)
                {
                    string thm = themes_list[i] as string;

                    Dictionary<string, object> dict = ReadTrackMetadataFromPath(path, thm);
                    ThemeInfo ti = ParseTheme(dict, thm, path);
                    ti.bundleID = Path.GetFileName(path);
                    themes.Add(ti);
                    s.themes.Add(ti);
                }

                List<object> playlists = sectionData["playlists"] as List<object>;
                foreach (var playlistObject in playlists)
                {
                    Dictionary<string, object> playlist = playlistObject as Dictionary<string, object>;
                    Playlist pl = new Playlist();
                    string playlistName = playlist["name"] as string;
                    pl.name = playlistName;

                    List<object> tracks_list = playlist["tracks"] as List<object>;
                    for (int i = 0; i < tracks_list.Count; i++)
                    {
                        string trck = tracks_list[i] as string;

                        var dict = ReadTrackMetadataFromPath(path, trck);
                        TrackInfo ti = ParseTrack(dict, trck, path);
                        pl.tracks.Add(ti);
                    }

                    s.playlists.Add(pl);
                }

                sectionsList.Add(s);
            }

            sectionsList.Sort((a, b) => a.order.CompareTo(b.order));

            return (sectionsList, json.GetValueOrDefault("name", Path.GetFileName(path)).ToString());
        }

        internal static TrackInfo ParseTrack(Dictionary<string, object> trackDict, string hash, string bundlePath = "none")
        {
            var ti = new TrackInfo();
            ti.hash = hash;
            ti.ID = -1;
            ti.name = trackDict["name"]?.ToString() ?? "";
            ti.artist = trackDict["artists"]?.ToString() ?? "";
            ti.album = trackDict["album"]?.ToString() ?? "";
            ti.cover = trackDict["cover"]?.ToString() ?? "";
            ti.genre = trackDict["genre"]?.ToString() ?? "";
            ti.duration = int.Parse(trackDict["duration"]?.ToString() ?? "0");
            ti.time = float.Parse(trackDict["time"]?.ToString() ?? "0");
            ti.BPM = trackDict["bpm"]?.ToString() ?? "";
            if (trackDict["time_signature"] is List<object> list && list.Count == 2)
            {
                ti.timeSignature = new TimeSignature(Convert.ToInt32(list[0]), Convert.ToInt32(list[1]));
            }
            else
            {   
                ti.timeSignature = new TimeSignature(4, 4);
            }
            ti.bundleID = System.IO.Path.GetFileName(bundlePath);
            return ti;
        }

        internal static ThemeInfo ParseTheme(Dictionary<string, object> themeDict, string trck, string bundlePath = "none")
        {
            var theme = new ThemeInfo();
            theme.hash = trck;
            theme.ID = -1;
            theme.name = themeDict["name"]?.ToString() ?? "";
            theme.artist = themeDict["artists"]?.ToString() ?? "";
            theme.album = themeDict["album"]?.ToString() ?? "";
            theme.cover = themeDict["cover"]?.ToString() ?? "";
            theme.genre = themeDict["genre"]?.ToString() ?? "";
            theme.duration = int.Parse(themeDict["duration"]?.ToString() ?? "0");
            theme.time = float.Parse(themeDict["time"]?.ToString() ?? "0");
            theme.BPM = themeDict["bpm"]?.ToString() ?? "";
            theme.bundleID = Path.GetFileName(bundlePath);
            if (themeDict["controls"] is List<object> controlsList)
            {
                foreach (var controlObject in controlsList)
                {
                    if (controlObject is not Dictionary<string, object> control) continue;
                    
                    string name = control["name"]?.ToString() ?? "";
                    string type = control["type"]?.ToString() ?? "";
                    switch (type)
                    {
                        case "parameter":
                            theme.macros.Add(name);
                            break;
                        case "part":
                            name = name.Replace("part: ", "");
                            theme.parts.Add(name);
                            break;
                        case "stinger":
                            theme.stingers.Add(name);
                            break;
                        case "playable":
                            name = name.Replace("Playable Performer: ", "");
                            theme.overridableInstruments.Add(name);
                            break;
                    }
                }
            }
            if (themeDict["performers"] is List<object> performerList)
            {
                foreach (var performerObject in performerList)
                {
                    if (performerObject is not Dictionary<string, object> performer) continue;
                    Performer p = new Performer();
                    
                    string name = performer["name"]?.ToString() ?? "";
                    string lane = performer["lane_index"]?.ToString() ?? "";
                    List<object> sinks = performer["sink_indices"] as List<object> ?? new List<object>();
                    
                    p.name = name;
                    p.lane_index = Convert.ToInt32(lane);
                    foreach (var sink in sinks) {
                        p.sink_indices.Add(Convert.ToInt32(sink));
                    }
                    theme.performerRoutings.Add(p);
                }
            }
            return theme;
        }

        public static async Task LoadTrackAssets(Engine engine, int trackid, string projectPath, bool loadAsync = true, bool streaming = false)
        {            
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < engine.GetNumAssets(trackid); i++)
            {
                string id = engine.GetAssetId(trackid, i);
                string type = engine.GetAssetType(trackid, i);
                string uri = engine.GetAssetUri(trackid, i);

                if (loadAsync)
                    tasks.Add(Task.Run(() => SetAssetData(engine, trackid, id, uri, type, projectPath, streaming: streaming)));
                else
                    SetAssetData(engine, trackid, id, uri, type, projectPath, loadAsync: false, streaming: streaming);
            }

            await Task.WhenAll(tasks);
        }

        private static void SetAssetData(Engine engine, int trackid, string assetID, string uri, string type, string path, bool loadFromRemote = false, bool loadAsync = true, bool streaming = false)
        {
            path = path + "/" + uri;

            if (System.IO.File.Exists(uri)) path = uri;

            if (Application.platform == RuntimePlatform.Android)
                loadFromRemote = true;

            if (!loadFromRemote)
            {
                if (streaming)
                {                                    
                    engine.SetAssetPath(trackid, assetID, type);
                    return;
                }
                else
                {
                    byte[] data = File.ReadAllBytes(path);
                    if (data != null)
                    {
                        engine.SetAssetData(trackid, assetID, type, data, null);
                    }
                    data = null;
                }
            }
            else
            {
                if (Application.platform != RuntimePlatform.Android)
                {
                    path = "file://" + path;
                }
                path = path.Replace("#", "%23");

                ReactionalEngine.Instance.StartCoroutine(AsyncWebLoader(path, type, assetID, engine, trackid));
            }
        }

        private static IEnumerator AsyncWebLoader(string path, string type, string assetID, Engine engine, int trackid)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(path))
            {
                var operation = www.SendWebRequest();
                yield return operation;

                bool requestValid = true;
                
#if UNITY_2020_3_OR_NEWER
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
                if (!string.IsNullOrWhiteSpace(www.error))
#endif
                {
                    Setup.ReactionalLog(www.error);                    
                    requestValid = false;
                }

                if (requestValid)
                {
                    byte[] data = www.downloadHandler.data;
                    if (data != null)
                    {
                        engine.SetAssetData(trackid, assetID, type, data, null);
                    }
                    data = null;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetFirstOfType<T>() where T : UnityEngine.Object
        {
#if UNITY_6000_0_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }
    }
    /// <summary>
    /// Musical time signature: [numerator, denominator]. Example: [4,4] = 4/4 time"
    /// </summary>
    [Serializable]
    public struct TimeSignature
    {
        public TimeSignature(int numerator, int denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;   
        }
        public int numerator;
        public int denominator;

        public override string ToString() => $"{numerator.ToString()}/{denominator.ToString()}";
        public int this[int index] => index switch {
            0 => numerator,
            1 => denominator,
            _ => throw new IndexOutOfRangeException()
        };
    }
    
    /// <summary>
    /// Members of TrackInfo
    /// <list type="bullet">
    /// <item> public string name;</item>
    /// <item> public int ID;</item>
    /// <item> public string artist;</item>
    /// <item> public string album;</item>
    /// <item> public string genre;</item>
    /// <item> public string cover;</item>
    /// <item> public string BPM;</item>
    /// <item> public TimeSignature timeSignature</item>
    /// <item> public int duration;</item>
    /// <item> public float time;</item>
    /// <item> public string hash;</item>
    /// <item> public string bundleID;</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class TrackInfo
    {
        public string name;
        public int ID;
        [HideInInspector] public string artist;
        [HideInInspector] public string album;
        [HideInInspector] public string genre;
        [HideInInspector] public string cover;
        [HideInInspector] public string BPM;
        [HideInInspector] public TimeSignature timeSignature;
        [HideInInspector] public int duration;
        [HideInInspector] public float time;
        [HideInInspector] public string hash;
        [HideInInspector] public string bundleID;
    }
    
    /// <summary>
    /// Members of ThemeInfo
    /// <list type="bullet">
    /// <item> public string name;</item>
    /// <item> public int ID;</item>
    /// <item> public string artist;</item>
    /// <item> public string album;</item>
    /// <item> public string genre;</item>
    /// <item> public string cover;</item>
    /// <item> public string BPM;</item>
    /// <item> public int duration;</item>
    /// <item> public float time;</item>
    /// <item> public string hash;</item>
    /// <item> public string bundleID;</item>
    /// <item> public List&lt;string&gt; macros;</item>
    /// <item> public List&lt;string&gt; parts;</item>
    /// <item> public List&lt;string&gt; stingers;</item>
    /// <item> public List&lt;string&gt; overridableInstruments;</item>
    /// <item> public List&lt;Preformer&gt; preformerRoutings;</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class ThemeInfo : TrackInfo
    {
        public List<string> macros = new();
        public List<string> parts = new();
        public List<string> stingers = new();
        public List<string> overridableInstruments = new();
        public List<Performer> performerRoutings = new();
    }

    [Serializable]
    public class Performer
    {
        public string name;
        public int lane_index;
        public List<int> sink_indices = new();
    }
    
    /// <summary>
    /// Members of Bundle
    /// <list type="bullet">
    /// <item> public string name;</item>
    /// <item> public string path;</item>
    /// <item> public List&lt;Section&gt; sections;</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class Bundle
    {
        [HideInInspector] public string name;
        [HideInInspector] public string path;
        public List<Section> sections = new List<Section>();
    }
    
    /// <summary>
    /// Members of Section
    /// <list type="bullet">
    /// <item> public string name;</item>
    /// <item> public List&lt;TrackInfo&gt; themes;</item>
    /// <item> public List&lt;Playlist&gt; playlists;</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class Section
    {
        [HideInInspector] public string name;
        [HideInInspector] public int order;
        public List<ThemeInfo> themes = new List<ThemeInfo>();
        public List<Playlist> playlists = new List<Playlist>();
    }
    
    /// <summary>
    /// Members of PlayList
    /// <list type="bullet">
    /// <item> public string name;</item>
    /// <item> public List&lt;TrackInfo&gt; tracks;</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class Playlist
    {
        [HideInInspector] public string name;
        public List<TrackInfo> tracks = new List<TrackInfo>();
    }
}