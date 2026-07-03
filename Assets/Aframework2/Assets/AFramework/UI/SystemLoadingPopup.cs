using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AFramework.UI
{
    public class SystemLoadingPopup : BaseUIMenu
    {
        TMPro.TextMeshProUGUI mWaitText;

        private void Start()
        {
            _UILayer = eUILayer.AlwaysOnTop;

            var background = new GameObject("Background").AddComponent<Image>();
            background.transform.SetParent(this.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = Vector3.one;
            background.color = new Color(0, 0, 0, 0.75f);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.anchoredPosition = Vector2.zero;

            var waitText = new GameObject("WaitText").AddComponent<TMPro.TextMeshProUGUI>();
            waitText.transform.SetParent(this.transform);
            waitText.transform.localPosition = Vector3.zero;
            waitText.transform.localScale = Vector3.one;
            waitText.color = Color.white;            
            waitText.alignment = TMPro.TextAlignmentOptions.Center;
            if (AFramework.UI.CanvasManager.UICanvas != null)
            {
                var canvasSize = AFramework.UI.CanvasManager.UIRectTrans.rect.size;
                waitText.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(canvasSize.x, canvasSize.y), Mathf.Min(canvasSize.x, canvasSize.y) * 0.05f);
                //float minSize = Mathf.Min(canvasSize.x, canvasSize.y);
                waitText.fontSize = 80;// * (minSize / 1080);
                waitText.enableAutoSizing = true;
            }
            else
            {
                waitText.fontSize = 60;
                waitText.enableWordWrapping = false;
            }

            waitText.text = "Loading...";
            mWaitText = waitText;
        }

        private void Update()
        {
            this.transform.SetAsLastSibling();
            var timeOffset = Time.unscaledTime % 3;
            mWaitText.text = "Loading." + (timeOffset >= 1 ? "." : " ") + (timeOffset >= 2 ? "." : " "); 
        }
    }
}