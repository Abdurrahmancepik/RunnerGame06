using System;
using UnityEngine;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reactional.Core
{
    [DefaultExecutionOrder(5000)]
    public class BasicPlayback : MonoBehaviour
    {
        public enum UnloadOptions{UnloadTrack, UnloadTheme, UnloadAll}
        
        [Header("Autoplay Options")]
        [SerializeField] private bool _autoplayTheme;
        [SerializeField] private bool _autoplayTrack;

        [Header("Volume Controls")]
        [Range(0f, 1f)] public float _themeVolume = 1f;
        [Range(0f, 1f)] public float _playlistVolume = 0.6f;
        
        [Header("Unload Options (Editor Only)")]
        [SerializeField] private UnloadOptions _unloadOptions;
        [SerializeField] private string ThemeName;
        [SerializeField] private string TrackName;
        
        public void Start()
        {
            if (Reactional.Setup.IsValid)
            {
                Play();
            }
            else
            {
                Debug.LogWarning("Reactional is not setup correctly. Please check the setup guide.");
            }
        }

        private void Update()
        {
            Debug.Log("Artist: " + Reactional.Playback.Playlist.GetCurrentTrackInfo().artist);
            Debug.Log("Time: " + Reactional.Playback.Playlist.GetCurrentTrackInfo().time);
            Debug.Log("Duration: " + Reactional.Playback.Playlist.GetCurrentTrackInfo().duration);
        }

        private async void Play()
        {
            await Task.Delay(100);

            // Reactional.Setup.UpdateBundles();                                    // Check for new bundles in StreamingAssets

            // await Reactional.Setup.LoadBundles();                                // Load everything in StreamingAssets
            // await Reactional.Setup.LoadBundle("BundleName");                     // Load everything in a specific bundle

            // await Reactional.Setup.LoadSection("BundleName","Default");          // Load specific section in specific bundle
            await Reactional.Setup.LoadSection(); // Load "Default Section" from inspector, or first defined section in first bundle

            // await Reactional.Setup.LoadTheme("BundleName","Default","ThemeName");// Load specific theme in specific bundle
            // await Reactional.Setup.LoadTheme("ThemeName")                        // Find and load specifc theme in any bundle
            //await Reactional.Setup.LoadTheme();                                   // Load the first theme defined in first bundle

            // await Reactional.Setup.LoadPlaylist("BundleName","Default");         // Load specific playlist in specific bundle
            // await Reactional.Setup.LoadPlaylist("Default");                      // Find and load specifc playlist in any bundle
            // await Reactional.Setup.LoadPlaylist();                               // Load the first playlist defined in first bundle

            // await Reactional.Setup.LoadTrack("BundleName","TrackName");          // Load specific track in specific bundle

            if (_autoplayTheme)
                Reactional.Playback.Theme.Play();
            if (_autoplayTrack)
                Reactional.Playback.Playlist.Play();

            // Important to call this; otherwise there will be a samplerate mismatch; time will drift and music sound bad
            Reactional.Setup.InitAudio();

            // Optionally set volume of theme and playlist
            Reactional.Playback.Theme.Volume = _themeVolume;
            Reactional.Playback.Playlist.Volume = _playlistVolume;

            await Task.Delay(200);
            Reactional.Playback.MusicSystem.PlaybackAllowed = true;

            
        }


        /// <summary>
        /// Test unload your asset by Name
        /// </summary>
        /// <summary>
        /// Test unload your asset by Name
        /// </summary>
        private void Unload()
        {
            Debug.Log("Test Unloading assets by name");
            switch (_unloadOptions)
            {
                case UnloadOptions.UnloadTrack:
                    Reactional.Setup.UnloadTrack(TrackName);
                    break;
                case UnloadOptions.UnloadTheme:
                    Reactional.Setup.UnloadTrack(TrackName);
                    break;
                case UnloadOptions.UnloadAll:
                    Reactional.Setup.UnloadTrack(TrackName);
                    Reactional.Setup.UnloadTrack(TrackName);
                    break;
            }
        }
        
#if UNITY_EDITOR
        [CustomEditor(typeof(BasicPlayback))]
        private class _ : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                if (GUILayout.Button("Unload")) { ((BasicPlayback)target).Unload(); }
            }
        }
#endif
    }
}