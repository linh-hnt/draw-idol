using System;
using System.Collections.Generic;
using UnityEngine;
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using System.Reflection;
//#endif

namespace AFramework
{
    public class GSheetScriptableObject<TRecord> : ScriptableObject where TRecord : class, new()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const string MyString = "om9ZLpyrFwlMe03MPu6W7mnLRbvgswU4JCWH3MPwuGsei1BSQiKLyV1vaAwsb8dv";
#endif

        [SerializeField, HideInInspector] protected string _RandomText;
        [SerializeField, HideInInspector] protected string[] _IgnoreFile;

        public static List<T> ParseCSV<T>(string rawData, int nameIndex = 0, int dataIndex = 1, string[] ignoreFiles = null) where T : class, new()
        {
            var data = SplitCsvGrid(rawData);
            int numRow = data.GetLength(0);
            int numCol = data.GetLength(1);

            List<List<string>> listStr = new List<List<string>>();
            for (int i = 0; i < numCol - 1; i++)
            {
                var line = new List<string>();
                for (int j = 0; j < numRow - 1; j++)
                    line.Add(data[j, i]);
                listStr.Add(line);
            }

            return ParseCSV<T>(listStr, nameIndex, dataIndex, ignoreFiles);
        }

        public static List<T> ParseCSV<T>(List<List<string>> arr, int nameIndex = 0, int dataIndex = 1, string[] ignoreFiles = null) where T : class, new()
        {
            List<Sprite> listSprites = null;
            List<Texture> listTextures = null;
            List<Material> listMaterials = null;
            List<Mesh> listMeshs = null;
#if UNITY_EDITOR && AF_ADDRESSABLES_UI
            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> listAddressableEntries = null;
#endif

            var type = typeof(T);
            List<T> lst = new List<T>();
            for (int i = dataIndex; i < arr.Count; i++)
            {
                if (string.IsNullOrEmpty(arr[i][0]))
                {
                    Debug.LogWarning("Line index " + i + " is empty: " + arr[i]);
                    break;
                }
                var t = new T();
                lst.Add(t);
            }

            var header = arr[nameIndex];
            for (int i = 0; i < header.Count; i++)
            {
                if (!string.IsNullOrEmpty(header[i]))
                {
                    var property = type.GetField(header[i].Replace("\r", string.Empty),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null)
                    {
                        for (int j = 0; j < lst.Count; j++)
                        {
                            if (lst[j] == null) continue;
                            var value = arr[dataIndex + j][i];
                            if (!string.IsNullOrEmpty(value))
                            {
                                value = arr[dataIndex + j][i].Replace("\r", string.Empty).Trim();
                                var value_lowercase = value.ToLowerInvariant();
                                var x = property.FieldType;

                                if (x.IsEnum)
                                {
                                    object objValue = Enum.Parse(property.FieldType, value);
                                    property.SetValue(lst[j], objValue);
                                }
                                else if (x.Name == "Vector3")
                                {
                                    var v3Arr = value.Replace("(", string.Empty).Replace(")", string.Empty).Split(',');
                                    Vector3 v = new Vector3(float.Parse(v3Arr[0]), float.Parse(v3Arr[1]),
                                        float.Parse(v3Arr[2]));
                                    property.SetValue(lst[j], v);
                                }
                                else if (x.Name == "Int32[]")
                                {
                                    var listValue = value.Split(';');
                                    var array = listValue.Select(s => int.Parse(s));
                                    property.SetValue(lst[j], array.ToArray());
                                }
                                else if (x.Name == "String[]")
                                {
                                    var listValue = value.Split(';');
                                    var array = new List<string>();
                                    foreach (var strValue in listValue)
                                    {
                                        if (!string.IsNullOrEmpty(strValue))
                                        {
                                            array.Add(strValue.Replace(" ", String.Empty));
                                        }
                                    }

                                    property.SetValue(lst[j], array.ToArray());
                                }
                                else if (x.Name == "Color")
                                {
                                    ColorUtility.TryParseHtmlString(value, out var color);
                                    property.SetValue(lst[j], color);
                                }
#if UNITY_EDITOR
                                else if (x.Name == "Sprite")
                                {
                                    if (listSprites == null)
                                        listSprites = AFramework.Utility.GetAllSpriteAssetsAtPath("Assets", ignoreFiles);
                                    var obj = listSprites.Find(s => s.name.ToLowerInvariant() == value_lowercase);
                                    property.SetValue(lst[j], obj);
                                    if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                }
                                else if (x.Name == "Sprite[]")
                                {
                                    if (listSprites == null)
                                        listSprites = AFramework.Utility.GetAllSpriteAssetsAtPath("Assets", ignoreFiles);
                                    var listValue = value_lowercase.Split(';');
                                    var result = new List<Sprite>();
                                    foreach (var spriteName in listValue)
                                    {
                                        if (!string.IsNullOrEmpty(spriteName))
                                        {
                                            var obj = listSprites.Find(s => s.name.ToLowerInvariant() == spriteName);
                                            result.Add(obj);
                                            if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                        }
                                    }

                                    property.SetValue(lst[j], result.ToArray());
                                }
                                else if (x.Name == "Texture")
                                {
                                    if (listTextures == null)
                                        listTextures = AFramework.Utility.GetAllTexturesAssetsAtPath("Assets", ignoreFiles);
                                    var obj = listTextures.Find(s => s.name.ToLowerInvariant() == value_lowercase);
                                    property.SetValue(lst[j], obj);
                                    if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                }
                                else if (x.Name == "Texture[]")
                                {
                                    if (listTextures == null)
                                        listTextures = AFramework.Utility.GetAllTexturesAssetsAtPath("Assets", ignoreFiles);
                                    var listValue = value_lowercase.Split(';');
                                    var result = new List<Texture>();
                                    foreach (var spriteName in listValue)
                                    {
                                        if (!string.IsNullOrEmpty(spriteName))
                                        {
                                            var obj = listTextures.Find(s => s.name.ToLowerInvariant() == spriteName);
                                            result.Add(obj);
                                            if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                        }
                                    }

                                    property.SetValue(lst[j], result.ToArray());
                                }
                                else if (x.Name == "Material")
                                {
                                    if (listMaterials == null)
                                        listMaterials = AFramework.Utility.GetAllMaterialAssetsAtPath("Assets", ignoreFiles);
                                    var obj = listMaterials.Find(s => s.name.ToLowerInvariant() == value_lowercase);
                                    property.SetValue(lst[j], obj);
                                    if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                }
                                else if (x.Name == "Material[]")
                                {
                                    if (listMaterials == null)
                                        listMaterials = AFramework.Utility.GetAllMaterialAssetsAtPath("Assets", ignoreFiles);
                                    var listValue = value_lowercase.Split(';');
                                    var result = new List<Material>();
                                    foreach (var valueName in listValue)
                                    {
                                        if (!string.IsNullOrEmpty(valueName))
                                        {
                                            var obj = listMaterials.Find(s => s.name.ToLowerInvariant() == valueName);
                                            result.Add(obj);
                                            if (obj == null) Debug.LogWarning(x.Name + " " + valueName + " is null");
                                        }
                                    }

                                    property.SetValue(lst[j], result.ToArray());
                                }
                                else if (x.Name == "Mesh")
                                {
                                    if (listMeshs == null)
                                        listMeshs = AFramework.Utility.GetAllMeshAssetsAtPath("Assets", ignoreFiles);
                                    var obj = listMeshs.Find(s => s.name.ToLowerInvariant() == value_lowercase);
                                    property.SetValue(lst[j], obj);
                                    if (obj == null) Debug.LogWarning(x.Name + " " + value + " is null");
                                }
                                else if (x.Name == "Mesh[]")
                                {
                                    if (listMeshs == null)
                                        listMeshs = AFramework.Utility.GetAllMeshAssetsAtPath("Assets", ignoreFiles);
                                    var listValue = value_lowercase.Split(';');
                                    var result = new List<Mesh>();
                                    foreach (var valueName in listValue)
                                    {
                                        if (!string.IsNullOrEmpty(valueName))
                                        {
                                            var obj = listMeshs.Find(s => s.name.ToLowerInvariant() == valueName);
                                            result.Add(obj);
                                            if (obj == null) Debug.LogWarning(x.Name + " " + valueName + " is null");
                                        }
                                    }

                                    property.SetValue(lst[j], result.ToArray());
                                }
#if AF_ADDRESSABLES_UI
                                    else if (x.Name == "AssetReference")
                                    {
                                        if (listAddressableEntries == null)
                                            listAddressableEntries = GetAllAddressableEntries();
                                        var obj = listAddressableEntries.Find(s => s.MainAsset.name.ToLowerInvariant() == value_lowercase);
                                        if (obj == null)
                                        {
                                            Debug.LogWarning(x.Name + " " + value + " is null");
                                            property.SetValue(lst[j], null);
                                        }
                                        else
                                        {
                                            var reference = new UnityEngine.AddressableAssets.AssetReference(obj.guid);
                                            property.SetValue(lst[j], reference);
                                        }
                                    }
                                    else if (x.Name == "AssetReference[]")
                                    {
                                        if (listAddressableEntries == null)
                                            listAddressableEntries = GetAllAddressableEntries();
                                        var result = new List<UnityEngine.AddressableAssets.AssetReference>();
                                        var listValue = value.Split(';');
                                        foreach (var valueName in listValue)
                                        {
                                            if (!string.IsNullOrEmpty(valueName))
                                            {
                                                var obj = listAddressableEntries.Find(s => s.MainAsset.name.ToLowerInvariant() == valueName.ToLowerInvariant());
                                                if (obj == null)
                                                {
                                                    Debug.LogWarning(x.Name + " " + valueName + " is null");
                                                    result.Add(null);
                                                }
                                                else
                                                {
                                                    var reference = new UnityEngine.AddressableAssets.AssetReference(obj.guid);
                                                    result.Add(reference);
                                                }
                                            }
                                        }
                                        property.SetValue(lst[j], result.ToArray());
                                    }
#endif
#endif
                                else
                                {
                                    //Debug.Log("value:" + value + " - " +property.Name);
                                    object objValue = Convert.ChangeType(value, property.FieldType);
                                    property.SetValue(lst[j], objValue);
                                }
                            }
                            else
                            {
                                //lst[j] = null;
                                //Debug.LogWarning("Data has Empty cell: " + arr[ggDataIndex + j]);
                            }
                        }
                    }
                }
            }
            lst.RemoveAll(item => item == null);
            return lst;
        }

