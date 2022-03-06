using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine.UI;

namespace SimpleMediaSDK
{
    public class UIMediaPlayer : MediaPlayer
    {
        public Slider seekSlider;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (seekSlider)
                seekSlider.onValueChanged.AddListener(OnSeekSliderValueChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (seekSlider)
                seekSlider.onValueChanged.RemoveListener(OnSeekSliderValueChanged);
        }

        private void OnSeekSliderValueChanged(float value)
        {

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
            var extensions = new[] {
                new ExtensionFilter("Video Files", "mp4")
            };
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, false, OnOpenVideoFile);
        }

        private void OnOpenVideoFile(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;
            var path = paths[0];
            if (File.Exists(path))
            {
                var splitedPath = path.Split('.');
                MediaManager.Instance.Upload(playListId, File.ReadAllBytes(path), splitedPath[splitedPath.Length - 1]);
            }
            else
            {
                Debug.LogError("Wrong select file path");
            }
        }

        /*

        public void OnClickAddAudio()
        {
            if (!CanManage())
                return;
            var extensions = new[] {
                new ExtensionFilter("Audio Files", "wav")
        };
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, false, OnOpenAudioFile);
        }

        private void OnOpenAudioFile(string[] paths)
        {
            if (!CanManage())
                return;
            if (paths == null || paths.Length == 0)
                return;
            var path = paths[0];
            if (File.Exists(path))
            {
                var splitedPath = path.Split('.');
                StartCoroutine(UploadMediaRoutine(File.ReadAllBytes(path), splitedPath[splitedPath.Length - 1]));
            }
            else
            {
                Debug.LogError("Wrong select file path");
            }
        }
        */
    }
}
