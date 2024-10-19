﻿using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

/// <summary>
/// Filter settings
/// </summary>
public class FilterSettings
{
    /// <summary>
    /// Name of the filter
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string Name { set; get; }

    /// <summary>
    /// Type of the specified filter
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public string Type { set; get; }

    /// <summary>
    /// Status of the specified filter
    /// </summary>
    [JsonProperty(PropertyName = "enabled")]
    public bool IsEnabled { set; get; }

    /// <summary>
    /// Settings for the filter
    /// </summary>
    [JsonIgnore]
    public IFilterProperties Settings { set; get; }
}