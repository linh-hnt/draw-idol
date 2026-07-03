using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if (UNITY_WINRT && !UNITY_EDITOR)
using Reflection = MarkerMetro.Unity.WinLegacy.Reflection.ReflectionExtensions;
#endif
namespace AFramework
{
    public class HardwareInfo
    {
        public string soc;
        public string gpu;
        public int ram;
    }

    

    public static class Utility {
        public static string TimeFormat(float time)
        {
            int timeInt = Mathf.CeilToInt(time);
            int hour = timeInt / 3600;
            int minute = (timeInt % 3600) / 60;
            int second = timeInt - hour * 3600 - minute * 60;

            return string.Format("{0:00}:{1:00}:{2:00}", hour, minute, second);
        }

        public static System.DateTime UnixTimestampToDateTime(long milisecond)
        {
            System.DateTime unixStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return unixStart.AddMilliseconds(milisecond);
        }

        public static string SecondsToTimeAgo(double seconds, bool isShort = true)
        {
            int secondsInt = Mathf.CeilToInt((float)seconds);

            int minutes = secondsInt / 60;
            int hours = secondsInt / 3600;
            int day = secondsInt / (3600 * 24);
            int month = day / 30;
            int year = day / 365;

            string ret = string.Empty;

            if (year > 0) {
                month = month % 12;
                ret = year + "y " + month + "m";
                if(!isShort)
                {
                    day = day % 30;
                    ret += " " + day + "d";
                }    
            }
            else if(month > 0)
            {
                day = day % 30;
                ret = month + "m " + day + "d";

                if (!isShort)
                {
                    hours = hours % 24;
                    ret += " " + hours + "h";
                }
            }
            else if(day > 0)
            {
                hours = hours % 24;
                ret = day + "d " + hours + "h";

                if (!isShort)
                {
                    minutes = minutes % 60;
                    ret += " " + minutes + "m";
                }
            }
            else if(hours > 0)
            {
                minutes = minutes % 60;
                ret = hours + "h " + minutes + "m";

                if (!isShort)
                {
                    secondsInt = secondsInt % 60;
                    ret += " " + secondsInt + "s";
                }
            } 
            else if(minutes > 0)
            {
                secondsInt = secondsInt % 60;
                ret = minutes + "m " + (secondsInt == 0 ? "" : secondsInt + "s");
            }   
            else
            {
                ret = string.Format("{0}s", secondsInt);
            }    

            return ret;
        }

        public static System.DateTime GetCurrentTime()
        {
            return System.DateTime.UtcNow;
        }

        public static System.DateTime GetCurrentDate()
        {
            return GetCurrentTime().Date;
        }

        public static double GetCurrentTimeMillisecond()
        {
            return System.DateTime.UtcNow.Subtract(System.DateTime.MinValue).TotalMilliseconds;
        }

        public static double GetCurrentTimeSecond()
        {
            return System.DateTime.UtcNow.Subtract(System.DateTime.MinValue).TotalSeconds;
        }

        public static System.DateTime FromEpochTime(long sec)
        {
            return new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(sec);
        }

