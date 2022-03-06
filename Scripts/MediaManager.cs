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

        public async Task Upload(string id, string playListId, byte[] file, string fileExt)
        {
            WWWForm form = new WWWForm();
            if (fileExt.Equals("mp4"))
                form.AddBinaryData("file", file, "file.mp4", "video/mp4");
            else if (fileExt.Equals("wav"))
                form.AddBinaryData("file", file, "file.wav", "audio/x-wav");


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
            // Do something when delete video
        }

        public async Task Delete(string id)
        {
            RestClient.Result result = await RestClient.Delete(RestClient.GetUrl(serviceAddress, "/" + id), UserToken);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when delete video
        }

        public async Task Get(string playListId)
        {
            RestClient.Result result = await RestClient.Get(RestClient.GetUrl(serviceAddress, "/" + playListId), string.Empty);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when get playlist
        }
    }
}
