using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;

namespace App.Editor
{
    public class CreateLevelWindow : EditorWindow
    {
        // ── TAB SYSTEM ──
        private int _selectedTab;
        private readonly string[] _tabNames = { "Create Spline", "Create Collider Draw" };

        // ── CREATE SPLINE FIELDS ──
        private GameObject _splineObject;
        private GameObject _pivotObject;
        private GameObject _beginPoint;
        private GameObject _endPoint;
        private bool _reverseDirection;
        private Sprite _sprite;
        private bool _autoSmooth = true;
        private float _detailLevel = 3f;

        // ── CREATE COLLIDER DRAW FIELDS ──
        private GameObject _cdColliderObject;
        private Sprite _cdSprite;
        private float _cdAlphaThreshold = 200f;
        private float _cdDetailLevel = 2f;
        private float _cdExpandOffset = 0.05f;

        [MenuItem("Tools/Create Level")]
        private static void ShowWindow()
        {
            var window = GetWindow<CreateLevelWindow>("Create Level");
            window.minSize = new Vector2(420, 520);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Create Level", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0:
                    DrawCreateSplineTab();
                    break;
                case 1:
                    DrawCreateColliderDrawTab();
                    break;
            }
        }

        // ═══════════════════════════════════════════════
        //  TAB: CREATE SPLINE
        // ═══════════════════════════════════════════════