        public static long ToEpochTime(System.DateTime time)
        {
            return (long)(time - new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

        public static float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}

        private static int sVersionNumber = -1;
        public static int GetVersionNumber()
        {
            if (sVersionNumber < 0)
            {
                var versionArray = Application.version.Split('.');
                sVersionNumber = 0;
                int multiplier = 1;
                for (int i = versionArray.Length - 1; i >= 0; --i)
                {
                    sVersionNumber += int.Parse(versionArray[i]) * multiplier;
                    multiplier *= 10;
                }
            }
            return sVersionNumber;
        }

        private static string sDeviceUDID = "";
        public static string GetUDID()
        {
            if (sDeviceUDID.Length <= 0)
            {
                sDeviceUDID = PlayerPrefs.GetString("didu", string.Empty);
                if (string.IsNullOrEmpty(sDeviceUDID))
                {
#if UNITY_EDITOR
                    sDeviceUDID = SystemInfo.deviceUniqueIdentifier;
#else
    #if UNITY_ANDROID
                    sDeviceUDID = AFramework.Utility.GetAndroidAdvertiserId();
    #elif UNITY_IOS
                    sDeviceUDID = UnityEngine.iOS.Device.vendorIdentifier;
    #else
                    sDeviceUDID = SystemInfo.deviceUniqueIdentifier;
    #endif
#endif
                    if (string.IsNullOrEmpty(sDeviceUDID))
                    {
                        sDeviceUDID = SystemInfo.deviceUniqueIdentifier;
                    }
                    PlayerPrefs.SetString("didu", sDeviceUDID);
                }
            }
            return sDeviceUDID;
        }

        private static string sSavePath = "";
        public static string GetSavePath()
        {
            if (sSavePath.Length <= 0)
            {
                sSavePath = Application.persistentDataPath + '/';
            }
            return sSavePath;
        }

        public static string GetUrlFilename(string url)
        {
            if (url == null) return null;
            int questionMark = url.IndexOf('?');
            var temp = questionMark >= 0 ? url.Remove(questionMark) : url;
            var lastSlast = temp.LastIndexOf('/');
            if (lastSlast < 0) return null;
            var result = temp.Substring(lastSlast + 1);
            return result;
        }

        public static TValue GetValueOrDefault<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
             TKey key,
             TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static bool HasInternet()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;//TODO
        }

        static bool sLastInternetState = true;
        static float sLastInternetCheckTime = -999;
        public static bool DelayHasInternet()
        {
            if (Mathf.Abs(Time.unscaledTime - sLastInternetCheckTime) >= 3)
            {
                sLastInternetCheckTime = Time.unscaledTime;
                sLastInternetState = HasInternet();
            }
            return sLastInternetState;
        }

        public static List<string> GetLocalIPAddress()
        {
            List<string> localIPs = new List<string>();
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIPs.Add(ip.ToString());
                }
            }

            return localIPs;
        }

        public static float NormalizeFloat(float value, int fractNum)
        {
            return Mathf.RoundToInt(value * Mathf.Pow(10, fractNum)) / Mathf.Pow(10, fractNum);
        }

        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst as T;
        }

        public static float AngleInDeg(Vector3 vec1, Vector3 vec2)
        {
            // the vector that we want to measure an angle from
            Vector3 referenceForward = vec1;
            Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
            // the vector of interest
            Vector3 newDirection = vec2;
            float angle = Vector3.Angle(newDirection, referenceForward);
            // Determine if the degree value should be negative.  Here, a positive value
            // from the dot product means that our vector is on the right of the reference vector   
            // whereas a negative value means we're on the left.
            float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
            return sign * angle;
        }

        public static float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n)
        {
            // angle in [0,180]
            float angle = Vector3.Angle(a, b);
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

            // angle in [-179,180]
            float signed_angle = angle * sign;

            // angle in [0,360] (not used but included here for completeness)
            //float angle360 =  (signed_angle + 180) % 360;

            return signed_angle;
        }

        public static Vector2 WorldToScreenPointProjected(Camera camera, Vector3 worldPos)
        {
            Vector3 camNormal = camera.transform.forward;
            Vector3 vectorFromCam = worldPos - camera.transform.position;
            float camNormDot = Vector3.Dot(camNormal, vectorFromCam);
            if (camNormDot <= 0)
            {
                // we are behind the camera forward facing plane, project the position in front of the plane
                Vector3 proj = (camNormal * camNormDot * 1.01f);
                worldPos = camera.transform.position + (vectorFromCam - proj);
            }

            return RectTransformUtility.WorldToScreenPoint(camera, worldPos);
        }

        public static bool IsPrefab(GameObject obj)
        {
            return obj.scene.rootCount == 0;
        }

        public static T[] ResizeArray<T>(T[] input, int length, T defaultValue)
        {
            T[] output = new T[length];
            int index = 0;
            for (; index < input.Length && index < length; ++index)
            {
                output[index] = input[index];
            }
            for (; index < length; ++index)
            {
                output[index] = defaultValue;
            }
            return output;
        }

        public static bool CompareArray<T>(T[] arrayA, T[] arrayB)
        {
            if ((arrayA == null && arrayB != null) || (arrayA != null && arrayB == null)) return false;
            if (arrayA.Length != arrayB.Length) return false;

            for (int i = 0; i < arrayA.Length; i++)
            {
                if (!arrayA[i].Equals(arrayB[i])) return false;
            }

            return true;
        }

        //Usage GetParameterNameXXXX(new { variable });
        public static string GetParameterNameSlow<T>(T item) where T : class
        {
            if (item == null)
                return string.Empty;

            return item.ToString().TrimStart('{').TrimEnd('}').Split('=')[0].Trim();
        }
        public static string GetParameterNameFast<T>(T item) where T : class
        {
            if (item == null)
                return string.Empty;

            return typeof(T).GetProperties()[0].Name;
        }

        public static void CopyRectTransform(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }

        public static Vector3 TrajectoryForce(Vector3 startPoint, Vector3 endPoint, float angle, float gravityMagnitude)
        {
            Vector3 direction = endPoint - startPoint;
            float h = direction.y;
            direction.y = 0;
            float distance = direction.magnitude;
            float a = angle * Mathf.Deg2Rad;
            direction.y = distance * Mathf.Tan(a);
            distance += h / Mathf.Tan(a);

            // calculate velocity
            float velocity = Mathf.Sqrt(distance * Physics.gravity.magnitude / Mathf.Sin(2 * a));
            return direction.normalized * velocity;
        }

        public static Vector2 TrajectoryForce(Vector2 startPoint, Vector2 endPoint, float angle, float gravityMagnitude)
        {
            var direction = endPoint - startPoint;
            var h = direction.y;
            direction.y = 0;
            float distance = direction.magnitude;
            float a = angle * Mathf.Deg2Rad;
            direction.y = distance * Mathf.Tan(a);
            distance += h / Mathf.Tan(a);

            // calculate velocity
            float velocity = Mathf.Sqrt(distance * gravityMagnitude / Mathf.Sin(2 * a));
            return direction.normalized * velocity;
        }

