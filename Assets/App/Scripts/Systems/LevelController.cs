using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Dreamteck.Splines;
using System.Collections.Generic;
using AFramework.UI;
using App;

namespace App
{
    /// <summary>
    /// Manages sequential 2-phase gameplay: Draw phase (pen moves along splines) → Paint phase (free-draw painting).
    /// Configure draw steps and paint steps in the Inspector, then call <see cref="Begin"/> to start.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // Nested Types
        // ──────────────────────────────────────────────

        /// <summary>Current phase of the level controller.</summary>
        public enum Phase
        {
            /// <summary>Not yet started.</summary>
            Idle,
            /// <summary>Draw phase: pen traverses splines.</summary>
            Draw,
            /// <summary>Paint phase: user paints textures.</summary>
            Paint,
            /// <summary>All steps completed.</summary>
            Complete
        }

        /// <summary>
        /// Data for a single draw step: a spline the pen follows, a PenTrailDrawer to animate it,
        /// a SplinePrefabSpawner to place point prefabs, and objects to show/hide.
        /// </summary>
        [System.Serializable]
        public sealed class DrawStepData
        {
            [Tooltip("The spline the pen travels along during this step.")]
            public SplineComputer spline;

            [Tooltip("The PenTrailDrawer that moves the pen and draws the trail.")]
            public PenTrailDrawer penTrailDrawer;

            [Tooltip("Spawner that places prefab instances along the spline.")]
            public SplinePrefabSpawner pointSpawner;

            [Header("Show / Hide")]
            [Tooltip("GameObjects to activate when this step starts.")]
            public GameObject[] showObjects;

            [Tooltip("GameObjects to deactivate when this step starts.")]
            public GameObject[] hideObjects;
        }

        /// <summary>
        /// Data for a single paint step: a SpritePainter the user paints on, plus objects to show/hide.
        /// </summary>
        [System.Serializable]
        public sealed class PaintStepData
        {
            [Tooltip("The SpritePainter that handles CPU painting on the target texture.")]
            public SpritePainter spritePainter;

            [Header("Brush Colors")]
            [Tooltip("Color content with selectable colors and correct color index for this paint step.")]
            [SerializeField] private ColorContent _brushColorContent;

            [Header("Pen")]
            [Tooltip("Optional per-step pen idle offset override. Uses SpritePainter's default if (0,0,0).")]
            public Vector3 penOffsetOverride = Vector3.zero;

            [Header("Show / Hide")]
            [Tooltip("GameObjects to activate when this step starts.")]
            public GameObject[] showObjects;

            [Tooltip("GameObjects to deactivate when this step starts.")]
            public GameObject[] hideObjects;

            /// <summary>Returns the ColorContent with selectable colors and correct color index for this paint step.</summary>
            public ColorContent GetBrushColorContent() => _brushColorContent;
        }

        // ──────────────────────────────────────────────
        // Serialized Fields
        // ──────────────────────────────────────────────
        [Header("Draw Phase")]
        [SerializeField] private DrawStepData[] _drawSteps;

        [Header("Paint Phase")]
        [SerializeField] private PaintStepData[] _paintSteps;
        [SerializeField] private GameObject _iconObject;

        [Header("Camera")]
        [SerializeField] private CameraController _cameraController;

        [Header("Settings")]
        [SerializeField] private bool _autoBegin = false;

        [Header("Pen Transition")]
        [SerializeField] private float _penTransitionDuration = 0.3f;
        [SerializeField] private Ease _penTransitionEase = Ease.OutCubic;

        [Header("Events")]
        [SerializeField] private UnityEvent _onLevelComplete = new UnityEvent();

        // ──────────────────────────────────────────────
        // Private State
        // ──────────────────────────────────────────────

        private Phase _currentPhase = Phase.Idle;
        private int _currentStepIndex = -1;
        private PenTrailDrawer _activePenTrailDrawer;
        private SpritePainter _activeSpritePainter;
        private bool _finishPaintStep = false;
        private Transform _penTransform;
        private Tweener _penTween;
        private int _paintCorrectCount = 0;
        private bool[] _paintStepCorrectSelection;

        // ──────────────────────────────────────────────
        // Public Properties
        // ──────────────────────────────────────────────

        /// <summary>The current phase of the level controller.</summary>
        public Phase CurrentPhase => _currentPhase;

