using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleMediaSDK
{
    public class UIMediaListEntry : MonoBehaviour
    {
        public Text textId;
        public Text textPlayListId;
        public Text textTitle;
        public Text textDuration;
        public Text textSortOrder;
        public MediaData Data { get; set; }

        void Update()
        {
            if (textId)
                textId.text = Data.id;

            if (textPlayListId)
                textPlayListId.text = Data.playListId;

            if (textTitle)
                textTitle.text = "";

            if (textDuration)
                textDuration.text = Data.duration.ToString("N2");

            if (textSortOrder)
                textSortOrder.text = Data.sortOrder.ToString("N2");
        }

        public void OnClickSwitch()
        {
            MediaManager.Instance.Switch(Data.playListId, Data.id);
        }

        public void OnClickDelete()
        {
            MediaManager.Instance.Delete(Data.id);
        }
    }
}
