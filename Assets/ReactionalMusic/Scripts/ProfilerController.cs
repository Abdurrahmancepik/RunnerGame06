using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace Reactional.Core
{
    
    
    /// <summary>
    /// Using Markers to check preformance in Reactionals API.
    /// Markers are predefined or can be used by creating a new marker via helper fucntion
    /// </summary>
    /// <para/>
    /// <para/> Usage:
    /// <list type="bullet">
    /// <item><description> Use the markers in the API </description></item>
    /// <item><description> Add them to the profile controller</description></item>
    /// </list>
    ///
    /// <code>
    /// void MyMethod()
    /// {
    ///     // Whole scope is profiled
    ///     using var _ = Profiler.k_AudioProcessing.Auto();
    /// }
    ///
    /// void MyMethod()
    /// {
    ///     // Part of scope is profiled
    ///     using (Profiler.k_AudioProcessing.Auto())
    ///     {
    ///     // Some code..
    ///     }
    /// }
    /// </code>

    public static class ProfilerMarkers
    {
        #region Reactional Engine Profilers

        // Audio Processing & Engine Operations
        public static readonly ProfilerMarker k_EngineUpdate = new ProfilerMarker("Reactional.Engine.Update");
        public static readonly ProfilerMarker k_AudioFilterRead = new ProfilerMarker("Reactional.Audio.FilterRead");


        //Memory & Resourcehandeling
        public static readonly ProfilerMarker k_MemoryManagement = new ProfilerMarker("Reactional.Memory.Management");
        public static readonly ProfilerMarker k_ResourceCleanup = new ProfilerMarker("Reactional.Resource.Cleanup");


        // Events
        public static readonly ProfilerMarker k_EventProcessing = new ProfilerMarker("Reactional.Events.Processing");

        #endregion

        #region Reactional Manager Profilers

        public static readonly ProfilerMarker k_ManagerUpdateBundles = new ProfilerMarker("Reactional.Manager.UpdateBundles");
        public static readonly ProfilerMarker k_ParseBundle = new ProfilerMarker("Reactional.Bundle.Parsing");

        #endregion

        #region Reactional Unity API Profilers

        // Loading, Unloading & Management
        
        // TODO check these , they might not work on async functions with this setup
        public static readonly ProfilerMarker k_LoadBundles = new ProfilerMarker("Reactional.Bundles.Loading");
        public static readonly ProfilerMarker k_LoadBundle = new ProfilerMarker("Reactional.Bundle.Load");
        public static readonly ProfilerMarker k_LoadSection = new ProfilerMarker("Reactional.Sections.Loading");
        public static readonly ProfilerMarker k_LoadPlaylist = new ProfilerMarker("Reactional.Playlists.Loading");
        public static readonly ProfilerMarker k_LoadTrack = new ProfilerMarker("Reactional.Tracks.Loading");
        public static readonly ProfilerMarker k_LoadTheme = new ProfilerMarker("Reactional.Themes.Loading");

        //TODO 
        public static readonly ProfilerMarker k_RemoveAsset = new ProfilerMarker("Reactional.Tracks.Unloading");

        #endregion


        #region Helper Methods

        /// <summary>
        /// Easy Access Profile Marker Creation with a string name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ProfilerMarker Marker(string name) => new ProfilerMarker(name);

        #endregion
        
        
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Reactional/Profile Controller", 0)]
    public class ProfilerController : MonoBehaviour
    {
        [Header("Profile Settings")] 
        [SerializeField] private bool DisplayProfiler = false;
        [SerializeField] private int _fontSize = 16;
        [SerializeField] private float UIScale = 1.2f;


        [HideInInspector] 
        [SerializeField] private bool _displayNamesToConsoleAtStart = false;

        #region Profiler Markers To Controller

        private static readonly ProfilerMarker k_EngineUpdate = ProfilerMarkers.k_EngineUpdate;
        private static readonly ProfilerMarker k_AudioFilterRead = ProfilerMarkers.k_AudioFilterRead;
        private static readonly ProfilerMarker k_EventProcessing = ProfilerMarkers.k_EventProcessing;
        
       

        #endregion

        #region ProfileRecorders

        ProfilerRecorder systemMemoryRecorder;
        ProfilerRecorder gcMemoryRecorder;
        ProfilerRecorder mainThreadTimeRecorder;
        ProfilerRecorder audioThreadTimeRecorder;

        // Reactional Engine
        ProfilerRecorder reactionalEngineUpdateRecorder;
        ProfilerRecorder reactionalAudioFilterReadRecorder;
        ProfilerRecorder reactionalDSPOperationsRecorder;
        ProfilerRecorder reactionalMemoryManagementRecorder;
        ProfilerRecorder reactionalResourceCleanupRecorder;
        ProfilerRecorder reactionalEventProcessingRecorder;
        ProfilerRecorder reactionalThreadManagementRecorder;
        
       

        #endregion

        private string statsText;

        private void Start()
        {
            if (_displayNamesToConsoleAtStart)
            {
                List<ProfilerRecorderHandle> list = new List<ProfilerRecorderHandle>();
                ProfilerRecorderHandle.GetAvailable(list);
                foreach (var handle in list)
                {
                    Debug.Log(handle.ToString());

                    var desc = ProfilerRecorderHandle.GetDescription(handle);
                    Debug.Log($"{desc.Category} / {desc.Name} ({desc.UnitType})");
                }
            }
        }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0) return 0;
            double r = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; i++)
            {
                r += samples[i].Value;
                r /= samplesCount;
            }

            return r;
        }

        void OnEnable()
        {
            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            audioThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Audio Thread", 15);

            // Reactional Engine
            reactionalEngineUpdateRecorder = ProfilerRecorder.StartNew(k_EngineUpdate, 15);
            reactionalAudioFilterReadRecorder = ProfilerRecorder.StartNew(k_AudioFilterRead, 15);
            reactionalEventProcessingRecorder = ProfilerRecorder.StartNew(k_EventProcessing, 15);
        }

        void OnDisable()
        {
            systemMemoryRecorder.Dispose();
            gcMemoryRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
            audioThreadTimeRecorder.Dispose();

            // Reactional Engine
            reactionalEngineUpdateRecorder.Dispose();
            reactionalAudioFilterReadRecorder.Dispose();
            reactionalEventProcessingRecorder.Dispose();
        }

        void Update()
        {
            if (DisplayProfiler)
            {
                var sb = new StringBuilder(500);
                sb.AppendLine("---- System Profiler ----");
                sb.AppendLine($"System Frame Time: {GetRecorderFrameAverage(mainThreadTimeRecorder) * (1e-6f):F1} ms");
                sb.AppendLine(
                    $"System DSP Time: {GetRecorderFrameAverage(audioThreadTimeRecorder) * (1e-6f):F3} ms");
                sb.AppendLine($"System GC Memory: {gcMemoryRecorder.LastValue / (1024 * 1024)} MB");
                sb.AppendLine($"System Memory: {systemMemoryRecorder.LastValue / (1024 * 1024)} MB");
                sb.AppendLine("");
                sb.AppendLine("---- Reactional Profiler ----");


                // Reactional Engine
                sb.AppendLine(
                    $"Reactional Engine Update Time: {GetRecorderFrameAverage(reactionalEngineUpdateRecorder) * (1e-6f):F3} ms");
                sb.AppendLine(
                    $"Reactional Audio Render Time: {GetRecorderFrameAverage(reactionalAudioFilterReadRecorder) * (1e-6f):F3} ms");
                sb.AppendLine(
                    $"Reactional Event Processing Time: {GetRecorderFrameAverage(reactionalEventProcessingRecorder) * (1e-6f):F3} ms");
                sb.AppendLine(
                    $"Reactional Total CPU Time: {(GetRecorderFrameAverage(reactionalEngineUpdateRecorder) + GetRecorderFrameAverage(reactionalAudioFilterReadRecorder) + GetRecorderFrameAverage(reactionalEventProcessingRecorder)) * (1e-6f):F3} ms"
                );


                statsText = sb.ToString();
            }
        }

        void OnGUI()
        {
            if (DisplayProfiler)
            {
                Matrix4x4 originalMatrix = GUI.matrix;


                GUI.matrix = Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(UIScale, UIScale, 1.0f)
                );

                float x = 30;
                float y = 90;
                float width = 350;
                float height = 350;

                GUIStyle style = new GUIStyle(GUI.skin.textArea);
                style.fontSize = Mathf.RoundToInt(_fontSize * UIScale);

                GUI.TextArea(new Rect(x, y, width, height), statsText, style);

                GUI.matrix = originalMatrix;
            }
        }
    }
}