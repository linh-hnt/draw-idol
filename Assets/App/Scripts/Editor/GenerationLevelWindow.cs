using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;

namespace App.Editor
{
    public partial class GenerationLevelWindow : EditorWindow
    {
        // ── PATH TAB ──
        private GameObject _pathPrefab;
        private List<GameObject> _pathObjects = new List<GameObject>();
        private Vector2 _pathScrollPos;

        // ── PAINT TAB ──
        private GameObject _paintItemPrefab;
        private List<GameObject> _paintObjects = new List<GameObject>();
        private List<Sprite> _sprites = new List<Sprite>();
        private Vector2 _paintScrollPos;
        private Vector2 _spriteScrollPos;

        // ── BLUEPRINT TAB ──
        // Part 1: Path
        private List<GameObject> _bpPathObjects = new List<GameObject>();
        private List<Sprite> _bpPathSprites = new List<Sprite>();
        private Material _bpPathMaterial;
        private Vector2 _bpPathScrollPos;
        private Vector2 _bpPathSpriteScrollPos;

        // Part 2: Template
        private List<GameObject> _bpTemplateObjects = new List<GameObject>();
        private List<Sprite> _bpTemplateSprites = new List<Sprite>();
        private Material _bpTemplateMaterial;
        private Vector2 _bpTemplateScrollPos;
        private Vector2 _bpTemplateSpriteScrollPos;

        // ── LEVELUPDATE TAB ──
        private GameObject _levelPrefab;
        private List<GameObject> _levelDrawItems = new List<GameObject>();
        private List<GameObject> _levelPaintItems = new List<GameObject>();
        private Vector2 _levelUpdateDrawScrollPos;
        private Vector2 _levelUpdatePaintScrollPos;

        private int _selectedTab;
        private readonly string[] _tabNames = { "Path", "Paint", "BluePrint", "LevelUpdate" };

        [MenuItem("Tools/Clone From Ref Game")]
        private static void ShowWindow()
        {
            var window = GetWindow<GenerationLevelWindow>("Clone From Ref Game");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Clone From Ref Game", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0:
                    DrawPathTab();
                    break;
                case 1:
                    DrawPaintTab();
                    break;
                case 2:
                    DrawBluePrintTab();
                    break;
                case 3:
                    DrawLevelUpdateTab();
                    break;
            }
        }

        // ═══════════════════════════════════════════════
        //  PATH TAB
        // ═══════════════════════════════════════════════

