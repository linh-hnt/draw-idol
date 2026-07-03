using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPCurvedText : MonoBehaviour
{
    public enum SpacingPivot
    {
        Left,   // ký tự ĐẦU TIÊN cố định vị trí, các chữ còn lại giãn dần về bên phải
        Center, // giãn đều 2 bên quanh tâm dãy ký tự (mặc định)
        Right   // ký tự CUỐI CÙNG cố định vị trí, các chữ còn lại giãn dần về bên trái
    }

    [Header("Curve Settings")]
    [Tooltip("Bán kính cung tròn (đơn vị local).\n" +
             "= 0  : chữ xếp thẳng hàng ngang bình thường (không cong).\n" +
             "> 0  : cong bình thường (giữa nhô lên, 2 đầu chúc xuống).\n" +
             "< 0  : cong ngược (giữa lõm xuống, 2 đầu vểnh lên).")]
    public float radius = 0f;
    public bool clockwise = true;

    [Tooltip("Offset của tâm cung, tính từ tâm (local origin - vị trí 0,0) của Object đang chứa component TMP này.\n" +
             "X: dịch tâm cung sang trái/phải. Y: dịch toàn bộ cung lên/xuống.")]
    public Vector2 curveCenterOffset = Vector2.zero;

    [Header("Options")]
    public bool rotateCharacters = true; // có xoay từng ký tự theo tiếp tuyến cung không

    [Tooltip("Khoảng cách giữa tâm 2 ký tự liền kề, đo dọc theo cung (đơn vị local).\n" +
             "= 0 : giữ đúng khoảng cách tự nhiên như văn bản CHƯA bị bẻ cong.\n" +
             "≠ 0 : ép khoảng cách giữa các ký tự về đúng giá trị này (âm = các chữ xích lại gần nhau).")]
    public float letterSpacing = 0f;

    [Tooltip("Điểm neo khi phân bố letterSpacing:\n" +
             "Left = ký tự đầu cố định, chữ giãn dần sang phải\n" +
             "Center = giãn đều 2 bên (mặc định)\n" +
             "Right = ký tự cuối cố định, chữ giãn dần sang trái")]
    public SpacingPivot spacingPivot = SpacingPivot.Center;

    [Tooltip("Nếu true: mỗi dòng (line) sẽ được bo cong riêng theo bounds của chính nó. " +
             "Nếu false: dùng bounds tổng của toàn bộ text (chỉ đúng cho 1 dòng).")]
    public bool supportMultiLine = false;

    // Cờ điều khiển nội bộ: false = không bo cong, giữ layout TMP mặc định
    bool applyCurve = true;

    // Cache vertex GỐC (chưa cong) của từng submesh, lấy 1 lần mỗi khi text/layout đổi
    // -> tránh phải gọi ForceMeshUpdate() (rất nặng vì regenerate toàn bộ glyph layout) mỗi frame
    Vector3[][] m_OriginalVertices;
    bool m_IsDirty = true;

    TMP_Text m_TextComponent;

    void OnEnable()
    {
        m_TextComponent = GetComponent<TMP_Text>();
        // Lắng nghe sự kiện TMP tự báo khi text/layout thay đổi (đổi nội dung, font size, RectTransform...)
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        m_IsDirty = true;
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnTextChanged(UnityEngine.Object obj)
    {
        if (obj == (UnityEngine.Object)m_TextComponent)
            m_IsDirty = true;
    }

    /// <summary>
    /// Gọi hàm này nếu bạn tự đổi radius / letterSpacing / curveCenterOffset / rotateCharacters bằng code
    /// (đổi qua Inspector trong Editor thì OnValidate bên dưới đã tự lo).
    /// </summary>
    public void MarkDirty()
    {
        m_IsDirty = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        m_IsDirty = true;
    }
#endif

    /// <summary>
    /// Reset text về đúng dạng TextMeshPro bình thường (không bị bo cong),
    /// giữ nguyên component trên GameObject để có thể bật cong trở lại sau này
    /// bằng cách gọi SetCurveEnabled(true).
    /// </summary>
    public void ResetAll()
    {
        if (m_TextComponent == null) m_TextComponent = GetComponent<TMP_Text>();
        if (m_TextComponent == null) return;

        applyCurve = false;

        // Buộc TMP dựng lại mesh theo layout gốc (chưa bị chỉnh vertex) và cập nhật ngay lập tức
        m_TextComponent.ForceMeshUpdate(true, true);
        m_OriginalVertices = null;
    }

    /// <summary>
    /// Bật/tắt hiệu ứng bo cong theo yêu cầu (gọi ResetAll() nếu muốn tắt hẳn và về trạng thái mặc định).
    /// </summary>
    public void SetCurveEnabled(bool value)
    {
        applyCurve = value;
        if (!value && m_TextComponent != null)
        {
            m_TextComponent.ForceMeshUpdate(true, true);
            m_OriginalVertices = null;
        }
        else if (value)
        {
            m_IsDirty = true;
        }
    }

    void LateUpdate()
    {
        if (m_TextComponent == null) return;
        if (!applyCurve) return;

        // CHỈ regenerate layout (nặng) khi thật sự có thay đổi, không chạy vô điều kiện mỗi frame
        if (m_IsDirty)
        {
            m_TextComponent.ForceMeshUpdate();
            CacheOriginalVertices();
            m_IsDirty = false;
        }

        if (m_OriginalVertices == null) return;
        TMP_TextInfo textInfo = m_TextComponent.textInfo;
        int characterCount = textInfo.characterCount;
        if (characterCount == 0) return;

        float dir = clockwise ? -1f : 1f;

        if (supportMultiLine && textInfo.lineCount > 1)
        {
            for (int lineIndex = 0; lineIndex < textInfo.lineCount; lineIndex++)
            {
                TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];
                ApplyCurveToRange(textInfo, lineInfo.firstCharacterIndex, lineInfo.lastCharacterIndex, dir);
            }
        }
        else
        {
            ApplyCurveToRange(textInfo, 0, characterCount - 1, dir);
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    void CacheOriginalVertices()
    {
        TMP_TextInfo textInfo = m_TextComponent.textInfo;
        int meshCount = textInfo.meshInfo.Length;
        m_OriginalVertices = new Vector3[meshCount][];

        for (int i = 0; i < meshCount; i++)
        {
            Vector3[] src = textInfo.meshInfo[i].vertices;
            Vector3[] copy = new Vector3[src.Length];
            System.Array.Copy(src, copy, src.Length);
            m_OriginalVertices[i] = copy;
        }
    }

    Vector3 GetCharPivotPoint(TMP_TextInfo textInfo, int charIndex, Vector3[] verts, int vertexIndex)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

        // X lấy tâm ngang theo mép dưới ký tự
        float x = (verts[vertexIndex + 0].x + verts[vertexIndex + 3].x) * 0.5f;
        float z = verts[vertexIndex + 0].z;

        // Y LUÔN lấy baseline thật của TMP (giống nhau cho mọi ký tự cùng dòng) -
        // KHÔNG lấy từ bounding box riêng từng ký tự, vì bbox của "g","y","p","q" (có đuôi)
        // thấp hơn hẳn "A","T" -> nếu neo theo bbox sẽ đẩy sai lệch chiều cao giữa các chữ.
        // Dùng baseline đúng giữ nguyên hình dạng gốc (kể cả đuôi chữ) y hệt TMP layout bình thường.
        float y = charInfo.baseLine;

        return new Vector3(x, y, z);
    }

    // Buffer tái sử dụng để tránh cấp phát mảng mới mỗi lần gọi (đỡ GC alloc trên mobile)
    int[] m_TempCharIndices = new int[64];
    Vector3[] m_TempPivots = new Vector3[64];
    float[] m_TempLinearPos = new float[64];

    void ApplyCurveToRange(TMP_TextInfo textInfo, int firstChar, int lastChar, float dir)
    {
        // --- Pass 1: gom danh sách ký tự visible + tính pivot point gốc ---
        int count = 0;
        for (int i = firstChar; i <= lastChar; i++)
        {
            if (i >= textInfo.characterCount || !textInfo.characterInfo[i].isVisible) continue;

            if (count >= m_TempCharIndices.Length)
            {
                System.Array.Resize(ref m_TempCharIndices, m_TempCharIndices.Length * 2);
                System.Array.Resize(ref m_TempPivots, m_TempPivots.Length * 2);
                System.Array.Resize(ref m_TempLinearPos, m_TempLinearPos.Length * 2);
            }

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] originalVerts = m_OriginalVertices[materialIndex];

            m_TempCharIndices[count] = i;
            m_TempPivots[count] = GetCharPivotPoint(textInfo, i, originalVerts, vertexIndex);
            count++;
        }

        if (count == 0) return;

        // --- Tính "vị trí dọc theo baseline" (linear position) của từng ký tự ---
        // letterSpacing == 0 -> dùng đúng khoảng cách tự nhiên gốc (pivot.x của layout TMP, không đổi gì cả)
        // letterSpacing != 0 -> thay khoảng cách TỰ NHIÊN giữa 2 ký tự liền kề bằng letterSpacing cố định
        bool useCustomSpacing = !Mathf.Approximately(letterSpacing, 0f);
        if (useCustomSpacing)
        {
            m_TempLinearPos[0] = 0f;
            for (int k = 1; k < count; k++)
                m_TempLinearPos[k] = m_TempLinearPos[k - 1] + letterSpacing;
        }
        else
        {
            for (int k = 0; k < count; k++)
                m_TempLinearPos[k] = m_TempPivots[k].x; // giữ nguyên khoảng cách gốc như chưa bẻ cong
        }

        // --- Chọn điểm neo (reference) theo spacingPivot để tính "dist" cho từng ký tự ---
        float referencePos;
        switch (spacingPivot)
        {
            case SpacingPivot.Left:
                referencePos = m_TempLinearPos[0];
                break;
            case SpacingPivot.Right:
                referencePos = m_TempLinearPos[count - 1];
                break;
            case SpacingPivot.Center:
            default:
                float minPos = m_TempLinearPos[0];
                float maxPos = m_TempLinearPos[0];
                for (int k = 1; k < count; k++)
                {
                    if (m_TempLinearPos[k] < minPos) minPos = m_TempLinearPos[k];
                    if (m_TempLinearPos[k] > maxPos) maxPos = m_TempLinearPos[k];
                }
                referencePos = (minPos + maxPos) * 0.5f;
                break;
        }

        bool isFlat = Mathf.Abs(radius) < 0.0001f; // radius = 0 -> xếp thẳng hàng bình thường, không cong

        // Tâm cung LẤY TRỰC TIẾP từ local origin (0,0) của Object chứa TMP + offset người dùng set,
        // KHÔNG còn tự tính từ bounds của text nữa -> tâm cung ổn định, không "nhảy" khi đổi nội dung text.
        Vector3 arcCenter = new Vector3(curveCenterOffset.x, curveCenterOffset.y - radius, 0f);

        for (int k = 0; k < count; k++)
        {
            int i = m_TempCharIndices[k];
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

            Vector3[] originalVerts = m_OriginalVertices[materialIndex];
            Vector3[] targetVerts = textInfo.meshInfo[materialIndex].vertices;
            Vector3 pivotPoint = m_TempPivots[k];

            float dist = m_TempLinearPos[k] - referencePos;

            Vector3 pointOnArc;
            float angle = 0f;

            if (isFlat)
            {
                // Không cong: chỉ dịch theo phương ngang (letterSpacing + curveCenterOffset), giữ nguyên Y gốc, không xoay
                pointOnArc = new Vector3(
                    curveCenterOffset.x + dist,
                    pivotPoint.y + curveCenterOffset.y,
                    pivotPoint.z);
            }
            else
            {
                // arcLength = radius * angle  =>  angle = arcLength / radius
                angle = (dist / radius) * dir;

                pointOnArc = arcCenter + new Vector3(
                    Mathf.Sin(angle) * radius,
                    Mathf.Cos(angle) * radius,
                    0f);
            }

            for (int j = 0; j < 4; j++)
            {
                Vector3 orig = originalVerts[vertexIndex + j] - pivotPoint;

                if (!isFlat && rotateCharacters)
                {
                    Quaternion rot = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
                    targetVerts[vertexIndex + j] = pointOnArc + rot * orig;
                }
                else
                {
                    targetVerts[vertexIndex + j] = pointOnArc + orig;
                }
            }
        }
    }
}