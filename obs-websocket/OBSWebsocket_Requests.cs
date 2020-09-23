/*
    The MIT License (MIT)

    Copyright (c) 2017 Stéphane Lepin

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OBSWebsocketDotNet
{
    /// <summary>
    /// Instance of a connection with an obs-websocket server
    /// </summary>
    public partial class OBSWebsocket
    {
        #region Private Members

        private const string SOURCE_TYPE_JSON_FIELD = "sourceType";
        private const string SOURCE_TYPE_BROWSER_SOURCE = "browser_source";

        #endregion

        /// <summary>
        /// Get basic OBS video information
        /// </summary>
        public async Task<OBSVideoInfo> GetVideoInfo()
        {
            JObject response = await SendRequestAsync("GetVideoInfo");
            return JsonConvert.DeserializeObject<OBSVideoInfo>(response.ToString());
        }

        /// <summary>
        /// At least embedPictureFormat or saveToFilePath must be specified.
        /// Clients can specify width and height parameters to receive scaled pictures. Aspect ratio is preserved if only one of these two parameters is specified.
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="embedPictureFormat">Format of the Data URI encoded picture. Can be "png", "jpg", "jpeg" or "bmp" (or any other value supported by Qt's Image module)</param>
        /// <param name="saveToFilePath">Full file path (file extension included) where the captured image is to be saved. Can be in a format different from pictureFormat. Can be a relative path.</param>
        /// <param name="width">Screenshot width. Defaults to the source's base width.</param>
        /// <param name="height">Screenshot height. Defaults to the source's base height.</param>
        public SourceScreenshotResponse TakeSourceScreenshot(string sourceName, string embedPictureFormat = null, string saveToFilePath = null, int width = -1, int height = -1)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            if (embedPictureFormat != null)
            {
                requestFields.Add("embedPictureFormat", embedPictureFormat);
            }
            if (saveToFilePath != null)
            {
                requestFields.Add("saveToFilePath", saveToFilePath);
            }
            if (width > -1)
            {
                requestFields.Add("width", width);
            }
            if (height > -1)
            {
                requestFields.Add("height", height);
            }

            var response = SendRequestAsync("TakeSourceScreenshot", requestFields);
            return JsonConvert.DeserializeObject<SourceScreenshotResponse>(response.ToString());
        }

        /// <summary>
        /// At least embedPictureFormat or saveToFilePath must be specified.
        /// Clients can specify width and height parameters to receive scaled pictures. Aspect ratio is preserved if only one of these two parameters is specified.
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="embedPictureFormat">Format of the Data URI encoded picture. Can be "png", "jpg", "jpeg" or "bmp" (or any other value supported by Qt's Image module)</param>
        /// <param name="saveToFilePath">Full file path (file extension included) where the captured image is to be saved. Can be in a format different from pictureFormat. Can be a relative path.</param>
        public SourceScreenshotResponse TakeSourceScreenshot(string sourceName, string embedPictureFormat = null, string saveToFilePath = null)
        {
            return TakeSourceScreenshot(sourceName, embedPictureFormat, saveToFilePath, -1, -1);
        }

        /// <summary>
        /// At least embedPictureFormat or saveToFilePath must be specified.
        /// Clients can specify width and height parameters to receive scaled pictures. Aspect ratio is preserved if only one of these two parameters is specified.
        /// </summary>
        /// <param name="sourceName"></param>
        public SourceScreenshotResponse TakeSourceScreenshot(string sourceName)
        {
            return TakeSourceScreenshot(sourceName, null, null);
        }

        /// <summary>
        /// Get the current scene info along with its items
        /// </summary>
        /// <returns>An <see cref="OBSScene"/> object describing the current scene</returns>
        public async Task<OBSScene> GetCurrentScene()
        {
            JObject response = await SendRequestAsync("GetCurrentScene");
            return new OBSScene(response);
        }

        /// <summary>
        /// Set the current scene to the specified one
        /// </summary>
        /// <param name="sceneName">The desired scene name</param>
        public async Task SetCurrentSceneAsync(string sceneName)
        {
            var requestFields = new JObject();
            requestFields.Add("scene-name", sceneName);

            await SendRequestAsync("SetCurrentScene", requestFields);
        }

        /// <summary>
        /// Get the filename formatting string
        /// </summary>
        /// <returns>Current filename formatting string</returns>
        public async Task<string> GetFilenameFormatting()
        {
            JObject response = await SendRequestAsync("GetFilenameFormatting");
            return (string)response["filename-formatting"];
        }

        /// <summary>
        /// Get OBS stats (almost the same info as provided in OBS' stats window)
        /// </summary>
        public async Task<OBSStats> GetStats()
        {
            JObject response = await SendRequestAsync("GetStats");
            return JsonConvert.DeserializeObject<OBSStats>(response["stats"].ToString());
        }

        /// <summary>
        /// List every available scene
        /// </summary>
        /// <returns>A <see cref="List{OBSScene}" /> of <see cref="OBSScene"/> objects describing each scene</returns>
        public async Task<List<OBSScene>> ListScenes()
        {
            var response = await GetSceneList();
            return response.Scenes;
        }

        /// <summary>
        /// Get a list of scenes in the currently active profile
        /// </summary>
        public async Task<GetSceneListInfo> GetSceneList()
        {
            JObject response = await SendRequestAsync("GetSceneList");
            return JsonConvert.DeserializeObject<GetSceneListInfo>(response.ToString());
        }

        /// <summary>
        /// Changes the order of scene items in the requested scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to reorder (defaults to current)</param>
        /// <param name="sceneItems">List of items to reorder, only ID or Name required</param>
        public async Task ReorderSceneItems(List<SceneItemStub> sceneItems, string sceneName = null)
        {
            var requestFields = new JObject();
            if (sceneName != null)
                requestFields.Add("scene", sceneName);

            var items = JObject.Parse(JsonConvert.SerializeObject(sceneItems));
            requestFields.Add("items", items);

            await SendRequestAsync("ReorderSceneItems", requestFields);
        }

        /// <summary>
        /// Get the specified scene's transition override info
        /// </summary>
        /// <param name="sceneName">Name of the scene to return the override info</param>
        /// <returns>TransitionOverrideInfo</returns>
        public async Task<TransitionOverrideInfo> GetSceneTransitionOverride(string sceneName)
        {
            var requestFields = new JObject();
            requestFields.Add("sceneName", sceneName);

            JObject response = await SendRequestAsync("GetSceneTransitionOverride", requestFields);
            return response.ToObject<TransitionOverrideInfo>();
        }

        /// <summary>
        /// Set specific transition override for a scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to set the transition override</param>
        /// <param name="transitionName">Name of the transition to use</param>
        /// <param name="transitionDuration">Duration in milliseconds of the transition if transition is not fixed. Defaults to the current duration specified in the UI if there is no current override and this value is not given</param>
        public async Task SetSceneTransitionOverride(string sceneName, string transitionName, int transitionDuration = -1)
        {
            var requestFields = new JObject();
            requestFields.Add("sceneName", sceneName);
            requestFields.Add("transitionName", transitionName);

            if (transitionDuration >= 0)
            {
                requestFields.Add("transitionDuration", transitionDuration);
            }

            await SendRequestAsync("SetSceneTransitionOverride", requestFields);
        }

        /// <summary>
        /// Remove any transition override from a specific scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to remove the transition override</param>
        public async Task RemoveSceneTransitionOverride(string sceneName)
        {
            var requestFields = new JObject();
            requestFields.Add("sceneName", sceneName);

            await SendRequestAsync("RemoveSceneTransitionOverride", requestFields);
        }

        /// <summary>
        /// List all sources available in the running OBS instance
        /// </summary>
        public async Task<List<SourceInfo>> GetSourcesList()
        {
            JObject response = await SendRequestAsync("GetSourcesList");
            return JsonConvert.DeserializeObject<List<SourceInfo>>(response["sources"].ToString());
        }

        /// <summary>
        /// List all sources available in the running OBS instance
        /// </summary>
        public async Task<List<SourceType>> GetSourceTypesList()
        {
            JObject response = await SendRequestAsync("GetSourceTypesList");
            return JsonConvert.DeserializeObject<List<SourceType>>(response["types"].ToString());
        }

        /// <summary>
        /// Change the visibility of the specified scene item
        /// </summary>
        /// <param name="itemName">Scene item which visiblity will be changed</param>
        /// <param name="visible">Desired visiblity</param>
        /// <param name="sceneName">Scene name of the specified item</param>
        public async Task SetSourceRender(string itemName, bool visible, string sceneName = null)
        {
            var requestFields = new JObject();
            requestFields.Add("item", itemName);
            requestFields.Add("visible", visible);

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            await SendRequestAsync("SetSceneItemProperties", requestFields);
        }

        /// <summary>
        /// Gets the scene specific properties of the specified source item. Coordinates are relative to the item's parent (the scene or group it belongs to).
        /// </summary>
        /// <param name="itemName">The name of the source</param>
        /// <param name="sceneName">The name of the scene that the source item belongs to. Defaults to the current scene.</param>
        public SceneItemProperties GetSceneItemProperties(string itemName, string sceneName = null)
        {
            return JsonConvert.DeserializeObject<SceneItemProperties>(GetSceneItemPropertiesJson(itemName, sceneName).ToString());
        }

        /// <summary>
        /// Gets the scene specific properties of the specified source item. Coordinates are relative to the item's parent (the scene or group it belongs to).
        /// Response is a JObject
        /// </summary>
        /// <param name="itemName">The name of the source</param>
        /// <param name="sceneName">The name of the scene that the source item belongs to. Defaults to the current scene.</param>
        public async Task<JObject> GetSceneItemPropertiesJson(string itemName, string sceneName = null)
        {
            var requestFields = new JObject();
            requestFields.Add("item", itemName);

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            return await SendRequestAsync("GetSceneItemProperties", requestFields);
        }

        /// <summary>
        /// Get the current properties of a Text GDI Plus source.
        /// </summary>
        /// <param name="sourceName">The name of the source</param>
        public async Task<TextGDIPlusProperties> GetTextGDIPlusProperties(string sourceName)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);

            JObject response = await SendRequestAsync("GetTextGDIPlusProperties", requestFields);
            return JsonConvert.DeserializeObject<TextGDIPlusProperties>(response.ToString());
        }

        /// <summary>
        /// Set the current properties of a Text GDI Plus source.
        /// </summary>
        /// <param name="properties">properties for the source</param>
        public async Task SetTextGDIPlusProperties(TextGDIPlusProperties properties)
        {
            var requestFields = JObject.Parse(JsonConvert.SerializeObject(properties));

            await SendRequestAsync("SetTextGDIPlusProperties", requestFields);
        }

        /// <summary>
        /// Move a filter in the chain (relative positioning)
        /// </summary>
        /// <param name="sourceName">Scene Name</param>
        /// <param name="filterName">Filter Name</param>
        /// <param name="movement">Direction to move</param>
        public async Task MoveSourceFilter(string sourceName, string filterName, FilterMovementType movement)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);
            requestFields.Add("movementType", movement.ToString().ToLowerInvariant());

            await SendRequestAsync("MoveSourceFilter", requestFields);
        }

        /// <summary>
        /// Move a filter in the chain (absolute index positioning)
        /// </summary>
        /// <param name="sourceName">Scene Name</param>
        /// <param name="filterName">Filter Name</param>
        /// <param name="newIndex">Desired position of the filter in the chain</param>
        public async Task ReorderSourceFilter(string sourceName, string filterName, int newIndex)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);
            requestFields.Add("newIndex", newIndex);

            await SendRequestAsync("ReorderSourceFilter", requestFields);
        }

        /// <summary>
        /// Apply settings to a source filter
        /// </summary>
        /// <param name="sourceName">Source with filter</param>
        /// <param name="filterName">Filter name</param>
        /// <param name="filterSettings">Filter settings</param>
        public async Task SetSourceFilterSettings(string sourceName, string filterName, JObject filterSettings)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);
            requestFields.Add("filterSettings", filterSettings);

            await SendRequestAsync("SetSourceFilterSettings", requestFields);
        }

        /// <summary>
        /// Modify the Source Filter's visibility
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="filterName">Source filter name</param>
        /// <param name="filterEnabled">New filter state</param>
        public async Task SetSourceFilterVisibility(string sourceName, string filterName, bool filterEnabled)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);
            requestFields.Add("filterEnabled", filterEnabled);

            await SendRequestAsync("SetSourceFilterVisibility", requestFields);
        }

        /// <summary>
        /// Return a list of all filters on a source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        public async Task<List<FilterSettings>> GetSourceFilters(string sourceName)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);

            JObject response = await SendRequestAsync("GetSourceFilters", requestFields);

            return JsonConvert.DeserializeObject<List<FilterSettings>>(response["filters"].ToString());
        }

        /// <summary>
        /// Return a list of all filters on a source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="filterName">Filter name</param>
        public async Task<FilterSettings> GetSourceFilterInfo(string sourceName, string filterName)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);

            JObject response = await SendRequestAsync("GetSourceFilterInfo", requestFields);

            return JsonConvert.DeserializeObject<FilterSettings>(response.ToString());
        }

        /// <summary>
        /// Remove the filter from a source
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="filterName"></param>
        public async Task<bool> RemoveFilterFromSource(string sourceName, string filterName)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterName", filterName);
            try
            {
                await SendRequestAsync("RemoveFilterFromSource", requestFields);
                return true;
            }
            catch (Exception e)
            {
                //TODO exception handling
                Console.WriteLine(e.Message);
            }
            return false;
        }

        /// <summary>
        /// Add a filter to a source
        /// </summary>
        /// <param name="sourceName">Name of the source for the filter</param>
        /// <param name="filterName">Name of the filter</param>
        /// <param name="filterType">Type of filter</param>
        /// <param name="filterSettings">Filter settings object</param>
        public async Task AddFilterToSource(string sourceName, string filterName, string filterType, JObject filterSettings)
        {
            var requestFields = new JObject();
            requestFields.Add("sourceName", sourceName);
            requestFields.Add("filterType", filterType);
            requestFields.Add("filterName", filterName);
            requestFields.Add("filterSettings", filterSettings);

            await SendRequestAsync("AddFilterToSource", requestFields);
        }

        /// <summary>
        /// Start/Stop the streaming output
        /// </summary>
        public async Task<StandardResponse> ToggleStreaming()
        {
            var response = await SendRequestAsync("StartStopStreaming");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Start/Stop the recording output
        /// </summary>
        public async Task<StandardResponse> ToggleRecording()
        {
            var response = await SendRequestAsync("StartStopRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Get the current status of the streaming and recording outputs
        /// </summary>
        /// <returns>An <see cref="OutputStatus"/> object describing the current outputs states</returns>
        public async Task<OutputStatus> GetStreamingStatus()
        {
            JObject response = await SendRequestAsync("GetStreamingStatus");
            return new OutputStatus(response);
        }

        /// <summary>
        /// List all transitions
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of all transition names</returns>
        public async Task<List<string>> ListTransitions()
        {
            var transitions = await GetTransitionList();

            List<string> transitionNames = new List<string>();
            foreach (var item in transitions.Transitions)
                transitionNames.Add(item.Name);

            return transitionNames;
        }

        /// <summary>
        /// Get the current transition name and duration
        /// </summary>
        /// <returns>An <see cref="TransitionSettings"/> object with the current transition name and duration</returns>
        public async Task<TransitionSettings> GetCurrentTransition()
        {
            JObject respBody = await SendRequestAsync("GetCurrentTransition");
            return new TransitionSettings(respBody);
        }

        /// <summary>
        /// Set the current transition to the specified one
        /// </summary>
        /// <param name="transitionName">Desired transition name</param>
        public async Task SetCurrentTransition(string transitionName)
        {
            var requestFields = new JObject();
            requestFields.Add("transition-name", transitionName);

            await SendRequestAsync("SetCurrentTransition", requestFields);
        }

        /// <summary>
        /// Change the transition's duration
        /// </summary>
        /// <param name="duration">Desired transition duration (in milliseconds)</param>
        public async Task SetTransitionDuration(int duration)
        {
            var requestFields = new JObject();
            requestFields.Add("duration", duration);

            await SendRequestAsync("SetTransitionDuration", requestFields);
        }

        /// <summary>
        /// Change the volume of the specified source
        /// </summary>
        /// <param name="sourceName">Name of the source which volume will be changed</param>
        /// <param name="volume">Desired volume. Must be between `0.0` and `1.0` for amplitude/mul (useDecibel is false), and under 0.0 for dB (useDecibel is true). Note: OBS will interpret dB values under -100.0 as Inf.</param>
        /// <param name="useDecibel">Interperet `volume` data as decibels instead of amplitude/mul.</param>
        public async Task SetVolume(string sourceName, float volume, bool useDecibel = false)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);
            requestFields.Add("volume", volume);
            requestFields.Add("useDecibel", useDecibel);

            await SendRequestAsync("SetVolume", requestFields);
        }

        /// <summary>
        /// Get the volume of the specified source
        /// Volume is between `0.0` and `1.0` if using amplitude/mul (useDecibel is false), under `0.0` if using dB (useDecibel is true).
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="useDecibel">Output volume in decibels of attenuation instead of amplitude/mul.</param>
        /// <returns>An <see cref="VolumeInfo"/>Object containing the volume and mute state of the specified source.</returns>
        public async Task<VolumeInfo> GetVolume(string sourceName, bool useDecibel = false)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);
            requestFields.Add("useDecibel", useDecibel);

            var response = await SendRequestAsync("GetVolume", requestFields);
            return new VolumeInfo(response);
        }

        /// <summary>
        /// Set the mute state of the specified source
        /// </summary>
        /// <param name="sourceName">Name of the source which mute state will be changed</param>
        /// <param name="mute">Desired mute state</param>
        public async Task SetMuteAsync(string sourceName, bool mute)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);
            requestFields.Add("mute", mute);

            await SendRequestAsync("SetMute", requestFields);
        }

        /// <summary>
        /// Toggle the mute state of the specified source
        /// </summary>
        /// <param name="sourceName">Name of the source which mute state will be toggled</param>
        public async Task ToggleMute(string sourceName)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);

            await SendRequestAsync("ToggleMute", requestFields);
        }

        /// <summary>
        /// Set the position of the specified scene item
        /// </summary>
        /// <param name="itemName">Name of the scene item which position will be changed</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="sceneName">(optional) name of the scene the item belongs to</param>
        public async Task SetSceneItemPosition(string itemName, float x, float y, string sceneName = null)
        {
            var requestFields = new JObject();
            requestFields.Add("item", itemName);
            requestFields.Add("x", x);
            requestFields.Add("y", y);

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            await SendRequestAsync("SetSceneItemPosition", requestFields);
        }

        /// <summary>
        /// Set the scale and rotation of the specified scene item
        /// </summary>
        /// <param name="itemName">Name of the scene item which transform will be changed</param>
        /// <param name="rotation">Rotation in Degrees</param>
        /// <param name="xScale">Horizontal scale factor</param>
        /// <param name="yScale">Vertical scale factor</param>
        /// <param name="sceneName">(optional) name of the scene the item belongs to</param>
        public async Task SetSceneItemTransform(string itemName, float rotation = 0, float xScale = 1, float yScale = 1, string sceneName = null)
        {
            var requestFields = new JObject();
            requestFields.Add("item", itemName);
            requestFields.Add("x-scale", xScale);
            requestFields.Add("y-scale", yScale);
            requestFields.Add("rotation", rotation);

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            await SendRequestAsync("SetSceneItemTransform", requestFields);
        }

        /// <summary>
        /// Sets the scene specific properties of a source. Unspecified properties will remain unchanged. Coordinates are relative to the item's parent (the scene or group it belongs to).
        /// </summary>
        /// <param name="props">Object containing changes</param>
        /// <param name="sceneName">Option scene name</param>
        public async Task SetSceneItemProperties(SceneItemProperties props, string sceneName = null)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            var requestFields = JObject.Parse(JsonConvert.SerializeObject(props, settings));

            if (requestFields["item"] == null)
            {
                requestFields["item"] = props.ItemName;
            }

            if (sceneName != null)
            {
                requestFields.Add("scene-name", sceneName);
            }

            await SendRequestAsync("SetSceneItemProperties", requestFields);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sceneName"></param>
        public async Task SetSceneItemProperties(JObject obj, string sceneName = null)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;

            // Serialize object to SceneItemProperties (needed before proper deserialization)
            var props = JsonConvert.DeserializeObject<SceneItemProperties>(obj.ToString(), settings);

            // Deserialize object
            var requestFields = JObject.Parse(JsonConvert.SerializeObject(props, settings));

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            await SendRequestAsync("SetSceneItemProperties", requestFields);
        }

        /// <summary>
        /// Set the current scene collection to the specified one
        /// </summary>
        /// <param name="scName">Desired scene collection name</param>
        public async Task SetCurrentSceneCollection(string scName)
        {
            var requestFields = new JObject();
            requestFields.Add("sc-name", scName);

            await SendRequestAsync("SetCurrentSceneCollection", requestFields);
        }

        /// <summary>
        /// Get the name of the current scene collection
        /// </summary>
        /// <returns>Name of the current scene collection</returns>
        public async Task<string> GetCurrentSceneCollection()
        {
            var response = await SendRequestAsync("GetCurrentSceneCollection");
            return (string)response["sc-name"];
        }

        /// <summary>
        /// List all scene collections
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of the names of all scene collections</returns>
        public async Task<List<string>> ListSceneCollections()
        {
            var response = await SendRequestAsync("ListSceneCollections");
            var items = (JArray)response["scene-collections"];

            List<string> sceneCollections = new List<string>();
            foreach (JObject item in items)
            {
                sceneCollections.Add((string)item["sc-name"]);
            }

            return sceneCollections;
        }

        /// <summary>
        /// Set the current profile to the specified one
        /// </summary>
        /// <param name="profileName">Name of the desired profile</param>
        public async Task SetCurrentProfileAsync(string profileName)
        {
            var requestFields = new JObject();
            requestFields.Add("profile-name", profileName);

            await SendRequestAsync("SetCurrentProfile", requestFields);
        }

        /// <summary>
        /// Get the name of the current profile
        /// </summary>
        /// <returns>Name of the current profile</returns>
        public async Task<string> GetCurrentProfile()
        {
            var response = await SendRequestAsync("GetCurrentProfile");
            return (string)response["profile-name"];
        }

        /// <summary>
        /// List all profiles
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of the names of all profiles</returns>
        public async Task<List<string>> ListProfiles()
        {
            var response = await SendRequestAsync("ListProfiles");
            var items = (JArray)response["profiles"];

            List<string> profiles = new List<string>();
            foreach (JObject item in items)
            {
                profiles.Add((string)item["profile-name"]);
            }

            return profiles;
        }

        // TODO: needs updating
        /// <summary>
        /// Start streaming. Will trigger an error if streaming is already active
        /// </summary>
        public async Task<StandardResponse> StartStreaming()
        {
            var response = await SendRequestAsync("StartStreaming");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Stop streaming. Will trigger an error if streaming is not active.
        /// </summary>
        public async Task<StandardResponse> StopStreaming()
        {
            var response = await SendRequestAsync("StopStreaming");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Toggle Streaming
        /// </summary>
        public async Task<StandardResponse> StartStopStreaming()
        {
            var response = await SendRequestAsync("StartStopStreaming");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Start recording. Will trigger an error if recording is already active.
        /// </summary>
        public async Task<StandardResponse> StartRecording()
        {
            var response = await SendRequestAsync("StartRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Stop recording. Will trigger an error if recording is not active.
        /// </summary>
        public async Task<StandardResponse> StopRecording()
        {
            var response = await SendRequestAsync("StopRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Toggle recording
        /// </summary>
        public async Task<StandardResponse> StartStopRecording()
        {
            var response = await SendRequestAsync("StartStopRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Pause the current recording. Returns an error if recording is not active or already paused.
        /// </summary>
        public async Task<StandardResponse> PauseRecording()
        {
            var response = await SendRequestAsync("PauseRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Resume/unpause the current recording (if paused). Returns an error if recording is not active or not paused.
        /// </summary>
        public async Task<StandardResponse> ResumeRecording()
        {
            var response = await SendRequestAsync("ResumeRecording");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Change the current recording folder
        /// </summary>
        /// <param name="recFolder">Recording folder path</param>
        public async Task SetRecordingFolder(string recFolder)
        {
            var requestFields = new JObject();
            requestFields.Add("rec-folder", recFolder);
            await SendRequestAsync("SetRecordingFolder", requestFields);
        }

        /// <summary>
        /// Get the path of the current recording folder
        /// </summary>
        /// <returns>Current recording folder path</returns>
        public async Task<string> GetRecordingFolder()
        {
            var response = await SendRequestAsync("GetRecordingFolder");
            return (string)response["rec-folder"];
        }

        /// <summary>
        /// Get duration of the currently selected transition (if supported)
        /// </summary>
        /// <returns>Current transition duration (in milliseconds)</returns>
        public async Task<int> GetTransitionDuration()
        {
            var response = await SendRequestAsync("GetTransitionDuration");
            return (int)response["transition-duration"];
        }

        /// <summary>
        /// Get duration of the currently selected transition (if supported)
        /// </summary>
        /// <returns>Current transition duration (in milliseconds)</returns>
        public async Task<GetTransitionListInfo> GetTransitionList()
        {
            var response = await SendRequestAsync("GetTransitionList");

            return JsonConvert.DeserializeObject<GetTransitionListInfo>(response.ToString());
        }

        /// <summary>
        /// Get the position of the current transition. Value will be between 0.0 and 1.0.
        /// Note: Returns 1.0 when not active.
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetTransitionPosition()
        {
            var response = await SendRequestAsync("GetTransitionPosition");

            return (double)response["position"];
        }

        /// <summary>
        /// Get status of Studio Mode
        /// </summary>
        /// <returns>Studio Mode status (on/off)</returns>
        public async Task<bool> StudioModeEnabled()
        {
            var response = await SendRequestAsync("GetStudioModeStatus");
            return (bool)response["studio-mode"];
        }

        /// <summary>
        /// Disable Studio Mode
        /// </summary>
        public async Task DisableStudioModeAsync()
        {
            await SendRequestAsync("DisableStudioMode");
        }

        /// <summary>
        /// Enable Studio Mode
        /// </summary>
        public async Task EnableStudioModeAsync()
        {
            await SendRequestAsync("EnableStudioMode");
        }

        /// <summary>
        /// Enable Studio Mode
        /// </summary>
        public async Task<bool> GetStudioModeStatus()
        {
            var response = await SendRequestAsync("GetStudioModeStatus");
            return (bool)response["studio-mode"];
        }

        /// <summary>
        /// Enable/disable Studio Mode
        /// </summary>
        /// <param name="enable">Desired Studio Mode status</param>
        public async Task SetStudioModeAsync(bool enable)
        {
            if (enable)
                await EnableStudioModeAsync();
            else
                await DisableStudioModeAsync();
        }

        /// <summary>
        /// Toggle Studio Mode status (on to off or off to on)
        /// </summary>
        public async Task ToggleStudioMode()
        {
            await SendRequestAsync("ToggleStudioMode");
        }

        /// <summary>
        /// Get the currently selected preview scene. Triggers an error
        /// if Studio Mode is disabled
        /// </summary>
        /// <returns>Preview scene object</returns>
        public async Task<OBSScene> GetPreviewScene()
        {
            var response = await SendRequestAsync("GetPreviewScene");
            return new OBSScene(response);
        }

        /// <summary>
        /// Change the currently active preview scene to the one specified.
        /// Triggers an error if Studio Mode is disabled
        /// </summary>
        /// <param name="previewScene">Preview scene name</param>
        public async Task SetPreviewScene(string previewScene)
        {
            var requestFields = new JObject();
            requestFields.Add("scene-name", previewScene);
            await SendRequestAsync("SetPreviewScene", requestFields);
        }

        /// <summary>
        /// Change the currently active preview scene to the one specified.
        /// Triggers an error if Studio Mode is disabled.
        /// </summary>
        /// <param name="previewScene">Preview scene object</param>
        public Task SetPreviewSceneAsync(OBSScene previewScene)
        {
            return SetPreviewScene(previewScene.Name);
        }

        /// <summary>
        /// Triggers a Studio Mode transition (preview scene to program)
        /// </summary>
        /// <param name="transitionDuration">(optional) Transition duration</param>
        /// <param name="transitionName">(optional) Name of transition to use</param>
        public async Task TransitionToProgram(int transitionDuration = -1, string transitionName = null)
        {
            var requestFields = new JObject();

            if (transitionDuration > -1 || transitionName != null)
            {
                var withTransition = new JObject();

                if (transitionDuration > -1)
                    withTransition.Add("duration");

                if (transitionName != null)
                    withTransition.Add("name", transitionName);

                requestFields.Add("with-transition", withTransition);
            }

            await SendRequestAsync("TransitionToProgram", requestFields);
        }

        /// <summary>
        /// Get if the specified source is muted
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>Source mute status (on/off)</returns>
        public async Task<bool> GetMuteAsync(string sourceName)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);

            var response = await SendRequestAsync("GetMute", requestFields);
            return (bool)response["muted"];
        }

        /// <summary>
        /// Toggle the Replay Buffer on/off
        /// </summary>
        public Task ToggleReplayBuffer()
        {
            return SendRequestAsync("StartStopReplayBuffer");
        }

        /// <summary>
        /// Start recording into the Replay Buffer. Triggers an error
        /// if the Replay Buffer is already active, or if the "Save Replay Buffer"
        /// hotkey is not set in OBS' settings
        /// </summary>
        public Task StartReplayBuffer()
        {
            return SendRequestAsync("StartReplayBuffer");
        }

        /// <summary>
        /// Stop recording into the Replay Buffer. Triggers an error if the
        /// Replay Buffer is not active.
        /// </summary>
        public Task StopReplayBuffer()
        {
            return SendRequestAsync("StopReplayBuffer");
        }

        /// <summary>
        /// Toggle replay buffer
        /// </summary>
        public Task StartStopReplayBuffer()
        {
            return SendRequestAsync("StartStopReplayBuffer");
        }

        /// <summary>
        /// Save and flush the contents of the Replay Buffer to disk. Basically
        /// the same as triggering the "Save Replay Buffer" hotkey in OBS.
        /// Triggers an error if Replay Buffer is not active.
        /// </summary>
        public Task SaveReplayBuffer()
        {
            return SendRequestAsync("SaveReplayBuffer");
        }

        /// <summary>
        /// Set the audio sync offset of the specified source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="syncOffset">Audio offset (in nanoseconds) for the specified source</param>
        public async Task SetSyncOffset(string sourceName, int syncOffset)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);
            requestFields.Add("offset", syncOffset);
            await SendRequestAsync("SetSyncOffset", requestFields);
        }

        /// <summary>
        /// Get the audio sync offset of the specified source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>Audio offset (in nanoseconds) of the specified source</returns>
        public async Task<int> GetSyncOffset(string sourceName)
        {
            var requestFields = new JObject();
            requestFields.Add("source", sourceName);
            var response = await SendRequestAsync("GetSyncOffset", requestFields);
            return (int)response["offset"];
        }

        /// <summary>
        /// Deletes a scene item
        /// </summary>
        /// <param name="sceneItem">Scene item, requires name or id of item</param>
        /// /// <param name="sceneName">Scene name to delete item from (optional)</param>
        public async Task DeleteSceneItem(SceneItemStub sceneItem, string sceneName = null)
        {
            var requestFields = new JObject();

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            JObject minReqs = new JObject();
            if (sceneItem.SourceName != null)
                minReqs.Add("name", sceneItem.SourceName);

            minReqs.Add("id", sceneItem.ID);

            requestFields.Add("item", minReqs);

            await SendRequestAsync("DeleteSceneItem", requestFields);
        }

        /// <summary>
        /// Deletes a scene item
        /// </summary>
        /// <param name="sceneItemId">Scene item id</param>
        /// /// <param name="sceneName">Scene name to delete item from (optional)</param>
        public async Task DeleteSceneItem(int sceneItemId, string sceneName = null)
        {
            var requestFields = new JObject();

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            JObject minReqs = new JObject();

            minReqs.Add("id", sceneItemId);

            requestFields.Add("item", minReqs);

            await SendRequestAsync("DeleteSceneItem", requestFields);
        }

        /// <summary>
        /// Set the relative crop coordinates of the specified source item
        /// </summary>
        /// <param name="sceneItemName">Name of the scene item</param>
        /// <param name="cropInfo">Crop coordinates</param>
        /// <param name="sceneName">(optional) parent scene name of the specified source</param>
        public async Task SetSceneItemCrop(string sceneItemName, SceneItemCropInfo cropInfo, string sceneName = null)
        {
            var requestFields = new JObject();

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            requestFields.Add("item", sceneItemName);
            requestFields.Add("top", cropInfo.Top);
            requestFields.Add("bottom", cropInfo.Bottom);
            requestFields.Add("left", cropInfo.Left);
            requestFields.Add("right", cropInfo.Right);

            await SendRequestAsync("SetSceneItemCrop", requestFields);
        }

        /// <summary>
        /// Set the relative crop coordinates of the specified source item
        /// </summary>
        /// <param name="sceneItem">Scene item object</param>
        /// <param name="cropInfo">Crop coordinates</param>
        /// <param name="scene">Parent scene of scene item</param>
        public Task SetSceneItemCrop(SceneItem sceneItem, SceneItemCropInfo cropInfo, OBSScene scene)
        {
            return SetSceneItemCrop(sceneItem.SourceName, cropInfo, scene.Name);
        }

        /// <summary>
        /// Reset a scene item
        /// </summary>
        /// <param name="itemName">Name of the source item</param>
        /// <param name="sceneName">Name of the scene the source belongs to. Defaults to the current scene.</param>
        public Task ResetSceneItem(string itemName, string sceneName = null)
        {
            var requestFields = new JObject();
            requestFields.Add("item", itemName);

            if (sceneName != null)
                requestFields.Add("scene-name", sceneName);

            return SendRequestAsync("ResetSceneItem", requestFields);
        }

        /// <summary>
        /// Send the provided text as embedded CEA-608 caption data. As of OBS Studio 23.1, captions are not yet available on Linux.
        /// </summary>
        /// <param name="text">Captions text</param>
        public async Task SendCaptions(string text)
        {
            var requestFields = new JObject();
            requestFields.Add("text", text);

            await SendRequestAsync("SendCaptions", requestFields);
        }

        /// <summary>
        /// Set the filename formatting string
        /// </summary>
        /// <param name="filenameFormatting">Filename formatting string to set</param>
        public async Task SetFilenameFormatting(string filenameFormatting)
        {
            var requestFields = new JObject();
            requestFields.Add("filename-formatting", filenameFormatting);

            await SendRequestAsync("SetFilenameFormatting", requestFields);
        }

        /// <summary>
        /// Set the relative crop coordinates of the specified source item
        /// </summary>
        /// <param name="fromSceneName">Source of the scene item</param>
        /// <param name="toSceneName">Destination for the scene item</param>
        /// <param name="sceneItem">Scene item, requires name or id</param>
        public async Task DuplicateSceneItem(string fromSceneName, string toSceneName, SceneItem sceneItem)
        {
            var requestFields = new JObject();

            requestFields.Add("fromScene", fromSceneName);
            requestFields.Add("toScene", toSceneName);

            JObject minReqs = new JObject();
            if (sceneItem.SourceName != null)
                minReqs.Add("name", sceneItem.SourceName);

            minReqs.Add("id", sceneItem.ID);

            requestFields.Add("item", minReqs);

            await SendRequestAsync("DuplicateSceneItem", requestFields);
        }

        /// <summary>
        /// Set the relative crop coordinates of the specified source item
        /// </summary>
        /// <param name="fromSceneName">Source of the scene item</param>
        /// <param name="toSceneName">Destination for the scene item</param>
        /// <param name="sceneItemID">Scene item id to duplicate</param>
        public async Task DuplicateSceneItem(string fromSceneName, string toSceneName, int sceneItemID)
        {
            var requestFields = new JObject();

            requestFields.Add("fromScene", fromSceneName);
            requestFields.Add("toScene", toSceneName);

            JObject minReqs = new JObject();
            minReqs.Add("id", sceneItemID);

            requestFields.Add("item", minReqs);

            await SendRequestAsync("DuplicateSceneItem", requestFields);
        }

        /// <summary>
        /// Get names of configured special sources (like Desktop Audio
        /// and Mic sources)
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetSpecialSources()
        {
            var response = await SendRequestAsync("GetSpecialSources");
            var sources = new Dictionary<string, string>();
            foreach (KeyValuePair<string, JToken> kvp in response)
            {
                string key = kvp.Key;
                string value = (string)kvp.Value;
                if (key != "request-type" && key != "message-id" && key != "status")
                {
                    sources.Add(key, value);
                }
            }
            return sources;
        }

        /// <summary>
        /// Set current streaming settings
        /// </summary>
        /// <param name="service">Service settings</param>
        /// <param name="save">Save to disk</param>
        public async Task<JObject> SetStreamingSettingsAsync(StreamingService service, bool save)
        {
            //var jsonSettings = JsonConvert.SerializeObject(service.Settings);
            var jsonSettings = JObject.FromObject(service.Settings);

            var requestFields = new JObject();
            requestFields.Add("type", service.Type);
            requestFields.Add("settings", jsonSettings);
            requestFields.Add("save", save);
            return await SendRequestAsync("SetStreamSettings", requestFields);
        }

        /// <summary>
        /// Get current streaming settings
        /// </summary>
        /// <returns></returns>
        public async Task<StreamingService> GetStreamSettings()
        {
            var response = await SendRequestAsync("GetStreamSettings");
            return response.ToObject<StreamingService>();
        }

        /// <summary>
        /// Set current streaming settings
        /// </summary>
        /// <param name="service">Service settings</param>
        /// <param name="save">Save to disk</param>
        public async Task<StandardResponse> SetStreamSettings(StreamingService service, bool save)
        {
            var response = await SetStreamingSettingsAsync(service, save);
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Save current Streaming settings to disk
        /// </summary>
        public async Task<StandardResponse> SaveStreamSettings()
        {
            var response = await SendRequestAsync("SaveStreamSettings");
            return response.ToObject<StandardResponse>();
        }

        /// <summary>
        /// Get settings of the specified BrowserSource
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="sceneName">Optional name of a scene where the specified source can be found</param>
        /// <returns>BrowserSource properties</returns>
        public async Task<BrowserSourceProperties> GetBrowserSourceProperties(string sourceName, string sceneName = null)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);
            if (sceneName != null)
            {
                request.Add("scene-name", sourceName);
            }
            var response = await SendRequestAsync("GetSourceSettings", request);
            if (response[SOURCE_TYPE_JSON_FIELD].ToString() != SOURCE_TYPE_BROWSER_SOURCE)
            {
                throw new Exception($"Invalid source_type. Expected: {SOURCE_TYPE_BROWSER_SOURCE} Received: {response[SOURCE_TYPE_JSON_FIELD]}");
            }

            return new BrowserSourceProperties(response);
        }

        /// <summary>
        /// Set settings of the specified BrowserSource
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="props">BrowserSource properties</param>
        /// <param name="sceneName">Optional name of a scene where the specified source can be found</param>
        public async Task SetBrowserSourceProperties(string sourceName, BrowserSourceProperties props, string sceneName = null)
        {
            props.Source = sourceName;
            var request = JObject.FromObject(props);
            if (sceneName != null)
            {
                request.Add("scene-name", sourceName);
            }

            await SetSourceSettings(sourceName, request, SOURCE_TYPE_BROWSER_SOURCE);
        }

        /// <summary>
        /// Enable/disable the heartbeat event
        /// </summary>
        /// <param name="enable"></param>
        [Obsolete("Will be deprecated in v4.9 and completely removed in v5.0")]
        public async Task SetHeartbeat(bool enable)
        {
            var request = new JObject();
            request.Add("enable", enable);

            await SendRequestAsync("SetHeartbeat", request);
        }

        /// <summary>
        /// Get the settings from a source item
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="sourceType">Type of the specified source. Useful for type-checking to avoid settings a set of settings incompatible with the actual source's type.</param>
        /// <returns>settings</returns>
        public async Task<SourceSettings> GetSourceSettings(string sourceName, string sourceType = null)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);
            if (sourceType != null)
            {
                request.Add("sourceType", sourceType);
            }

            JObject result = await SendRequestAsync("GetSourceSettings", request);
            SourceSettings settings = new SourceSettings(result);

            return settings;
        }

        /// <summary>
        /// Set settings of the specified source.
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="settings">Settings for the source</param>
        /// <param name="sourceType">Type of the specified source. Useful for type-checking to avoid settings a set of settings incompatible with the actual source's type.</param>
        public async Task SetSourceSettings(string sourceName, JObject settings, string sourceType = null)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);
            request.Add("sourceSettings", settings);
            if (sourceType != null)
            {
                request.Add("sourceType", sourceType);
            }

            await SendRequestAsync("SetSourceSettings", request);
        }

        /// <summary>
        /// Gets settings for a media source
        /// </summary>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        public async Task<MediaSourceSettings> GetMediaSourceSettings(string sourceName)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);
            request.Add("sourceType", "ffmpeg_source");

            var response = await SendRequestAsync("GetSourceSettings", request);
            return response.ToObject<MediaSourceSettings>();
        }

        /// <summary>
        /// Sets settings of a media source
        /// </summary>
        /// <param name="sourceSettings"></param>
        public async Task SetMediaSourceSettings(MediaSourceSettings sourceSettings)
        {
            if (sourceSettings.SourceType != "ffmpeg_source")
            {
                throw new System.Exception("Invalid SourceType");
            }
            await SendRequestAsync("SetSourceSettings", JObject.FromObject(sourceSettings));
        }

        /// <summary>
        /// Open a projector window or create a projector on a monitor. Requires OBS v24.0.4 or newer.
        /// </summary>
        /// <param name="projectorType">Type of projector: "Preview" (default), "Source", "Scene", "StudioProgram", or "Multiview" (case insensitive)</param>
        /// <param name="monitor">Monitor to open the projector on. If -1 or omitted, opens a window</param>
        /// <param name="geometry">Size and position of the projector window (only if monitor is -1). Encoded in Base64 using Qt's geometry encoding. Corresponds to OBS's saved projectors</param>
        /// <param name="name">Name of the source or scene to be displayed (ignored for other projector types)</param>
        public async Task OpenProjector(string projectorType = "preview", int monitor = -1, string geometry = null, string name = null)
        {
            var request = new JObject();
            request.Add("type", projectorType);
            request.Add("monitor", monitor);

            if (geometry != null)
            {
                request.Add("geometry", geometry);
            }

            if (name != null)
            {
                request.Add("name", name);
            }

            await SendRequestAsync("OpenProjector", request);
        }

        /// <summary>
        /// Renames a source.
        /// Note: If the new name already exists as a source, obs-websocket will return an error.
        /// </summary>
        /// <param name="currentName">Current source name</param>
        /// <param name="newName">New source name</param>
        public async Task SetSourceName(string currentName, string newName)
        {
            var request = new JObject();
            request.Add("sourceName", currentName);
            request.Add("newName", newName);

            await SendRequestAsync("SetSourceName", request);
        }

        /// <summary>
        /// List existing outputs
        /// </summary>
        /// <returns>Array of OutputInfo</returns>
        public async Task<List<OBSOutputInfo>> ListOutputs()
        {
            var response = await SendRequestAsync("ListOutputs");
            return response["outputs"].ToObject<List<OBSOutputInfo>>();
        }
        /// <summary>
        /// Get the audio monitoring type of the specified source.
        /// Valid return values: none, monitorOnly, monitorAndOutput
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <returns>The monitor type in use</returns>
        public async Task<string> GetAudioMonitorType(string sourceName)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);

            var response = await SendRequestAsync("GetAudioMonitorType", request);
            return (string)response["monitorType"];
        }

        /// <summary>
        /// Set the audio monitoring type of the specified source
        /// </summary>
        /// <param name="sourceName">Source name</param>
        /// <param name="monitorType">The monitor type to use. Options: none, monitorOnly, monitorAndOutput</param>
        public async Task SetAudioMonitorType(string sourceName, string monitorType)
        {
            var request = new JObject();
            request.Add("sourceName", sourceName);
            request.Add("monitorType", monitorType);

            await SendRequestAsync("SetAudioMonitorType", request);
        }

        /// <summary>
        /// Broadcast custom message to all connected WebSocket clients
        /// </summary>
        /// <param name="realm">Identifier to be choosen by the client</param>
        /// <param name="data">User-defined data</param>
        public async Task BroadcastCustomMessageAsync(string realm, JObject data)
        {
            var request = new JObject();
            request.Add("realm", realm);
            request.Add("data", data);

            await SendRequestAsync("BroadcastCustomMessage", request);
        }
    }
}
