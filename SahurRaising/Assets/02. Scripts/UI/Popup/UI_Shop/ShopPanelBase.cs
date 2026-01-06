using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    public abstract class ShopPanelBase : MonoBehaviour
    {
        [SerializeField] protected ShopType _shopType;

        public ShopType ShopType => _shopType;

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public virtual void OnShow()
        {
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
        }

        public virtual void OnHide()
        {
        }

        public virtual void Initialize() { }

        public virtual async UniTask InitializeAsync() { await UniTask.Yield(); }
    }
}
