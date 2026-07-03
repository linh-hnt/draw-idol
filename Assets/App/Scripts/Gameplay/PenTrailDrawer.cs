using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;
using Dreamteck.Splines;
using DigitalRuby.FastLineRenderer;

namespace App
{
    /// <summary>
    /// Moves a Pen Transform along a SplineComputer and draws the traveled path using a FastLineRenderer.
    /// Supports auto-play mode and manual touch/click mode with DOTween transitions and idle offset.
    /// </summary>
    public class PenTrailDrawer : MonoBehaviour
    {
        [Header("Spline")]
        [SerializeField] private SplineComputer _spline;
        [SerializeField] private Transform _penTransform;

        [Header("Movement")]
        [SerializeField] private float _speed = 2f;
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] private bool _loop;

        [Header("Trail")]
        [SerializeField] private float _trailPointSpacing = 0.05f;
        [SerializeField] private int _maxTrailPoints = 500;

        [Header("Events")]
        public UnityEvent onPaintComplete;

        [Header("Manual Input")]
        [SerializeField] private Vector3 _idleOffset = new Vector3(0.15f, 0.15f, 0f);
        [SerializeField] private float _transitionDuration = 0.2f;
        [SerializeField] private Ease _transitionEase = Ease.OutCubic;

        [Header("Line")]
        [SerializeField] private FastLineRenderer _fastLineRenderer;
        [SerializeField] private float _lineRadius = 0.13f;

        private FastLineRendererProperties _lineProps;
        private List<Vector3> _trailPoints;
        private double _currentPercent;
        private float _traveledDistance;
        private bool _isComplete;

        private bool _isPaused;
        private bool _isManualMoving;
        private Tweener _transitionTween;
        private Transform _penTransformCache;

        public SplineComputer Spline
        {
            get => _spline;
            set => _spline = value;
        }

        public Transform PenTransform
        {
            get => _penTransform;
            set
            {
                _penTransform = value;
                _penTransformCache = value;
            }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public bool IsComplete => _isComplete;

        public double CurrentPercent => _currentPercent;

        private void Awake()
        {
            _lineProps = new FastLineRendererProperties();
            _trailPoints = new List<Vector3>(_maxTrailPoints);
        }

        private void Start()
        {
            if (_penTransform == null)
            {
                _penTransform = transform;
            }
            _penTransformCache = _penTransform;

            if (!_autoPlay && _spline != null)
            {
                ApplyIdleOffset();
            }
        }

        private void Update()
        {
            if (_spline == null || _isComplete || _isPaused)
            {
                return;
            }

            if (_autoPlay)
            {
                MoveAlongSpline();
            }
            else
            {
                HandleManualInput();
            }
        }

        private void MoveAlongSpline()
        {
            var previousPercent = _currentPercent;

            _traveledDistance += _speed * Time.deltaTime;
            var totalLength = _spline.CalculateLength();
            var distance = Mathf.Min(_traveledDistance, totalLength);

            _currentPercent = _spline.Travel(0.0, distance);

            if (_traveledDistance >= totalLength)
            {
                _currentPercent = 1.0;
                _traveledDistance = totalLength;

                if (!_loop)
                {
                    _isComplete = true;
                    _isManualMoving = false;
                    onPaintComplete?.Invoke();
                }
                else
                {
                    _traveledDistance = 0f;
                    _currentPercent = 0.0;
                }
            }

            var sample = _spline.Evaluate(_currentPercent);
            _penTransformCache.position = sample.position;

            if (!Mathf.Approximately((float)_currentPercent, (float)previousPercent))
            {
                UpdateTrail();
            }
        }

        private void UpdateTrail()
        {
            if (_fastLineRenderer == null) return;

            if (_currentPercent <= 0.0)
            {
                _fastLineRenderer.Reset();
                _fastLineRenderer.Apply();
                return;
            }

            var totalLength = _spline.CalculateLength(0.0, _currentPercent);
            var pointCount = Mathf.Min(
                Mathf.CeilToInt(totalLength / _trailPointSpacing) + 1,
                _maxTrailPoints
            );

            if (pointCount < 2)
            {
                _fastLineRenderer.Reset();
                _fastLineRenderer.Apply();
                return;
            }

            _trailPoints.Clear();

            for (var i = 0; i < pointCount; i++)
            {
                var t = (double)i / (pointCount - 1);
                var percent = t * _currentPercent;
                _trailPoints.Add(_spline.EvaluatePosition(percent));
            }

            _lineProps.Radius = _lineRadius;
            _lineProps.LineJoin = FastLineRendererLineJoin.Round;
            _fastLineRenderer.Reset();
            _fastLineRenderer.AddLine(_lineProps, _trailPoints, null, startCap: true, endCap: true);
            _fastLineRenderer.Apply();
        }

        // ──────────────────────────────────────────────
        // Manual Input
        // ──────────────────────────────────────────────

        private bool IsTouchingDown()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    return !IsPointerOverUI(touch.fingerId);
                }
            }
            return false;