        private void DrawPathTab()
        {
            // ── Path Prefab ──
            EditorGUILayout.LabelField("Path Prefab", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Prefab goc cua Path, chua SplineComputer + PenTrailDrawer, se duoc spawn lam sibling cua tung Path Object.", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _pathPrefab = (GameObject)EditorGUILayout.ObjectField("Path Prefab", _pathPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Path Objects", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach cac GameObject tham chieu co SplineComputer. Moi Path Object se tao 1 ban sao tu Path Prefab, copy Spline tu Object sang ban sao.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_pathObjects.Count}");

            EditorGUILayout.Space(2);

            var pathDropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            GUI.Box(pathDropArea, "KEO THA PATH OBJECTS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(pathDropArea, _pathObjects);

            EditorGUILayout.Space(5);

            _pathScrollPos = EditorGUILayout.BeginScrollView(_pathScrollPos, GUILayout.Height(150));
            {
                for (var i = 0; i < _pathObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _pathObjects[i] = (GameObject)EditorGUILayout.ObjectField(_pathObjects[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _pathObjects.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Path Object"))
                {
                    _pathObjects.Add(null);
                }

                if (_pathObjects.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _pathObjects.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            var errorMsg = GetPathValidationError();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorGUILayout.HelpBox(errorMsg, MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = string.IsNullOrEmpty(errorMsg);
            if (GUILayout.Button("Run Path", GUILayout.Height(35)))
            {
                RunPathGeneration();
            }
            GUI.enabled = true;
        }

        // ═══════════════════════════════════════════════
        //  PAINT TAB
        // ═══════════════════════════════════════════════

        private void DrawPaintTab()
        {
            // ── PaintItem Prefab ──
            EditorGUILayout.LabelField("PaintItem Prefab", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Prefab goc cua Paint Item, chua SpriteRenderer + SpriteMask + PolygonCollider2D, se duoc spawn lam sibling cua tung Paint Object.", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _paintItemPrefab = (GameObject)EditorGUILayout.ObjectField("PaintItem Prefab", _paintItemPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Paint Objects", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach cac GameObject tham chieu. Moi Paint Object se tao 1 ban sao PaintItem, copy Transform + PolygonCollider2D sang ban sao.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_paintObjects.Count}");

            EditorGUILayout.Space(2);

            var paintDropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            GUI.Box(paintDropArea, "KEO THA PAINT OBJECTS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(paintDropArea, _paintObjects);

            EditorGUILayout.Space(5);

            _paintScrollPos = EditorGUILayout.BeginScrollView(_paintScrollPos, GUILayout.Height(100));
            {
                for (var i = 0; i < _paintObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _paintObjects[i] = (GameObject)EditorGUILayout.ObjectField(_paintObjects[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _paintObjects.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Paint Object"))
                {
                    _paintObjects.Add(null);
                }

                if (_paintObjects.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _paintObjects.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach Sprite tuong ung voi tung Paint Object (theo index). Sprite se duoc gan vao SpriteRenderer + SpriteMask cua ban sao.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_sprites.Count}");

            EditorGUILayout.Space(2);

            var spriteDropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            GUI.Box(spriteDropArea, "KEO THA SPRITES VAO DAY", EditorStyles.helpBox);
            HandleSpriteListDrop(spriteDropArea, _sprites);

            EditorGUILayout.Space(5);

            _spriteScrollPos = EditorGUILayout.BeginScrollView(_spriteScrollPos, GUILayout.Height(100));
            {
                for (var i = 0; i < _sprites.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _sprites[i] = (Sprite)EditorGUILayout.ObjectField(_sprites[i], typeof(Sprite), false);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _sprites.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Sprite"))
                {
                    _sprites.Add(null);
                }

                if (_sprites.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _sprites.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            var errorMsg = GetPaintValidationError();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorGUILayout.HelpBox(errorMsg, MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = string.IsNullOrEmpty(errorMsg);
            if (GUILayout.Button("Run Paint", GUILayout.Height(35)))
            {
                RunPaintGeneration();
            }
            GUI.enabled = true;
        }

        // ═══════════════════════════════════════════════
        //  BLUEPRINT TAB
        // ═══════════════════════════════════════════════

        private void DrawBluePrintTab()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2f - 20f));
                {
                    DrawBluePrintPathPart();
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2f - 20f));
                {
                    DrawBluePrintTemplatePart();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            var errorMsg = GetBluePrintValidationError();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorGUILayout.HelpBox(errorMsg, MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = string.IsNullOrEmpty(errorMsg);
            if (GUILayout.Button("Run BluePrint", GUILayout.Height(35)))
            {
                RunBluePrintPath();
                RunBluePrintTemplate();
            }
            GUI.enabled = true;
        }

        private void DrawBluePrintPathPart()
        {
            EditorGUILayout.LabelField("Part 1: Path Objects", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Gan Sprite & Material cho SpriteRenderer cua cac Object Path. Dung de set hinh anh + material sau khi da tao Path.", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            _bpPathMaterial = (Material)EditorGUILayout.ObjectField("Material", _bpPathMaterial, typeof(Material), false);
            EditorGUILayout.LabelField("Material se duoc gan cho toan bo SpriteRenderer trong Object Path.", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // ── BP Path Objects ──
            EditorGUILayout.LabelField("Object Path", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach GameObject Path can gan Sprite & Material. Moi Object se duoc duyet toan bo SpriteRenderer trong children.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_bpPathObjects.Count}");

            EditorGUILayout.Space(2);

            var bpPathDropArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
            GUI.Box(bpPathDropArea, "KEO THA PATH OBJECTS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(bpPathDropArea, _bpPathObjects);

            EditorGUILayout.Space(5);

            _bpPathScrollPos = EditorGUILayout.BeginScrollView(_bpPathScrollPos, GUILayout.Height(80));
            {
                for (var i = 0; i < _bpPathObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _bpPathObjects[i] = (GameObject)EditorGUILayout.ObjectField(_bpPathObjects[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _bpPathObjects.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Object Path"))
                {
                    _bpPathObjects.Add(null);
                }

                if (_bpPathObjects.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _bpPathObjects.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ── BP Path Sprites ──
            EditorGUILayout.LabelField("Sprite Path", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach Sprite tuong ung voi tung Object Path (theo index). Sprite se duoc gan vao SpriteRenderer cua Object.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_bpPathSprites.Count}");

            EditorGUILayout.Space(2);

            var bpPathSpriteDropArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
            GUI.Box(bpPathSpriteDropArea, "KEO THA SPRITE PATH VAO DAY", EditorStyles.helpBox);
            HandleSpriteListDrop(bpPathSpriteDropArea, _bpPathSprites);

            EditorGUILayout.Space(5);

            _bpPathSpriteScrollPos = EditorGUILayout.BeginScrollView(_bpPathSpriteScrollPos, GUILayout.Height(80));
            {
                for (var i = 0; i < _bpPathSprites.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _bpPathSprites[i] = (Sprite)EditorGUILayout.ObjectField(_bpPathSprites[i], typeof(Sprite), false);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _bpPathSprites.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Sprite Path"))
                {
                    _bpPathSprites.Add(null);
                }

                if (_bpPathSprites.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _bpPathSprites.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawBluePrintTemplatePart()
        {
            EditorGUILayout.LabelField("Part 2: Template Objects", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Gan Sprite & Material cho SpriteRenderer cua cac Object Template. Tuong tu BluePrint Path nhung cho Template.", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            _bpTemplateMaterial = (Material)EditorGUILayout.ObjectField("Material", _bpTemplateMaterial, typeof(Material), false);
            EditorGUILayout.LabelField("Material se duoc gan cho toan bo SpriteRenderer trong Object Template.", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // ── BP Template Objects ──
            EditorGUILayout.LabelField("Object Template", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach GameObject Template can gan Sprite & Material. Moi Object se duoc duyet toan bo SpriteRenderer trong children.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_bpTemplateObjects.Count}");

            EditorGUILayout.Space(2);

            var bpTemplateDropArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
            GUI.Box(bpTemplateDropArea, "KEO THA TEMPLATE OBJECTS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(bpTemplateDropArea, _bpTemplateObjects);

            EditorGUILayout.Space(5);

            _bpTemplateScrollPos = EditorGUILayout.BeginScrollView(_bpTemplateScrollPos, GUILayout.Height(80));
            {
                for (var i = 0; i < _bpTemplateObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _bpTemplateObjects[i] = (GameObject)EditorGUILayout.ObjectField(_bpTemplateObjects[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _bpTemplateObjects.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Object Template"))
                {
                    _bpTemplateObjects.Add(null);
                }

                if (_bpTemplateObjects.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _bpTemplateObjects.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ── BP Template Sprites ──
            EditorGUILayout.LabelField("Sprite Template", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach Sprite tuong ung voi tung Object Template (theo index). Sprite se duoc gan vao SpriteRenderer cua Object.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_bpTemplateSprites.Count}");

            EditorGUILayout.Space(2);

            var bpTemplateSpriteDropArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
            GUI.Box(bpTemplateSpriteDropArea, "KEO THA SPRITE TEMPLATE VAO DAY", EditorStyles.helpBox);
            HandleSpriteListDrop(bpTemplateSpriteDropArea, _bpTemplateSprites);

            EditorGUILayout.Space(5);

            _bpTemplateSpriteScrollPos = EditorGUILayout.BeginScrollView(_bpTemplateSpriteScrollPos, GUILayout.Height(80));
            {
                for (var i = 0; i < _bpTemplateSprites.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _bpTemplateSprites[i] = (Sprite)EditorGUILayout.ObjectField(_bpTemplateSprites[i], typeof(Sprite), false);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _bpTemplateSprites.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Sprite Template"))
                {
                    _bpTemplateSprites.Add(null);
                }

                if (_bpTemplateSprites.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _bpTemplateSprites.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        // ═══════════════════════════════════════════════
        //  LEVELUPDATE TAB
        // ═══════════════════════════════════════════════

        private void DrawLevelUpdateTab()
        {
            // ── Level Prefab ──
            EditorGUILayout.LabelField("Level Prefab", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Level Prefab asset co chua LevelController component. Du lieu _drawSteps va _paintSteps se duoc populate tu danh sach Draw/Paint Items ben duoi.", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);

            _levelPrefab = (GameObject)EditorGUILayout.ObjectField("Level Prefab", _levelPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(10);

            // ── Draw Items ──
            EditorGUILayout.LabelField("Draw Items", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach cac GameObject Draw Item trong Level Prefab. Moi item se populate SplineComputer + PenTrailDrawer + SplinePrefabSpawner vao _drawSteps.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_levelDrawItems.Count}");

            EditorGUILayout.Space(2);

            var drawDropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            GUI.Box(drawDropArea, "KEO THA DRAW ITEMS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(drawDropArea, _levelDrawItems);

            EditorGUILayout.Space(5);

            _levelUpdateDrawScrollPos = EditorGUILayout.BeginScrollView(_levelUpdateDrawScrollPos, GUILayout.Height(100));
            {
                for (var i = 0; i < _levelDrawItems.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _levelDrawItems[i] = (GameObject)EditorGUILayout.ObjectField(_levelDrawItems[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _levelDrawItems.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Draw Item"))
                {
                    _levelDrawItems.Add(null);
                }

                if (_levelDrawItems.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _levelDrawItems.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // ── Paint Items ──
            EditorGUILayout.LabelField("Paint Items", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Danh sach cac GameObject Paint Item trong Level Prefab. Moi item se populate SpritePainter + Caro vao _paintSteps.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"So luong: {_levelPaintItems.Count}");

            EditorGUILayout.Space(2);

            var paintDropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
            GUI.Box(paintDropArea, "KEO THA PAINT ITEMS VAO DAY", EditorStyles.helpBox);
            HandleGameObjectListDrop(paintDropArea, _levelPaintItems);

            EditorGUILayout.Space(5);

            _levelUpdatePaintScrollPos = EditorGUILayout.BeginScrollView(_levelUpdatePaintScrollPos, GUILayout.Height(100));
            {
                for (var i = 0; i < _levelPaintItems.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _levelPaintItems[i] = (GameObject)EditorGUILayout.ObjectField(_levelPaintItems[i], typeof(GameObject), true);

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            _levelPaintItems.RemoveAt(i);
                            i--;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Them Paint Item"))
                {
                    _levelPaintItems.Add(null);
                }

                if (_levelPaintItems.Count > 0 && GUILayout.Button("Xoa Tat Ca"))
                {
                    _levelPaintItems.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            var errorMsg = GetLevelUpdateValidationError();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorGUILayout.HelpBox(errorMsg, MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            GUI.enabled = string.IsNullOrEmpty(errorMsg);
            if (GUILayout.Button("Run LevelUpdate", GUILayout.Height(35)))
            {
                RunLevelUpdate();
            }

            GUI.enabled = _levelPrefab != null && _levelPrefab.GetComponent<App.LevelController>() != null;
            // if (GUILayout.Button("Copy Brush Color From PaintController", GUILayout.Height(30)))
            // {
            //     CopyBrushColorFromPaintController();
            // }
            GUI.enabled = true;
        }

        // ═══════════════════════════════════════════════
        //  DROP HANDLERS
        // ═══════════════════════════════════════════════

        private void HandleGameObjectListDrop(Rect dropArea, List<GameObject> targetList)
        {
            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    var added = false;
                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is GameObject go && !targetList.Contains(go))
                        {
                            targetList.Add(go);
                            added = true;
                        }
                    }

                    // fallback: nếu kéo từ Project window, thử load theo path
                    if (!added)
                    {
                        foreach (var path in DragAndDrop.paths)
                        {
                            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (go != null && !targetList.Contains(go))
                            {
                                targetList.Add(go);
                                added = true;
                            }
                        }
                    }

                    if (added) Repaint();
                    evt.Use();
                    break;
            }
        }

        private void HandleSpriteListDrop(Rect dropArea, List<Sprite> targetList)
        {
            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    var added = false;
                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is Sprite sprite && !targetList.Contains(sprite))
                        {
                            targetList.Add(sprite);
                            added = true;
                        }
                    }

                    // fallback: nếu kéo texture/sprite từ Project window
                    if (!added)
                    {
                        foreach (var path in DragAndDrop.paths)
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                            if (sprite != null && !targetList.Contains(sprite))
                            {
                                targetList.Add(sprite);
                                added = true;
                            }
                        }
                    }

                    if (added) Repaint();
                    evt.Use();
                    break;
            }
        }

        // ═══════════════════════════════════════════════
        //  PATH VALIDATION
        // ═══════════════════════════════════════════════

        private string GetPathValidationError()
        {
            if (_pathPrefab == null)
                return "Chua keo Path Prefab.";

            if (_pathObjects.Count == 0)
                return "Chua co Path Object nao trong danh sach.";

            if (!ValidateSplineInPrefab(_pathPrefab))
                return "Path Prefab khong co SplineComputer o child.";

            for (var i = 0; i < _pathObjects.Count; i++)
            {
                if (_pathObjects[i] == null)
                    return $"Path Object [{i}] dang null.";

                if (_pathObjects[i].GetComponentInChildren<SplineComputer>() == null)
                    return $"Path Object [{i}] \"{_pathObjects[i].name}\" khong co SplineComputer o child.";
            }

            return null;
        }

        // ═══════════════════════════════════════════════
        //  PAINT VALIDATION
        // ═══════════════════════════════════════════════

        private string GetPaintValidationError()
        {
            if (_paintItemPrefab == null)
                return "Chua keo PaintItem Prefab.";

            if (_paintObjects.Count == 0)
                return "Chua co Paint Object nao trong danh sach.";

            if (_sprites.Count == 0)
                return "Chua co Sprite nao trong danh sach.";

            if (_sprites.Count < _paintObjects.Count)
                return $"So luong Sprite ({_sprites.Count}) it hon so luong Paint Object ({_paintObjects.Count}).";

            for (var i = 0; i < _paintObjects.Count; i++)
            {
                if (_paintObjects[i] == null)
                    return $"Paint Object [{i}] dang null.";
            }

            for (var i = 0; i < _sprites.Count; i++)
            {
                if (_sprites[i] == null)
                    return $"Sprite [{i}] dang null.";
            }

            return null;
        }

        // ═══════════════════════════════════════════════
        //  BLUEPRINT PATH VALIDATION
        // ═══════════════════════════════════════════════

        private string GetBluePrintPathValidationError()
        {
            if (_bpPathObjects.Count == 0)
                return "[BluePrint Path] Chua co Object Path nao trong danh sach.";

            if (_bpPathSprites.Count == 0)
                return "[BluePrint Path] Chua co Sprite Path nao trong danh sach.";

            if (_bpPathSprites.Count < _bpPathObjects.Count)
                return $"[BluePrint Path] So luong Sprite Path ({_bpPathSprites.Count}) it hon so luong Object Path ({_bpPathObjects.Count}).";

            for (var i = 0; i < _bpPathObjects.Count; i++)
            {
                if (_bpPathObjects[i] == null)
                    return $"[BluePrint Path] Object Path [{i}] dang null.";
            }

            for (var i = 0; i < _bpPathSprites.Count; i++)
            {
                if (_bpPathSprites[i] == null)
                    return $"[BluePrint Path] Sprite Path [{i}] dang null.";
            }

            return null;
        }

        // ═══════════════════════════════════════════════
        //  BLUEPRINT TEMPLATE VALIDATION
        // ═══════════════════════════════════════════════

        private string GetBluePrintTemplateValidationError()
        {
            if (_bpTemplateObjects.Count == 0)
                return "[BluePrint Template] Chua co Object Template nao trong danh sach.";

            if (_bpTemplateSprites.Count == 0)
                return "[BluePrint Template] Chua co Sprite Template nao trong danh sach.";

            if (_bpTemplateSprites.Count < _bpTemplateObjects.Count)
                return $"[BluePrint Template] So luong Sprite Template ({_bpTemplateSprites.Count}) it hon so luong Object Template ({_bpTemplateObjects.Count}).";

            for (var i = 0; i < _bpTemplateObjects.Count; i++)
            {
                if (_bpTemplateObjects[i] == null)
                    return $"[BluePrint Template] Object Template [{i}] dang null.";
            }

            for (var i = 0; i < _bpTemplateSprites.Count; i++)
            {
                if (_bpTemplateSprites[i] == null)
                    return $"[BluePrint Template] Sprite Template [{i}] dang null.";
            }

            return null;
        }

        private string GetBluePrintValidationError()
        {
            var pathError = GetBluePrintPathValidationError();
            if (!string.IsNullOrEmpty(pathError)) return pathError;

            return GetBluePrintTemplateValidationError();
        }

        // ═══════════════════════════════════════════════
        //  LEVELUPDATE VALIDATION
        // ═══════════════════════════════════════════════

        private string GetLevelUpdateValidationError()
        {
            if (_levelPrefab == null)
                return "Chua keo Level Prefab.";

            if (_levelPrefab.GetComponent<App.LevelController>() == null)
                return "Level Prefab khong co LevelController component.";

            if (_levelDrawItems.Count == 0)
                return "Chua co Draw Item nao trong danh sach.";

            if (_levelPaintItems.Count == 0)
                return "Chua co Paint Item nao trong danh sach.";

            for (var i = 0; i < _levelDrawItems.Count; i++)
            {
                if (_levelDrawItems[i] == null)
                    return $"Draw Item [{i}] dang null.";
            }

            for (var i = 0; i < _levelPaintItems.Count; i++)
            {
                if (_levelPaintItems[i] == null)
                    return $"Paint Item [{i}] dang null.";
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private bool ValidateSplineInPrefab(GameObject prefab)
        {
            if (prefab == null) return false;

            if (prefab.GetComponentInChildren<SplineComputer>() != null) return true;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var hasSpline = instance.GetComponentInChildren<SplineComputer>() != null;
            DestroyImmediate(instance);
            return hasSpline;
        }

        // ═══════════════════════════════════════════════
        //  RUN PATH
        // ═══════════════════════════════════════════════

        private void RunPathGeneration()
        {
            if (_pathPrefab == null || _pathObjects.Count == 0) return;

            var createdCount = 0;
            GameObject instance = null;
            GameObject spawnPointObject = null;

            foreach (var pathObj in _pathObjects)
            {
                var parent = pathObj.transform.parent;
                instance = (GameObject)PrefabUtility.InstantiatePrefab(_pathPrefab, parent);

                if (instance == null)
                {
                    Debug.LogError($"[GenerationLevel/Path] Khong the instantiate prefab cho Path Object \"{pathObj.name}\".");
                    continue;
                }

                instance.name = $"{_pathPrefab.name}_{createdCount.ToString()}";

                Undo.RegisterCreatedObjectUndo(instance, "Generation Level - Path");

                var sourceSpline = pathObj.GetComponentInChildren<SplineComputer>();
                var destSpline = instance.GetComponentInChildren<SplineComputer>();

                if (sourceSpline == null || destSpline == null)
                {
                    Debug.LogWarning($"[GenerationLevel/Path] Khong tim thay SplineComputer o child.");
                    continue;
                }

                destSpline.gameObject.name = $"path_{createdCount.ToString()}";

                spawnPointObject = instance.GetComponentInChildren<App.SplinePrefabSpawner>()?.gameObject;
                if(spawnPointObject != null)
                {
                    spawnPointObject.name = $"dot_{createdCount.ToString()}";
                }

                Undo.RecordObject(destSpline.transform, "Generation Level - Path");
                destSpline.transform.position = sourceSpline.transform.position;
                destSpline.transform.rotation = sourceSpline.transform.rotation;
                destSpline.transform.localScale = sourceSpline.transform.localScale;

                Undo.RecordObject(destSpline, "Generation Level - Path");
                var points = sourceSpline.GetPoints(SplineComputer.Space.World);
                destSpline.SetPoints(points, SplineComputer.Space.World);
                destSpline.RebuildImmediate();

                EditorUtility.SetDirty(destSpline);

                createdCount++;
            }

            Debug.Log($"[GenerationLevel/Path] Da tao {createdCount} Path instance.");
        }

        // ═══════════════════════════════════════════════
        //  RUN PAINT
        // ═══════════════════════════════════════════════

        private void RunPaintGeneration()
        {
            if (_paintItemPrefab == null || _paintObjects.Count == 0) return;

            var createdCount = 0;
            GameObject instance = null;
            GameObject caroObject = null;

            for (var i = 0; i < _paintObjects.Count; i++)
            {
                var paintObj = _paintObjects[i];
                var sprite = _sprites[i];

                var parent = paintObj.transform.parent;
                instance = (GameObject)PrefabUtility.InstantiatePrefab(_paintItemPrefab, parent);

                if (instance == null)
                {
                    Debug.LogError($"[GenerationLevel/Paint] Khong the instantiate prefab cho Paint Object \"{paintObj.name}\".");
                    continue;
                }

                instance.name = $"{_paintItemPrefab.name}_{createdCount.ToString()}";
                caroObject = instance.transform.Find("Caro")?.gameObject;
                if(caroObject != null)
                {
                    caroObject.name = $"Caro_{createdCount.ToString()}";
                }

                Undo.RegisterCreatedObjectUndo(instance, "Generation Level - Paint");

                Undo.RecordObject(instance.transform, "Generation Level - Paint");
                instance.transform.position = paintObj.transform.position;
                instance.transform.rotation = paintObj.transform.rotation;
                instance.transform.localScale = paintObj.transform.localScale;

                if (sprite != null)
                {
                    var allRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in allRenderers)
                    {
                        Undo.RecordObject(sr, "Generation Level - Paint");
                        sr.sprite = sprite;
                        EditorUtility.SetDirty(sr);
                    }

                    var allMasks = instance.GetComponentsInChildren<SpriteMask>(true);
                    foreach (var sm in allMasks)
                    {
                        Undo.RecordObject(sm, "Generation Level - Paint");
                        sm.sprite = sprite;
                        EditorUtility.SetDirty(sm);
                    }
                }

                var sourceColliders = paintObj.GetComponentsInChildren<PolygonCollider2D>(true);
                var destColliders = instance.GetComponentsInChildren<PolygonCollider2D>(true);

                var minCount = Mathf.Min(sourceColliders.Length, destColliders.Length);
                for (var c = 0; c < minCount; c++)
                {
                    Undo.RecordObject(destColliders[c], "Generation Level - Paint");
                    destColliders[c].pathCount = sourceColliders[c].pathCount;
                    for (var p = 0; p < sourceColliders[c].pathCount; p++)
                    {
                        destColliders[c].SetPath(p, sourceColliders[c].GetPath(p));
                    }
                    EditorUtility.SetDirty(destColliders[c]);
                }

                EditorUtility.SetDirty(instance);

                createdCount++;
            }

            Debug.Log($"[GenerationLevel/Paint] Da tao {createdCount} Paint instance.");
        }

        // ═══════════════════════════════════════════════
        //  RUN LEVELUPDATE
        // ═══════════════════════════════════════════════

        private void RunLevelUpdate()
        {
            if (_levelPrefab == null || _levelDrawItems.Count == 0 || _levelPaintItems.Count == 0) return;

            var prefabPath = AssetDatabase.GetAssetPath(_levelPrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[GenerationLevel/LevelUpdate] Khong tim thay path cua Level Prefab.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError("[GenerationLevel/LevelUpdate] Khong the load Prefab contents.");
                return;
            }

            try
            {
                var levelController = prefabRoot.GetComponent<App.LevelController>();
                if (levelController == null)
                {
                    Debug.LogError("[GenerationLevel/LevelUpdate] Khong tim thay LevelController trong Prefab da load.");
                    return;
                }

                // Build lookup by name
                var childLookup = new Dictionary<string, Transform>();
                CollectChildrenByName(prefabRoot.transform, childLookup);

                var so = new SerializedObject(levelController);

                // ── _drawSteps ──
                var drawStepsProp = so.FindProperty("_drawSteps");
                if (drawStepsProp == null)
                {
                    Debug.LogError("[GenerationLevel/LevelUpdate] Khong tim thay property _drawSteps.");
                    return;
                }
                drawStepsProp.ClearArray();

                for (var i = 0; i < _levelDrawItems.Count; i++)
                {
                    var itemRef = _levelDrawItems[i];
                    if (itemRef == null) continue;

                    // Resolve child in loaded prefab by name
                    if (!childLookup.TryGetValue(itemRef.name, out var drawTransform))
                    {
                        Debug.LogWarning($"[GenerationLevel/LevelUpdate] Khong tim thay Draw Item \"{itemRef.name}\" trong Prefab.");
                        continue;
                    }
                    var drawItem = drawTransform.gameObject;

                    drawStepsProp.InsertArrayElementAtIndex(i);
                    var element = drawStepsProp.GetArrayElementAtIndex(i);

                    element.FindPropertyRelative("spline").objectReferenceValue
                        = drawItem.GetComponentInChildren<SplineComputer>();
                    element.FindPropertyRelative("penTrailDrawer").objectReferenceValue
                        = drawItem.GetComponentInChildren<App.PenTrailDrawer>();
                    element.FindPropertyRelative("pointSpawner").objectReferenceValue
                        = drawItem.GetComponentInChildren<App.SplinePrefabSpawner>();

                    var showProp = element.FindPropertyRelative("showObjects");
                    showProp.ClearArray();

                    var hideProp = element.FindPropertyRelative("hideObjects");
                    hideProp.ClearArray();
                    hideProp.InsertArrayElementAtIndex(0);
                    hideProp.GetArrayElementAtIndex(0).objectReferenceValue = drawItem;
                }

                // ── _paintSteps ──
                var paintStepsProp = so.FindProperty("_paintSteps");
                if (paintStepsProp == null)
                {
                    Debug.LogError("[GenerationLevel/LevelUpdate] Khong tim thay property _paintSteps.");
                    return;
                }
                paintStepsProp.ClearArray();

                for (var i = 0; i < _levelPaintItems.Count; i++)
                {
                    var itemRef = _levelPaintItems[i];
                    if (itemRef == null) continue;

                    if (!childLookup.TryGetValue(itemRef.name, out var paintTransform))
                    {
                        Debug.LogWarning($"[GenerationLevel/LevelUpdate] Khong tim thay Paint Item \"{itemRef.name}\" trong Prefab.");
                        continue;
                    }
                    var paintItem = paintTransform.gameObject;

                    paintStepsProp.InsertArrayElementAtIndex(i);
                    var element = paintStepsProp.GetArrayElementAtIndex(i);

                    element.FindPropertyRelative("spritePainter").objectReferenceValue
                        = paintItem.GetComponentInChildren<App.SpritePainter>();

                    var showProp = element.FindPropertyRelative("showObjects");
                    showProp.ClearArray();

                    var hideProp = element.FindPropertyRelative("hideObjects");
                    hideProp.ClearArray();

                    var caroTransform = FindChildRecursive(paintItem.transform, "Caro");
                    if (caroTransform != null)
                    {
                        hideProp.InsertArrayElementAtIndex(0);
                        hideProp.GetArrayElementAtIndex(0).objectReferenceValue = caroTransform.gameObject;
                    }
                }

                so.ApplyModifiedProperties();
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GenerationLevel/LevelUpdate] Da update LevelController tren \"{_levelPrefab.name}\": "
                + $"{_levelDrawItems.Count} draw steps, {_levelPaintItems.Count} paint steps.");
        }

        private static void CollectChildrenByName(Transform root, Dictionary<string, Transform> lookup)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!lookup.ContainsKey(child.name))
                {
                    lookup[child.name] = child;
                }
                CollectChildrenByName(child, lookup);
            }
        }

        // ═══════════════════════════════════════════════
        //  COPY BRUSH COLOR FROM PAINTCONTROLLER
        // ═══════════════════════════════════════════════

        // private void CopyBrushColorFromPaintController()
        // {
        //     if (_levelPrefab == null) return;

        //     var prefabPath = AssetDatabase.GetAssetPath(_levelPrefab);
        //     var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        //     if (prefabRoot == null) return;

        //     try
        //     {
        //         var paintController = prefabRoot.GetComponentInChildren<global::PaintController>();
        //         if (paintController == null || paintController.levelColor == null || paintController.levelColor.Count == 0)
        //         {
        //             Debug.LogWarning("[GenerationLevel/CopyBrushColor] Khong tim thay PaintController hoac levelColor trong.");
        //             return;
        //         }

        //         var levelController = prefabRoot.GetComponent<App.LevelController>();
        //         if (levelController == null)
        //         {
        //             Debug.LogWarning("[GenerationLevel/CopyBrushColor] Khong tim thay LevelController.");
        //             return;
        //         }

        //         var so = new SerializedObject(levelController);
        //         var paintStepsProp = so.FindProperty("_paintSteps");
        //         if (paintStepsProp == null) return;

        //         var count = Mathf.Min(paintController.levelColor.Count, paintStepsProp.arraySize);
        //         for (var i = 0; i < count; i++)
        //         {
        //             var paintStepElement = paintStepsProp.GetArrayElementAtIndex(i);
        //             var brushColorContentProp = paintStepElement.FindPropertyRelative("_brushColorContent");
        //             var source = paintController.levelColor[i];

        //             brushColorContentProp.FindPropertyRelative("indexCorrect").intValue = source.indexCorrect;

        //             var colorsProp = brushColorContentProp.FindPropertyRelative("colors");
        //             colorsProp.ClearArray();
        //             for (var c = 0; c < source.colors.Count; c++)
        //             {
        //                 colorsProp.InsertArrayElementAtIndex(c);
        //                 colorsProp.GetArrayElementAtIndex(c).colorValue = source.colors[c];
        //             }
        //         }

        //         so.ApplyModifiedProperties();
        //     }
        //     finally
        //     {
        //         PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        //         PrefabUtility.UnloadPrefabContents(prefabRoot);
        //     }

        //     AssetDatabase.SaveAssets();
        //     Debug.Log($"[GenerationLevel/CopyBrushColor] Da copy ColorContent vao \"{_levelPrefab.name}\".");
        // }

        // ═══════════════════════════════════════════════
        //  RUN BLUEPRINT PATH
        // ═══════════════════════════════════════════════

        private void RunBluePrintPath()
        {
            var updatedCount = 0;

            for (var i = 0; i < _bpPathObjects.Count; i++)
            {
                var obj = _bpPathObjects[i];
                var sprite = _bpPathSprites[i];

                var renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in renderers)
                {
                    Undo.RecordObject(sr, "Generation Level - BluePrint Path");

                    if (sprite != null)
                        sr.sprite = sprite;

                    if (_bpPathMaterial != null)
                        sr.material = _bpPathMaterial;

                    EditorUtility.SetDirty(sr);
                }

                updatedCount++;
            }

            Debug.Log($"[GenerationLevel/BluePrint Path] Da update {updatedCount} Object Path.");
        }

        // ═══════════════════════════════════════════════
        //  RUN BLUEPRINT TEMPLATE
        // ═══════════════════════════════════════════════

        private void RunBluePrintTemplate()
        {
            var updatedCount = 0;

            for (var i = 0; i < _bpTemplateObjects.Count; i++)
            {
                var obj = _bpTemplateObjects[i];
                var sprite = _bpTemplateSprites[i];

                var renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in renderers)
                {
                    Undo.RecordObject(sr, "Generation Level - BluePrint Template");

                    if (sprite != null)
                        sr.sprite = sprite;

                    if (_bpTemplateMaterial != null)
                        sr.material = _bpTemplateMaterial;

                    EditorUtility.SetDirty(sr);
                }

                updatedCount++;
            }

            Debug.Log($"[GenerationLevel/BluePrint Template] Da update {updatedCount} Object Template.");
        }
    }
}
