using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class SpriteRendererExtensions
    {
        public static void FitCamera(this SpriteRenderer sr, Camera cam)
        {
            if (cam.orthographic)
            {
                float worldScreenHeight = cam.orthographicSize * 2;
                float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

                sr.transform.localScale = new Vector3(
                    worldScreenWidth / sr.sprite.bounds.size.x,
                    worldScreenHeight / sr.sprite.bounds.size.y, 1);
            }
            else
            {
                float spriteHeight = sr.sprite.bounds.size.y;
                float spriteWidth = sr.sprite.bounds.size.x;
                float distance = sr.transform.position.z - cam.transform.position.z;
                float screenHeight = 2 * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2) * distance;
                float screenWidth = screenHeight * cam.aspect;
                sr.transform.localScale = new Vector3(screenWidth / spriteWidth, screenHeight / spriteHeight, 1f);
            }
        }
    }
}