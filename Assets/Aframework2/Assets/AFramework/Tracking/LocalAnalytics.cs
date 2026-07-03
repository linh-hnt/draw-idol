using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if true//USE_LOCAL_ANALYTICS
namespace AFramework
{
    namespace Analytics
    {
        public class LocalAnalytics : IAnalytic
        {
            const string ServerPrefix = "192.25.20";
            const string ServerPostLink = "http://192.25.20.231:51002/tracking";

            Queue<LocalEventData> currentStack;
            long eventCount = 0;
            public bool InitSuccess { get; set; }

            public void ApplicationOnPause(bool Paused)
            {

            }

            public void Init(params string[] args)
            {
#if !UNITY_EDITOR_OSX
                 var localAddresses = AFramework.Utility.GetLocalIPAddress();
                bool found = false;
                foreach (var address in localAddresses)
                {
                    if (address.Contains(ServerPrefix))
                    {
                        found = true;
                        break;
                    }
                }
                 if (!found)
                {
                    return;
                }
#endif
                currentStack = new Queue<LocalEventData>();
                InitSuccess = true;
                TrackingManager.I.StartCoroutine(CRPostEventThread());
            }

            public void TrackEvent(string eventName)
            {
                if (!InitSuccess) return;
                TrackEvent(eventName, null);
            }

            public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            {
                if (!InitSuccess) return;
                if (parameters == null) parameters = new Dictionary<string, object>();
                parameters["eventCount"] = eventCount;
                ++eventCount;
                currentStack.Enqueue(new LocalEventData() { name = eventName, data = parameters });
            }

            IEnumerator CRPostEventThread()
            {
#if UNITY_IOS
                while (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    yield return null;
                }

                if (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
                {
                    InitSuccess = false;
                    currentStack.Clear();
                    yield break;
                }
#endif
                string uniqueIdentifier = null;
                bool statusRequestAdvertisingIdentifierAsync = false;
                if (Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) => {
                    uniqueIdentifier = advertisingId;
                    statusRequestAdvertisingIdentifierAsync = true;
                }))
                {
                    while (uniqueIdentifier == null || !statusRequestAdvertisingIdentifierAsync) yield return null;
                }
                else
                {
#if UNITY_ANDROID
                    uniqueIdentifier = Utility.GetAndroidAdvertiserId();
#endif
                }
                if (string.IsNullOrEmpty(uniqueIdentifier))
                {
                    uniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
                }
                var waitTimeError = new WaitForSeconds(1f);
                string appId = TrackingManager.I.AppflyerAppId;
                string appVersion = Application.version;
                int errorCount = 0;
                while (true)
                {
                    while (currentStack.Count == 0) yield return waitTimeError;
                    var eventData = currentStack.Peek();
                    var body = new Dictionary<string, object>();
                    body["bundleId"] = appId;
                    body["appVersion"] = appVersion;
                    body["idfa"] = uniqueIdentifier;
                    body["eventName"] = eventData.name;
                    string bodyStr = null;
                    if (eventData.data != null && eventData.data.Count > 0)
                    {
                        body["params"] = 0;
                        bodyStr = AFramework.MiniJSON.Json.Serialize(body);
                        bodyStr = bodyStr.Replace("\"params\":0", "\"params\":" + AFramework.MiniJSON.Json.Serialize(eventData.data));
                    }
                    else
                    {
                        bodyStr = AFramework.MiniJSON.Json.Serialize(body);
                    }

                    var request = new UnityEngine.Networking.UnityWebRequest(ServerPostLink);
                    request.method = UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST;
                    request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyStr));
                    request.uploadHandler.contentType = "application/json";
                    request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    var requestThrad = request.SendWebRequest();
                    while (!requestThrad.isDone) yield return null;
                    var responseCode = requestThrad.webRequest.responseCode;
                    if (responseCode >= 200 && responseCode <= 299)
                    {
                        requestThrad.webRequest.Dispose();
                        errorCount = 0;
                        currentStack.Dequeue();
                        yield return null;
                    }
                    else
                    {
                        requestThrad.webRequest.Dispose();
                        ++errorCount;
                        if (errorCount >= 10)
                        {
                            InitSuccess = false;
                            currentStack.Clear();
                            yield break;
                        }
                        yield return waitTimeError;
                    }
                }
            }

            public class LocalEventData
            {
                public string name;
                public Dictionary<string, object> data;
            }
        }
    }
}
#endif
