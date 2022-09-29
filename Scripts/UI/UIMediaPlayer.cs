using System.Collections;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SimpleMediaSDK
{
    public class UIMediaPlayer : MonoBehaviour
    {
        public RenderHeads.Media.AVProVideo.MediaPlayer mediaPlayer;
        public Slider seekSlider;
        public Slider volumeSlider;
        public UIMediaList mediaList;
        protected float dirtyLastRespTime;
        protected string dirtyUrl;

        protected VideoRenderMode defaultSourceRenderMode;
        protected RenderTexture defaultSourceRenderTexture;
        protected VideoAudioOutputMode defaultSourceAudioOutputMode;
        protected MediaPlayer source;
        public MediaPlayer Source
        {
            get { return source; }
            set
            {
                if (source != null)
                {
                    mediaPlayer.Stop();
                    if (source.avProPlayer != null)
                        source.avProPlayer.AudioMuted = false;
                    MediaManager.Instance.Sub(source.playListId);
                }
                source = value;
                if (source != null)
                {
                    if (source.avProPlayer != null)
                        source.avProPlayer.AudioMuted = true;
                    MediaManager.Instance.Sub(source.playListId);
                    if (mediaList)
                        mediaList.Load(source.playListId);
                }
            }
        }

        protected void OnEnable()
        {
            if (seekSlider)
                seekSlider.onValueChanged.AddListener(OnSeekSliderValueChanged);
            if (volumeSlider)
                volumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
            MediaManager.Instance.onUpload += Instance_onUploadVideo;
            MediaManager.Instance.onDelete += Instance_onDeleteVideo;
            mediaPlayer.Events.AddListener(AVProMediaPlayer_HandleEvent);
        }

        protected void OnDisable()
        {
            if (seekSlider)
                seekSlider.onValueChanged.RemoveListener(OnSeekSliderValueChanged);
            if (volumeSlider)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
            MediaManager.Instance.onUpload -= Instance_onUploadVideo;
            MediaManager.Instance.onDelete -= Instance_onDeleteVideo;
            mediaPlayer.Events.RemoveListener(AVProMediaPlayer_HandleEvent);
            Source = null;
        }

        private void Instance_onUploadVideo()
        {
            if (mediaList)
                mediaList.Load(source.playListId);
        }

        private void Instance_onDeleteVideo()
        {
            if (mediaList)
                mediaList.Load(source.playListId);
        }

        protected void Update()
        {
            if (source == null)
                return;
            if (dirtyLastRespTime != source.LastRespTime || source.LastResp.isPlaying)
            {
                dirtyLastRespTime = source.LastRespTime;
                if (seekSlider != null)
                {
                    seekSlider.minValue = 0;
                    seekSlider.maxValue = (float)source.LastResp.duration;
                    seekSlider.SetValueWithoutNotify((float)source.LastResp.time + Time.unscaledTime - source.LastRespTime);
                }
                if (volumeSlider != null)
                {
                    volumeSlider.minValue = 0;
                    volumeSlider.maxValue = 1;
                    volumeSlider.SetValueWithoutNotify(source.LastResp.volume);
                }
            }
            if (dirtyUrl != source.CurrentVideoUrl)
            {
                dirtyUrl = source.CurrentVideoUrl;
                mediaPlayer.OpenMedia(new RenderHeads.Media.AVProVideo.MediaPath(source.CurrentVideoUrl, RenderHeads.Media.AVProVideo.MediaPathType.AbsolutePathOrURL), false);
            }
        }

        private void OnSeekSliderValueChanged(float value)
        {
            if (source == null)
                return;
            MediaManager.Instance.Seek(source.playListId, value);
        }

        private void OnVolumeSliderValueChanged(float value)
        {
            if (source == null)
                return;
            MediaManager.Instance.Volume(source.playListId, value);
        }

        public void OnClickPlay()
        {
            if (source == null)
                return;
            MediaManager.Instance.Play(source.playListId);
        }

        public void OnClickPause()
        {
            if (source == null)
                return;
            MediaManager.Instance.Pause(source.playListId);
        }

        public void OnClickStop()
        {
            if (source == null)
                return;
            MediaManager.Instance.Stop(source.playListId);
        }

        public void OnClickDelete()
        {
            if (source == null)
                return;
            MediaManager.Instance.Delete(source.CurrentMediaId);
        }

        public void OnClickUpload()
        {
            if (source == null)
                return;
            FileBrowser.SetFilters(true, new FileBrowser.Filter("Video Files", ".mp4"));
            StartCoroutine(OpenFile());
        }

        IEnumerator OpenFile()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Load Files", "Load");

            if (FileBrowser.Success)
            {
                var splitedPath = FileBrowser.Result[0].Split('.');
                byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);
                MediaManager.Instance.Upload(source.playListId, bytes, splitedPath[splitedPath.Length - 1]);
            }
            else
            {
                Debug.LogError("Wrong select file path");
            }
        }

        protected virtual void AVProMediaPlayer_HandleEvent(RenderHeads.Media.AVProVideo.MediaPlayer mediaPlayer, RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType eventType, RenderHeads.Media.AVProVideo.ErrorCode code)
        {
            if (eventType == RenderHeads.Media.AVProVideo.MediaPlayerEvent.EventType.ReadyToPlay)
            {
                mediaPlayer.Control.Seek(source.LastResp.time);
                if (source.LastResp.isPlaying)
                {
                    mediaPlayer.Play();
                }
                else if (source.LastResp.time <= 0f)
                {
                    mediaPlayer.Stop();
                }
                else
                {
                    mediaPlayer.Pause();
                }
            }
        }
    }
}
