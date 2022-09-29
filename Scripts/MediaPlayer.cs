using System.Reflection;
using UnityEngine;
using UnityEngine.Video;

namespace SimpleMediaSDK
{
    public class MediaPlayer : MonoBehaviour
    {
        public string playListId;
        public VideoPlayer videoPlayer;
        public RenderHeads.Media.AVProVideo.MediaPlayer avProPlayer;
        public bool convertToAvPro = true;
        public string CurrentMediaId { get; protected set; }
        public RespData LastResp { get; protected set; }
        public float LastRespTime { get; protected set; }
        private string _url = string.Empty;
        public string CurrentVideoUrl { get { return _url; } }
        private bool _prepared = false;
        private bool _avProCreated = false;

        public void SetAudioToUnity()
        {
            if (avProPlayer == null)
                return;
            avProPlayer.PlatformOptionsWindows.audioOutput = RenderHeads.Media.AVProVideo.Windows.AudioOutput.Unity;
            avProPlayer.PlatformOptionsMacOSX.audioMode = RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple.AudioMode.Unity;
            avProPlayer.PlatformOptionsAndroid.audioOutput = RenderHeads.Media.AVProVideo.Android.AudioOutput.Unity;
            avProPlayer.PlatformOptionsIOS.audioMode = RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple.AudioMode.Unity;
        }

        public void SetAudioToDirect()
        {
            if (avProPlayer == null)
                return;
            avProPlayer.PlatformOptionsWindows.audioOutput = RenderHeads.Media.AVProVideo.Windows.AudioOutput.System;
            avProPlayer.PlatformOptionsMacOSX.audioMode = RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple.AudioMode.SystemDirect;
            avProPlayer.PlatformOptionsAndroid.audioOutput = RenderHeads.Media.AVProVideo.Android.AudioOutput.System;
            avProPlayer.PlatformOptionsIOS.audioMode = RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple.AudioMode.SystemDirect;
        }