        /// <summary>Index of the currently active step within the current phase, or -1 if none.</summary>
        public int CurrentStepIndex => _currentStepIndex;

        /// <summary>True when all draw and paint steps have been completed.</summary>
        public bool IsCompleted => _currentPhase == Phase.Complete;

        /// <summary>Fired when all steps are complete.</summary>
        public UnityEvent onLevelComplete => _onLevelComplete;

        /// <summary>Total number of configured draw steps.</summary>
        public int DrawStepCount => _drawSteps?.Length ?? 0;

        /// <summary>Total number of configured paint steps.</summary>
        public int PaintStepCount => _paintSteps?.Length ?? 0;

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Assigns the pen transform to all PenTrailDrawers in draw steps.
        /// Called by GameplayManager after level is instantiated.
        /// </summary>
        public void SetPenTransform(Transform penTransform)
        {
            _penTransform = penTransform;

            if (_drawSteps == null) return;
            for (int i = 0; i < _drawSteps.Length; i++)
            {
                if (_drawSteps[i]?.penTrailDrawer != null)
                {
                    _drawSteps[i].penTrailDrawer.PenTransform = penTransform;
                }
            }

            if (_paintSteps != null)
            {
                for (int i = 0; i < _paintSteps.Length; i++)
                {
                    if (_paintSteps[i]?.spritePainter != null)
                    {
                        _paintSteps[i].spritePainter.PenTransform = penTransform;
                    }
                }
            }
        }

        /// <summary>
        /// Assigns the CameraController reference. Called by GameplayManager after level is instantiated.
        /// </summary>
        public void SetCameraController(CameraController controller)
        {
            _cameraController = controller;
        }

        private void Start()
        {
            if (_autoBegin)
            {
                Begin();
            }
        }

        private void OnEnable()
        {
            EventManager.I.SetPaintColorAction += OnSelectColor;
            EventManager.I.FinishPaintAction += OnFinishPaintAction;
        }

        private void OnDisable()
        {
            EventManager.I.SetPaintColorAction -= OnSelectColor;
            EventManager.I.FinishPaintAction -= OnFinishPaintAction;
        }

        /// <summary>
        /// Begins the level sequence. Starts the first draw step, or the first paint step
        /// if no draw steps are configured.
        /// </summary>
        public void Begin()
        {
            if (_currentPhase == Phase.Draw || _currentPhase == Phase.Paint)
            {
                Debug.LogWarning($"[{nameof(LevelController)}] Begin() ignored — already in {_currentPhase} phase.", this);
                return;
            }

            _finishPaintStep = false;

            InitLevel();

            UnsubscribeCurrentStep();
            _activePenTrailDrawer = null;
            _activeSpritePainter = null;

            if (_drawSteps != null && _drawSteps.Length > 0)
            {
                StartDrawStep(0);
            }
            // else if (_paintSteps != null && _paintSteps.Length > 0)
            // {
            //     StartPaintStep(0);
            // }
            else
            {
                Debug.LogWarning($"[{nameof(LevelController)}] No draw or paint steps configured. Completing immediately.", this);
                CompleteLevel();
            }
        }