        // splits a CSV file into a 2D string array
        private static string[,] SplitCsvGrid(string csvText)
        {
            string[] lines = csvText.Split("\n"[0]);

            // finds the max width of row
            int width = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string[] row = SplitCsvLine(lines[i]);
                width = Mathf.Max(width, row.Length);
            }

            // creates new 2D string grid to output to
            string[,] outputGrid = new string[width + 1, lines.Length + 1];
            for (int y = 0; y < lines.Length; y++)
            {
                string[] row = SplitCsvLine(lines[y]);
                for (int x = 0; x < row.Length; x++)
                {
                    outputGrid[x, y] = row[x];
                    outputGrid[x, y] = outputGrid[x, y].Replace("\"\"", "\"");
                }
            }

            return outputGrid;
        }

        // splits a CSV row 
        public static string[] SplitCsvLine(string line)
        {
            return (from System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(line,
                    @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)",
                    System.Text.RegularExpressions.RegexOptions.ExplicitCapture)
                    select m.Groups[1].Value).ToArray();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        protected string ggSheetId;
        protected int ggGridId = 0;
        protected int ggNameIndex = 0;
        protected int ggDataIndex = 1;

        void FillData2(string id, string gridId)
        {
            GetTable(id, gridId);
        }

        void GetTable(string id, string gridId)
        {
            LoadWebClient(id, gridId, s =>
            {
                records = ParseCSV<TRecord>(s, ggNameIndex, ggDataIndex, _IgnoreFile);
                OnParseFinished();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            });
        }

        static void LoadWebClient(string id, string gridId, Action<string> callBack)
        {
            string url = $@"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gridId}";
            LoadWebClient3(url, callBack);
        }

        static void LoadWebClient3(string id, Action<string> callBack)
        {
            WWW w = new WWW(id);
            while (!w.isDone)
                w.MoveNext();
            callBack(w.text);
        }

        [ContextMenu("Sync Data")]
        public virtual void Sync()
        {
            GetDecrypt(_RandomText, ref ggSheetId, ref ggGridId, ref ggNameIndex, ref ggDataIndex);
            FillData2(ggSheetId, ggGridId.ToString());
        }

        [ContextMenu("OpenSheet Data")]
        protected void OpenSheet()
        {
            GetDecrypt(_RandomText, ref ggSheetId, ref ggGridId, ref ggNameIndex, ref ggDataIndex);
            Application.OpenURL(string.Format("https://docs.google.com/spreadsheets/d/{0}/edit#gid={1}", ggSheetId, ggGridId));
        }

        protected virtual void OnParseFinished()
        {

        }

