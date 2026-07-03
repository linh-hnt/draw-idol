using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AFramework.UI
{
    public class HoldToScaleButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler, IPointerEnterHandler
    {
        [SerializeField] private Transform _scaleTransform;
        [SerializeField]
        Vector3 ScaleValue = new Vector3(1.2f, 1.2f, 1.2f);

        Vector3 mBaseScale;
        bool mOnHold = false;
        Button mButton = null;

        void Awake()
        {
            if (_scaleTransform == null)
            {
                _scaleTransform = transform;
            }
            mBaseScale = _scaleTransform.localScale;
            mButton = GetComponent<Button>();
        }

        private void OnDisable()
        {
            mOnHold = false;
            _scaleTransform.localScale = mBaseScale;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            mOnHold = true && mButton.interactable;
            if (mOnHold)
            {
                _scaleTransform.localScale = ScaleValue;
            }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (mOnHold)
            {
                _scaleTransform.localScale = mBaseScale;
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            mOnHold = false;
            _scaleTransform.localScale = mBaseScale;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (mOnHold)
            {
                _scaleTransform.localScale = ScaleValue;
            }
        }
    }
}