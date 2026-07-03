using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;

namespace App.Editor
{
    /// <summary>
    /// Traces a sprite's line art (pencil strokes) and generates SplineComputer control points.
    ///
    /// Pipeline:
    ///   Sprite → ExtractAlphaMask (alpha-based)
    ///          → ThinImage (Zhang-Suen, border-fixed)
    ///          → TraceSkeleton (iterative BFS longest-path, auto handle loop + branched)
    ///          → SimplifyPolyline (Douglas-Peucker)
    ///          → ToSplinePoints (open spline, SmoothMirrored)
    /// </summary>
    public static class ContourTracer
    {
        const byte AlphaThreshold = 10;
        internal static readonly Vector2Int[] Directions8 = {
            new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        // ═══════════════════════════════════════════════
        //  PIXEL READING
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Đọc pixel từ sprite texture.
        /// TextureImporter bật isReadable tạm thời nếu cần — chính xác tuyệt đối.
        /// Trả về Color32[width*height] top-left origin.
        /// </summary>
        private static Color32[] ReadSpritePixels(Sprite sprite, out int width, out int height)
        {
            width = Mathf.RoundToInt(sprite.rect.width);
            height = Mathf.RoundToInt(sprite.rect.height);
            var tex = sprite.texture;
            var rect = sprite.rect;

            Color32[] allPixels;
            try
            {
                allPixels = tex.GetPixels32();
            }
            catch
            {
                allPixels = ReadWithTempReadable(tex);
            }

            // Crop to sprite rect + flip Y (bottom-left → top-left)
            var result = new Color32[width * height];
            var srcWidth = tex.width;
            var srcX = Mathf.RoundToInt(rect.x);
            var srcY = Mathf.RoundToInt(rect.y);

            for (var row = 0; row < height; row++)
            {
                var srcRow = srcY + (height - 1 - row); // Flip Y
                var srcOffset = srcRow * srcWidth + srcX;
                var dstOffset = row * width;
                System.Array.Copy(allPixels, srcOffset, result, dstOffset, width);
            }

            return result;
        }

        private static Color32[] ReadWithTempReadable(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ContourTracer] Khong tim thay asset path cua texture.");
                return new Color32[texture.width * texture.height];
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[ContourTracer] Khong the lay TextureImporter.");
                return new Color32[texture.width * texture.height];
            }

            var oldReadable = importer.isReadable;
            if (!oldReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Color32[] pixels;
            try
            {
                pixels = texture.GetPixels32();
            }
            finally
            {
                if (!oldReadable)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }

            return pixels;
        }

        // ═══════════════════════════════════════════════
        //  ALPHA MASK
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Binary mask từ sprite: alpha >= threshold = 1 (nét vẽ), còn lại = 0.
        /// </summary>
        public static byte[,] ExtractAlphaMask(Sprite sprite)
        {
            var allPixels = ReadSpritePixels(sprite, out var width, out var height);

            var mask = new byte[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixelIndex = y * width + x;
                    mask[x, y] = allPixels[pixelIndex].a >= AlphaThreshold ? (byte)1 : (byte)0;
                }
            }

            return mask;
        }

        // ═══════════════════════════════════════════════
        //  ZHANG-SUEN THINNING (border-fixed)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Zhang-Suen thinning. Xóa pixel biên lặp đến khi còn skeleton 1-pixel.
        /// Xử lý border: neighbor ngoài image = 0 (background).
        /// </summary>
        public static byte[,] ThinImage(byte[,] image)
        {
            var w = image.GetLength(0);
            var h = image.GetLength(1);

            // Helper: lấy neighbor value, out-of-bounds = 0
            byte N(int x, int y) => (x >= 0 && x < w && y >= 0 && y < h) ? image[x, y] : (byte)0;

            var anyChanged = true;
            while (anyChanged)
            {
                anyChanged = false;
                anyChanged |= ThinPass(image, w, h, N, pass2: false);
                anyChanged |= ThinPass(image, w, h, N, pass2: true);
            }

            return image;
        }