#if UNITY_EDITOR && AF_ADDRESSABLES_UI
    List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> GetAllAddressableEntries(string[] ignoreFiles = null)
    {
        List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> all_entries = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.GetAllAssets(all_entries, true);
        if (ignoreFiles == null || ignoreFiles.Length == 0) return all_entries;
        List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> result = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
        for (int i = 0; i < all_entries.Count; ++i)
        {
            var entry = all_entries[i];
            bool ignore = false;
            for (int k = 0; k < ignoreFiles.Length; ++k)
            {
                if (entry.AssetPath.EndsWith(ignoreFiles[k]))
                {
                    ignore = true;
                    break;
                }
            }
            if (!ignore)
            {
                result.Add(entry);
            }
        }
        return result;
    }
#endif
#endif

        [Header("Config")]
        [SerializeField]
        protected List<TRecord> records = new List<TRecord>();
        public List<TRecord> GetRecords() { return records; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static string GetEncrypt(string sheet_id, int grid_id, int name_index, int data_index)
        {
            string temp = string.Format("{0};{1};{2};{3}", sheet_id, grid_id, name_index, data_index);
            byte[] encryptData = System.Text.Encoding.UTF8.GetBytes(temp);
            SimpleEncrypt(ref encryptData);
            return System.Convert.ToBase64String(encryptData);
        }

        public static void GetDecrypt(string input, ref string sheet_id, ref int grid_id, ref int name_index, ref int data_index)
        {
            if (string.IsNullOrEmpty(input)) return;
            var encryptData = System.Convert.FromBase64String(input);
            SimpleEncrypt(ref encryptData);
            var data = System.Text.Encoding.UTF8.GetString(encryptData);
            var split_data = data.Split(';');
            if (split_data.Length != 4) return;
            sheet_id = split_data[0];
            grid_id = int.Parse(split_data[1]);
            name_index = int.Parse(split_data[2]);
            data_index = int.Parse(split_data[3]);
        }

        static void SimpleEncrypt(ref byte[] data)
        {
            byte[] key = System.Text.Encoding.UTF8.GetBytes(MyString);
            int dLength = data.Length;
            int kLength = key.Length;
            for (int i = 0, length = dLength; i < length; ++i)
                data[i] ^= key[i % kLength];
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(GSheetScriptableObject<>), true)]
    public class BaseUICompEditor :
