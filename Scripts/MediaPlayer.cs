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
            MediaManager.Instance.onResp += Instance_onResp;
            videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
            MediaManager.Instance.Sub(playListId);
        }

        protected virtual void OnDisable()
        {
            MediaManager.Instance.onResp -= Instance_onResp;
            videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;
        }

        protected virtual void Instance_onResp(RespData resp)
        {
            if (!resp.playListId.Equals(playListId))
                return;
            CurrentMediaId = resp.mediaId;
            if (string.IsNullOrEmpty(resp.filePath))
            {
                videoPlayer.Stop();
            }
            else
            {
                var url = MediaManager.Instance.serviceAddress + resp.filePath.Substring(1);
                if (!url.Equals(videoPlayer.url) || System.Math.Abs(resp.time - videoPlayer.time) >= 1 || videoPlayer.time <= 0)
                {
                    videoPlayer.url = url;
                    videoPlayer.Prepare();
                }
            }
            LastResp = resp;
            LastRespTime = Time.unscaledTime;
            if (videoPlayer.isPrepared)
                SetVolume(videoPlayer, resp.volume);
        }

        protected virtual void VideoPlayer_prepareCompleted(VideoPlayer source)
        {
            source.time = LastResp.time;
            SetVolume(videoPlayer, LastResp.volume);
            if (LastResp.isPlaying)
                source.Play();
            else if (LastResp.time <= 0f)
                source.Stop();
            else
                source.Pause();
        }

        protected virtual void SetVolume(VideoPlayer source, float volume)
        {
            for (ushort i = 0; i < source.audioTrackCount; ++i)
            {
                source.SetDirectAudioVolume(i, LastResp.volume);
                var audio = source.GetTargetAudioSource(i);
                if (audio)
                    audio.volume = LastResp.volume;
            }
        }
    }
}