#if UNITY_EDITOR
        public static List<T> LoadAllPrefabsOfType<T>(string path) where T : UnityEngine.Object
        {
            if (path != "")
            {
                if (path.EndsWith("/"))
                {
                    path = path.TrimEnd('/');
                }
            }

            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            System.IO.FileInfo[] fileInf = dirInfo.GetFiles("*.prefab");

            //loop through directory loading the game object and checking if it has the component you want
            List<T> prefabComponents = new List<T>();
            foreach (System.IO.FileInfo fileInfo in fileInf)
            {
                string fullPath = fileInfo.FullName.Replace(@"\", "/");
                string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
                Object data = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (data is T)
                {
                    prefabComponents.Add(data as T);
                }
            }
            return prefabComponents;
        }

        public static List<T> FindObjectsOfTypeAll<T>()
        {
            List<T> results = new List<T>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (s.isLoaded)
                {
                    var allGameObjects = s.GetRootGameObjects();
                    for (int j = 0; j < allGameObjects.Length; j++)
                    {
                        var go = allGameObjects[j];
                        results.AddRange(go.GetComponentsInChildren<T>(true));
                    }
                }
            }
            return results;
        }

        public static List<T> GetAllAssetAtPath<T>(string filter, string path, string[] ignoreFiles = null)
        {
            string[] findAssets = UnityEditor.AssetDatabase.FindAssets(filter, new[] { path });
            List<T> os = new List<T>();
            foreach (var findAsset in findAssets)
            {
                var asset_path = UnityEditor.AssetDatabase.GUIDToAssetPath(findAsset);
                if (ignoreFiles != null && ignoreFiles.Length > 0)
                {
                    for (int k = 0; k < ignoreFiles.Length; ++k)
                    {
                        if (asset_path.EndsWith(ignoreFiles[k]))
                        {
                            asset_path = null;
                            break;
                        }
                    }
                    if (asset_path == null) continue;
                }
                os.Add((T)System.Convert.ChangeType(
                    UnityEditor.AssetDatabase.LoadAssetAtPath(asset_path,
                        typeof(T)), typeof(T)));
            }

            return os;
        }

        public static List<UnityEngine.Object> GetAllAssetsAtPath(string path, string[] ignoreFiles = null)
        {
            List<Object> result = new List<Object>();
            string[] paths = { path };
            var assets = UnityEditor.AssetDatabase.FindAssets(null, paths);
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset_path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[i]);
                if (ignoreFiles != null && ignoreFiles.Length > 0)
                {
                    for (int k = 0; k < ignoreFiles.Length; ++k)
                    {
                        if (asset_path.EndsWith(ignoreFiles[k]))
                        {
                            asset_path = null;
                            break;
                        }
                    }
                    if (asset_path == null) continue;
                }
                result.Add(UnityEditor.AssetDatabase.LoadMainAssetAtPath(asset_path));
            }
            return result;
        }

        public static List<Sprite> GetAllSpriteAssetsAtPath(string path, string[] ignoreFiles = null)
        {
            return GetAllAssetsAtPath<Sprite>(path, "t:sprite", ignoreFiles);
        }

        public static List<Texture> GetAllTexturesAssetsAtPath(string path, string[] ignoreFiles = null)
        {
            return GetAllAssetsAtPath<Texture>(path, "t:texture", ignoreFiles);
        }

        public static List<Material> GetAllMaterialAssetsAtPath(string path, string[] ignoreFiles = null)
        {
            return GetAllAssetsAtPath<Material>(path, "t:material", ignoreFiles);
        }

        public static List<Mesh> GetAllMeshAssetsAtPath(string path, string[] ignoreFiles = null)
        {
            return GetAllAssetsAtPath<Mesh>(path, "t:mesh", ignoreFiles);
        }

        public static List<T> GetAllAssetsAtPath<T>(string path, string filter, string[] ignoreFiles = null) where T: Object
        {
            List<T> result = new List<T>();
            string[] paths = { path };
            var assets = UnityEditor.AssetDatabase.FindAssets(filter, paths);
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset_path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[i]);
                if (ignoreFiles != null && ignoreFiles.Length > 0)
                {
                    for (int k = 0; k < ignoreFiles.Length; ++k)
                    {
                        if (asset_path.EndsWith(ignoreFiles[k]))
                        {
                            asset_path = null;
                            break;
                        }
                    }
                    if (asset_path == null) continue;
                }
                var data = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(asset_path);
                for (int j = 0; j < data.Length; ++j)
                {
                    if (data[j] is T)
                    {
                        result.Add(data[j] as T);
                    }
                }
            }
            return result;
        }
