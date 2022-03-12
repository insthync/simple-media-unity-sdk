using System.Collections;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleMediaSDK
{
    public class UIMediaPlayer : MediaPlayer
    {
        public Slider seekSlider;
        public UIMediaList mediaList;
        protected float dirtyLastRespTime;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (seekSlider)
                seekSlider.onValueChanged.AddListener(OnSeekSliderValueChanged);
            if (mediaList)
                mediaList.Load(playListId);
            MediaManager.Instance.onUpload += Instance_onUploadVideo;
            MediaManager.Instance.onDelete += Instance_onDeleteVideo;
            // Don't register UI media player
            RegisteredMediaPlayers.Remove(this);
            foreach (var player in RegisteredMediaPlayers)
            {
                player.Mute = true;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (seekSlider)
                seekSlider.onValueChanged.RemoveListener(OnSeekSliderValueChanged);
            MediaManager.Instance.onUpload -= Instance_onUploadVideo;
            MediaManager.Instance.onDelete -= Instance_onDeleteVideo;
            foreach (var player in RegisteredMediaPlayers)
            {
                player.Mute = false;
            }
        }

        private void Instance_onUploadVideo()
        {
            if (mediaList)
                mediaList.Load(playListId);
        }

        private void Instance_onDeleteVideo()
        {
            if (mediaList)
                mediaList.Load(playListId);
        }

        protected override void Update()
        {
            base.Update();
            if (seekSlider)
            {
                seekSlider.minValue = 0;
                seekSlider.maxValue = (float)LastResp.duration;
                if (dirtyLastRespTime != LastRespTime || LastResp.isPlaying)
                {
                    dirtyLastRespTime = LastRespTime;
                    seekSlider.SetValueWithoutNotify((float)LastResp.time + Time.unscaledTime - LastRespTime);
                }
            }
        }

        private void OnSeekSliderValueChanged(float value)
        {
            MediaManager.Instance.Seek(playListId, value);
        }

        public void OnClickPlay()
        {
            MediaManager.Instance.Play(playListId);
        }

        public void OnClickPause()
        {
            MediaManager.Instance.Pause(playListId);
        }

        public void OnClickStop()
        {
            MediaManager.Instance.Stop(playListId);
        }

        public void OnClickDelete()
        {
            MediaManager.Instance.Delete(CurrentMediaId);
        }

        public void OnClickUpload()
        {
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
                MediaManager.Instance.Upload(playListId, bytes, splitedPath[splitedPath.Length - 1]);
            }
            else
            {
                Debug.LogError("Wrong select file path");
            }
        }
    }
}
