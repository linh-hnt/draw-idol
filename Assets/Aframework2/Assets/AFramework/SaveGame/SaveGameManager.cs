using UnityEngine;
using System.Collections;
using AFramework;
using System.IO;
using System.Collections.Generic;
using com.spacepuppy.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AFramework
{
    public interface ISaveData
    {
        object GetData();
        void SetData(string data);
        void OnAllDataLoaded();
        void RegisterSaveData();
        bool DataChanged { get; set; }
    }

    public class SaveGameManager : AFramework.Singleton<SaveGameManager>
    {
        [System.Serializable]
        public class SaveGameDictionary : SerializableDictionaryBase<string, ISaveData> { }
        [System.Serializable]
        public class LoadGameDictionary : SerializableDictionaryBase<string, string> { }

        const string MANDATORY_SAVE_NAME = "mwovjtpamcjaytifnhyqlbprths";
        const string OPTIONAL_SAVE_NAME = "nalgowuthvnapqyewngoapwvz";

        // Debounce-save fields
        private CancellationTokenSource _saveDebounceCts;
        private bool _isSaving;
        private bool _dirtyAfterSave;

        public delegate object ObjectDataCallback();
        public delegate void StringDataCallback(string data);

        [UnityEngine.SerializeField()]
        private SaveGameDictionary mMandatory = new SaveGameDictionary();
        [UnityEngine.SerializeField()]
        private SaveGameDictionary mOptional = new SaveGameDictionary();

        public void RegisterMandatoryData(string name, ISaveData data)
        {
            mMandatory[name] = data;
        }

        public void RegisterOptionalData(string name, ISaveData data)
        {
            mOptional[name] = data;
        }

        public bool Save(bool mandatory = true, bool optional = true, bool hasBackup = true)
        {
            bool result = true;
#if USE_GPGS_SAVEGAME
            if(GooglePlayServiceManager.instance.OnLoadFromCloud)
            {
                return result;
            }
#endif
            if (mandatory)
            {
                try
                {
                    bool hasChanged = false;
                    foreach (string key in mMandatory.Keys)
                    {
                        hasChanged |= mMandatory[key].DataChanged;
                    }

                    if (hasChanged)
                    {
                        LoadGameDictionary temp = new LoadGameDictionary();
                        bool checkValid = false;
                        foreach (string key in mMandatory.Keys)
                        {
                            temp[key] = JsonUtility.ToJson(mMandatory[key].GetData());
                            checkValid = true;
                            mMandatory[key].DataChanged = false;
                        }

                        if (checkValid)
                        {
                            var jsonData = JsonUtility.ToJson(temp);
#if KEYCHAIN_AVAILABLE
                            FSG.iOSKeychain.Keychain.SetValue(MANDATORY_SAVE_NAME, jsonData);
#endif

                            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonData);
#if UNITY_WEBGL
                            result &= SaveToPlayerPrefs(MANDATORY_SAVE_NAME, data, hasBackup);
#else
                            result &= SaveToFile(MANDATORY_SAVE_NAME, data, hasBackup);
#endif
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }

            if (optional)
            {
                try
                {
                    bool hasChanged = false;
                    foreach (string key in mOptional.Keys)
                    {
                        hasChanged |= mOptional[key].DataChanged;
                    }

                    if (hasChanged)
                    {
                        LoadGameDictionary temp = new LoadGameDictionary();
                        bool checkValid = false;
                        foreach (string key in mOptional.Keys)
                        {
                            temp[key] = JsonUtility.ToJson(mOptional[key].GetData());
                            checkValid = true;
                            mOptional[key].DataChanged = false;
                        }

                        if (checkValid)
                        {
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(temp));
#if UNITY_WEBGL
                            result &= SaveToPlayerPrefs(OPTIONAL_SAVE_NAME, data, hasBackup);
#else
                            result &= SaveToFile(OPTIONAL_SAVE_NAME, data, hasBackup);
#endif
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }
            return result;
        }

#if NET_STANDARD
        public async Task<bool> SaveAsync(bool mandatory = true, bool optional = true, bool hasBackup = true)
        {
            bool result = true;
#if USE_GPGS_SAVEGAME
            if(GooglePlayServiceManager.instance.OnLoadFromCloud)
            {
                return result;
            }
#endif
            if (mandatory)
            {
                try
                {
                    bool hasChanged = false;
                    foreach (string key in mMandatory.Keys)
                    {
                        hasChanged |= mMandatory[key].DataChanged;
                    }

                    if (hasChanged)
                    {
                        LoadGameDictionary temp = new LoadGameDictionary();
                        bool checkValid = false;
                        foreach (string key in mMandatory.Keys)
                        {
                            temp[key] = JsonUtility.ToJson(mMandatory[key].GetData());
                            checkValid = true;
                            mMandatory[key].DataChanged = false;
                        }

                        if (checkValid)
                        {
                            var jsonData = JsonUtility.ToJson(temp);
#if KEYCHAIN_AVAILABLE
                            FSG.iOSKeychain.Keychain.SetValue(MANDATORY_SAVE_NAME, jsonData);
#endif

                            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonData);
#if UNITY_WEBGL
                            result &= SaveToPlayerPrefs(MANDATORY_SAVE_NAME, data, hasBackup);
#else
                            result &= await SaveToFileAsync(MANDATORY_SAVE_NAME, data, hasBackup);
#endif
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }

            if (optional)
            {
                try
                {
                    bool hasChanged = false;
                    foreach (string key in mOptional.Keys)
                    {
                        hasChanged |= mOptional[key].DataChanged;
                    }

                    if (hasChanged)
                    {
                        LoadGameDictionary temp = new LoadGameDictionary();
                        bool checkValid = false;
                        foreach (string key in mOptional.Keys)
                        {
                            temp[key] = JsonUtility.ToJson(mOptional[key].GetData());
                            checkValid = true;
                            mOptional[key].DataChanged = false;
                        }

                        if (checkValid)
                        {
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(temp));
#if UNITY_WEBGL
                            result &= SaveToPlayerPrefs(OPTIONAL_SAVE_NAME, data, hasBackup);
#else
                            result &= await SaveToFileAsync(OPTIONAL_SAVE_NAME, data, hasBackup);
#endif
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }
            return result;
        }
        #endif

        /// <summary>
        /// Background-safe save. Serializes and encrypts on main thread, writes file on a background thread.
        /// Works on ALL platforms (not gated by NET_STANDARD).
        /// </summary>
        public async UniTask SaveAsyncUni(bool mandatory = true, bool optional = true, bool hasBackup = true)
        {
            // Phase 1 — serialize + encrypt: must run on main thread
            // (JsonUtility, GetData(), GetUDID() all require main thread)
            byte[] mandatoryEncryptedData = null;
            byte[] optionalEncryptedData = null;

            {
                byte[] mandatoryData = TrySerializeMandatory(out bool mandatoryHasChanged);
                byte[] optionalData = TrySerializeOptional(out bool optionalHasChanged);

                if (!mandatoryHasChanged && !optionalHasChanged) return;

                if (mandatoryHasChanged && mandatoryData != null)
                {
                    mandatoryEncryptedData = mandatoryData;
                    SimpleEncrypt(ref mandatoryEncryptedData);
                }

                if (optionalHasChanged && optionalData != null)
                {
                    optionalEncryptedData = optionalData;
                    SimpleEncrypt(ref optionalEncryptedData);
                }
            }

            // Phase 2 — file I/O on background thread: data is already encrypted, don't re-encrypt.
            await UniTask.SwitchToThreadPool();
            try
            {
                if (mandatoryEncryptedData != null)
                {
#if UNITY_WEBGL
                    SaveEncryptedToPlayerPrefs(MANDATORY_SAVE_NAME, mandatoryEncryptedData);
#else
                    SaveEncryptedToFile(MANDATORY_SAVE_NAME, mandatoryEncryptedData, hasBackup);
#endif
                }

                if (optionalEncryptedData != null)
                {
#if UNITY_WEBGL
                    SaveEncryptedToPlayerPrefs(OPTIONAL_SAVE_NAME, optionalEncryptedData);
#else
                    SaveEncryptedToFile(OPTIONAL_SAVE_NAME, optionalEncryptedData, hasBackup);
#endif
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        // --- Helpers for SaveAsyncUni (must be called from main thread) ---
        private byte[] TrySerializeMandatory(out bool hasChanged)
        {
            hasChanged = false;
            foreach (string key in mMandatory.Keys)
            {
                hasChanged |= mMandatory[key].DataChanged;
            }
            if (!hasChanged) return null;

            var temp = new LoadGameDictionary();
            bool checkValid = false;
            foreach (string key in mMandatory.Keys)
            {
                temp[key] = JsonUtility.ToJson(mMandatory[key].GetData());
                checkValid = true;
                mMandatory[key].DataChanged = false;
            }
            return checkValid ? System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(temp)) : null;
        }

        private byte[] TrySerializeOptional(out bool hasChanged)
        {
            hasChanged = false;
            foreach (string key in mOptional.Keys)
            {
                hasChanged |= mOptional[key].DataChanged;
            }
            if (!hasChanged) return null;

            var temp = new LoadGameDictionary();
            bool checkValid = false;
            foreach (string key in mOptional.Keys)
            {
                temp[key] = JsonUtility.ToJson(mOptional[key].GetData());
                checkValid = true;
                mOptional[key].DataChanged = false;
            }
            return checkValid ? System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(temp)) : null;
        }

        private static LoadGameDictionary LoadGameDictionaryFromBytes(byte[] data)
        {
            return JsonUtility.FromJson<LoadGameDictionary>(System.Text.Encoding.UTF8.GetString(data, 0, data.Length));
        }

        /// <summary>
        /// Request a debounced save. If called again within <paramref name="debounceSeconds"/>,
        /// the previous timer is cancelled and a new one starts.
        /// Uses SaveAsyncUni internally to avoid blocking the main thread.
        /// </summary>
        public void RequestSave(float debounceSeconds = 1f)
        {
            // Cancel any pending scheduled save
            _saveDebounceCts?.Cancel();
            _saveDebounceCts?.Dispose();
            _saveDebounceCts = new CancellationTokenSource();

            // Schedule a new async save after debounceSeconds
            SaveDebouncedInternal(debounceSeconds, _saveDebounceCts.Token).Forget();
        }

        private async UniTask SaveDebouncedInternal(float debounceSeconds, CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(debounceSeconds * 1000f), cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                // Cancelled by another RequestSave call — expected, just return
                return;
            }

            // If a save is already in progress, mark dirty and skip
            if (_isSaving)
            {
                _dirtyAfterSave = true;
                return;
            }

            await SaveAsyncUni();
        }

        /// <summary>
        /// Flush any pending debounced save synchronously.
        /// MUST be called from the main thread before app quit / pause to ensure durability.
        /// </summary>
        public void FlushPendingSave()
        {
            _saveDebounceCts?.Cancel();
            _saveDebounceCts?.Dispose();
            _saveDebounceCts = null;

            // Run the pending synchronous save inline (uses Save() which is already main-thread safe)
            if (_isSaving) return; // skip if a save is currently running

            // Directly call the synchronous Save to guarantee persistence
            Save(true, true, true);
        }

        public void Load(bool mandatory = true, bool optional = true, bool notification = true)
        {
            LoadGameDictionary loadDictionary = null;
            if (mandatory)
            {
                try
                {
                    byte[] data = null;
                    string jsonData = null;
#if KEYCHAIN_AVAILABLE
                    jsonData = FSG.iOSKeychain.Keychain.GetValue(MANDATORY_SAVE_NAME);
#endif

                    if (string.IsNullOrEmpty(jsonData))
                    {
#if UNITY_WEBGL
                        data = LoadFromPlayerPrefs(MANDATORY_SAVE_NAME, true);
#else
                        data = LoadFromFile(MANDATORY_SAVE_NAME, true);
                        if (data == null)
                        {
                            data = LoadFromFile("_" + MANDATORY_SAVE_NAME, true);
                        }
#endif
                        jsonData = data == null ? "{}" : System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
                    }

                    loadDictionary = JsonUtility.FromJson<LoadGameDictionary>(jsonData);
                }
                catch (System.Exception)
                {
                    loadDictionary = null;
                }
                foreach (string key in mMandatory.Keys)
                {
                    mMandatory[key].SetData(loadDictionary != null && loadDictionary.ContainsKey(key) && loadDictionary[key] != null ? loadDictionary[key] : "");
                }
            }

            if (optional)
            {
                try
                {
                    byte[] data = null;
#if UNITY_WEBGL
                    data = LoadFromPlayerPrefs(OPTIONAL_SAVE_NAME, false);
#else
                    data = LoadFromFile(OPTIONAL_SAVE_NAME, false);
                    if (data == null)
                    {
                        data = LoadFromFile("_" + OPTIONAL_SAVE_NAME, false);
                    }
#endif
                    loadDictionary = JsonUtility.FromJson<LoadGameDictionary>(data == null ? "{}" : System.Text.Encoding.UTF8.GetString(data, 0, data.Length));
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                    loadDictionary = null;
                }
                foreach (string key in mOptional.Keys)
                {
                    mOptional[key].SetData(loadDictionary != null && loadDictionary.ContainsKey(key) && loadDictionary[key] != null ? loadDictionary[key] : "");
                }
            }
            if (notification)
            {
                if (mandatory)
                {
                    foreach (var item in mMandatory.Values)
                    {
                        item.OnAllDataLoaded();
                    }
                }
                if (optional)
                {
                    foreach (var item in mOptional.Values)
                    {
                        item.OnAllDataLoaded();
                    }
                }
            }
        }

#if NET_STANDARD
        public async Task LoadAsync(bool mandatory = true, bool optional = true, bool notification = true)
        {
            LoadGameDictionary loadDictionary = null;
            if (mandatory)
            {
                try
                {
                    byte[] data = null;
                    string jsonData = null;
#if KEYCHAIN_AVAILABLE
                    jsonData = FSG.iOSKeychain.Keychain.GetValue(MANDATORY_SAVE_NAME);
#endif

                    if (string.IsNullOrEmpty(jsonData))
                    {
#if UNITY_WEBGL
                        data = LoadFromPlayerPrefs(MANDATORY_SAVE_NAME, true);
#else
                        data = await LoadFromFileAsync(MANDATORY_SAVE_NAME, true);
                        if (data == null)
                        {
                            data = await LoadFromFileAsync("_" + MANDATORY_SAVE_NAME, true);
                        }
#endif
                        jsonData = data == null ? "{}" : System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
                    }
                    loadDictionary = JsonUtility.FromJson<LoadGameDictionary>(jsonData);
                }
                catch (System.Exception)
                {
                    loadDictionary = null;
                }
                foreach (string key in mMandatory.Keys)
                {
                    mMandatory[key].SetData(loadDictionary != null && loadDictionary.ContainsKey(key) && loadDictionary[key] != null ? loadDictionary[key] : "");
                }
            }

            if (optional)
            {
                try
                {
                    byte[] data = null;
#if UNITY_WEBGL
                    data = LoadFromPlayerPrefs(OPTIONAL_SAVE_NAME, false);
#else
                    data = await LoadFromFileAsync(OPTIONAL_SAVE_NAME, false);
                    if (data == null)
                    {
                        data = await LoadFromFileAsync("_" + OPTIONAL_SAVE_NAME, false);
                    }
#endif
                    loadDictionary = JsonUtility.FromJson<LoadGameDictionary>(data == null ? "{}" : System.Text.Encoding.UTF8.GetString(data, 0, data.Length));
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                    loadDictionary = null;
                }
                foreach (string key in mOptional.Keys)
                {
                    mOptional[key].SetData(loadDictionary != null && loadDictionary.ContainsKey(key) && loadDictionary[key] != null ? loadDictionary[key] : "");
                }
            }
            if (notification)
            {
                if (mandatory)
                {
                    foreach (var item in mMandatory.Values)
                    {
                        item.OnAllDataLoaded();
                    }
                }
                if (optional)
                {
                    foreach (var item in mOptional.Values)
                    {
                        item.OnAllDataLoaded();
                    }
                }
            }
        }
#endif

        public bool SaveToFile(string fileName, byte[] data, bool hasBackup = true)
        {
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (hasBackup)
                {
                    if (File.Exists(savepath + "_" + fileName))
                    {
                        File.Delete(savepath + "_" + fileName);
                    }
                    if (File.Exists(savepath + fileName))
                    {
                        File.Move(savepath + fileName, savepath + "_" + fileName);
                    }
                }

                //simple encrypt using UDID/decrypt
                SimpleEncrypt(ref data);
                File.WriteAllBytes(savepath + fileName, data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }

#if NET_STANDARD
        public async Task<bool> SaveToFileAsync(string fileName, byte[] data, bool hasBackup = true)
        {
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (hasBackup)
                {
                    if (File.Exists(savepath + "_" + fileName))
                    {
                        File.Delete(savepath + "_" + fileName);
                    }
                    if (File.Exists(savepath + fileName))
                    {
                        File.Move(savepath + fileName, savepath + "_" + fileName);
                    }
                }

                //simple encrypt using UDID/decrypt
                SimpleEncrypt(ref data);
                await File.WriteAllBytesAsync(savepath + fileName, data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }
#endif

        public bool SaveToPlayerPrefs(string fileName, byte[] data, bool hasBackup = true)
        {
            try
            {
                string savepath = AFramework.Utility.GetSavePath();

                //simple encrypt using UDID/decrypt
                SimpleEncrypt(ref data);
                PlayerPrefs.SetString(fileName, System.Convert.ToBase64String(data));
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }

        //public bool LoadMandatoryFile(ref byte[] data)
        //{
        //    return LoadFromFile(MANDATORY_SAVE_NAME, ref data);
        //}

        //public bool SaveToMandatoryFile(byte[] data)
        //{
        //    return SaveToFile(MANDATORY_SAVE_NAME, data);
        //}

        // --- Write helpers for SaveAsyncUni: data is already encrypted, just write to disk ---
        private bool SaveEncryptedToFile(string fileName, byte[] data, bool hasBackup = true)
        {
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (hasBackup)
                {
                    if (File.Exists(savepath + "_" + fileName))
                    {
                        File.Delete(savepath + "_" + fileName);
                    }
                    if (File.Exists(savepath + fileName))
                    {
                        File.Move(savepath + fileName, savepath + "_" + fileName);
                    }
                }
                File.WriteAllBytes(savepath + fileName, data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }

        private bool SaveEncryptedToPlayerPrefs(string fileName, byte[] data)
        {
            try
            {
                PlayerPrefs.SetString(fileName, System.Convert.ToBase64String(data));
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }

        public byte[] LoadFromFile(string fileName, bool hasBackup = false)
        {
            byte[] data = null;
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (File.Exists(savepath + fileName))
                {
                    data = File.ReadAllBytes(savepath + fileName);
                }
                else if (File.Exists(savepath + "_" + fileName))
                {
                    data = File.ReadAllBytes(savepath + "_" + fileName);
                }
                else
                {
                    return null;
                }

                SimpleEncrypt(ref data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            return data;
        }

#if NET_STANDARD
        public async Task<byte[]> LoadFromFileAsync(string fileName, bool hasBackup = false)
        {
            byte[] data = null;
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (File.Exists(savepath + fileName))
                {
                    data = await File.ReadAllBytesAsync(savepath + fileName);
                }
                else if (File.Exists(savepath + "_" + fileName))
                {
                    data = await File.ReadAllBytesAsync(savepath + "_" + fileName);
                }
                else
                {
                    return null;
                }

                SimpleEncrypt(ref data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            return data;
        }
#endif

        public byte[] LoadFromPlayerPrefs(string fileName, bool hasBackup = false)
        {
            byte[] data = null;
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                var save_data = PlayerPrefs.GetString(fileName, null);
                if (!string.IsNullOrEmpty(save_data))
                {
                    data = System.Convert.FromBase64String(save_data);
                }
                else
                {
                    return null;
                }

                SimpleEncrypt(ref data);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            return data;
        }

        void SimpleEncrypt(ref byte[] data)
        {
#if !(USE_CHEAT || DEVELOPMENT_BUILD)
            byte[] key = System.Text.Encoding.UTF8.GetBytes(AFramework.Utility.GetUDID());
            int d_len = data.Length;
            int k_len = key.Length;
            for (uint i = 0; i < d_len; i++)
                data[i] ^= key[i % k_len];
#endif
        }

        public string StringToEncryptBase64(string data)
        {
            byte[] encryptData = System.Text.Encoding.UTF8.GetBytes(data);
            SimpleEncrypt(ref encryptData);
            return System.Convert.ToBase64String(encryptData);
        }

        public string EncryptBase64ToString(string data)
        {
            try
            {
                byte[] decryptData = System.Convert.FromBase64String(data);
                SimpleEncrypt(ref decryptData);
                return System.Text.Encoding.UTF8.GetString(decryptData, 0, decryptData.Length);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return "";
            }
        }

        public void DeleteAll(bool other_data = true)
        {
            if (other_data)
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(AFramework.Utility.GetSavePath());

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                DeleteSave(MANDATORY_SAVE_NAME);
                DeleteSave(OPTIONAL_SAVE_NAME);
            }
            
            mMandatory.Clear();
            mOptional.Clear();

#if KEYCHAIN_AVAILABLE
            FSG.iOSKeychain.Keychain.DeleteValue(MANDATORY_SAVE_NAME);
#endif
        }

        public bool DeleteSave(string fileName)
        {
            try
            {
                string savepath = AFramework.Utility.GetSavePath();
                if (File.Exists(savepath + fileName))
                {
                    File.Delete(savepath + fileName);
                }
                if (File.Exists(savepath + "_" + fileName))
                {
                    File.Delete(savepath + "_" + fileName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            return true;
        }
    }
}