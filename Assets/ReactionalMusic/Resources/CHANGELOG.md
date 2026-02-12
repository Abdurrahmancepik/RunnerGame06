# Changelog

## \[1.0.3] - 2025-09-26

* Updated Reactional Engine.
* New direct backend connection to the Reactional Platform
* New Services namespace, containing new methods:

  * Remote

    * Initialize
    * GetPlaylists
    * GetPlaylist
    * GetTrackMetadata
    * GetTracksMetadata
    * GetTrack
    * GetClientEntitlements

  * Preview

    * DownloadAlbumArtwork
    * PreviewAudio

  * (see more in depth on the docs: https://docs.reactionalmusic.com/Unity/API/)

* Added various Profiler Markers to display Reactional systems in the profiler
* Added ProfilerController component for a popup-display of Reactional systems.
* UnloadTheme and UnloadTrack methods added to the API.
* Extended TrackInfo with timeSignature field.
* PauseToggle method added to the API.
* SkipToPercentage, SkipToBar and SkipToPart methods added to the API.
* Bugfix: Some load functions choosing wrong playlist when DefaultSection is set in the ReactionalManager
* Playlist.Start is no longer public, use Playlist.Play instead.
* Sorting Sections.
* BundleName now takes the Project Name from Platform and default to Path if no name exists.
