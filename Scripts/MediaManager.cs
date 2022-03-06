using SocketIOClient;
using System;
using System.Collections;
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
        public event Action<RespData> onResp;
        public event Action onUploadVideo;
        public event Action onDeleteVideo;
        public event Action onGetVideos;
        public string UserToken { get; set; }
        private SocketIO client;

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

        public async Task Connect()
        {
            await Disconnect();
            client = new SocketIO(serviceAddress);
            client.On("resp", OnResp);
            await client.ConnectAsync();
        }

        private void OnResp(SocketIOResponse resp)
        {
            RespData data = resp.GetValue<RespData>();
            onResp.Invoke(data);
        }

        public async Task Disconnect()
        {
            if (client != null && client.Connected)
                await client.DisconnectAsync();
            client = null;
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
            await client.EmitAsync("play", data);
        }

        public async Task Pause(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            await client.EmitAsync("pause", data);
        }

        public async Task Stop(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            await client.EmitAsync("stop", data);
        }

        public async Task Seek(string playListId, double position)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(position)] = position;
            await client.EmitAsync("seek", data);
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
            onUploadVideo.Invoke();
        }

        public async Task Delete(string id)
        {
            RestClient.Result result = await RestClient.Delete(RestClient.GetUrl(serviceAddress, "/" + id), UserToken);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when delete video
            onDeleteVideo.Invoke();
        }

        public async Task Get(string playListId)
        {
            RestClient.Result result = await RestClient.Get(RestClient.GetUrl(serviceAddress, "/" + playListId), string.Empty);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when get playlist
            onGetVideos.Invoke();
        }
    }
}