        private void DrawCreateSplineTab()
        {
            // ── Spline Object field ──
            EditorGUILayout.LabelField("Spline Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GameObject co chua SplineComputer component se luu spline.", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _splineObject = (GameObject)EditorGUILayout.ObjectField(
                "Spline Object", _splineObject, typeof(GameObject), true);

            if (_splineObject != null && _splineObject.GetComponentInChildren<SplineComputer>() == null)
            {
                EditorGUILayout.HelpBox(
                    "GameObject phai co SplineComputer component o child de luu spline.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // ── Sprite field ──
            EditorGUILayout.LabelField("Sprite", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sprite (Single Mode) de phan tich hinh dang va tao Spline.", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _sprite = (Sprite)EditorGUILayout.ObjectField(
                "Sprite", _sprite, typeof(Sprite), false);

            if (_sprite != null)
            {
                if (_sprite.packed)
                {
                    EditorGUILayout.HelpBox(
                        "Sprite bi packed (Atlas/Multiple mode). Chi ho tro Single Mode Sprite.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Kich thuoc: {Mathf.RoundToInt(_sprite.rect.width)}x{Mathf.RoundToInt(_sprite.rect.height)} px, "
                        + $"Pixels Per Unit: {_sprite.pixelsPerUnit}",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(10);

            // ── Pivot Object field ──
            EditorGUILayout.LabelField("Pivot Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "GameObject lam pivot de dinh vi Spline. Neu khong co, dung Spline Object lam pivot.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _pivotObject = (GameObject)EditorGUILayout.ObjectField(
                "Pivot Object", _pivotObject, typeof(GameObject), true);

            if (_pivotObject != null)
            {
                EditorGUILayout.HelpBox(
                    "Spline se dat theo vi tri cua Pivot Object. " +
                    (_splineObject != null ? $"Mac dinh: \"{_splineObject.name}\"" : ""),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Khong co Pivot Object => dung Spline Object lam pivot.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // ── Begin Point field ──
            EditorGUILayout.LabelField("Begin Point", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "GameObject xac dinh diem bat dau cua Spline. Vi tri Spline[0] se duoc dat trung vi tri nay.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _beginPoint = (GameObject)EditorGUILayout.ObjectField(
                "Begin Point", _beginPoint, typeof(GameObject), true);

            EditorGUILayout.Space(10);

            // ── End Point field ──
            EditorGUILayout.LabelField("End Point", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "GameObject xac dinh diem ket thuc cua Spline. Vi tri Spline[last] se duoc dat trung vi tri nay.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _endPoint = (GameObject)EditorGUILayout.ObjectField(
                "End Point", _endPoint, typeof(GameObject), true);

            EditorGUILayout.Space(10);

            // ── Reverse Direction toggle ──
            _reverseDirection = EditorGUILayout.ToggleLeft(
                new GUIContent("Reverse Direction",
                    "Khi bat, Spline se di tu BeginPoint → EndPoint theo huong nguoc lai (duong dai thay vi duong ngan).\n"
                    + "Huu ich khi loop kin co 2 duong di giua BeginPoint va EndPoint."),
                _reverseDirection);

            EditorGUILayout.Space(10);

            // ── Auto Smooth toggle ──
            _autoSmooth = EditorGUILayout.ToggleLeft(
                new GUIContent("Auto Smooth (Bezier only)",
                    "Lam muot Spline bang cach can chinh cac diem control.\nChi ap dung khi SplineComputer.type = Bezier."),
                _autoSmooth);

            EditorGUILayout.Space(10);

            // ── Detail level slider ──
            EditorGUILayout.LabelField("Detail Level", EditorStyles.boldLabel);
            _detailLevel = EditorGUILayout.Slider(
                new GUIContent("Chi tiet",
                    "Cang thap cang nhieu diem (0.5 = rat chi tiet, 10 = it diem nhat).\nMac dinh 3."),
                _detailLevel, 0.5f, 10f);
            EditorGUILayout.LabelField(
                _detailLevel <= 1f ? "Rat chi tiet (nhieu diem)" :
                _detailLevel <= 3f ? "Trung binh" :
                _detailLevel <= 6f ? "It diem" : "Rat it diem (tho)",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(15);

            // ── Validation ──
            var errorMessage = GetSplineValidationError();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            // ── Create button ──
            GUI.enabled = string.IsNullOrEmpty(errorMessage);
            if (GUILayout.Button("Create Spline", GUILayout.Height(40)))
            {
                RunCreateSpline();
            }
            GUI.enabled = true;
        }

        private string GetSplineValidationError()
        {
            if (_splineObject == null)
                return "Chua keo Spline Object.";

            if (_splineObject.GetComponentInChildren<SplineComputer>() == null)
                return "Spline Object khong co SplineComputer o child.";

            if (_sprite == null)
                return "Chua keo Sprite.";

            if (_sprite.packed)
                return "Sprite bi packed (Atlas). Chi ho tro Single Mode Sprite.";

            return null;
        }

        // ═══════════════════════════════════════════════
        //  TAB: CREATE COLLIDER DRAW
        // ═══════════════════════════════════════════════

        private void DrawCreateColliderDrawTab()
        {
            // ── Collider Object field ──
            EditorGUILayout.LabelField("Collider Object", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "GameObject co chua PolygonCollider2D component. Collider se duoc ghi de paths de bao phu cac vung trang.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _cdColliderObject = (GameObject)EditorGUILayout.ObjectField(
                "Collider Object", _cdColliderObject, typeof(GameObject), true);

            if (_cdColliderObject != null)
            {
                var colliderCheck = _cdColliderObject.GetComponentInChildren<PolygonCollider2D>();
                if (colliderCheck == null)
                {
                    EditorGUILayout.HelpBox(
                        "GameObject phai co PolygonCollider2D component o child.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Tim thay PolygonCollider2D: \"{colliderCheck.name}\" ({colliderCheck.pathCount} paths hien tai)",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(10);

            // ── Sprite field ──
            EditorGUILayout.LabelField("Sprite", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Sprite the hien cac vung to mau. Cac vung trang (alpha >= nguong) se duoc trace de tao collider.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _cdSprite = (Sprite)EditorGUILayout.ObjectField(
                "Sprite", _cdSprite, typeof(Sprite), false);

            if (_cdSprite != null)
            {
                if (_cdSprite.packed)
                {
                    EditorGUILayout.HelpBox(
                        "Sprite bi packed (Atlas/Multiple mode). Chi ho tro Single Mode Sprite.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Kich thuoc: {Mathf.RoundToInt(_cdSprite.rect.width)}x{Mathf.RoundToInt(_cdSprite.rect.height)} px, "
                        + $"Pixels Per Unit: {_cdSprite.pixelsPerUnit}",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(10);

            // ── Alpha Threshold slider ──
            EditorGUILayout.LabelField("Alpha Threshold", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Pixel co alpha >= nguong nay duoc xem la vung trang (co the to mau).",
                EditorStyles.miniLabel);

            _cdAlphaThreshold = EditorGUILayout.Slider(
                new GUIContent("Nguong Alpha", "Pixel co alpha >= gia tri nay duoc tinh la vung trang.\nMac dinh 200."),
                _cdAlphaThreshold, 0f, 255f);
            EditorGUILayout.LabelField(
                $"Gia tri hien tai: {Mathf.RoundToInt(_cdAlphaThreshold)}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // ── Detail Level slider ──
            EditorGUILayout.LabelField("Detail Level", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Dieu chinh muc do chi tiet cua collider. Thap = nhieu diem = chinh xac, cao = it diem = don gian.",
                EditorStyles.miniLabel);

            _cdDetailLevel = EditorGUILayout.Slider(
                new GUIContent("Chi tiet Collider",
                    "Cang thap cang nhieu diem (0.5 = rat chi tiet, 10 = it diem nhat).\nMac dinh 2."),
                _cdDetailLevel, 0.5f, 10f);
            EditorGUILayout.LabelField(
                _cdDetailLevel <= 1f ? "Rat chi tiet (nhieu diem)" :
                _cdDetailLevel <= 3f ? "Trung binh" :
                _cdDetailLevel <= 6f ? "It diem" : "Rat it diem (tho)",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // ── Expand Offset slider ──
            EditorGUILayout.LabelField("Expand Offset", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Day collider ra ngoai de de to mau hon. 0 = sat vung to, > 0 = collider lon hon vung.",
                EditorStyles.miniLabel);

            _cdExpandOffset = EditorGUILayout.Slider(
                new GUIContent("Do Gian No",
                    "Collider se duoc day ra ngoai tu centroid cua vung ve.\n0.05 = gian no nhe, 0.2 = gian no nhieu.\nMac dinh 0.05."),
                _cdExpandOffset, 0f, 0.5f);
            EditorGUILayout.LabelField(
                _cdExpandOffset <= 0f ? "Sat vung to mau" :
                _cdExpandOffset <= 0.1f ? "Gian no nhe" :
                _cdExpandOffset <= 0.3f ? "Gian no vua" : "Gian no nhieu",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(15);

            // ── Validation ──
            var errorMessage = GetColliderDrawValidationError();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            // ── Create button ──
            GUI.enabled = string.IsNullOrEmpty(errorMessage);
            if (GUILayout.Button("Create Collider Draw", GUILayout.Height(40)))
            {
                RunCreateColliderDraw();
            }
            GUI.enabled = true;
        }

        private string GetColliderDrawValidationError()
        {
            if (_cdColliderObject == null)
                return "Chua keo Collider Object.";

            if (_cdColliderObject.GetComponentInChildren<PolygonCollider2D>() == null)
                return "Collider Object khong co PolygonCollider2D o child.";

            if (_cdSprite == null)
                return "Chua keo Sprite.";

            if (_cdSprite.packed)
                return "Sprite bi packed (Atlas). Chi ho tro Single Mode Sprite.";

            return null;
        }

        // ═══════════════════════════════════════════════
        //  RUN CREATE SPLINE
        // ═══════════════════════════════════════════════

        private void RunCreateSpline()
        {
            var splineComputer = _splineObject.GetComponentInChildren<SplineComputer>();
            if (splineComputer == null)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Khong tim thay SplineComputer tren Spline Object.");
                return;
            }

            if (_sprite == null)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Chua keo Sprite.");
                return;
            }

            if (_sprite.packed)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Chi ho tro Single Mode Sprite, khong ho tro Atlas.");
                return;
            }

            // Step 1: Extract alpha mask (transparent = bg, opaque = line)
            var lineMask = ContourTracer.ExtractAlphaMask(_sprite);
            if (lineMask == null)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Khong the doc texture tu Sprite.");
                return;
            }

            var linePixelCount = CountLinePixels(lineMask);
            Debug.Log($"[CreateLevel/CreateSpline] {linePixelCount} pixel net (alpha >= 10).");

            if (linePixelCount < 4)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Sprite co qua it pixel net (< 4). Kiem tra lai Sprite.");
                return;
            }

            // Step 2: Zhang-Suen thinning → skeleton 1-pixel
            var skeleton = ContourTracer.ThinImage(lineMask);

            // Step 3: BFS longest path tracing
            var tracePixels = ContourTracer.TraceSkeleton(skeleton);

            if (tracePixels == null || tracePixels.Count < 2)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Khong trace duoc skeleton. Sprite co net ve khong?");
                return;
            }

            Debug.Log($"[CreateLevel/CreateSpline] Skeleton trace (BFS longest path): {tracePixels.Count} diem.");

            // Step 3b: Dieu chinh trace theo BeginPoint / EndPoint
            if (_beginPoint != null || _endPoint != null)
            {
                var currentPivotTransform = _pivotObject != null
                    ? _pivotObject.transform
                    : splineComputer.transform;

                tracePixels = AdjustTraceToPoints(
                    tracePixels, _sprite, currentPivotTransform,
                    _beginPoint, _endPoint, _reverseDirection);
            }

            // Step 4: Simplify polyline voi muc do chi tiet tuy chinh
            var simplifiedPixels = ContourTracer.SimplifyPolyline(tracePixels, _detailLevel);

            if (simplifiedPixels.Count < 2)
            {
                simplifiedPixels = tracePixels;
                Debug.LogWarning("[CreateLevel/CreateSpline] Simplify ve 0 diem, dung trace goc.");
            }

            Debug.Log($"[CreateLevel/CreateSpline] Sau simplify: {simplifiedPixels.Count} diem.");

            // Xac dinh pivot transform: Pivot Object hoac Spline Object
            var pivotTransform = _pivotObject != null
                ? _pivotObject.transform
                : splineComputer.transform;

            // Step 5: Convert to SplinePoints (open spline)
            var splinePoints = ContourTracer.ToSplinePoints(
                simplifiedPixels, _sprite, splineComputer.transform, pivotTransform);

            if (splinePoints.Length < 2)
            {
                Debug.LogError("[CreateLevel/CreateSpline] Khong du diem de tao Spline.");
                return;
            }

            Undo.RecordObject(splineComputer, "Create Spline");

            // Step 6: Set points on SplineComputer (open spline, khong close)
            splineComputer.SetPoints(splinePoints, SplineComputer.Space.World);
            splineComputer.RebuildImmediate();
            if (splineComputer.isClosed)
            {
                splineComputer.Break();
                splineComputer.RebuildImmediate();
            }

            // Step 7: Auto smooth (chi khi Bezier va bool bat)
            if (_autoSmooth && splineComputer.type == Spline.Type.Bezier && splinePoints.Length >= 3)
            {
                SmoothBezierPoints(splineComputer);
            }

            EditorUtility.SetDirty(splineComputer);

            Debug.Log($"[CreateLevel/CreateSpline] Da tao Spline voi {splinePoints.Length} diem "
                + $"tren \"{_splineObject.name}\".");
        }

        /// <summary>
        /// Smooth Bezier spline: can chinh vi tri cac diem control bang
        /// trung binh cong voi neighbor, giu nguyen 2 diem dau va cuoi.
        /// Chi goi khi SplineComputer.type == Bezier.
        /// </summary>
        private static void SmoothBezierPoints(SplineComputer splineComputer)
        {
            var points = splineComputer.GetPoints(SplineComputer.Space.World);
            if (points.Length < 3) return;

            var smoothed = new Vector3[points.Length];
            smoothed[0] = points[0].position;
            smoothed[points.Length - 1] = points[points.Length - 1].position;

            for (var i = 1; i < points.Length - 1; i++)
            {
                var prev = points[i - 1].position;
                var curr = points[i].position;
                var next = points[i + 1].position;
                smoothed[i] = (prev + curr * 2f + next) / 4f;
            }

            for (var i = 1; i < points.Length - 1; i++)
            {
                points[i].SetPosition(smoothed[i]);
            }

            splineComputer.SetPoints(points, SplineComputer.Space.World);
            splineComputer.RebuildImmediate();

            Debug.Log($"[CreateLevel/CreateSpline] Smooth Bezier: {points.Length} diem.");
        }

        /// <summary>
        /// Dieu chinh trace de diem dau va cuoi gan BeginPoint / EndPoint.
        /// Tim pixel trong trace gan nhat voi world position cua point,
        /// roi cat/xoay trace de bat dau tu begin va ket thuc o end.
        /// </summary>
        private static List<Vector2Int> AdjustTraceToPoints(
            List<Vector2Int> tracePixels, Sprite sprite,
            Transform pivotTransform, GameObject beginPoint, GameObject endPoint,
            bool reverseDirection = false)
        {
            // Convert world position → pixel coordinate
            System.Func<GameObject, Vector2Int> worldToPixel = (go) =>
            {
                var localPos = pivotTransform.InverseTransformPoint(go.transform.position);
                var px = Mathf.RoundToInt(localPos.x * sprite.pixelsPerUnit + sprite.pivot.x);
                var py = Mathf.RoundToInt(sprite.pivot.y - localPos.y * sprite.pixelsPerUnit);
                return new Vector2Int(px, py);
            };

            // Tim pixel trong trace gan BeginPoint nhat
            int? beginIndex = null;
            if (beginPoint != null)
            {
                var targetPixel = worldToPixel(beginPoint);
                var bestDistSq = int.MaxValue;
                for (var i = 0; i < tracePixels.Count; i++)
                {
                    var d = (tracePixels[i] - targetPixel).sqrMagnitude;
                    if (d < bestDistSq) { bestDistSq = d; beginIndex = i; }
                }
            }

            // Tim pixel trong trace gan EndPoint nhat
            int? endIndex = null;
            if (endPoint != null)
            {
                var targetPixel = worldToPixel(endPoint);
                var bestDistSq = int.MaxValue;
                for (var i = 0; i < tracePixels.Count; i++)
                {
                    var d = (tracePixels[i] - targetPixel).sqrMagnitude;
                    if (d < bestDistSq) { bestDistSq = d; endIndex = i; }
                }
            }

            if (!beginIndex.HasValue && !endIndex.HasValue)
                return tracePixels;

            var begin = beginIndex ?? 0;
            var end = endIndex ?? tracePixels.Count - 1;

            if (begin == end)
            {
                Debug.LogWarning("[CreateLevel/CreateSpline] BeginPoint va EndPoint gan cung 1 pixel, dung trace goc.");
                return tracePixels;
            }

            // Cat va sap xep lai trace: tu begin → end (di theo chieu trace hien tai)
            var result = new List<Vector2Int>();
            if (begin < end)
            {
                result.AddRange(tracePixels.GetRange(begin, end - begin + 1));
            }
            else
            {
                // begin > end: wrap around
                result.AddRange(tracePixels.GetRange(begin, tracePixels.Count - begin));
                result.AddRange(tracePixels.GetRange(0, end + 1));
            }

            Debug.Log($"[CreateLevel/CreateSpline] Adjusted trace: {tracePixels.Count} → {result.Count} "
                + $"(beginIndex={beginIndex}, endIndex={endIndex}).");

            if (reverseDirection)
            {
                // Lay doan con lai cua loop (complementary segment)
                List<Vector2Int> complement;
                if (begin < end)
                {
                    complement = new List<Vector2Int>();
                    complement.AddRange(tracePixels.GetRange(end, tracePixels.Count - end));
                    complement.AddRange(tracePixels.GetRange(0, begin + 1));
                }
                else
                {
                    complement = tracePixels.GetRange(end, begin - end + 1);
                }
                complement.Reverse();

                Debug.Log($"[CreateLevel/CreateSpline] Reverse direction: {complement.Count} points (was {result.Count}).");
                return complement;
            }

            return result;
        }

        /// <summary>
        /// Dem so pixel line (= 1) trong mask. Dung cho debug.
        /// </summary>
        private static int CountLinePixels(byte[,] mask)
        {
            var width = mask.GetLength(0);
            var height = mask.GetLength(1);
            var count = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (mask[x, y] == 1)
                        count++;
                }
            }

            return count;
        }

        // ═══════════════════════════════════════════════
        //  RUN CREATE COLLIDER DRAW
        // ═══════════════════════════════════════════════

        private void RunCreateColliderDraw()
        {
            var polygonCollider = _cdColliderObject.GetComponentInChildren<PolygonCollider2D>();
            if (polygonCollider == null)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Khong tim thay PolygonCollider2D tren Collider Object.");
                return;
            }

            if (_cdSprite == null || _cdSprite.packed)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Sprite khong hop le.");
                return;
            }

            // Step 1: Tao alpha mask voi nguong tuy chinh
            var mask = ContourTracer.ExtractWhiteMask(_cdSprite, (byte)Mathf.RoundToInt(_cdAlphaThreshold));
            if (mask == null)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Khong the doc texture tu Sprite.");
                return;
            }

            var whitePixelCount = CountLinePixels(mask);
            Debug.Log($"[CreateLevel/ColliderDraw] {whitePixelCount} pixel trang (alpha >= {Mathf.RoundToInt(_cdAlphaThreshold)}).");

            if (whitePixelCount < 4)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Sprite co qua it pixel trang (< 4). Tang alpha threshold hoac kiem tra Sprite.");
                return;
            }

            // Step 2: Tim tat ca cac vung trang lien thong
            var regions = ContourTracer.FindConnectedWhiteRegions(mask);
            Debug.Log($"[CreateLevel/ColliderDraw] Tim thay {regions.Count} vung trang lien thong.");

            if (regions.Count == 0)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Khong tim thay vung trang nao.");
                return;
            }

            // Step 3: Trace boundary + simplify + convert cho tung region
            var allPaths = new List<Vector2[]>(regions.Count);

            for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                var region = regions[regionIndex];

                var boundary = ContourTracer.TraceRegionBoundary(mask, region);
                if (boundary == null || boundary.Count < 3)
                {
                    Debug.LogWarning($"[CreateLevel/ColliderDraw] Vung {regionIndex}: boundary qua it diem, bo qua.");
                    continue;
                }

                var simplified = ContourTracer.SimplifyPolyline(boundary, _cdDetailLevel);
                if (simplified.Count < 3)
                {
                    simplified = boundary;
                }

                Debug.Log($"[CreateLevel/ColliderDraw] Vung {regionIndex}: {boundary.Count} pixel boundary → {simplified.Count} sau simplify (epsilon={_cdDetailLevel:F1}).");

                var worldPoints = ContourTracer.BoundaryToLocalPoints(simplified, _cdSprite, _cdExpandOffset);
                allPaths.Add(worldPoints);
            }

            if (allPaths.Count == 0)
            {
                Debug.LogError("[CreateLevel/ColliderDraw] Khong co path nao duoc tao.");
                return;
            }

            // Step 4: Gan paths vao PolygonCollider2D
            Undo.RecordObject(polygonCollider, "Create Collider Draw");

            polygonCollider.pathCount = allPaths.Count;
            for (var i = 0; i < allPaths.Count; i++)
            {
                polygonCollider.SetPath(i, allPaths[i]);
            }

            EditorUtility.SetDirty(polygonCollider);

            Debug.Log($"[CreateLevel/ColliderDraw] Da gan {allPaths.Count} paths "
                + $"vao PolygonCollider2D \"{polygonCollider.name}\".");
        }
    }
}
