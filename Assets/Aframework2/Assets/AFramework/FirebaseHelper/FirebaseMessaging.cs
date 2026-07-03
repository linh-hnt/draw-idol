using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.FirebaseService
{
    public class FirebaseMessaging : MonoBehaviour
    {
#if USE_FIREBASE && USE_FIREBASE_MESSAGING
        public static System.Action<Firebase.Messaging.TokenReceivedEventArgs> EventOnTokenReceived;
        public static System.Action<Firebase.Messaging.MessageReceivedEventArgs> EventOnMessageReceived;
        public static string FirebaseMessagingToken { get; protected set; }

        void Start()
        {
            FirebaseInstance.ChecAndTryInit(Init);
        }

        void Init()
        {
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
            Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

            // Cách lấy Token thủ công ngay lập tức
            Firebase.Messaging.FirebaseMessaging.GetTokenAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    string token = task.Result;
                    Debug.Log($"[FCM Token]: {token}");
                }
            });
        }

        void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            FirebaseMessagingToken = token.Token;
            if (EventOnTokenReceived != null) EventOnTokenReceived(token);

            Debug.Log($"[FCM Token Received]: {token.Token}");
        }

        void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
        {
            if (EventOnMessageReceived != null) EventOnMessageReceived(e);
        }
#endif
    }
}