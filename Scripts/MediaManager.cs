using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityRestClient;

namespace SimpleMediaSDK
{
    public class MediaManager : MonoBehaviour
    {
        private static MediaManager instance;
        public static MediaManager Instance
        {
            get
            {
                if (!instance)
                    new GameObject("_SimpleMediaSDK").AddComponent<MediaManager>();
                return instance;
            }
        }

        public string serviceAddress = "http://localhost:8216";
        public string serviceSecretKey = "secret";
        public event Action<string> onAddUser;
        public event Action<RespData> onResp;
        public event Action onUploadVideo;
        public event Action onDeleteVideo;
        public event Action<List<MediaData>> onGetVideos;
        public string userToken { get; set; }
        private SocketIO client;
        private ConcurrentQueue<RespData> respQueue = new ConcurrentQueue<RespData>();
        private ConcurrentDictionary<string, RespData> lastRespEachPlaylists = new ConcurrentDictionary<string, RespData>();

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void OnDestroy()
        {
            await Disconnect();
        }

        private void LateUpdate()
        {
            while (respQueue.Count > 0)
            {
                RespData data;
                if (respQueue.TryDequeue(out data))
                    onResp.Invoke(data);
            }
        }

        public async Task Connect()
        {
            await Disconnect();
            lastRespEachPlaylists.Clear();
            client = new SocketIO(serviceAddress);
            client.On("resp", OnResp);
            await client.ConnectAsync();
        }

        private void OnResp(SocketIOResponse resp)
        {
            RespData data = resp.GetValue<RespData>();
            respQueue.Enqueue(data);
            lastRespEachPlaylists[data.playListId] = data;
        }

        public async Task Disconnect()
        {
            if (client != null && client.Connected)
                await client.DisconnectAsync();
            client = null;
        }

        public async Task AddUser(string userToken)
        {
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(nameof(userToken), userToken);
            RestClient.Result result = await RestClient.Post(RestClient.GetUrl(serviceAddress, "/add-user"), form, serviceSecretKey);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            if (onAddUser != null)
                onAddUser.Invoke(userToken);
        }

        public async Task Sub(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            await client.EmitAsync("sub", data);
        }

        public async Task Play(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("play", data);
        }

        public async Task Pause(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("pause", data);
        }

        public async Task Stop(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("stop", data);
        }

        public async Task Seek(string playListId, double time)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(time)] = time;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("seek", data);
        }

        public async Task Switch(string playListId, string mediaId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(mediaId)] = mediaId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("switch", data);
        }

        public async Task Upload(string playListId, byte[] file, string fileExt)
        {
            WWWForm form = new WWWForm();
            if (fileExt.Equals("mp4"))
                form.AddBinaryData("file", file, "file.mp4", "video/mp4");
            else if (fileExt.Equals("wav"))
                form.AddBinaryData("file", file, "file.wav", "audio/x-wav");
            form.AddField(nameof(playListId), playListId);

            UnityWebRequest webRequest = UnityWebRequest.Post(RestClient.GetUrl(serviceAddress, "/upload"), form);
            webRequest.certificateHandler = new SimpleWebRequestCert();
            webRequest.SetRequestHeader("Authorization", "Bearer " + userToken);

            UnityWebRequestAsyncOperation ayncOp = webRequest.SendWebRequest();
            while (!ayncOp.isDone)
            {
                await Task.Yield();
            }

            long responseCode = -1;
            bool isHttpError = true;
            bool isNetworkError = true;
            string stringContent = string.Empty;
            string error = string.Empty;

            responseCode = webRequest.responseCode;
#if UNITY_2020_2_OR_NEWER
            isHttpError = (webRequest.result == UnityWebRequest.Result.ProtocolError);
            isNetworkError = (webRequest.result == UnityWebRequest.Result.ConnectionError);
#else
            isHttpError = webRequest.isHttpError;
            isNetworkError = webRequest.isNetworkError;
#endif
            if (isHttpError || isNetworkError)
                return;
            // Do something when upload video
            if (onUploadVideo != null)
                onUploadVideo.Invoke();
        }

        public async Task Delete(string id)
        {
            RestClient.Result result = await RestClient.Delete(RestClient.GetUrl(serviceAddress, "/" + id), userToken);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when delete video
            if (onDeleteVideo != null)
                onDeleteVideo.Invoke();
        }

        public async Task<List<MediaData>> Get(string playListId)
        {
            RestClient.Result<List<MediaData>> result = await RestClient.Get<List<MediaData>>(RestClient.GetUrl(serviceAddress, "/" + playListId));
            if (result.IsNetworkError || result.IsHttpError)
                return new List<MediaData>();
            // Do something when get playlist
            if (onGetVideos != null)
                onGetVideos.Invoke(result.Content);
            return result.Content;
        }
    }
}
