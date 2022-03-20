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
        private enum TaskQueueType
        {
            Resp,
            Upload,
            Delete,
            Get,
            AddUser,
            RemoveUser,
        }
        private struct TaskQueue
        {
            public TaskQueueType type;
            public object data;
        }
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
        public event Action<string> onRemoveUser;
        public event Action<RespData> onResp;
        public event Action onUpload;
        public event Action onDelete;
        public event Action<List<MediaData>> onGet;
        public string userToken { get; set; }
        private SocketIO client;
        private ConcurrentQueue<TaskQueue> taskQueues = new ConcurrentQueue<TaskQueue>();
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

        private void LateUpdate()
        {
            while (taskQueues.Count > 0)
            {
                TaskQueue taskQueue;
                if (taskQueues.TryDequeue(out taskQueue))
                {
                    switch (taskQueue.type)
                    {
                        case TaskQueueType.Resp:
                            if (onResp != null)
                                onResp.Invoke((RespData)taskQueue.data);
                            break;
                        case TaskQueueType.Upload:
                            if (onUpload != null)
                                onUpload.Invoke();
                            break;
                        case TaskQueueType.Delete:
                            if (onDelete != null)
                                onDelete.Invoke();
                            break;
                        case TaskQueueType.Get:
                            if (onGet != null)
                                onGet.Invoke((List<MediaData>)taskQueue.data);
                            break;
                        case TaskQueueType.AddUser:
                            if (onAddUser != null)
                                onAddUser.Invoke((string)taskQueue.data);
                            break;
                        case TaskQueueType.RemoveUser:
                            if (onRemoveUser != null)
                                onRemoveUser.Invoke((string)taskQueue.data);
                            break;
                    }
                }
            }
        }

        public async Task Connect()
        {
            await Disconnect();
            lastRespEachPlaylists.Clear();
            client = new SocketIO(serviceAddress);
            client.OnConnected += Client_OnConnected;
            client.On("resp", OnResp);
            await client.ConnectAsync();
        }

        private async void Client_OnConnected(object sender, EventArgs e)
        {
            foreach (var pendingSub in pendingSubs)
                await Sub(pendingSub);
            pendingSubs.Clear();
        }

        private void OnResp(SocketIOResponse resp)
        {
            RespData data = resp.GetValue<RespData>();
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.Resp,
                data = data,
            });
            lastRespEachPlaylists[data.playListId] = data;
        }

        public async Task Disconnect()
        {
            if (client != null && client.Connected)
            {
                client.OnConnected -= Client_OnConnected;
                await client.DisconnectAsync();
            }
            client = null;
        }

        public async Task AddUser(string userToken)
        {
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(nameof(userToken), userToken);
            RestClient.Result result = await RestClient.Post(RestClient.GetUrl(serviceAddress, "/add-user"), form, serviceSecretKey);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.AddUser,
                data = userToken,
            });
        }

        public async Task RemoveUser(string userToken)
        {
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add(nameof(userToken), userToken);
            RestClient.Result result = await RestClient.Post(RestClient.GetUrl(serviceAddress, "/remove-user"), form, serviceSecretKey);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.RemoveUser,
                data = userToken,
            });
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

        public async Task Volume(string playListId, float volume)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[nameof(playListId)] = playListId;
            data[nameof(volume)] = volume;
            data[nameof(userToken)] = userToken;
            await client.EmitAsync("volume", data);
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
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.Upload,
            });
        }

        public async Task Delete(string id)
        {
            RestClient.Result result = await RestClient.Delete(RestClient.GetUrl(serviceAddress, "/" + id), userToken);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            // Do something when delete video
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.Delete,
            });
        }

        public async Task<List<MediaData>> Get(string playListId)
        {
            RestClient.Result<List<MediaData>> result = await RestClient.Get<List<MediaData>>(RestClient.GetUrl(serviceAddress, "/" + playListId));
            if (result.IsNetworkError || result.IsHttpError)
                return new List<MediaData>();
            // Do something when get playlist
            taskQueues.Enqueue(new TaskQueue()
            {
                type = TaskQueueType.Get,
                data = result.Content,
            });
            return result.Content;
        }
    }
}
