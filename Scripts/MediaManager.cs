using Cysharp.Threading.Tasks;
using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
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
                return instance;
            }
        }

        public string serviceAddress = "http://localhost:8216";
        public string serviceSecretKey = "secret";
        public GameObject rootUploadProgress;
        public Text textUploadProgress;
        public string formatUploadProgress = "Uploading... {0}%";

        public event Action<string> onAddUser;
        public event Action<string> onRemoveUser;
        public event Action<RespData> onResp;
        public event Action onUpload;
        public event Action onDelete;
        public event Action<List<MediaData>> onGet;
        public string userToken { get; set; }
        private SocketIO client;
        private HashSet<string> pendingSubs = new HashSet<string>();
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

        public async Task Connect()
        {
            await Disconnect();
            lastRespEachPlaylists.Clear();
            client = new SocketIO(serviceAddress, new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            });
            client.OnConnected += Client_OnConnected;
            client.On("resp", OnResp);
            // Always accept SSL
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, policyErrors) => true;
            await client.ConnectAsync();
            await UniTask.SwitchToMainThread();
        }

        private async void Client_OnConnected(object sender, EventArgs e)
        {
            foreach (var pendingSub in pendingSubs)
                await Sub(pendingSub);
            pendingSubs.Clear();
        }

        private async void OnResp(SocketIOResponse resp)
        {
            await UniTask.SwitchToMainThread();
            RespData data = resp.GetValue<RespData>();
            if (onResp != null)
                onResp.Invoke(data);
            lastRespEachPlaylists[data.playListId] = data;
        }

        public async Task Disconnect()
        {
            if (client != null && client.Connected)
            {
                client.OnConnected -= Client_OnConnected;
                await client.DisconnectAsync();
            }
            await UniTask.SwitchToMainThread();
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

        public async Task RemoveUser(string userToken)
        {
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(nameof(userToken), userToken);
            RestClient.Result result = await RestClient.Post(RestClient.GetUrl(serviceAddress, "/remove-user"), form, serviceSecretKey);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            if (onRemoveUser != null)
                onRemoveUser.Invoke(userToken);
        }

        public async Task Sub(string playListId)
        {
            if (client == null || !client.Connected)
            {
                Debug.LogError("Client not connected, pending sub " + playListId);
                pendingSubs.Add(playListId);
                return;
            }
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            await client.EmitAsync("sub", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Play(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("play", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Pause(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("pause", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Stop(string playListId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("stop", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Seek(string playListId, double time)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(time)] = time;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("seek", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Volume(string playListId, float volume)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(volume)] = volume;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("volume", data);
            await UniTask.SwitchToMainThread();
        }

        public async Task Switch(string playListId, string mediaId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(mediaId)] = mediaId;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("switch", data);
            await UniTask.SwitchToMainThread();
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

            UnityWebRequestAsyncOperation asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
            {
                if (rootUploadProgress != null)
                    rootUploadProgress.SetActive(true);
                if (textUploadProgress != null)
                    textUploadProgress.text = string.Format(formatUploadProgress, (asyncOp.progress * 100).ToString("N0"));
                await Task.Yield();
            }
            if (rootUploadProgress != null)
                rootUploadProgress.SetActive(false);

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
            if (onUpload != null)
                onUpload.Invoke();
        }

        public async Task Delete(string id)
        {
            RestClient.Result result = await RestClient.Delete(RestClient.GetUrl(serviceAddress, "/" + id), userToken);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            if (onDelete != null)
                onDelete.Invoke();
        }

        public async Task<List<MediaData>> Get(string playListId)
        {
            await UniTask.SwitchToMainThread();
            RestClient.Result<List<MediaData>> result = await RestClient.Get<List<MediaData>>(RestClient.GetUrl(serviceAddress, "/" + playListId));
            if (result.IsNetworkError || result.IsHttpError)
                return new List<MediaData>();
            if (onGet != null)
                onGet.Invoke(result.Content);
            return result.Content;
        }
    }
}
