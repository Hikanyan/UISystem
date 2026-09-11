using UnityEngine;

namespace HikanyanLibrary.UITools
{
    public interface IInfiniteScroll
    {
        void OnPostSetupItems();
        void OnUpdateItem(int itemCount, GameObject obj);
    }
}