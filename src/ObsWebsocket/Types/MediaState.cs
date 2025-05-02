namespace ObsWebsocket.Types;

public enum MediaState
{
    /// <summary>
    /// No media is loaded
    /// </summary>
    OBS_MEDIA_STATE_NONE,

    /// <summary>
    /// Media is playing
    /// </summary>
    OBS_MEDIA_STATE_PLAYING,

    /// <summary>
    /// Media is opening
    /// </summary>
    OBS_MEDIA_STATE_OPENING,

    /// <summary>
    /// Media is buffering
    /// </summary>
    OBS_MEDIA_STATE_BUFFERING,

    /// <summary>
    /// Media is playing but is paused
    /// </summary>
    OBS_MEDIA_STATE_PAUSED,

    /// <summary>
    /// Media is stopped
    /// </summary>
    OBS_MEDIA_STATE_STOPPED,

    /// <summary>
    /// Media is ended
    /// </summary>
    OBS_MEDIA_STATE_ENDED,

    /// <summary>
    /// Media has errored
    /// </summary>
    OBS_MEDIA_STATE_ERROR
}