        private void InitLevel()
        {
            _paintCorrectCount = 0;
            int paintCount = _paintSteps?.Length ?? 0;
            _paintStepCorrectSelection = new bool[paintCount];

            if (_iconObject != null)
                _iconObject.SetActive(false);

            for (int i = 0, length = _drawSteps.Length; i < length; i++)
            {
                var step = _drawSteps[i];
                if (step == null) continue;

                if (_drawSteps[i] != null && _drawSteps[i].penTrailDrawer != null)
                {
                    _drawSteps[i].penTrailDrawer.gameObject.SetActive(false);
                }
            }

            for (int i = 0, length = _paintSteps.Length; i < length; i++)
            {
                var step = _paintSteps[i];
                if (step == null) continue;

                if (_paintSteps[i] != null && _paintSteps[i].spritePainter != null)
                {
                    _paintSteps[i].spritePainter.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Skips the current step and advances to the next one.
        /// During Draw: pauses the PenTrailDrawer. During Paint: fills remaining pixels via <see cref="SpritePainter.FillAll"/>.
        /// </summary>
        public void SkipCurrentStep()
        {
            switch (_currentPhase)
            {
                case Phase.Draw:
                    SkipDrawStep();
                    break;
                case Phase.Paint:
                    SkipPaintStep();
                    break;
                default:
                    Debug.LogWarning($"[{nameof(LevelController)}] Cannot skip — current phase is {_currentPhase}.", this);
                    break;
            }
        }

        // ──────────────────────────────────────────────
        // Draw Phase
        // ──────────────────────────────────────────────

        private void StartDrawStep(int index)
        {
            if (_drawSteps == null || index >= _drawSteps.Length)
            {
                TransitionToPaint();
                return;
            }

            _currentPhase = Phase.Draw;
            _currentStepIndex = index;

            var step = _drawSteps[index];
            if (step == null)
            {
                Debug.LogError($"[{nameof(LevelController)}] Draw step {index} is null. Skipping.", this);
                AdvanceDrawStep();
                return;
            }

            step.penTrailDrawer.gameObject.SetActive(true);

            if (step.pointSpawner != null)
            {
                if (step.spline != null)
                    step.pointSpawner.Spline = step.spline;

                step.pointSpawner.Spawn();
            }

            if (step.penTrailDrawer == null)
            {
                Debug.LogError($"[{nameof(LevelController)}] PenTrailDrawer is null for draw step {index}. Skipping.", this);
                AdvanceDrawStep();
                return;
            }

            if (step.spline != null)
                step.penTrailDrawer.Spline = step.spline;

            UnsubscribeCurrentStep();
            _activePenTrailDrawer = step.penTrailDrawer;
            _activePenTrailDrawer.onPaintComplete.AddListener(OnPenTrailComplete);
            _activePenTrailDrawer.ResetPen();
            _activePenTrailDrawer.Play();

            FocusCameraOnDrawStep(index);
        }

        private void OnPenTrailComplete()
        {
            if (_currentPhase != Phase.Draw) return;

            var step = _drawSteps[_currentStepIndex];
            if (step != null)
            {
                ApplyVisibility(step.showObjects, step.hideObjects);
            }

            UnsubscribeCurrentStep();
            AdvanceDrawStep();
        }

        private void AdvanceDrawStep()
        {
            var nextIndex = _currentStepIndex + 1;
            if (_drawSteps != null && nextIndex < _drawSteps.Length)
            {
                StartDrawStep(nextIndex);
            }
            else
            {
                TransitionToPaint();
            }
        }

        private void SkipDrawStep()
        {
            if (_activePenTrailDrawer != null)
                _activePenTrailDrawer.Pause();

            UnsubscribeCurrentStep();
            AdvanceDrawStep();
        }

        private void TransitionToPaint()
        {
            UnsubscribeCurrentStep();
            _activePenTrailDrawer = null;

            UpdateLevelProgress();

            if (_paintSteps != null && _paintSteps.Length > 0)
            {
                FinishDraw();
            }
            else
            {
                CompleteLevel();
            }
        }

        private void FinishDraw()
        {
            _currentStepIndex = 0;

            StartPaintStep(_currentStepIndex);

            if (_paintSteps[_currentStepIndex]?.spritePainter != null)
            {
                _paintSteps[_currentStepIndex].spritePainter.PaintingEnabled = false;
            }

            TweenPenOutOfCamera(() =>
            {
                var colorContent = _paintSteps[_currentStepIndex].GetBrushColorContent();
                // CanvasManager.Push(SelectColorPopup.Identifier, new object[] { colorContent, false });
                EventManager.I.ShowIngameSelectColorAction?.Invoke(colorContent, false);
            });
        }

        // ──────────────────────────────────────────────
        // Paint Phase
        // ──────────────────────────────────────────────
        private void OnSelectColor(Color selectColor, int colorIndex)
        {
            CanvasManager.PopAllLayer(eUILayer.Popup);

            EventManager.I.HideIngameSelectColorAction?.Invoke();

            var step = _paintSteps[_currentStepIndex];
            step.spritePainter.BrushColor = selectColor;

            var colorContent = step.GetBrushColorContent();
            if (colorContent != null && colorIndex >= 0 && colorIndex == colorContent.indexCorrect)
            {
                if (!_paintStepCorrectSelection[_currentStepIndex])
                {
                    _paintStepCorrectSelection[_currentStepIndex] = true;
                    _paintCorrectCount++;
                    UpdateLevelProgress();
                }
            }

            TweenPenToPaintPosition(step, () =>
            {
                step.spritePainter.PaintingEnabled = true;
            });
        }

        private void OnFinishPaintAction()
        {
            int preStepIndex = Mathf.Max(0, _currentStepIndex - 1);
            var preStep = _paintSteps[preStepIndex];
            if (preStep != null)
            {
                preStep.spritePainter.FillAll(preStep.spritePainter.BrushColor);
                preStep.spritePainter.SetCompleted(true);
                ApplyVisibility(preStep.showObjects, preStep.hideObjects);
            }

            TweenPenOutOfCamera(() =>
            {
                StartPaintStep(_currentStepIndex);
            });
        }

        private void StartPaintStep(int index)
        {
            _finishPaintStep = false;

            if (_paintSteps == null || index >= _paintSteps.Length)
            {
                CompleteLevel();
                return;
            }

            _currentPhase = Phase.Paint;
            _currentStepIndex = index;

            var step = _paintSteps[index];
            if (step == null)
            {
                Debug.LogError($"[{nameof(LevelController)}] Paint step {index} is null. Skipping.", this);
                AdvancePaintStep();
                return;
            }

            step.spritePainter.gameObject.SetActive(true);

            if (step.spritePainter == null)
            {
                Debug.LogError($"[{nameof(LevelController)}] SpritePainter is null for paint step {index}. Skipping.", this);
                AdvancePaintStep();
                return;
            }

            if (step.penOffsetOverride != Vector3.zero)
            {
                step.spritePainter.PenIdleOffset = step.penOffsetOverride;
            }

            UnsubscribeCurrentStep();
            step.spritePainter.ResetToOriginal();
            step.spritePainter.PaintingEnabled = false;
            _activeSpritePainter = step.spritePainter;
            _activeSpritePainter.onPaintComplete.AddListener(OnPainterComplete);

            FocusCameraOnPaintStep(index);
        }

        private void OnPainterComplete()
        {
            if (_currentPhase != Phase.Paint) return;

            var step = _paintSteps[_currentStepIndex];

            AdvancePaintStep();
            UnsubscribeCurrentStep();
        }

        private void AdvancePaintStep()
        {
            int nextStepIndex = _currentStepIndex + 1;
            if (_paintSteps != null && nextStepIndex < _paintSteps.Length)
            {
                _currentStepIndex++;

                ShowSelectColorPopup();
            }
            else
            {
                if (_activeSpritePainter != null)
                {
                    _activeSpritePainter.SetCompleted(true);
                    _activeSpritePainter.FillAll(_activeSpritePainter.BrushColor);
                }

                var step = _paintSteps[_currentStepIndex];
                if (step != null)
                {
                    ApplyVisibility(step.showObjects, step.hideObjects);
                }

                TweenPenOutOfCamera(null);

                CompleteLevel();
            }
        }

        private void ShowSelectColorPopup()
        {
            if (!_finishPaintStep)
            {
                _finishPaintStep = true;

                var colorContent = _paintSteps[_currentStepIndex].GetBrushColorContent();
                // CanvasManager.Push(SelectColorPopup.Identifier, new object[] { colorContent, true });
                EventManager.I.ShowIngameSelectColorAction?.Invoke(colorContent, true);
            }
        }

        private void SkipPaintStep()
        {
            if (_activeSpritePainter != null)
            {
                var brushColor = _activeSpritePainter.BrushColor;
                UnsubscribeCurrentStep();
                // FillAll triggers CheckCompletion → onPaintComplete, but we already unsubscribed,
                // so we handle advance manually here.
                _activeSpritePainter.FillAll(brushColor);
            }
            else
            {
                UnsubscribeCurrentStep();
            }

            AdvancePaintStep();
        }

        // ──────────────────────────────────────────────
        // Restart Step
        // ──────────────────────────────────────────────

        private void RestartDrawStep()
        {
            if (_drawSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _drawSteps.Length) return;

            var step = _drawSteps[_currentStepIndex];
            if (step?.penTrailDrawer == null) return;

            _penTween?.Kill();

            step.penTrailDrawer.Pause();

            // if (step.pointSpawner != null)
            // {
            //     step.pointSpawner.Respawn();
            // }

            _activePenTrailDrawer.ResetPen();
            _activePenTrailDrawer.Play();
        }

        private void RestartPaintStep()
        {
            if (_paintSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _paintSteps.Length) return;

            var step = _paintSteps[_currentStepIndex];
            if (step?.spritePainter == null) return;

            _penTween?.Kill();

            step.spritePainter.ResetToOriginal();
            step.spritePainter.PaintingEnabled = false;
            _finishPaintStep = false;

            if (_paintStepCorrectSelection != null && _currentStepIndex < _paintStepCorrectSelection.Length)
            {
                if (_paintStepCorrectSelection[_currentStepIndex])
                {
                    _paintStepCorrectSelection[_currentStepIndex] = false;
                    _paintCorrectCount--;
                    UpdateLevelProgress();
                }
            }

            // CanvasManager.Pop(SelectColorPopup.Identifier);
            EventManager.I.HideIngameSelectColorAction?.Invoke();

            var colorContent = step.GetBrushColorContent();
            var hasShowFinishPaint = /* _currentStepIndex > 0 */false;
            // CanvasManager.Push(SelectColorPopup.Identifier, new object[] { colorContent, hasShowFinishPaint });
            EventManager.I.ShowIngameSelectColorAction?.Invoke(colorContent, hasShowFinishPaint);
        }

        // ──────────────────────────────────────────────
        // Smart Restart
        // ──────────────────────────────────────────────

        public void SmartRestartStep()
        {
            switch (_currentPhase)
            {
                case Phase.Draw:
                    HandleSmartRestartDraw();
                    break;
                case Phase.Paint:
                    HandleSmartRestartPaint();
                    break;
                default:
                    Debug.LogWarning($"[{nameof(LevelController)}] Cannot smart-restart — phase is {_currentPhase}.", this);
                    break;
            }
        }

        private void HandleSmartRestartDraw()
        {
            if (_drawSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _drawSteps.Length) return;
            var step = _drawSteps[_currentStepIndex];
            if (step?.penTrailDrawer == null) return;

            if (step.penTrailDrawer.CurrentPercent > 0.0)
            {
                RestartDrawStep();
            }
            else if (_currentStepIndex > 0)
            {
                GoToDrawStep(_currentStepIndex - 1);
            }
        }

        private void HandleSmartRestartPaint()
        {
            if (_paintSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _paintSteps.Length) return;
            var step = _paintSteps[_currentStepIndex];
            if (step?.spritePainter == null) return;

            if (step.spritePainter.PaintedPercentage > 0f)
            {
                RestartPaintStep();
            }
            else if (_currentStepIndex > 0)
            {
                GoToPaintStep(_currentStepIndex - 1);
            }
            else
            {
                GoToLastDrawStep();
            }
        }

        private void GoToDrawStep(int index)
        {
            // Hide all objects and deactivate penTrailDrawer of the current (incomplete) draw step
            if (_currentStepIndex >= 0 && _currentStepIndex < _drawSteps.Length)
            {
                var currentStep = _drawSteps[_currentStepIndex];
                if (currentStep != null)
                {
                    if (currentStep.penTrailDrawer != null)
                        currentStep.penTrailDrawer.gameObject.SetActive(false);
                    if (currentStep.pointSpawner != null)
                        currentStep.pointSpawner.Clear();

                    SetGameObjectsActive(currentStep.showObjects, false);
                    SetGameObjectsActive(currentStep.hideObjects, false);
                }
            }

            // Reverse visibility of the target step (was completed before, now we restart it)
            if (index >= 0 && index < _drawSteps.Length)
            {
                var targetStep = _drawSteps[index];
                if (targetStep != null)
                {
                    SetGameObjectsActive(targetStep.showObjects, false);
                    SetGameObjectsActive(targetStep.hideObjects, true);
                }
            }

            _penTween?.Kill();
            UnsubscribeCurrentStep();
            _activeSpritePainter = null;
            _finishPaintStep = false;

            ResetPaintCorrectSelection(0);
            UpdateLevelProgress();

            StartDrawStep(index);
        }

        private void GoToPaintStep(int index)
        {
            // Hide all objects and deactivate spritePainter of the current paint step
            if (_currentStepIndex >= 0 && _currentStepIndex < _paintSteps.Length)
            {
                var currentStep = _paintSteps[_currentStepIndex];
                if (currentStep != null)
                {
                    if (currentStep.spritePainter != null)
                        currentStep.spritePainter.gameObject.SetActive(false);

                    SetGameObjectsActive(currentStep.showObjects, false);
                    SetGameObjectsActive(currentStep.hideObjects, true);
                }
            }

            // Reverse visibility of the target paint step (was completed before, now we restart it)
            if (index >= 0 && index < _paintSteps.Length)
            {
                var targetStep = _paintSteps[index];
                if (targetStep != null)
                {
                    SetGameObjectsActive(targetStep.showObjects, false);
                    SetGameObjectsActive(targetStep.hideObjects, true);
                }
            }

            _penTween?.Kill();
            UnsubscribeCurrentStep();
            // CanvasManager.Pop(SelectColorPopup.Identifier);
            EventManager.I.HideIngameSelectColorAction?.Invoke();
            _finishPaintStep = false;

            ResetPaintCorrectSelection(index);
            UpdateLevelProgress();

            StartPaintStep(index);

            TweenPenOutOfCamera(() =>
            {
                var colorContent = _paintSteps[_currentStepIndex].GetBrushColorContent();
                // CanvasManager.Push(SelectColorPopup.Identifier, new object[] { colorContent, false });
                EventManager.I.ShowIngameSelectColorAction?.Invoke(colorContent, false);
            });
        }

        private void GoToLastDrawStep()
        {
            if (_drawSteps == null || _drawSteps.Length == 0) return;

            EventManager.I.HideIngameSelectColorAction?.Invoke();

            // Deactivate paint step's spritePainter
            if (_currentPhase == Phase.Paint && _currentStepIndex >= 0 && _currentStepIndex < _paintSteps.Length)
            {
                var paintStep = _paintSteps[_currentStepIndex];
                if (paintStep?.spritePainter != null)
                {
                    paintStep.spritePainter.gameObject.SetActive(false);
                }
            }

            _penTween?.Kill();
            UnsubscribeCurrentStep();
            _activeSpritePainter = null;
            _finishPaintStep = false;

            ResetPaintCorrectSelection(0);
            UpdateLevelProgress();

            int lastDrawIndex = _drawSteps.Length - 1;

            // Reverse visibility of the last draw step (was completed before, now we restart it)
            var targetStep = _drawSteps[lastDrawIndex];
            if (targetStep != null)
            {
                SetGameObjectsActive(targetStep.showObjects, false);
                SetGameObjectsActive(targetStep.hideObjects, true);
            }

            StartDrawStep(lastDrawIndex);
        }

        private static void SetGameObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(active);
            }
        }

        // ──────────────────────────────────────────────
        // Win Level (Skip All)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Immediately completes the entire level by filling all remaining paint steps
        /// with their correct colors and triggering <see cref="CompleteLevel"/>.
        /// Works from any phase (Draw or Paint).
        /// </summary>
        public void WinLevel()
        {
            _penTween?.Kill();
            _penTween = null;

            UnsubscribeCurrentStep();

            // Mark all paint steps as correct
            if (_paintSteps != null && _paintStepCorrectSelection != null)
            {
                for (int i = 0; i < _paintSteps.Length && i < _paintStepCorrectSelection.Length; i++)
                {
                    if (!_paintStepCorrectSelection[i])
                    {
                        _paintStepCorrectSelection[i] = true;
                        _paintCorrectCount++;
                    }
                }
            }

            // Pause all active pen trail drawers
            if (_drawSteps != null)
            {
                for (int i = 0; i < _drawSteps.Length; i++)
                {
                    var step = _drawSteps[i];
                    if (step == null) continue;

                    if (step.penTrailDrawer != null)
                    {
                        step.penTrailDrawer.Pause();
                    }

                    if (step.pointSpawner != null)
                    {
                        step.pointSpawner.Clear();
                    }

                    ApplyVisibility(step.showObjects, step.hideObjects);
                }
            }

            // Fill all paint steps with correct color and mark completed
            if (_paintSteps != null)
            {
                for (int i = 0; i < _paintSteps.Length; i++)
                {
                    var step = _paintSteps[i];
                    if (step == null || step.spritePainter == null) continue;

                    var colorContent = step.GetBrushColorContent();
                    if (colorContent != null && colorContent.colors != null)
                    {
                        var correctIndex = colorContent.indexCorrect;
                        if (correctIndex >= 0 && correctIndex < colorContent.colors.Count)
                        {
                            step.spritePainter.BrushColor = colorContent.colors[correctIndex];
                            step.spritePainter.FillAll(colorContent.colors[correctIndex]);
                        }
                        else
                        {
                            step.spritePainter.FillAll(step.spritePainter.BrushColor);
                        }
                    }
                    else
                    {
                        step.spritePainter.FillAll(step.spritePainter.BrushColor);
                    }

                    step.spritePainter.SetCompleted(true);
                    ApplyVisibility(step.showObjects, step.hideObjects);
                }
            }

            CanvasManager.PopAllLayer(eUILayer.Popup);

            CompleteLevel();
        }

        private void UpdateLevelProgress()
        {
            int totalPaintSteps = _paintSteps?.Length ?? 0;
            float drawWeight = totalPaintSteps > 0 ? 0.5f : 1f;
            float paintWeight = 1f - drawWeight;

            // Draw phase: complete = 50% (or 100% if no paint steps)
            float drawProgress = _currentPhase >= Phase.Paint ? drawWeight : 0f;

            // Paint phase: mỗi lần chọn đúng màu = thêm 50% / total paint steps
            float paintProgress = 0f;
            if (totalPaintSteps > 0)
            {
                paintProgress = (float)_paintCorrectCount / totalPaintSteps * paintWeight;
            }

            float progress = drawProgress + paintProgress;
            GameplayManager.I.ProgressLevel = progress;
        }

        private void ResetPaintCorrectSelection(int fromIndex)
        {
            if (_paintStepCorrectSelection == null) return;

            for (int i = fromIndex; i < _paintStepCorrectSelection.Length; i++)
            {
                if (_paintStepCorrectSelection[i])
                {
                    _paintStepCorrectSelection[i] = false;
                    _paintCorrectCount--;
                }
            }
        }

        // ──────────────────────────────────────────────
        // Completion
        // ──────────────────────────────────────────────

        private void CompleteLevel()
        {
            _currentPhase = Phase.Complete;
            _currentStepIndex = -1;
            _activePenTrailDrawer = null;
            _activeSpritePainter = null;

            UpdateLevelProgress();

            // if (_iconObject != null)
            //     _iconObject.SetActive(true);

            FocusCameraOnComplete();
            _onLevelComplete?.Invoke();

            CanvasManager.Push(FinishLevelPopup.Identifier, null);

            EventManager.I.CompleteLevelAction?.Invoke();
        }

        private void FocusCameraOnDrawStep(int index)
        {
            if (_cameraController == null) return;

            var combinedBounds = GetCombinedDrawBounds(index);
            if (combinedBounds.size != Vector3.zero)
            {
                _cameraController.MoveToBounds(combinedBounds);
            }
        }

        private void FocusCameraOnPaintStep(int index)
        {
            if (_cameraController == null || _paintSteps == null || index >= _paintSteps.Length) return;

            var bounds = GetSpritePainterWorldBounds(_paintSteps[index]?.spritePainter);
            if (bounds.size != Vector3.zero)
            {
                _cameraController.MoveToBounds(bounds);
            }
        }

        private void FocusCameraOnComplete()
        {
            if (_cameraController == null) return;

            var allBounds = GetCombinedAllBounds();
            if (allBounds.size != Vector3.zero)
            {
                _cameraController.MoveToBounds(allBounds);
            }
        }

        // ──────────────────────────────────────────────
        // Camera Bounds Helpers
        // ──────────────────────────────────────────────

        private static Bounds GetSplineWorldBounds(SplineComputer spline)
        {
            var bounds = new Bounds();
            var initialized = false;

            if (spline != null)
            {
                var points = spline.GetPoints(SplineComputer.Space.World);
                if (points != null)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (!initialized)
                        {
                            bounds = new Bounds(points[i].position, Vector3.zero);
                            initialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(points[i].position);
                        }
                    }
                }
            }

