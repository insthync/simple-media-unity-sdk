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
        public RespData LastResp { get; protected set; }
        public float LastRespTime { get; protected set; }

        protected virtual void OnEnable()
        {
            MediaManager.Instance.Sub(playListId);
            MediaManager.Instance.onResp += Instance_onResp;
            videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
        }

        protected virtual void OnDisable()
        {
            MediaManager.Instance.onResp -= Instance_onResp;
        }

        private void Instance_onResp(RespData resp)
        {
            CurrentMediaId = resp.mediaId;
            videoPlayer.url = MediaManager.Instance.serviceAddress + resp.filePath.Substring(1);
            videoPlayer.Prepare();
            LastResp = resp;
            LastRespTime = Time.unscaledTime;
        }

        private void VideoPlayer_prepareCompleted(VideoPlayer source)
        {
            source.time = LastResp.time;
            if (LastResp.isPlaying)
                videoPlayer.Play();
            else if (LastResp.time <= 0f)
                videoPlayer.Stop();
            else
                videoPlayer.Pause();
        }
    }
}
