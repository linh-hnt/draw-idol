using System.Collections;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace AFramework.IAP
{
    public enum eBillingSystem
    {
        Invalid = -1,
        SimpleIAPSystem,
        SDKBOXIAP
    }

    public enum eProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    [System.Serializable]
    public class RewardInfo
    {
        public string Name;
        public int Amount;
    }

    [System.Serializable]
    public class PackageInfo
    {
        public string PackageName;
        public PlatformString PackageIdentifier;
        public Sprite Icon;
        public Sprite[] CustomIcon;
        public eProductType Type;
        public string Title;
        public string Description;
        public double Price;
        public string Currency;

#if ODIN_INSPECTOR
        [ListDrawerSettings(ListElementLabelName = "Name")]
#endif
        public List<RewardInfo> Rewards;

        [System.NonSerialized] public string DisplayPrice;

        public virtual PackageInfo Clone()
        {
            var newData = new PackageInfo();
            newData.PackageName = PackageName;
            newData.PackageIdentifier = new PlatformString(PackageIdentifier);
            newData.Icon = Icon;
            newData.CustomIcon = CustomIcon;
            newData.Type = Type;
            newData.Title = Title;
            newData.Description = Description;
            newData.Price = Price;
            newData.Currency = Currency;
            newData.DisplayPrice = DisplayPrice;
            newData.Rewards = new List<RewardInfo>();
            for (int i = 0; i < Rewards.Count; ++i)
            {
                newData.Rewards.Add(Rewards[i]);
            }

            return newData;
        }
    }

    public class ActivePackageInfo
    {
        PackageInfo mCurrentActivePackage;

        public PackageInfo CurrentActivePackage
        {
            get { return mCurrentActivePackage; }
        }

        string mDefaultPackageIdentifier;

        public string DefaultPackageIdentifier
        {
            get { return mDefaultPackageIdentifier; }
        }

        Dictionary<string, PackageInfo> mPackageList;

        public void SetPackageList(string defaultPackageIdentifier, PackageInfo[] newList)
        {
            mPackageList = new Dictionary<string, PackageInfo>();
            for (int i = 0; i < newList.Length; ++i)
            {
                mPackageList[newList[i].PackageIdentifier.getString()] = newList[i];
            }

            mDefaultPackageIdentifier = defaultPackageIdentifier;
            mCurrentActivePackage = mPackageList[mDefaultPackageIdentifier];
        }

        public void SetActivePackage(string packageIdentifier)
        {
            if (mPackageList.ContainsKey(packageIdentifier))
            {
                mCurrentActivePackage = mPackageList[packageIdentifier];
            }
        }
    }

    [System.Serializable]
    [CreateAssetMenu(menuName = "ScriptableObject/AFramework/IAP/IAPPackagesInfo")]
    public class IAPPackages : ScriptableObject
    {
#if ODIN_INSPECTOR
        [ListDrawerSettings(ListElementLabelName = "PackageName")]
#endif
        [SerializeField]
        protected PackageInfo[] Data;

        public virtual PackageInfo[] CurrentData => Data;

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ExportFileCSV()
        {
            if (Data == null || Data.Length == 0)
            {
                Debug.LogWarning("No IAP data to export");
                return;
            }
            
            // Create CSV header
            System.Text.StringBuilder csv = new System.Text.StringBuilder();
            csv.AppendLine("PackageName,PackageIdentifier,Type,Title,Description,Price,Currency,Rewards");
            
            // Add data rows
            foreach (var package in Data)
            {
                string rewards = "";
                if (package.Rewards != null && package.Rewards.Count > 0)
                {
                    System.Text.StringBuilder rewardsBuilder = new System.Text.StringBuilder();
                    foreach (var reward in package.Rewards)
                    {
                        rewardsBuilder.Append($"{reward.Name}:{reward.Amount};");
                    }
                    rewards = rewardsBuilder.ToString().TrimEnd(';');
                }
                
                // Escape commas and quotes in string fields
                string escapedTitle = package.Title?.Replace("\"", "\"\"").Replace(",", "\",\"") ?? "";
                string escapedDesc = package.Description?.Replace("\"", "\"\"").Replace(",", "\",\"") ?? "";
                string escapedName = package.PackageName?.Replace("\"", "\"\"").Replace(",", "\",\"") ?? "";
                
                csv.AppendLine($"\"{escapedName}\",{package.PackageIdentifier.getString()},{package.Type},\"{escapedTitle}\",\"{escapedDesc}\",{package.Price},{package.Currency},\"{rewards}\"");
            }
            
            // Save to file
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save IAP Data as CSV", "", "IAP_Packages", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    System.IO.File.WriteAllText(path, csv.ToString());
                    Debug.Log($"Successfully exported IAP data to {path}");
                    UnityEditor.EditorUtility.RevealInFinder(path);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to export IAP data: {e.Message}");
                }
            }
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void ImportFileCSV()
        {
            // Open file browser to select CSV file
            string path = UnityEditor.EditorUtility.OpenFilePanel("Import IAP Data from CSV", "", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(path);
                if (lines.Length <= 1)
                {
                    Debug.LogWarning("CSV file is empty or contains only headers");
                    return;
                }

                // Skip header line and create data array
                List<PackageInfo> importedPackages = new List<PackageInfo>();
                
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;
                        
                    string line = lines[i];
                    List<string> fields = ParseCSVLine(line);
                    
                    if (fields.Count < 7)
                    {
                        Debug.LogWarning($"Line {i+1} has insufficient fields. Skipping: {line}");
                        continue;
                    }

                    // Create new PackageInfo
                    PackageInfo package = new();
                    package.PackageName = fields[0];
                    package.PackageIdentifier = new (); // Will set platform-specific value below
                    package.Type = ParseEnum<eProductType>(fields[2]);
                    package.Title = fields[3];
                    package.Description = fields[4];
                    package.Price = double.Parse(fields[5], System.Globalization.CultureInfo.InvariantCulture);
                    package.Currency = fields[6];
                    package.Rewards = new List<RewardInfo>();

                    // Handle platform-specific identifier
                    // Assuming same value for all platforms in this basic implementation
                    string platformId = fields[1];
                    SetPlatformSpecificId(ref package.PackageIdentifier, platformId);
                    
                    // Parse rewards if available
                    if (fields.Count >= 8 && !string.IsNullOrEmpty(fields[7]))
                    {
                        string[] rewardEntries = fields[7].Split(';');
                        foreach (string rewardEntry in rewardEntries)
                        {
                            string[] rewardParts = rewardEntry.Split(':');
                            if (rewardParts.Length == 2 && int.TryParse(rewardParts[1], out int amount))
                            {
                                package.Rewards.Add(new RewardInfo 
                                { 
                                    Name = rewardParts[0], 
                                    Amount = amount 
                                });
                            }
                        }
                    }
                    
                    importedPackages.Add(package);
                }

                // Update Data array
                if (importedPackages.Count > 0)
                {
                    Data = importedPackages.ToArray();
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log($"Successfully imported {importedPackages.Count} IAP packages from {path}");
                }
                else
                {
                    Debug.LogWarning("No valid IAP packages found in the CSV file");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import IAP data: {e.Message}");
            }
        }

        private List<string> ParseCSVLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            System.Text.StringBuilder field = new System.Text.StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        // Toggle quote mode
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of field
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
            
            // Add the last field
            fields.Add(field.ToString());
            return fields;
        }

        private T ParseEnum<T>(string value) where T : struct
        {
            if (System.Enum.TryParse<T>(value, out T result))
                return result;
                
            return default(T);
        }

        private void SetPlatformSpecificId(ref PlatformString platformString, string value)
        {
            // Since PlatformString doesn't have public setters, we'll need to use reflection
            // to set the platform-specific values
            System.Type type = typeof(PlatformString);
            
            System.Reflection.FieldInfo winField = type.GetField("win32", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo androidField = type.GetField("android", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo iosField = type.GetField("ios", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (winField != null) winField.SetValue(platformString, value);
            if (androidField != null) androidField.SetValue(platformString, value);
            if (iosField != null) iosField.SetValue(platformString, value);
        }
#endif
    }

}