        private static bool ThinPass(byte[,] image, int w, int h, System.Func<int, int, byte> N, bool pass2)
        {
            var removeList = new List<Vector2Int>();

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    if (image[x, y] != 1) continue;

                    var p2 = N(x, y - 1);
                    var p3 = N(x + 1, y - 1);
                    var p4 = N(x + 1, y);
                    var p5 = N(x + 1, y + 1);
                    var p6 = N(x, y + 1);
                    var p7 = N(x - 1, y + 1);
                    var p8 = N(x - 1, y);
                    var p9 = N(x - 1, y - 1);

                    var b = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                    if (b < 2 || b > 6) continue;

                    var a = 0;
                    if (p2 == 0 && p3 == 1) a++;
                    if (p3 == 0 && p4 == 1) a++;
                    if (p4 == 0 && p5 == 1) a++;
                    if (p5 == 0 && p6 == 1) a++;
                    if (p6 == 0 && p7 == 1) a++;
                    if (p7 == 0 && p8 == 1) a++;
                    if (p8 == 0 && p9 == 1) a++;
                    if (p9 == 0 && p2 == 1) a++;
                    if (a != 1) continue;

                    if (!pass2)
                    {
                        if (p2 * p4 * p6 != 0) continue;
                        if (p4 * p6 * p8 != 0) continue;
                    }
                    else
                    {
                        if (p2 * p4 * p8 != 0) continue;
                        if (p2 * p6 * p8 != 0) continue;
                    }

                    removeList.Add(new Vector2Int(x, y));
                }
            }

            foreach (var p in removeList)
                image[p.x, p.y] = 0;

            return removeList.Count > 0;
        }

        // ═══════════════════════════════════════════════
        //  SKELETON TRACING (BFS longest path, iterative)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Trace skeleton: BFS tim longest path, sau do tim pixel con lai
        /// va BFS lai de trace tiep. Lap den khi het pixel.
        /// Xu ly path ho (1 lan BFS la du), loop kin (2 lan BFS), va branched shape.
        /// </summary>
        public static List<Vector2Int> TraceSkeleton(byte[,] skeleton)
        {
            var w = skeleton.GetLength(0);
            var h = skeleton.GetLength(1);

            // Lan BFS dau tien
            var trace = TraceLongestPath(skeleton, w, h);
            if (trace == null || trace.Count < 2) return trace;

            var totalTraced = trace.Count;

            // Lap BFS tren pixel con lai
            for (var iter = 0; iter < 100; iter++)
            {
                var traceSet = new HashSet<Vector2Int>(trace);

                // Kiem tra con pixel skeleton chua trace khong
                var hasRemaining = false;
                for (var y = 0; y < h && !hasRemaining; y++)
                    for (var x = 0; x < w && !hasRemaining; x++)
                        if (skeleton[x, y] == 1 && !traceSet.Contains(new Vector2Int(x, y)))
                            hasRemaining = true;

                if (!hasRemaining) break;

                // Tao skeleton copy, xoa pixel da trace
                var remainingSkel = new byte[w, h];
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                        if (skeleton[x, y] == 1 && !traceSet.Contains(new Vector2Int(x, y)))
                            remainingSkel[x, y] = 1;

                var remainingTrace = TraceLongestPath(remainingSkel, w, h);
                if (remainingTrace == null || remainingTrace.Count < 2) break;

                totalTraced += remainingTrace.Count;

                // Noi remainingTrace vao trace — tim diem 8-adjacent de ghep
                if (!SpliceRemainingIntoTrace(ref trace, remainingTrace))
                {
                    // Fallback: append cuoi
                    trace.AddRange(remainingTrace);
                }
            }

            Debug.Log($"[ContourTracer] Traced: {totalTraced} pixels in {trace.Count} ordered.");
            return trace;
        }

        /// <summary>
        /// Ghep remainingTrace vao trace tai diem 8-adjacent gan nhat.
        /// Tra ve false neu khong tim thay diem ghep.
        /// </summary>
        private static bool SpliceRemainingIntoTrace(
            ref List<Vector2Int> trace, List<Vector2Int> remainingTrace)
        {
            var firstRem = remainingTrace[0];
            var lastRem = remainingTrace[remainingTrace.Count - 1];
            var firstTrace = trace[0];
            var lastTrace = trace[trace.Count - 1];

            // Dau remaining gan cuoi trace → append as-is
            if (Is8Adjacent(firstRem, lastTrace))
            {
                trace.AddRange(remainingTrace);
                return true;
            }
            // Cuoi remaining gan cuoi trace → reverse roi append
            if (Is8Adjacent(lastRem, lastTrace))
            {
                remainingTrace.Reverse();
                trace.AddRange(remainingTrace);
                return true;
            }
            // Cuoi remaining gan dau trace → prepend as-is
            if (Is8Adjacent(lastRem, firstTrace))
            {
                trace.InsertRange(0, remainingTrace);
                return true;
            }
            // Dau remaining gan dau trace → reverse roi prepend
            if (Is8Adjacent(firstRem, firstTrace))
            {
                remainingTrace.Reverse();
                trace.InsertRange(0, remainingTrace);
                return true;
            }

            return false;
        }

        private static bool Is8Adjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1;
        }

        /// <summary>
        /// BFS longest-path: tim 2 pixel xa nhat, trace duong di giua chung.
        /// </summary>
        private static List<Vector2Int> TraceLongestPath(byte[,] skeleton, int w, int h)
        {
            Vector2Int? firstPixel = null;
            for (var y = 0; y < h && !firstPixel.HasValue; y++)
                for (var x = 0; x < w && !firstPixel.HasValue; x++)
                    if (skeleton[x, y] == 1)
                        firstPixel = new Vector2Int(x, y);

            if (!firstPixel.HasValue) return null;

            var farthestA = BfsFarthest(skeleton, w, h, firstPixel.Value);
            if (!farthestA.HasValue) return new List<Vector2Int> { firstPixel.Value };

            var (farthestB, parent) = BfsFarthestWithParent(skeleton, w, h, farthestA.Value);

            var trace = new List<Vector2Int>();
            var cur = farthestB;
            while (cur != farthestA.Value)
            {
                trace.Add(cur);
                cur = parent[cur];
            }
            trace.Add(farthestA.Value);
            trace.Reverse();
            return trace;
        }

        /// <summary>
        /// BFS tu start, tra ve pixel skeleton xa nhat.
        /// </summary>
        private static Vector2Int? BfsFarthest(byte[,] skeleton, int w, int h, Vector2Int start)
        {
            var visited = new bool[w, h];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            Vector2Int farthest = start;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                farthest = cur;

                for (var d = 0; d < 8; d++)
                {
                    var nx = cur.x + Directions8[d].x;
                    var ny = cur.y + Directions8[d].y;
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h
                        && skeleton[nx, ny] == 1 && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            // Dem so pixel da visit de xac dinh co path khong
            var visitedCount = 0;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    if (visited[x, y]) visitedCount++;

            return visitedCount > 1 ? farthest : (Vector2Int?)null;
        }

        /// <summary>
        /// BFS tu start, tra ve farthest + parent dict.
        /// </summary>
        private static (Vector2Int farthest, Dictionary<Vector2Int, Vector2Int> parent)
            BfsFarthestWithParent(byte[,] skeleton, int w, int h, Vector2Int start)
        {
            var visited = new bool[w, h];
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            Vector2Int farthest = start;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                farthest = cur;

                for (var d = 0; d < 8; d++)
                {
                    var nx = cur.x + Directions8[d].x;
                    var ny = cur.y + Directions8[d].y;
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h
                        && skeleton[nx, ny] == 1 && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        var neighbor = new Vector2Int(nx, ny);
                        parent[neighbor] = cur;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return (farthest, parent);
        }

        // ═══════════════════════════════════════════════
        //  SIMPLIFY + CONVERT
        // ═══════════════════════════════════════════════

        public static List<Vector2Int> SimplifyPolyline(List<Vector2Int> points, float epsilon = 3f)
        {
            if (points == null || points.Count <= 2) return points;
            return DouglasPeucker(points, epsilon);
        }

        public static SplinePoint[] ToSplinePoints(
            List<Vector2Int> tracePixels, Sprite sprite, Transform targetTransform,
            Transform pivotTransform = null)
        {
            if (tracePixels == null || tracePixels.Count < 2)
                return new SplinePoint[0];

            // Pivot transform quyet dinh vi tri dat Spline trong world space
            var pivot = pivotTransform ?? targetTransform;

            var result = new SplinePoint[tracePixels.Count];
            var pivotX = sprite.pivot.x;
            var pivotY = sprite.pivot.y;
            var pps = sprite.pixelsPerUnit;

            for (var i = 0; i < tracePixels.Count; i++)
            {
                var localX = (tracePixels[i].x - pivotX) / pps;
                var localY = (pivotY - tracePixels[i].y) / pps;
                var wp = pivot.TransformPoint(new Vector3(localX, localY, 0f));

                result[i] = new SplinePoint(wp)
                {
                    type = SplinePoint.Type.SmoothMirrored,
                    size = 1f, color = Color.white, normal = Vector3.forward
                };
            }

            return result;
        }

        // ═══════════════════════════════════════════════
        //  WHITE REGION DETECTION (cho Collider Draw)
        // ═══════════════════════════════════════════════

        // 8 huong clockwise, bat dau tu LEFT
        private static readonly int[] DirectionDeltaX = { -1, -1, 0, 1, 1, 1, 0, -1 };
        private static readonly int[] DirectionDeltaY = { 0, -1, -1, -1, 0, 1, 1, 1 };

        /// <summary>
        /// Binary mask tu sprite voi nguong alpha tuy chinh.
        /// Pixel co alpha >= threshold = 1 (vung trang), con lai = 0.
        /// </summary>
        public static byte[,] ExtractWhiteMask(Sprite sprite, byte threshold)
        {
            var allPixels = ReadSpritePixels(sprite, out var width, out var height);

            var mask = new byte[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixelIndex = y * width + x;
                    mask[x, y] = allPixels[pixelIndex].a >= threshold ? (byte)1 : (byte)0;
                }
            }

            return mask;
        }

        /// <summary>
        /// Tim tat ca cac vung trang (pixel = 1) lien thong trong mask.
        /// Dung 4-huong BFS flood-fill, khong allocation ben trong vong lap.
        /// Tra ve: moi vung la 1 mang cac Vector2Int + state array de trace boundary.
        /// </summary>
        public static List<List<Vector2Int>> FindConnectedWhiteRegions(byte[,] mask)
        {
            var w = mask.GetLength(0);
            var h = mask.GetLength(1);
            var visited = new bool[w, h];
            var regions = new List<List<Vector2Int>>();

            // 4 huong
            var dx4 = new int[] { 1, -1, 0, 0 };
            var dy4 = new int[] { 0, 0, 1, -1 };

            for (var scanY = 0; scanY < h; scanY++)
            {
                for (var scanX = 0; scanX < w; scanX++)
                {
                    if (mask[scanX, scanY] != 1 || visited[scanX, scanY]) continue;

                    var regionPixels = new List<Vector2Int>();
                    var queue = new Queue<Vector2Int>();
                    var start = new Vector2Int(scanX, scanY);
                    queue.Enqueue(start);
                    visited[scanX, scanY] = true;
                    regionPixels.Add(start);

                    while (queue.Count > 0)
                    {
                        var currentPixel = queue.Dequeue();
                        var curX = currentPixel.x;
                        var curY = currentPixel.y;

                        for (var dirIndex = 0; dirIndex < 4; dirIndex++)
                        {
                            var neighborX = curX + dx4[dirIndex];
                            var neighborY = curY + dy4[dirIndex];

                            if (neighborX < 0 || neighborX >= w || neighborY < 0 || neighborY >= h)
                                continue;
                            if (mask[neighborX, neighborY] != 1 || visited[neighborX, neighborY])
                                continue;

                            visited[neighborX, neighborY] = true;
                            var neighbor = new Vector2Int(neighborX, neighborY);
                            regionPixels.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }

                    if (regionPixels.Count >= 4)
                        regions.Add(regionPixels);
                }
            }

            return regions;
        }

        /// <summary>
        /// Trace boundary cua 1 vung trang, tra ve danh sach pixel boundary theo thu tu.
        /// Thuat toan: build 2D state array → walk 8 huong clockwise theo perimeter.
        /// state: 0=outside, 1=interior region, 2=boundary unvisited, 3=boundary visited.
        /// Safe: khong Moore-Neighbor, khong allocation trong vong lap, co max step guard.
        /// </summary>
        public static List<Vector2Int> TraceRegionBoundary(byte[,] mask, List<Vector2Int> regionPixels)
        {
            var w = mask.GetLength(0);
            var h = mask.GetLength(1);

            // Build state grid
            var state = new byte[w, h];

            // Step 1: Danh dau pixel region = 1 (interior)
            foreach (var pixel in regionPixels)
                state[pixel.x, pixel.y] = 1;

            // Step 2: Pixel co 4-neighbor ngoai region → boundary = 2
            var minBoundaryX = int.MaxValue;
            var startIndex = -1;
            for (var i = 0; i < regionPixels.Count; i++)
            {
                var px = regionPixels[i].x;
                var py = regionPixels[i].y;
                var isBoundary = false;

                // Kiem tra 4 huong
                if (px - 1 < 0 || state[px - 1, py] == 0) isBoundary = true;
                else if (px + 1 >= w || state[px + 1, py] == 0) isBoundary = true;
                else if (py - 1 < 0 || state[px, py - 1] == 0) isBoundary = true;
                else if (py + 1 >= h || state[px, py + 1] == 0) isBoundary = true;

                if (isBoundary)
                {
                    state[px, py] = 2;
                    if (px < minBoundaryX)
                    {
                        minBoundaryX = px;
                        startIndex = i;
                    }
                }
            }

            if (startIndex < 0) return new List<Vector2Int>();

            var start = regionPixels[startIndex];
            var result = new List<Vector2Int>();
            var currentX = start.x;
            var currentY = start.y;
            state[currentX, currentY] = 3;
            result.Add(start);

            const int maxSteps = 120000;

            for (var step = 0; step < maxSteps; step++)
            {
                var moved = false;

                // Duyet 8 huong clockwise tim boundary unvisited
                for (var dirIndex = 0; dirIndex < 8; dirIndex++)
                {
                    var neighborX = currentX + DirectionDeltaX[dirIndex];
                    var neighborY = currentY + DirectionDeltaY[dirIndex];

                    if (neighborX < 0 || neighborX >= w || neighborY < 0 || neighborY >= h)
                        continue;
                    if (state[neighborX, neighborY] != 2)
                        continue;

                    // Da tim thay boundary chua visit
                    state[neighborX, neighborY] = 3;
                    currentX = neighborX;
                    currentY = neighborY;
                    result.Add(new Vector2Int(currentX, currentY));
                    moved = true;
                    break;
                }

                if (!moved) break;

                // Kiem tra da tro ve gan start chua (8-adjacent)
                var distToStartX = currentX - start.x;
                if (distToStartX < 0) distToStartX = -distToStartX;
                var distToStartY = currentY - start.y;
                if (distToStartY < 0) distToStartY = -distToStartY;

                if (distToStartX <= 1 && distToStartY <= 1)
                {
                    // Kiem tra con boundary unvisited quanh current?
                    var hasUnvisited = false;
                    for (var dirIndex = 0; dirIndex < 8; dirIndex++)
                    {
                        var nx = currentX + DirectionDeltaX[dirIndex];
                        var ny = currentY + DirectionDeltaY[dirIndex];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && state[nx, ny] == 2)
                        {
                            hasUnvisited = true;
                            break;
                        }
                    }

                    if (!hasUnvisited)
                    {
                        // Dong loop: dam bao diem cuoi ket noi ve start
                        if (result.Count > 1 && result[result.Count - 1] != start)
                            result.Add(start);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Chuyen doi pixel boundary → local-space Vector2[] cho PolygonCollider2D.
        /// PolygonCollider2D.SetPath() nhan toa do local (relative to collider GameObject).
        /// expandOffset (world units): day collider ra ngoai tu centroid de de to mau hon.
        /// </summary>
        public static Vector2[] BoundaryToLocalPoints(
            List<Vector2Int> boundary, Sprite sprite, float expandOffset = 0f)
        {
            if (boundary == null || boundary.Count < 3)
                return new Vector2[0];

            var result = new Vector2[boundary.Count];
            var pivotX = sprite.pivot.x;
            var pivotY = sprite.pivot.y;
            var pixelsPerUnit = sprite.pixelsPerUnit;

            for (var i = 0; i < boundary.Count; i++)
            {
                var localX = (boundary[i].x - pivotX) / pixelsPerUnit;
                var localY = (pivotY - boundary[i].y) / pixelsPerUnit;
                result[i] = new Vector2(localX, localY);
            }

            if (expandOffset <= 0f)
                return result;

            // Tinh centroid cua cac diem local
            var centroid = Vector2.zero;
            for (var i = 0; i < result.Length; i++)
                centroid += result[i];
            centroid /= result.Length;

            // Day moi diem ra xa centroid theo huong tu centroid → diem
            for (var i = 0; i < result.Length; i++)
            {
                var dir = result[i] - centroid;
                var len = dir.magnitude;
                if (len > Mathf.Epsilon)
                    result[i] += dir / len * expandOffset;
            }

            return result;
        }

        // ── Douglas-Peucker ──

        private static List<Vector2Int> DouglasPeucker(List<Vector2Int> points, float epsilon)
        {
            if (points.Count <= 2) return new List<Vector2Int>(points);

            var epsSq = epsilon * epsilon;
            var maxDistSq = -1f;
            var splitIndex = -1;

            var first = points[0];
            var last = points[points.Count - 1];

            for (var i = 1; i < points.Count - 1; i++)
            {
                var d = SqrDistToLine(points[i], first, last);
                if (d > maxDistSq) { maxDistSq = d; splitIndex = i; }
            }

            if (maxDistSq > epsSq && splitIndex > 0)
            {
                var left = DouglasPeucker(points.GetRange(0, splitIndex + 1), epsilon);
                var right = DouglasPeucker(points.GetRange(splitIndex, points.Count - splitIndex), epsilon);
                left.RemoveAt(left.Count - 1);
                left.AddRange(right);
                return left;
            }

            return new List<Vector2Int> { first, last };
        }

        private static float SqrDistToLine(Vector2Int p, Vector2Int a, Vector2Int b)
        {
            var ab = b - a;
            var ap = p - a;
            var abSq = ab.sqrMagnitude;
            if (abSq < Mathf.Epsilon) return ap.sqrMagnitude;

            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abSq);
            var closest = new Vector2(a.x + t * ab.x, a.y + t * ab.y);
            var dx = p.x - closest.x;
            var dy = p.y - closest.y;
            return dx * dx + dy * dy;
        }
    }
}
