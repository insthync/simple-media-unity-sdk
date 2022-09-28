using System.Collections;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SimpleMediaSDK
{
    public class UIMediaPlayer : MonoBehaviour
    {
        public RenderTexture targetTexture;
        public RenderHeads.Media.AVProVideo.ApplyToMaterial applyToMaterial;
        public Slider seekSlider;
        public Slider volumeSlider;
        public UIMediaList mediaList;
        protected float dirtyLastRespTime;

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
                    if (source.avProPlayer == null && source.videoPlayer != null)
                    {
                        source.videoPlayer.renderMode = defaultSourceRenderMode;
                        source.videoPlayer.targetTexture = defaultSourceRenderTexture;
                        source.videoPlayer.audioOutputMode = defaultSourceAudioOutputMode;
                        source.videoPlayer.Stop();
                    }
                    MediaManager.Instance.Sub(source.playListId);
                }
                source = value;
                if (applyToMaterial != null)
                {
                    if (source.avProPlayer != null)
                        applyToMaterial.Player = source.avProPlayer;
                    applyToMaterial.gameObject.SetActive(source.avProPlayer != null);
                }
                if (source.avProPlayer != null)
                {
                    if (source != null)
                        source.SetAudioToDirect();
                    else
                        source.SetAudioToUnity();
                }
                if (source != null)
                {
                    if (source.avProPlayer == null && source.videoPlayer != null)
                    {
                        defaultSourceRenderMode = source.videoPlayer.renderMode;
                        defaultSourceRenderTexture = source.videoPlayer.targetTexture;
                        defaultSourceAudioOutputMode = source.videoPlayer.audioOutputMode;
                        source.videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                        source.videoPlayer.targetTexture = targetTexture;
                        source.videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                        source.videoPlayer.Stop();
                    }
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
        }

        protected void OnDisable()
        {
            if (seekSlider)
                seekSlider.onValueChanged.RemoveListener(OnSeekSliderValueChanged);
            if (volumeSlider)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
            MediaManager.Instance.onUpload -= Instance_onUploadVideo;
            MediaManager.Instance.onDelete -= Instance_onDeleteVideo;
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
                if (seekSlider)
                {
                    seekSlider.minValue = 0;
                    seekSlider.maxValue = (float)source.LastResp.duration;
                    seekSlider.SetValueWithoutNotify((float)source.LastResp.time + Time.unscaledTime - source.LastRespTime);
                }
                if (volumeSlider)
                {
                    volumeSlider.minValue = 0;
                    volumeSlider.maxValue = 1;
                    volumeSlider.SetValueWithoutNotify(source.LastResp.volume);
                }
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
    }
}
