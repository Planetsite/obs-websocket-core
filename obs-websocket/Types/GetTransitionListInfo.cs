using Newtonsoft.Json;
using System.Collections.Generic;

namespace OBSWebsocketDotNet.Types;

/// <summary>
/// Response from <see cref="OBSWebsocket.GetTransitionList"/>
/// </summary>
public class GetTransitionListInfo
{
    /// <summary>
    /// Name of the currently active transition
    /// </summary>
    [JsonProperty(PropertyName = "current-transition")]
    public string CurrentTransition { set; get; }

    /// <summary>
    /// List of transitions.
    /// </summary>
    [JsonProperty(PropertyName = "transitions")]
    public List<TransitionSettings> Transitions { set; get; }
}