#else
            if (Input.GetMouseButtonDown(0))
            {
                return !IsPointerOverUI();
            }
            return false;
#endif
        }

        private bool IsTouching()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    return false;
                }
                return !IsPointerOverUI(touch.fingerId);
            }
            return false;
#else
            if (Input.GetMouseButton(0))
            {
                return !IsPointerOverUI();
            }
            return false;
#endif
        }

        private bool IsPointerOverUI(int fingerId = -1)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return eventSystem.IsPointerOverGameObject(fingerId);
#else
            return eventSystem.IsPointerOverGameObject();
#endif
        }

        private void HandleManualInput()
        {
            if (_isComplete)
            {
                return;
            }

            if (_transitionTween != null && _transitionTween.IsPlaying())
            {
                return;
            }

            if (IsTouchingDown())
            {
                if (!_isManualMoving)
                {
                    StartManualMove();
                }
                return;
            }

            if (_isManualMoving)
            {
                if (IsTouching())
                {
                    MoveAlongSpline();
                }
                else
                {
                    StopManualMove();
                }
            }
        }

        private void StartManualMove()
        {
            _transitionTween?.Kill();
            _transitionTween = null;

            _isManualMoving = false;

            var targetPos = _spline.EvaluatePosition(_currentPercent);

            _transitionTween = _penTransformCache
                .DOMove(targetPos, _transitionDuration)
                .SetEase(_transitionEase)
                .OnComplete(() =>
                {
                    _transitionTween = null;
                    _isManualMoving = true;
                });
        }

        private void StopManualMove()
        {
            _isManualMoving = false;

            Debug.Log("Stopping manual move, transitioning to idle offset position.");

            _transitionTween?.Kill();
            _transitionTween = null;

            if (_spline == null || _penTransformCache == null) return;

            var targetPos = _spline.EvaluatePosition(_currentPercent) + _idleOffset;

            _transitionTween = _penTransformCache
                .DOMove(targetPos, _transitionDuration)
                .SetEase(_transitionEase)
                .OnComplete(() =>
                {
                    _transitionTween = null;
                });
        }

        private void ApplyIdleOffset()
        {
            if (_spline == null || _penTransformCache == null) return;
            _penTransformCache.position = _spline.EvaluatePosition(_currentPercent) + _idleOffset;
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Resets the pen to the beginning of the spline and clears the trail.
        /// </summary>
        public void ResetPen()
        {
            _transitionTween?.Kill();
            _transitionTween = null;

            _currentPercent = 0.0;
            _traveledDistance = 0f;
            _isComplete = false;
            _isManualMoving = false;

            if (_fastLineRenderer != null)
            {
                _fastLineRenderer.Reset();
                _fastLineRenderer.Apply();
            }

            if (_spline != null && _penTransformCache != null)
            {
                if (_autoPlay)
                {
                    var sample = _spline.Evaluate(0.0);
                    _penTransformCache.position = sample.position;
                }
                else
                {
                    ApplyIdleOffset();
                }
            }
        }

        /// <summary>
        /// Starts or resumes pen movement. In auto mode, pen moves automatically.
        /// In manual mode, enables input listening (pen moves while touch/click is held).
        /// </summary>
        public void Play() => _isPaused = false;

        /// <summary>
        /// Pauses pen movement. In manual mode, transitions pen to idle offset position.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;

            if (!_autoPlay && _isManualMoving)
            {
                StopManualMove();
            }
        }

        /// <summary>
        /// Jumps the pen to a specific percent along the spline.
        /// </summary>
        public void SetPercent(double percent)
        {
            if (_spline == null) return;

            _transitionTween?.Kill();
            _transitionTween = null;

            _currentPercent = Mathf.Clamp01((float)percent);
            _traveledDistance = _spline.CalculateLength(0.0, _currentPercent);

            if (_penTransformCache != null)
            {
                if (_autoPlay || _isManualMoving)
                {
                    var sample = _spline.Evaluate(_currentPercent);
                    _penTransformCache.position = sample.position;
                }
                else
                {
                    ApplyIdleOffset();
                }
            }

            UpdateTrail();
        }

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void OnDisable()
        {
            _transitionTween?.Kill();
            _transitionTween = null;
        }

        private void OnDestroy()
        {
            _transitionTween?.Kill();
            _transitionTween = null;
        }
    }
}