#if ODIN_INSPECTOR
        Sirenix.OdinInspector.Editor.OdinEditor
#else
        UnityEditor.Editor
#endif
    {
        string tempEncryptString;
        string ggSheetId = "";
        int ggGridId = 0;
        int ggNameIndex = 0;
        int ggDataIndex = 1;

        public override void OnInspectorGUI()
        {
            var encryptString = serializedObject.FindProperty("_RandomText");
            if (encryptString.stringValue != tempEncryptString)
            {
                tempEncryptString = encryptString.stringValue;
                GSheetScriptableObject<MonoBehaviour>.GetDecrypt(tempEncryptString, ref ggSheetId, ref ggGridId, ref ggNameIndex, ref ggDataIndex);
            }

            var headerLabel = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            GUILayout.Label("Google Sheet Config", headerLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sheet Id", GUILayout.Width(125));
            var temp_ggSheetId = GUILayout.TextField(ggSheetId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Id", GUILayout.Width(125));
            var temp_ggGridId = UnityEditor.EditorGUILayout.IntField("", ggGridId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name Index", GUILayout.Width(125));
            var temp_ggNameIndex = UnityEditor.EditorGUILayout.IntField("", ggNameIndex);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Data Index", GUILayout.Width(125));
            var temp_ggDataIndex = UnityEditor.EditorGUILayout.IntField("", ggDataIndex);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            UnityEditor.EditorGUILayout.PropertyField
              (
                 serializedObject.FindProperty("_IgnoreFile"),
                 new GUIContent("Ignore File")
              );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool status = GUILayout.Button("Sync Data");
            if (status)
            {
                System.Type ourType = serializedObject.targetObject.GetType();
                MethodInfo mi = ourType.GetMethod("Sync", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                mi.Invoke(serializedObject.targetObject, null);
            }

            status = GUILayout.Button("Open Sheet");
            if (status)
            {
                System.Type ourType = serializedObject.targetObject.GetType();
                MethodInfo mi = ourType.GetMethod("OpenSheet", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                mi.Invoke(serializedObject.targetObject, null);
            }
            GUILayout.EndHorizontal();

            DrawUILine(Color.gray);

            base.DrawDefaultInspector();

            if (temp_ggSheetId != ggSheetId || temp_ggGridId != ggGridId || temp_ggNameIndex != ggNameIndex || temp_ggDataIndex != ggDataIndex)
            {
                ggSheetId = temp_ggSheetId;
                ggGridId = temp_ggGridId;
                ggNameIndex = temp_ggNameIndex;
                ggDataIndex = temp_ggDataIndex;
                tempEncryptString = GSheetScriptableObject<MonoBehaviour>.GetEncrypt(ggSheetId, ggGridId, ggNameIndex, ggDataIndex);
                encryptString.stringValue = tempEncryptString;
                serializedObject.ApplyModifiedProperties();
            }
            serializedObject.Update();
        }

        protected virtual void OnCustomInspectorGUI()
        {
            serializedObject.Update();
        }

        public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = UnityEditor.EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            UnityEditor.EditorGUI.DrawRect(r, color);
        }
    }
#endif
}