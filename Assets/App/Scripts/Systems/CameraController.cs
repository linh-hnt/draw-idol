using UnityEngine;
using DG.Tweening;

namespace App
{
    /// <summary>
    /// Controls camera zoom and pan with DOTween transitions.
    /// Blocks all input (including UI) via a full-screen blocker while transitioning.
    /// Designed for portrait-mode orthographic cameras.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private Ease _transitionEase = Ease.OutCubic;
        [Tooltip("Multiplier applied to the calculated orthographic size. 1.0 = exact fit, >1 = zoom out more.")]

        [SerializeField] private float minOrthorNumber = 5;
        [SerializeField] private float _scaleMultiplier = 1f;
        [Tooltip("World-space offset added to the calculated camera position (e.g., to make room for UI/banner).")]
        [SerializeField] private Vector2 _offset;

        [Header("Input Blocking")]
        [Tooltip("Full-screen transparent Image with RaycastTarget enabled. Activated during transitions to block all input.")]
        [SerializeField] private GameObject _blockerObject;

        private Tweener _moveTween;
        private Tweener _zoomTween;
        private bool _isTransitioning;

        /// <summary>True while the camera is moving or zooming to a new target.</summary>
        public bool IsTransitioning => _isTransitioning;

        public Camera Camera => _targetCamera;

        /// <summary>
        /// Animates the camera so <paramref name="bounds"/> are fully visible and centered on screen,
        /// accounting for <see cref="_offset"/> and <see cref="_scaleMultiplier"/>.
        /// Kills any in-progress transition before starting a new one.
        /// </summary>
        public void MoveToBounds(Bounds bounds)
        {
            if (_targetCamera == null)
            {
                Debug.LogError($"[{nameof(CameraController)}] Target camera is not assigned.", this);
                return;
            }

            KillTransition();
            BlockInput(true);

            var aspect = (float)Screen.width / Screen.height;
            var fitByHeight = bounds.size.y / 2f;
            var fitByWidth = bounds.size.x / (aspect * 2f);
            var orthoSize = Mathf.Max(fitByHeight, fitByWidth) * _scaleMultiplier;

            orthoSize = Mathf.Max(minOrthorNumber, orthoSize);

            var targetPos = new Vector3(
                bounds.center.x + _offset.x,
                bounds.center.y + _offset.y,
                _targetCamera.transform.position.z
            );

            _moveTween = _targetCamera.transform
                .DOMove(targetPos, _transitionDuration)
                .SetEase(_transitionEase)
                .OnComplete(() =>
                {
                    _moveTween = null;
                    CheckTransitionComplete();
                });

            _zoomTween = _targetCamera
                .DOOrthoSize(orthoSize, _transitionDuration)
                .SetEase(_transitionEase)
                .OnComplete(() =>
                {
                    _zoomTween = null;
                    CheckTransitionComplete();
                });
        }

        /// <summary>
        /// Immediately kills any active camera tween and unblocks input.
        /// </summary>
        public void KillTransition()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _zoomTween?.Kill();
            _zoomTween = null;
        }

        private void CheckTransitionComplete()
        {
            if (_moveTween == null && _zoomTween == null)
            {
                BlockInput(false);
            }
        }

        private void BlockInput(bool block)
        {
            _isTransitioning = block;
            if (_blockerObject != null)
            {
                _blockerObject.SetActive(block);
            }
        }

        private void OnDisable()
        {
            KillTransition();
        }

        private void OnDestroy()
        {
            KillTransition();
        }
    }
}
