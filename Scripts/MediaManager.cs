using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
        public event Action<RespData> onResp;
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
    }
}