            return bounds;
        }

        private static Bounds GetSpritePainterWorldBounds(SpritePainter painter)
        {
            if (painter == null) return new Bounds();

            var sr = painter.GetComponent<SpriteRenderer>();
            if (sr == null) return new Bounds();

            return sr.bounds;
        }

        private Bounds GetCombinedDrawBounds(int upToStepIndex)
        {
            if (_drawSteps == null) return new Bounds();

            var bounds = new Bounds();
            var initialized = false;

            for (int i = 0; i <= upToStepIndex && i < _drawSteps.Length; i++)
            {
                var stepBounds = GetSplineWorldBounds(_drawSteps[i]?.spline);
                if (stepBounds.size != Vector3.zero)
                {
                    if (!initialized)
                    {
                        bounds = stepBounds;
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(stepBounds);
                    }
                }
            }

            return bounds;
        }

        private Bounds GetCombinedAllBounds()
        {
            var bounds = new Bounds();
            var initialized = false;

            if (_drawSteps != null)
            {
                for (int i = 0; i < _drawSteps.Length; i++)
                {
                    var stepBounds = GetSplineWorldBounds(_drawSteps[i]?.spline);
                    if (stepBounds.size != Vector3.zero)
                    {
                        if (!initialized)
                        {
                            bounds = stepBounds;
                            initialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(stepBounds);
                        }
                    }
                }
            }

            if (_paintSteps != null)
            {
                for (int i = 0; i < _paintSteps.Length; i++)
                {
                    var stepBounds = GetSpritePainterWorldBounds(_paintSteps[i]?.spritePainter);
                    if (stepBounds.size != Vector3.zero)
                    {
                        if (!initialized)
                        {
                            bounds = stepBounds;
                            initialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(stepBounds);
                        }
                    }
                }
            }

            return bounds;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static void ApplyVisibility(GameObject[] showObjects, GameObject[] hideObjects)
        {
            if (showObjects != null)
            {
                foreach (var obj in showObjects)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }

            if (hideObjects != null)
            {
                foreach (var obj in hideObjects)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }

        private void UnsubscribeCurrentStep()
        {
            if (_activePenTrailDrawer != null)
            {
                _activePenTrailDrawer.onPaintComplete.RemoveListener(OnPenTrailComplete);
                _activePenTrailDrawer = null;
            }

            if (_activeSpritePainter != null)
            {
                _activeSpritePainter.onPaintComplete.RemoveListener(OnPainterComplete);
                _activeSpritePainter = null;
            }
        }

        // ──────────────────────────────────────────────
        // Pen Transition
        // ──────────────────────────────────────────────

        private void TweenPenOutOfCamera(TweenCallback onComplete)
        {
            _penTween?.Kill();

            if (_penTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            var cam = _cameraController != null ? _cameraController.Camera : Camera.main;
            if (cam == null)
            {
                onComplete?.Invoke();
                return;
            }

            var camBottom = cam.ViewportToWorldPoint(new Vector3(1.1f, 0.3f, -cam.transform.position.z));
            _penTween = _penTransform
                .DOMove(camBottom, _penTransitionDuration)
                .SetEase(_penTransitionEase)
                .OnComplete(onComplete);
        }

        private void TweenPenToPaintPosition(PaintStepData step, TweenCallback onComplete)
        {
            _penTween?.Kill();

            if (_penTransform == null || step?.spritePainter == null)
            {
                onComplete?.Invoke();
                return;
            }

            var targetPos = step.spritePainter.GetPenStartWorldPosition();
            _penTween = _penTransform
                .DOMove(targetPos, _penTransitionDuration)
                .SetEase(_penTransitionEase)
                .OnComplete(onComplete);
        }

        // ──────────────────────────────────────────────
        // Gizmos
        // ──────────────────────────────────────────────

        [Header("Gizmos")]
        [SerializeField] private bool _drawGizmoBounds = true;
        [SerializeField] private Color _drawGizmoColor = Color.green;
        [SerializeField] private Color _paintGizmoColor = Color.cyan;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmoBounds) return;

            switch (_currentPhase)
            {
                case Phase.Draw:
                    DrawCurrentDrawBoundsGizmo();
                    break;
                case Phase.Paint:
                    DrawCurrentPaintBoundsGizmo();
                    break;
            }
        }

        private void DrawCurrentDrawBoundsGizmo()
        {
            var combinedBounds = GetCombinedDrawBounds(_currentStepIndex);
            if (combinedBounds.size == Vector3.zero) return;

            Gizmos.color = _drawGizmoColor;
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
        }

        private void DrawCurrentPaintBoundsGizmo()
        {
            if (_paintSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _paintSteps.Length) return;

            var bounds = GetSpritePainterWorldBounds(_paintSteps[_currentStepIndex]?.spritePainter);
            if (bounds.size == Vector3.zero) return;

            Gizmos.color = _paintGizmoColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif

        // ──────────────────────────────────────────────
        // Cleanup
        // ──────────────────────────────────────────────

        private void OnDestroy()
        {
            _penTween?.Kill();
            _penTween = null;
            UnsubscribeCurrentStep();
        }
    }
}