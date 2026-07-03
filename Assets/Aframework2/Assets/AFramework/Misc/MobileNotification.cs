using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;

#if UNITY_NOTIFICATION
using Unity.Notifications;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace AFramework
{
    public class MobileNotification : AFramework.ManualSingletonMono<MobileNotification>
    {
#region Define
        const string ONLINE_TOKEN = "af_onlinepush_token";

        public enum UserPermission
        {
            NotRequested = 0,
            Allowed,
            Denied,
            Limited
        }

        [System.Serializable]
        public class AndroidChannelInfo
        {
            public string Id = "EventChannel";
            public string Name = "Event";
            public string Description = "Event";
            public Importance Importance = Importance.High;
        }
#endregion

        [SerializeField] bool autoInit = true;
        [SerializeField] protected AndroidChannelInfo[] androidChannels;

        protected bool inited = false;

        private void Start()
        {
            if (autoInit) Init();
        }

        public void Init()
        {
            if (inited) return;
            var permission = CurrentPermission;
            if (!(permission == UserPermission.Allowed || permission == UserPermission.Limited)) return;

#if UNITY_ANDROID
            foreach (var info in androidChannels)
            {
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel()
                { 
                    Id = info.Id,
                    Name = info.Name,
                    Description = info.Description,
                    Importance = info.Importance,
                });
            }
#endif
        }

        public static UserPermission CurrentPermission
        {
            get
            {
#if UNITY_ANDROID
                return ToAFPermission(AndroidNotificationCenter.UserPermissionToPost);
#elif UNITY_IOS
                return ToAFPermission(iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus);
#endif
                return UserPermission.Denied;
            }
        }

        public static string DeviceToken
        {
            get { return PlayerPrefs.GetString(ONLINE_TOKEN, string.Empty); }
        }

        public async Task<UserPermission> RequestAuthorization(bool online)
        {
#if UNITY_ANDROID
            var request = new Unity.Notifications.Android.PermissionRequest();
            while (request.Status == PermissionStatus.RequestPending) await Task.Yield();

            if (online)
            {
#if USE_FIREBASE && USE_FIREBASE_MESSAGING
                bool waiting = true;
                EventHandler<Firebase.Messaging.TokenReceivedEventArgs> tokenCallback = (sender, token) => {
                    waiting = false;
                    PlayerPrefs.SetString(ONLINE_TOKEN, token.Token);
                };
                Firebase.Messaging.FirebaseMessaging.TokenReceived += tokenCallback;
                await Firebase.Messaging.FirebaseMessaging.GetTokenAsync();
                while (waiting) await Task.Yield();
                Firebase.Messaging.FirebaseMessaging.TokenReceived -= tokenCallback;
#else
                Debug.LogError("MobileNotification Android need to support RemoteNotification");
#endif
            }
            Init();
            return ToAFPermission(request.Status);
#elif UNITY_IOS
            var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
            var req = new AuthorizationRequest(authorizationOption, true);
            while (!req.IsFinished)
            {
                await Task.Yield();
            };

            var token = req.Granted ? req.DeviceToken : null;
            PlayerPrefs.SetString(ONLINE_TOKEN, token);
            Init();
            return CurrentPermission;
#endif
            }

#if UNITY_ANDROID
        public void SendNotification(AndroidNotification notification, string channelId)
        {
            if (!inited) return;
            if (androidChannels == null || androidChannels.Length == 0) return;
            AndroidNotificationCenter.SendNotification(notification, channelId);
        }

        static UserPermission ToAFPermission(Unity.Notifications.Android.PermissionStatus status)
        {
            if (status == PermissionStatus.NotRequested) return UserPermission.NotRequested;
            else if (status == PermissionStatus.Allowed) return UserPermission.Allowed;
            return UserPermission.Denied;
        }
#elif UNITY_IOS
        public void SendNotification(iOSNotification notification)
        {
            iOSNotificationCenter.ScheduleNotification(notification);
        }

        static UserPermission ToAFPermission(Unity.Notifications.iOS.AuthorizationStatus status)
        {
            if (status == AuthorizationStatus.NotDetermined) return UserPermission.NotRequested;
            else if (status == AuthorizationStatus.Denied) return UserPermission.Denied;
            else if (status == AuthorizationStatus.Authorized) return UserPermission.Allowed;
            return UserPermission.Limited;
        }
#endif
    }
}
#endif