        protected virtual void OnEnable()
        {
            MediaManager.Instance.onResp += Instance_onResp;
            if (convertToAvPro && !_avProCreated && videoPlayer != null && avProPlayer == null)
            {
                Renderer renderer = videoPlayer.targetMaterialRenderer;
                GameObject rendererGameObject = renderer == null ? null : renderer.gameObject;
                if (rendererGameObject != null)
                {
                    avProPlayer = videoPlayer.gameObject.AddComponent<RenderHeads.Media.AVProVideo.MediaPlayer>();
                    SetAudioToUnity();
                    RenderHeads.Media.AVProVideo.ApplyToMesh applyToMaterial = rendererGameObject.AddComponent<RenderHeads.Media.AVProVideo.ApplyToMesh>();
                    applyToMaterial.Player = avProPlayer;
                    applyToMaterial.MeshRenderer = renderer;
                    applyToMaterial.TexturePropertyName = videoPlayer.targetMaterialProperty;

                    RenderHeads.Media.AVProVideo.AudioOutput audioOutput;
                    if (videoPlayer.audioTrackCount > 0)
                    {
                        AudioSource audioSource = videoPlayer.GetTargetAudioSource(0);
                        audioOutput = audioSource.gameObject.AddComponent<RenderHeads.Media.AVProVideo.AudioOutput>();
                    }
                    else
                    {
                        AudioSource audioSource = GetComponentInChildren<AudioSource>(true);
                        audioOutput = audioSource.gameObject.AddComponent<RenderHeads.Media.AVProVideo.AudioOutput>();
                    }
                    System.Type type = typeof(RenderHeads.Media.AVProVideo.AudioOutput);
                    FieldInfo fieldInfo = type.GetField("_supportPositionalAudio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    fieldInfo.SetValue(audioOutput, true);
                    audioOutput.Player = avProPlayer;
                }
                _avProCreated = true;
            }
            // Setup events
            if (videoPlayer != null)
                videoPlayer.prepareCompleted += VideoPlayer_PrepareCompleted;
            if (avProPlayer != null)
                avProPlayer.Events.AddListener(AVProMediaPlayer_HandleEvent);
            MediaManager.Instance.Sub(playListId);
        }

        protected virtual void OnDisable()
        {
            MediaManager.Instance.onResp -= Instance_onResp;
            if (videoPlayer != null)
                videoPlayer.prepareCompleted -= VideoPlayer_PrepareCompleted;
            if (avProPlayer != null)
                avProPlayer.Events.RemoveListener(AVProMediaPlayer_HandleEvent);
        }

        protected virtual void Instance_onResp(RespData resp)
        {
            if (!resp.playListId.Equals(playListId))
                return;
            CurrentMediaId = resp.mediaId;
            SetVolume(resp.volume);
            if (string.IsNullOrEmpty(resp.filePath))
            {
                // AVPro
                if (avProPlayer != null)
                    avProPlayer.Stop();
                // Unity's video player
                else if (videoPlayer != null)
                    videoPlayer.Stop();
            }
            else
            {
                // Prepare data to play video
                var url = MediaManager.Instance.serviceAddress + resp.filePath.Substring(1);
                // AVPro
                if (avProPlayer != null)
                {
                    if (_prepared)
                    {
                        if (avProPlayer.Control.IsPlaying())
                            avProPlayer.Play();
                        else
                            avProPlayer.Pause();
                    }
                    if (!url.Equals(_url) || System.Math.Abs(resp.time - avProPlayer.Control.GetCurrentTime()) >= 1 || avProPlayer.Control.GetCurrentTime() <= 0)
                    {
                        _url = url;
                        _prepared = false;
                        avProPlayer.OpenMedia(new RenderHeads.Media.AVProVideo.MediaPath(_url, RenderHeads.Media.AVProVideo.MediaPathType.AbsolutePathOrURL), false);
                    }
                }
                // Unity's video player
                else if (videoPlayer != null)
                {
                    if (_prepared)
                    {
                        if (resp.isPlaying)
                            videoPlayer.Play();
                        else
                            videoPlayer.Pause();
                    }
                    if (!url.Equals(videoPlayer.url) || System.Math.Abs(resp.time - videoPlayer.time) >= 1 || videoPlayer.time <= 0)
                    {
                        _url = url;
                        _prepared = false;
                        videoPlayer.url = _url;
                        videoPlayer.Prepare();
                    }
                }
            }
            LastResp = resp;
            LastRespTime = Time.unscaledTime;
        }

        protected virtual void VideoPlayer_PrepareCompleted(VideoPlayer source)
        {
            source.time = LastResp.time;
            SetVolume(LastResp.volume);
            if (LastResp.isPlaying)
            {
                source.Play();
            }
            else if (LastResp.time <= 0f)
            {
                source.Stop();
            }
            else
            {
                source.Pause();
            }
            _prepared = true;
        }

        protected virtual void AVProMediaPlayer_HandleEvent(RenderHeads.Media.AVProVideo.MediaPlayer source, RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType eventType, RenderHeads.Media.AVProVideo.ErrorCode code)
        {
            if (eventType == RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.ReadyToPlay)
            {
                source.Control.Seek(LastResp.time);
                SetVolume(LastResp.volume);
                if (LastResp.isPlaying)
                {
                    source.Play();
                }
                else if (LastResp.time <= 0f)
                {
                    source.Stop();
                }
                else
                {
                    source.Pause();
                }
                _prepared = true;
            }
        }

        protected virtual void SetVolume(float volume)
        {
            // AVPro
            if (avProPlayer != null)
            {
                avProPlayer.AudioVolume = volume;
            }
            // Unity's video player
            else if (videoPlayer != null)
            {
                for (ushort i = 0; i < videoPlayer.audioTrackCount; ++i)
                {
                    videoPlayer.SetDirectAudioVolume(i, volume);
                    var audio = videoPlayer.GetTargetAudioSource(i);
                    if (audio)
                        audio.volume = volume;
                }
            }
        }
    }
}
