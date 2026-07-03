using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ==========================================
// 1. MODEL: Xu ly du lieu va logic trich xuat Pixel
// ==========================================
public class SpriteExporterModel
{
    public bool IsValidSprite(Sprite sprite) => sprite != null && sprite.texture != null;

    public string GetOutputPath(int index, Sprite sprite, string targetFolder, string namePattern)
    {
        var baseName = string.Empty;
        if (string.IsNullOrWhiteSpace(namePattern))
        {
            baseName = sprite.name;
        }
        else
        {
            var spriteNameSplits = sprite.name.Split(new char[] { '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
            string addString = string.Empty;
            if (spriteNameSplits.Length > 1)
            {
                if (spriteNameSplits.Length >= 2)
                    addString = sprite.name;
                else
                    addString = spriteNameSplits[0] + "_" + index;
            }
            else if (spriteNameSplits.Length == 1)
            {
                addString = sprite.name;
            }
            else
            {
                addString = index.ToString();
            }

            baseName = namePattern + "_" + addString;
        }

        return Path.Combine(targetFolder, baseName + ".png");
    }

    public void ApplyImportSettings(string assetPath, TextureImporterType textureType, SpriteMeshType meshType)
    {
        if (!assetPath.StartsWith("Assets"))
            return;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = textureType;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = meshType;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    public byte[] GetTextureBytes(Sprite sprite, bool cropToSprite)
    {
        Texture2D atlasTexture = sprite.texture;

        RenderTexture rt = RenderTexture.GetTemporary(atlasTexture.width, atlasTexture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(atlasTexture, rt);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableAtlas = new Texture2D(atlasTexture.width, atlasTexture.height);
        readableAtlas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableAtlas.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(rt);

        byte[] finalBytes;

        if (cropToSprite)
        {
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

            int x = Mathf.RoundToInt(uv.x * atlasTexture.width);
            int y = Mathf.RoundToInt(uv.y * atlasTexture.height);
            int maxX = Mathf.RoundToInt(uv.z * atlasTexture.width);
            int maxY = Mathf.RoundToInt(uv.w * atlasTexture.height);

            int width = maxX - x;
            int height = maxY - y;

            bool isRotated = sprite.packed && (sprite.packingRotation != SpritePackingRotation.None);
            Color[] pixels = readableAtlas.GetPixels(x, y, width, height);
            Texture2D croppedTexture;

            if (isRotated)
            {
                croppedTexture = new Texture2D(height, width);
                Color[] rotatedPixels = new Color[pixels.Length];

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        rotatedPixels[i * height + j] = pixels[(height - 1 - j) * width + i];
                    }
                }
                croppedTexture.SetPixels(rotatedPixels);
            }
            else
            {
                croppedTexture = new Texture2D(width, height);
                croppedTexture.SetPixels(pixels);
            }

            croppedTexture.Apply();
            finalBytes = croppedTexture.EncodeToPNG();
            Object.DestroyImmediate(croppedTexture);
        }
        else
        {
            finalBytes = readableAtlas.EncodeToPNG();
        }

        Object.DestroyImmediate(readableAtlas);
        return finalBytes;
    }
}

// ==========================================
// 2. PRESENTER: Dieu phoi trung gian giua Giao dien va Du lieu
// ==========================================
public class SpriteExporterPresenter
{
    private readonly SpriteExporterModel _model;
    private readonly SpriteAtlasExporterWindow _view;

    public SpriteExporterPresenter(SpriteAtlasExporterWindow view, SpriteExporterModel model)
    {
        _view = view;
        _model = model;
    }

    public void ExecuteExport(
        List<Sprite> sprites,
        string saveFolder,
        bool cropToSprite,
        string namePattern,
        TextureImporterType textureType,
        SpriteMeshType meshType)
    {
        if (sprites == null || sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Thong bao", "Danh sach Sprite trong!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(saveFolder) || !Directory.Exists(saveFolder))
        {
            EditorUtility.DisplayDialog("Loi", "Thu muc luu tru khong hop le hoac khong ton tai!", "OK");
            return;
        }

        int successCount = 0;

        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null) continue;

            float progress = (float)i / sprites.Count;
            EditorUtility.DisplayProgressBar("Exporting Sprites", $"Dang xu ly: {sprite.name}...", progress);

            if (!_model.IsValidSprite(sprite))
            {
                Debug.LogWarning($"[Skip] Sprite '{sprite.name}' khong co texture hop le.");
                continue;
            }

            try
            {
                byte[] bytes = _model.GetTextureBytes(sprite, cropToSprite);
                string fullPath = _model.GetOutputPath(i, sprite, saveFolder, namePattern);

                File.WriteAllBytes(fullPath, bytes);

                if (fullPath.StartsWith("Assets"))
                    _model.ApplyImportSettings(fullPath, textureType, meshType);

                successCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Loi khi export sprite '{sprite.name}': {ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("Hoan thanh", $"Da xuat thanh cong {successCount}/{sprites.Count} file anh.", "OK");
    }
}

// ==========================================
// 3. VIEW: Giao dien nguoi dung (Editor Window Popup)
// ==========================================
public class SpriteAtlasExporterWindow : EditorWindow
{
    private SpriteExporterModel _model;
    private SpriteExporterPresenter _presenter;

    private List<Sprite> _spriteList = new List<Sprite>();
    private string _saveFolder = "Assets";
    private string _namePattern = "";
    private TextureImporterType _textureType = TextureImporterType.Sprite;
    private SpriteMeshType _meshType = SpriteMeshType.FullRect;
    private GameObject _prefab;
    private bool _usePrefab;
    private List<GameObject> _prefabTargets = new List<GameObject>();
    private Vector2 _scrollPosition;
    private Vector2 _scrollPrefabTargets;

    [MenuItem("Tools/Sprite Atlas Exporter")]
    public static void ShowWindow()
    {
        var window = GetWindow<SpriteAtlasExporterWindow>("Atlas Exporter");
        window.minSize = new Vector2(400, 750);
        window.Show();
    }

    private void OnEnable()
    {
        _model = new SpriteExporterModel();
        _presenter = new SpriteExporterPresenter(this, _model);
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Atlas Exporter Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- SECTION 1: CAU HINH XUAT ---
        GUILayout.BeginVertical("box");
        GUILayout.Label("1. Cau hinh xuat anh:", EditorStyles.miniBoldLabel);

        GUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Save Folder:", _saveFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Save Folder", _saveFolder, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                    _saveFolder = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                else
                    _saveFolder = selectedPath;
            }
        }
        GUILayout.EndHorizontal();

        _namePattern = EditorGUILayout.TextField("Name Pattern:", _namePattern);

        _textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type:", _textureType);
        _meshType = (SpriteMeshType)EditorGUILayout.EnumPopup("Sprite Mesh Type:", _meshType);

        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // --- SECTION 2: PREFAB & AUTO ADD ---
        GUILayout.BeginVertical("box");
        _usePrefab = EditorGUILayout.Toggle("2. Prefab & Auto Add Assets:", _usePrefab);

        if (_usePrefab)
        {
            EditorGUI.BeginChangeCheck();
            _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab:", _prefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && _prefab != null)
            {
                _prefabTargets.Clear();
                _spriteList.Clear();
                FocusOnPrefab(_prefab);
            }

            EditorGUILayout.LabelField("Prefab Targets (GameObjects de tim SpriteRenderer):", EditorStyles.miniLabel);

            Rect dropTargetsArea = GUILayoutUtility.GetRect(0.0f, 30.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropTargetsArea, "KEO THA GAMEOBJECT VAO DAY DE THEM TARGET", EditorStyles.helpBox);

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropTargetsArea.Contains(Event.current.mousePosition)) break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is GameObject go && !_prefabTargets.Contains(go))
                                _prefabTargets.Add(go);
                        }
                    }
                    Event.current.Use();
                    break;
            }

            _scrollPrefabTargets = EditorGUILayout.BeginScrollView(_scrollPrefabTargets, GUILayout.Height(80));
            for (int i = _prefabTargets.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                _prefabTargets[i] = (GameObject)EditorGUILayout.ObjectField(_prefabTargets[i], typeof(GameObject), true);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                    _prefabTargets.RemoveAt(i);
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Them Target", GUILayout.Height(22)))
                _prefabTargets.Add(null);

            if (GUILayout.Button("Auto Add Assets", GUILayout.Height(22)))
                AutoAddSpritesFromTargets();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // --- SECTION 3: KEO THA ASSETS ---
        GUILayout.BeginVertical("box");

        GUILayout.Label("3. Danh sach Sprite can Export (Keo tha vao day):", EditorStyles.miniBoldLabel);

        EditorGUILayout.LabelField("So luong Sprites:", _spriteList.Count.ToString());

        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 40.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "KEO THA FILE .ASSET HOAC SPRITE VAO DAY", EditorStyles.helpBox);

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Sprite sprite)
                        {
                            if (!_spriteList.Contains(sprite)) _spriteList.Add(sprite);
                        }
                        else if (draggedObject is Texture2D tex)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                            foreach (Object asset in assets)
                            {
                                if (asset is Sprite s && !_spriteList.Contains(s)) _spriteList.Add(s);
                            }
                        }
                    }
                }
                break;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
        for (int i = _spriteList.Count - 1; i >= 0; i--)
        {
            GUILayout.BeginHorizontal();
            _spriteList[i] = (Sprite)EditorGUILayout.ObjectField($"{_spriteList[i].name}", _spriteList[i], typeof(Sprite), false);
            if (GUILayout.Button("Xoa", GUILayout.Width(50)))
                _spriteList.RemoveAt(i);
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (_spriteList.Count > 0 && GUILayout.Button("Xoa toan bo danh sach"))
            _spriteList.Clear();

        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // --- SECTION 4: HANH DONG EXPORT ---
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("Chi xuat rieng hinh Asset (Crop)", GUILayout.Height(35)))
        {
            _presenter.ExecuteExport(_spriteList, _saveFolder, true, _namePattern, _textureType, _meshType);
        }

        GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
        if (GUILayout.Button("Xuat nguyen ban (Ca Atlas)", GUILayout.Height(35)))
        {
            _presenter.ExecuteExport(_spriteList, _saveFolder, false, _namePattern, _textureType, _meshType);
        }

        GUILayout.EndHorizontal();
    }

    private void AutoAddSpritesFromTargets()
    {
        foreach (var target in _prefabTargets)
        {
            if (target == null) continue;

            var renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr.sprite != null && !_spriteList.Contains(sr.sprite))
                    _spriteList.Add(sr.sprite);
            }
        }
    }

    private void FocusOnPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        var assetPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(assetPath)) return;

        var assetType = PrefabUtility.GetPrefabAssetType(prefab);
        if (assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
