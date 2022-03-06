using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace SimpleMediaSDK
{
    public class MediaPlayer : MonoBehaviour
    {
        public string playListId;
        public VideoPlayer videoPlayer;
        public string CurrentMediaId { get; protected set; }

        protected virtual void OnEnable()
        {
            MediaManager.Instance.onResp += Instance_onResp;
        }

        protected virtual void OnDisable()
        {
            MediaManager.Instance.onResp -= Instance_onResp;
        }

        private void Instance_onResp(RespData resp)
        {
            CurrentMediaId = resp.mediaId;
            videoPlayer.time = resp.time;
        }
    }
}
