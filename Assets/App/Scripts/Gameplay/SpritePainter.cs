using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace App
{
    /// <summary>
    /// Allows free-draw painting on a SpriteRenderer's texture using mouse/touch input.
    /// Paints a brush circle onto a writable copy of the sprite's texture via CPU-side pixel manipulation.
    /// Auto-adds a BoxCollider2D if no Collider2D is present.
    /// </summary>
    public class SpritePainter : MonoBehaviour
    {
        [SerializeField] private Collider2D _cachedCollider;

        [Header("Target")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Caro")]
        [SerializeField] private SpriteRenderer _caroSpriteRenderer;

        [Header("Brush Settings")]
        [SerializeField] private Color _brushColor = Color.red;
        [SerializeField] private int _brushSize = 8;
        [SerializeField][Range(1f, 3f)] private float _brushFeatherMultiplier = 1.2f;
        [SerializeField][Range(0.1f, 2f)] private float _brushFeatherDuration = 0.3f;
        [SerializeField] private float _applyInterval = 0.03f;

        [Header("Completion")]
        [SerializeField][Range(0f, 1f)] private float _completionThreshold = 0.95f;

        [Header("Alpha")]
        [SerializeField] private bool _showOriginalColor = true;
        [SerializeField][Range(0, 255)] private byte _alphaThreshold = 10;

        [Header("Pen")]
        [SerializeField] private Transform _penTransform;
        [SerializeField] private Vector3 _penIdleOffset = new Vector3(0.15f, 0f, 0f);
        [SerializeField] private bool _useInterpolation = true;

        [SerializeField] private int _paintedCount;

        [Header("Setting")]
        [SerializeField] private bool _hasShowBackground = true;

        [Header("Events")]
        public UnityEvent onPaintStart;
        public UnityEvent onPaintEnd;
        public UnityEvent onPaintComplete;

        private Texture2D _texture;
        private Color32[] _pixels;
        private Color32[] _originalPixels;
        private Color32[] _originalColors;
        private Color32[] _uploadBuffer;
        private bool[] _paintedMask;
        private int _texWidth;
        private int _texHeight;
        private float _pixelsPerUnit;
        private Rect _spriteRect;
        private Vector2 _spritePivot;
        private int _totalPaintablePixels;

        private bool _hasCompleted;
        private bool _isPainting;
        private bool _paintingEnabled = true;
        private bool _dirty;
        private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY;
        private float _lastApplyTime;
        private Camera _mainCamera;

        private Plane _paintPlane;
        private Vector3 _dragOriginWorld;
        private Vector3 _penOriginOnDragStart;
        private bool _isDraggingPen;
        private Vector2Int _lastPaintPixelPos;
        private bool _hasLastPaintPos;

        /// <summary>
        /// Tracks the state of a feather spread animation for a single brush stroke.
        /// As time progresses, the color spreads outward from the core radius to the outer radius.
        /// </summary>
        private struct FeatherAnimation
        {
            public int centerPixelX;
            public int centerPixelY;
            public int coreRadius;
            public float outerRadius;
            public int outerRadiusInPixels;
            public Color32 brushColor;
            public float elapsedTime;
            public float totalDuration;
            /// <summary>The furthest radius that has already been painted for this animation.</summary>
            public float lastSpreadRadius;
        }

        /// <summary>Queue of active feather spread animations, processed each frame.</summary>
        private List<FeatherAnimation> _pendingFeatherAnimations = new List<FeatherAnimation>(8);
        private bool _hasPendingFeatherAnimations;

        public Color BrushColor
        {
            get => _brushColor;
            set => _brushColor = value;
        }

        public Transform PenTransform
        {
            get => _penTransform;
            set => _penTransform = value;
        }

        public Vector3 PenIdleOffset
        {
            get => _penIdleOffset;
            set => _penIdleOffset = value;
        }

        public int BrushSize
        {
            get => _brushSize;
            set => _brushSize = Mathf.Max(1, value);
        }

        public bool IsPainting => _isPainting;

        public float CompletionThreshold
        {
            get => _completionThreshold;
            set => _completionThreshold = Mathf.Clamp01(value);
        }

        [SerializeField] private float _currentCompletedPercentage;
        public float PaintedPercentage
        {
            get
            {
                _currentCompletedPercentage = _totalPaintablePixels > 0 ? (float)_paintedCount / _totalPaintablePixels : 0f;
                return _currentCompletedPercentage;
            }
        }

        public bool HasCompleted => _hasCompleted;

        public bool PaintingEnabled
        {
            get => _paintingEnabled;
            set => _paintingEnabled = value;
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            _paintPlane = new Plane(-transform.forward, transform.position);

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            SetShowBackground();

            var sprite = _spriteRenderer.sprite;
            var sourceTex = sprite.texture;

            _spriteRect = sprite.rect;
            _spritePivot = sprite.pivot;
            _pixelsPerUnit = sprite.pixelsPerUnit;

            var rectX = (int)_spriteRect.x;
            var rectY = (int)_spriteRect.y;
            _texWidth = (int)_spriteRect.width;
            _texHeight = (int)_spriteRect.height;

            var srcWidth = sourceTex.width;
            var allPixels = sourceTex.GetPixels32();
            var pixelCount = _texWidth * _texHeight;
            _originalPixels = new Color32[pixelCount];
            _originalColors = new Color32[pixelCount];
            _paintedMask = new bool[pixelCount];
            _uploadBuffer = new Color32[pixelCount];

            for (var row = 0; row < _texHeight; row++)
            {
                var srcOffset = (rectY + row) * srcWidth + rectX;
                var dstOffset = row * _texWidth;
                System.Array.Copy(allPixels, srcOffset, _originalColors, dstOffset, _texWidth);
                System.Array.Copy(allPixels, srcOffset, _originalPixels, dstOffset, _texWidth);

                for (var column = 0; column < _texWidth; column++)
                {
                    var idx = dstOffset + column;
                    if (_originalColors[idx].a >= _alphaThreshold)
                    {
                        _totalPaintablePixels++;
                        if (!_showOriginalColor)
                        {
                            _originalPixels[idx].a = 0;
                        }
                    }
                    else
                    {
                        _originalPixels[idx] = new Color32(0, 0, 0, 0);
                        _paintedMask[idx] = true;
                    }
                }
            }

            _texture = new Texture2D(_texWidth, _texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            _texture.SetPixels32(_originalPixels);
            _texture.Apply();

            _spriteRenderer.sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, _texWidth, _texHeight),
                new Vector2(_spritePivot.x / _spriteRect.width, _spritePivot.y / _spriteRect.height),
                _pixelsPerUnit
            );

            _pixels = _texture.GetPixels32();

            if (_cachedCollider == null)
            {
                _cachedCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            _cachedCollider.isTrigger = true;
        }

        private void Init()
        {

        }

        private void SetShowBackground()
        {
            if (_hasShowBackground)
            {
                GameObject backgroundObject = new GameObject("BackGround");
                backgroundObject.transform.SetParent(transform);
                backgroundObject.transform.localPosition = Vector3.zero;
                SpriteRenderer spriteRenderer = backgroundObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _spriteRenderer.sprite;
                spriteRenderer.sortingOrder = -30;
            }
        }

        private void OnEnable()
        {
            RunFadeAnimationOfCaro();
        }

        private void OnDisable()
        {
            if (_caroSpriteRenderer)
            {
                _caroSpriteRenderer.DOKill();
                _caroSpriteRenderer.color = new Color(_caroSpriteRenderer.color.a, _caroSpriteRenderer.color.g, _caroSpriteRenderer.color.b, 1);
            }
        }

        private void Update()
        {
            HandleInput();
            TickFeatherAnimations();
            TryApplyTexture();
        }

        private bool IsPointerOverUI(int fingerId = -1)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            return eventSystem.IsPointerOverGameObject(fingerId);
#else
            return eventSystem.IsPointerOverGameObject();
#endif
        }

        private void HandleInput()
        {
            if (_hasCompleted || _penTransform == null || !_paintingEnabled) return;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            var hasTouch = Input.touchCount > 0;
            var touchPhase = hasTouch ? Input.GetTouch(0).phase : TouchPhase.Canceled;
            var touchDown = hasTouch && touchPhase == TouchPhase.Began;
            var touchHeld = hasTouch && touchPhase != TouchPhase.Ended && touchPhase != TouchPhase.Canceled;
            var touchUp = !hasTouch || touchPhase == TouchPhase.Ended || touchPhase == TouchPhase.Canceled;
            var fingerId = hasTouch ? Input.GetTouch(0).fingerId : -1;
#else
            var touchDown = Input.GetMouseButtonDown(0);
            var touchHeld = Input.GetMouseButton(0);
            var touchUp = Input.GetMouseButtonUp(0);
            var fingerId = -1;
#endif

            if (touchDown && !_isPainting)
            {
                if (IsPointerOverUI(fingerId)) return;
                if (!TryGetFingerWorldPosition(out var fingerWorldPos)) return;

                _isPainting = true;
                _isDraggingPen = true;
                _dragOriginWorld = fingerWorldPos;
                _penOriginOnDragStart = _penTransform.position;

                onPaintStart?.Invoke();
                PaintAtPenPosition();
                _lastPaintPixelPos = WorldToPixelCoords(_penTransform.position);
                _hasLastPaintPos = true;
            }
            else if (touchHeld && _isPainting && _isDraggingPen)
            {
                if (!TryGetFingerWorldPosition(out var fingerWorldPos)) return;

                var delta = fingerWorldPos - _dragOriginWorld;
                _penTransform.position = _penOriginOnDragStart + delta;

                if (_useInterpolation)
                    PaintInterpolated();
                else
                    PaintAtPenPosition();
            }
            else if (touchUp && _isPainting)
            {
                _isPainting = false;
                _isDraggingPen = false;
                _hasLastPaintPos = false;
                onPaintEnd?.Invoke();

                _texture.SetPixels32(_pixels);
                _texture.Apply();
                _dirty = false;

                CheckCompletion();
            }
        }

        private bool TryGetFingerWorldPosition(out Vector3 worldPos)
        {
            worldPos = Vector3.zero;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            if (Input.touchCount == 0) return false;
            var screenPos = (Vector3)Input.GetTouch(0).position;
#else
            var screenPos = Input.mousePosition;
#endif

            var ray = _mainCamera.ScreenPointToRay(screenPos);
            if (_paintPlane.Raycast(ray, out var distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }

            return false;
        }

        private void PaintAtPenPosition()
        {
            if (_penTransform == null) return;

            if (_cachedCollider != null && !_cachedCollider.OverlapPoint(_penTransform.position)) return;

            PaintAt(_penTransform.position);
        }

        private void PaintInterpolated()
        {
            if (_penTransform == null) return;

            if (_cachedCollider != null && !_cachedCollider.OverlapPoint(_penTransform.position))
            {
                _hasLastPaintPos = false;
                return;
            }

            var currentPx = WorldToPixelCoords(_penTransform.position);

            if (!_hasLastPaintPos)
            {
                PaintAt(_penTransform.position);
                _lastPaintPixelPos = currentPx;
                _hasLastPaintPos = true;
                return;
            }

            var dx = (float)(currentPx.x - _lastPaintPixelPos.x);
            var dy = (float)(currentPx.y - _lastPaintPixelPos.y);
            var dist = Mathf.Sqrt(dx * dx + dy * dy);

            var stepSize = _brushSize * 0.6f;

            if (dist <= stepSize)
            {
                PaintAt(_penTransform.position);
            }
            else
            {
                var steps = Mathf.CeilToInt(dist / stepSize);
                for (var i = 0; i <= steps; i++)
                {
                    var t = (float)i / steps;
                    var px = Mathf.Lerp(_lastPaintPixelPos.x, currentPx.x, t);
                    var py = Mathf.Lerp(_lastPaintPixelPos.y, currentPx.y, t);
                    var worldPos = PixelToWorldCoords(px, py);
                    PaintAt(worldPos/* , skipFeather: i < steps */);
                }
            }

            _lastPaintPixelPos = currentPx;
        }

        private void PaintAt(Vector3 worldPos, bool skipFeather = false)
        {
            var pixelPosition = WorldToPixelCoords(worldPos);
            var index = pixelPosition.y * _texWidth + pixelPosition.x;
            if (_originalColors[index].a < _alphaThreshold) return;
            DrawHardCore(pixelPosition.x, pixelPosition.y, _brushSize, (Color32)_brushColor);
            if (!skipFeather && _brushFeatherMultiplier > 1.001f)
            {
                EnqueueFeather(pixelPosition.x, pixelPosition.y, _brushSize, (Color32)_brushColor);
            }
            var outerRadius = Mathf.CeilToInt(_brushSize * _brushFeatherMultiplier);
            MarkDirty(pixelPosition.x, pixelPosition.y, outerRadius);
        }

        private void MarkDirty(int cx, int cy, int radius)
        {
            var minX = Mathf.Max(0, cx - radius);
            var minY = Mathf.Max(0, cy - radius);
            var maxX = Mathf.Min(_texWidth - 1, cx + radius);
            var maxY = Mathf.Min(_texHeight - 1, cy + radius);

            if (!_dirty)
            {
                _dirtyMinX = minX;
                _dirtyMinY = minY;
                _dirtyMaxX = maxX;
                _dirtyMaxY = maxY;
            }
            else
            {
                if (minX < _dirtyMinX) _dirtyMinX = minX;
                if (minY < _dirtyMinY) _dirtyMinY = minY;
                if (maxX > _dirtyMaxX) _dirtyMaxX = maxX;
                if (maxY > _dirtyMaxY) _dirtyMaxY = maxY;
            }

            _dirty = true;
        }

        private Vector3 PixelToWorldCoords(float px, float py)
        {
            var bounds = _spriteRenderer.bounds;
            var u = px / _texWidth;
            var v = py / _texHeight;
            return bounds.min + new Vector3(u * bounds.size.x, v * bounds.size.y, 0f);
        }

        private Vector2Int WorldToPixelCoords(Vector3 worldPos)
        {
            var bounds = _spriteRenderer.bounds;
            var u = (worldPos.x - bounds.min.x) / bounds.size.x;
            var v = (worldPos.y - bounds.min.y) / bounds.size.y;

            var px = Mathf.RoundToInt(u * _texWidth);
            var py = Mathf.RoundToInt(v * _texHeight);

            px = Mathf.Clamp(px, 0, _texWidth - 1);
            py = Mathf.Clamp(py, 0, _texHeight - 1);

            return new Vector2Int(px, py);
        }

        private void DrawHardCore(int cx, int cy, int radius, Color32 color)
        {
            var r2 = radius * radius;
            var minY = Mathf.Max(0, cy - radius);
            var maxY = Mathf.Min(_texHeight - 1, cy + radius);
            var minX = Mathf.Max(0, cx - radius);
            var maxX = Mathf.Min(_texWidth - 1, cx + radius);

            var mask = _paintedMask;
            var pixels = _pixels;
            var texWidth = _texWidth;

            for (var y = minY; y <= maxY; y++)
            {
                var dy = y - cy;
                var dy2 = dy * dy;
                var rowOffset = y * texWidth;

                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    if (dx * dx + dy2 <= r2)
                    {
                        var i = rowOffset + x;
                        if (_originalColors[i].a >= _alphaThreshold && !mask[i])
                        {
                            mask[i] = true;
                            _paintedCount++;
                            pixels[i] = color;
                            pixels[i].a = 255;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enqueues a feather spread animation that will gradually paint
        /// from the outer edge of the core radius outward to the full feather zone.
        /// </summary>
        private void EnqueueFeather(int centerPixelX, int centerPixelY, int brushRadius, Color32 brushColor)
        {
            var outerRadius = brushRadius * _brushFeatherMultiplier;
            _pendingFeatherAnimations.Add(new FeatherAnimation
            {
                centerPixelX = centerPixelX,
                centerPixelY = centerPixelY,
                coreRadius = brushRadius,
                outerRadius = outerRadius,
                outerRadiusInPixels = Mathf.CeilToInt(outerRadius),
                brushColor = brushColor,
                elapsedTime = 0f,
                totalDuration = _brushFeatherDuration,
                lastSpreadRadius = brushRadius
            });
            _hasPendingFeatherAnimations = true;
        }

        /// <summary>
        /// Advances all active feather animations based on elapsed time.
        /// Each animation paints a ring from lastSpreadRadius to the current spread radius,
        /// using EaseOutQuad (1 - (1-t)^2) for a natural deceleration effect.
        /// </summary>
        private void TickFeatherAnimations()
        {
            if (!_hasPendingFeatherAnimations) return;

            var anyDirtyThisFrame = false;
            var deltaTime = Time.deltaTime;

            // Reverse iterate so RemoveAt does not affect indices of unprocessed items
            for (var index = _pendingFeatherAnimations.Count - 1; index >= 0; index--)
            {
                var animation = _pendingFeatherAnimations[index];
                animation.elapsedTime += deltaTime;

                var linearProgress = Mathf.Clamp01(animation.elapsedTime / animation.totalDuration);
                // EaseOutQuad: spreads fast initially, slows down near the outer edge (natural ink bleed)
                var easedProgress = 1f - (1f - linearProgress) * (1f - linearProgress);
                var currentSpreadRadius = animation.coreRadius + (animation.outerRadius - animation.coreRadius) * easedProgress;

                if (currentSpreadRadius > animation.lastSpreadRadius)
                {
                    DrawFeatherRing(
                        animation.centerPixelX, animation.centerPixelY,
                        animation.lastSpreadRadius, currentSpreadRadius,
                        animation.brushColor, animation.outerRadiusInPixels);
                    animation.lastSpreadRadius = currentSpreadRadius;
                    anyDirtyThisFrame = true;
                    MarkDirty(animation.centerPixelX, animation.centerPixelY, animation.outerRadiusInPixels);
                }

                if (animation.elapsedTime >= animation.totalDuration)
                {
                    // Ensure the final remaining ring is painted to the outer edge
                    if (animation.lastSpreadRadius < animation.outerRadius)
                    {
                        DrawFeatherRing(
                            animation.centerPixelX, animation.centerPixelY,
                            animation.lastSpreadRadius, animation.outerRadius,
                            animation.brushColor, animation.outerRadiusInPixels);
                        MarkDirty(animation.centerPixelX, animation.centerPixelY, animation.outerRadiusInPixels);
                        anyDirtyThisFrame = true;
                    }
                    _pendingFeatherAnimations.RemoveAt(index);
                }
                else
                {
                    _pendingFeatherAnimations[index] = animation;
                }
            }

            if (_pendingFeatherAnimations.Count == 0)
            {
                _hasPendingFeatherAnimations = false;
            }

            if (anyDirtyThisFrame)
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// Paints a ring-shaped region (annulus) onto the texture.
        /// Only pixels within [innerRadius, outerRadius] and above the alpha threshold are painted.
        /// Uses squared distances to avoid sqrt for mobile performance.
        /// </summary>
        private void DrawFeatherRing(int centerPixelX, int centerPixelY,
            float innerRadius, float outerRadius, Color32 brushColor, int outerRadiusInPixels)
        {
            var innerRadiusSquared = innerRadius * innerRadius;
            var outerRadiusSquared = outerRadius * outerRadius;

            var minPixelY = Mathf.Max(0, centerPixelY - outerRadiusInPixels);
            var maxPixelY = Mathf.Min(_texHeight - 1, centerPixelY + outerRadiusInPixels);
            var minPixelX = Mathf.Max(0, centerPixelX - outerRadiusInPixels);
            var maxPixelX = Mathf.Min(_texWidth - 1, centerPixelX + outerRadiusInPixels);

            var mask = _paintedMask;
            var pixels = _pixels;
            var texWidth = _texWidth;
            var threshold = _alphaThreshold;
            var originalColors = _originalColors;

            for (var pixelY = minPixelY; pixelY <= maxPixelY; pixelY++)
            {
                var deltaY = pixelY - centerPixelY;
                var deltaYSq = deltaY * deltaY;
                var rowOffset = pixelY * texWidth;

                for (var pixelX = minPixelX; pixelX <= maxPixelX; pixelX++)
                {
                    var deltaX = pixelX - centerPixelX;
                    // Squared distance from center — no sqrt needed
                    var distSq = deltaX * deltaX + deltaYSq;

                    if (distSq > innerRadiusSquared && distSq <= outerRadiusSquared)
                    {
                        var pixelIndex = rowOffset + pixelX;
                        if (originalColors[pixelIndex].a >= threshold && !mask[pixelIndex])
                        {
                            mask[pixelIndex] = true;
                            _paintedCount++;
                            pixels[pixelIndex] = brushColor;
                            pixels[pixelIndex].a = 255;
                        }
                    }
                }
            }
        }

        private void TryApplyTexture()
        {
            if (!_dirty) return;

            if (_isPainting /* || _hasPendingFeatherAnimations */)
            {
                if (Time.time - _lastApplyTime >= _applyInterval)
                {
                    ApplyNow();
                    CheckCompletion();
                }
            }
        }

        private void CheckCompletion()
        {
            if (_hasCompleted) return;
            if (_totalPaintablePixels == 0) return;

            if (PaintedPercentage >= _completionThreshold)
            {
                // _hasCompleted = true;
                onPaintComplete?.Invoke();
            }
        }

        public void SetCompleted(bool completed)
        {
            _hasCompleted = completed;
        }

        private void ApplyNow()
        {
            var w = _dirtyMaxX - _dirtyMinX + 1;
            var h = _dirtyMaxY - _dirtyMinY + 1;

            for (var row = 0; row < h; row++)
            {
                var srcOffset = (_dirtyMinY + row) * _texWidth + _dirtyMinX;
                var dstOffset = row * w;
                System.Array.Copy(_pixels, srcOffset, _uploadBuffer, dstOffset, w);
            }

            _texture.SetPixels32(_dirtyMinX, _dirtyMinY, w, h, _uploadBuffer);
            _texture.Apply(false);
            _dirty = false;
            _lastApplyTime = Time.time;
        }

        /// <summary>
        /// Fills the entire paintable area with a single color.
        /// </summary>
        public void FillAll(Color color)
        {
            _pendingFeatherAnimations.Clear();
            _hasPendingFeatherAnimations = false;

            var c32 = (Color32)color;
            for (var i = 0; i < _pixels.Length; i++)
            {
                if (_originalColors[i].a >= _alphaThreshold)
                {
                    _pixels[i] = c32;
                    if (!_paintedMask[i])
                    {
                        _paintedMask[i] = true;
                        _paintedCount++;
                    }
                }
            }

            MarkFullDirty();
            ApplyNow();
            CheckCompletion();
        }

        /// <summary>
        /// Returns the world-space position where the pen should rest when ready to paint
        /// (sprite center + idle offset).
        /// </summary>
        public Vector3 GetPenStartWorldPosition()
        {
            var centerWorld = PixelToWorldCoords(_texWidth / 2f, _texHeight / 2f);
            return centerWorld + _penIdleOffset;
        }

        /// <summary>
        /// Snaps the pen to the sprite center plus idle offset.
        /// </summary>
        public void PlacePenAtStart()
        {
            if (_penTransform == null) return;
            var centerWorld = PixelToWorldCoords(_texWidth / 2f, _texHeight / 2f);
            _penTransform.position = centerWorld + _penIdleOffset;
        }

        /// <summary>
        /// Resets the texture to the original sprite's appearance and clears paint progress.
        /// </summary>
        public void ResetToOriginal()
        {
            _pendingFeatherAnimations.Clear();
            _hasPendingFeatherAnimations = false;

            if (_showOriginalColor)
            {
                System.Array.Copy(_originalColors, _pixels, _pixels.Length);
                System.Array.Copy(_originalColors, _originalPixels, _originalPixels.Length);
            }
            else
            {
                for (var i = 0; i < _pixels.Length; i++)
                {
                    if (_originalColors[i].a >= _alphaThreshold)
                    {
                        _pixels[i] = _originalColors[i];
                        _pixels[i].a = 0;
                        _originalPixels[i] = _pixels[i];
                    }
                    else
                    {
                        _pixels[i] = new Color32(0, 0, 0, 0);
                        _originalPixels[i] = _pixels[i];
                    }
                }
            }

            System.Array.Clear(_paintedMask, 0, _paintedMask.Length);
            _paintedCount = 0;
            _hasCompleted = false;
            MarkFullDirty();
            ApplyNow();
        }

        private void MarkFullDirty()
        {
            _dirtyMinX = 0;
            _dirtyMinY = 0;
            _dirtyMaxX = _texWidth - 1;
            _dirtyMaxY = _texHeight - 1;
            _dirty = true;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        private void RunFadeAnimationOfCaro()
        {
            if (_caroSpriteRenderer == null)
                return;

            _caroSpriteRenderer.DOKill();
            _caroSpriteRenderer.DOFade(0.3f, 0.5f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }
    }
}