#endif

        public static IEnumerator CRDelayFunction(float time, System.Action callback)
        {
            yield return new WaitForSeconds(time);
            if (callback != null) callback();
        }

        public static Mesh CreateCube(Vector3 size, bool save = false)
        {
            float length = size.x;
            float width = size.y;
            float height = size.z;
            Vector3[] c = new Vector3[8];
            c[0] = new Vector3(-length * .5f, -width * .5f, height * .5f);
            c[1] = new Vector3(length * .5f, -width * .5f, height * .5f);
            c[2] = new Vector3(length * .5f, -width * .5f, -height * .5f);
            c[3] = new Vector3(-length * .5f, -width * .5f, -height * .5f);

            c[4] = new Vector3(-length * .5f, width * .5f, height * .5f);
            c[5] = new Vector3(length * .5f, width * .5f, height * .5f);
            c[6] = new Vector3(length * .5f, width * .5f, -height * .5f);
            c[7] = new Vector3(-length * .5f, width * .5f, -height * .5f);

            Vector3[] vertices = {
                c[0], c[1], c[2], c[3], // Bottom
	            c[7], c[4], c[0], c[3], // Left
	            c[4], c[5], c[1], c[0], // Front
	            c[6], c[7], c[3], c[2], // Back
	            c[5], c[6], c[2], c[1], // Right
                c[7], c[6], c[5], c[4] // Top
            };

            int[] triangles = {
                3, 1, 0,        3, 2, 1,        // Bottom	
	            7, 5, 4,        7, 6, 5,        // Left
	            11, 9, 8,       11, 10, 9,      // Front
	            15, 13, 12,     15, 14, 13,     // Back
	            19, 17, 16,     19, 18, 17,	    // Right
                23, 21, 20,     23, 22, 21, // Top
            };

            //5) Define each vertex's Normal
            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;
            Vector3 forward = Vector3.forward;
            Vector3 back = Vector3.back;
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;


            Vector3[] normals = new Vector3[]
            {
                down, down, down, down,             // Bottom
	            left, left, left, left,             // Left
	            forward, forward, forward, forward,	// Front
	            back, back, back, back,             // Back
	            right, right, right, right,         // Right
	            up, up, up, up                      // Top
            };

            //6) Define each vertex's UV co-ordinates
            Vector2 uv00 = new Vector2(0f, 0f);
            Vector2 uv10 = new Vector2(1f, 0f);
            Vector2 uv01 = new Vector2(0f, 1f);
            Vector2 uv11 = new Vector2(1f, 1f);

            Vector2[] uvs = new Vector2[]
            {
                uv11, uv01, uv00, uv10, // Bottom
	            uv11, uv01, uv00, uv10, // Left
	            uv11, uv01, uv00, uv10, // Front
	            uv11, uv01, uv00, uv10, // Back	        
	            uv11, uv01, uv00, uv10, // Right 
	            uv11, uv01, uv00, uv10  // Top
            };

            Mesh newMesh = new Mesh();

            newMesh.Clear();

            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            newMesh.normals = normals;
            newMesh.uv = uvs;

#if UNITY_EDITOR
            UnityEditor.MeshUtility.Optimize(newMesh);
            if (save)
            {
                save = false;

                var savePath = "Assets/" + string.Format("Cube_{0}", System.DateTime.UtcNow.Ticks) + ".asset";
                Debug.Log("Saved Mesh to:" + savePath);
                UnityEditor.AssetDatabase.CreateAsset(newMesh, savePath);
            }
#endif
            return newMesh;
        }

        public static string GetGoogleSheetCSVData(string fileId, string sheetId)
        {
            try
            {
                var httpClient = new System.Net.Http.HttpClient();
                httpClient.BaseAddress = new System.Uri(string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}", fileId, sheetId));
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
                var response = httpClient.GetAsync("").Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (System.Exception e) { return ""; }
            return "";
        }

        public static void OpenOSSettings()
        {
#if UNITY_IPHONE
            string url = GetSettingsURL();
            Application.OpenURL(url);
#endif
        }

        public static string GetAndroidAdvertiserId()
        {
            string advertisingID = "";
#if UNITY_ANDROID
            try
            {
                AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

                advertisingID = adInfo.Call<string>("getId").ToString();
            }
            catch (System.Exception)
            {
            }
#endif
            return advertisingID;
        }

        public static string GetCountryCode()
        {
#if !UNITY_EDITOR
#if UNITY_IOS
            return IOSgetPhoneCountryCode();
#elif UNITY_ANDROID
            using (AndroidJavaClass cls = new AndroidJavaClass("java.util.Locale"))
            {
                using (AndroidJavaObject locale = cls.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    //string android_idioma1 = locale.Call<string>("getLanguage");        //El resultado: es
                    //string android_idioma2 = locale.Call<string>("getDisplayLanguage"); //El resultado: español
                    //string android_idioma3 = locale.Call<string>("getISO3Language");    //El resultado: spa

                    //string android_pais1 = locale.Call<string>("getCountry");           //El resultado:ES
                    //string android_pais2 = locale.Call<string>("getDisplayCountry");    //El resultado:España
                    //string android_pais3 = locale.Call<string>("getISO3Country");       //El resultado:ESP
                    return locale.Call<string>("getCountry");
                }
            }
#endif
#endif
            return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }

        public static HardwareInfo GetHardwareInfo()
        {
            HardwareInfo info = new HardwareInfo();
            info.soc = GetSoCInfo();
            info.gpu = SystemInfo.graphicsDeviceName;
            info.ram = SystemInfo.systemMemorySize;
            return info;
        }

        public static string GetSoCInfo()
        {
            string Soc_Info = "Unknown";
#if UNITY_EDITOR || UNITY_STANDALONE
            Soc_Info = UnityEngine.SystemInfo.processorType;
#elif UNITY_ANDROID
            UnityEngine.AndroidJavaObject fileReader = null;
            UnityEngine.AndroidJavaObject bufferedReader = null;
            try
            {
                fileReader = new UnityEngine.AndroidJavaObject("java.io.FileReader", "/proc/cpuinfo");
                bufferedReader = new UnityEngine.AndroidJavaObject("java.io.BufferedReader", fileReader, 8192);
                if (bufferedReader != null)
                {
                    string info = null;
                    while ((info = bufferedReader.Call<string>("readLine")) != null)
                    {
                        if (info.Contains("Hardware"))
                        {
                            Soc_Info = info.Replace(" ", "").Replace("Hardware", "").Replace(":", "").Trim();
                            break;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
            finally
            {
                if (bufferedReader != null)
                {
                    bufferedReader.Call("close");
                    bufferedReader.Dispose();
                }
                if (fileReader != null)
                {
                    fileReader.Dispose();
                }
            }
#elif UNITY_IOS
            Soc_Info = UnityEngine.SystemInfo.graphicsDeviceName.Replace("GPU", "");
#endif
            if (Soc_Info == "Unknown")
            {
                Soc_Info = UnityEngine.SystemInfo.deviceName;
            }
            return Soc_Info;
        }

#if UNITY_IPHONE
        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern string GetSettingsURL();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern void OpenSettings();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        public static extern string IOSgetPhoneCountryCode();
#endif
    }

    public static class Encoding
    {
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }

    public static class ReflectionWrapper
    {
        public static FieldInfo GetField(ref System.Type type, string name)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.GetField(type, name);
#else
            return type.GetField(name);
#endif
        }

        public static FieldInfo[] GetFields(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.GetFields(type);
#else
            return type.GetFields();
#endif
        }

        public static bool IsClass(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.IsClass(type);
#else
            return type.IsClass;
#endif
        }

        public static bool IsEnum(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.IsEnum(type);
#else
            return type.IsEnum;
#endif
        }

        public static bool IsValueType(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.IsValueType(type);
#else
            return type.IsValueType;
#endif
        }

        public static System.Type GetBaseType(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.GetBaseType(type);
#else
            return type.BaseType;
#endif
        }

        public static MemberInfo[] GetMembers(ref System.Type type)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.GetMembers(type);
#else
            return type.GetMembers();
#endif
        }

        public static bool IsAssignableFrom(ref System.Type current, ref System.Type toCompare)
        {
#if (UNITY_WINRT && !UNITY_EDITOR)
            return Reflection.IsAssignableFrom(current, toCompare);
#else
            return current.IsAssignableFrom(toCompare);
#endif
        }

        public static List<T> GetAllChildsRecursively<T>(this Transform t) where T : MonoBehaviour
        {
            List<T> lstT = new List<T>();
            int childCount = t.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform tc = t.GetChild(i);
                if (tc.GetComponent<T>() != null)
                {
                    lstT.Add(tc.GetComponent<T>());
                }
                lstT.AddRange(GetAllChildsRecursively<T>(tc));
            }

            return lstT;
        }

        public static Dictionary<string, T> GetAllChildsAndPathRecursively<T>(this Transform t) where T : MonoBehaviour
        {
            Dictionary<string, T> lstT = new Dictionary<string, T>();
            int childCount = t.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform tc = t.GetChild(i);
                if (tc.GetComponent<T>() != null)
                {
                    lstT[t.name] = tc.GetComponent<T>();
                }

                foreach (KeyValuePair<string, T> e in GetAllChildsAndPathRecursively<T>(tc))
                {
                    lstT[t.name + "/" + e.Key] = e.Value;
                }
            }

            return lstT;
        }
    }
}