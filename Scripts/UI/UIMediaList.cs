using UnityEngine;

namespace SimpleMediaSDK
{
    public class UIMediaList : MonoBehaviour
    {
        public UIMediaListEntry entryPrefab;
        public Transform entryContainer;

        public async void Load(string playListId)
        {
            var list = await MediaManager.Instance.Get(playListId);
            for (int i = entryContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(entryContainer.GetChild(i).gameObject);
            }
            foreach (var entry in list)
            {
                var newEntry = Instantiate(entryPrefab, entryContainer);
                newEntry.transform.position = Vector3.zero;
                newEntry.transform.rotation = Quaternion.identity;
                newEntry.transform.localScale = Vector3.one;
                newEntry.Data = entry;
            }
        }
    }
}
