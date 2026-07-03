using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_SDKBOXIAP
using Sdkbox;
#endif

namespace AFramework.IAP
{
    public class SDKBOXIAPWrapper : IAPBaseWrapper
    {
#if USE_SDKBOXIAP
        Sdkbox.IAP mSystem;

        public override void Init(object[] inputs)
        {
            mManager = (AFramework.IAP.IAPManager)inputs[0];
            mSystem = this.gameObject.AddComponent<Sdkbox.IAP>();
            mSystem.androidKey = (string)inputs[1];
            var hackConstructor = typeof(Sdkbox.IAP.Callbacks).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[] { }, null);
            mSystem.callbacks = (Sdkbox.IAP.Callbacks)hackConstructor.Invoke(null);
            mSystem.iOSProducts = new List<ProductDescription>();
            mSystem.androidProducts = new List<ProductDescription>();

            var currentPackage = mManager.CurrentPackages;
            foreach (var pair in currentPackage)
            {
                var data = pair.Value;
                var item = new ProductDescription();
                item.id = data.Identifier;
                item.name = data.Identifier;
                item.consumable = data.Type == eProductType.Consumable;
#if UNITY_IOS
                mSystem.iOSProducts.Add(item);
#else
                mSystem.androidProducts.Add(item);
#endif
            }

            mSystem.callbacks.onProductRequestSuccess.AddListener(SdkboxProductRequestSuccess);
            mSystem.callbacks.onRestored.AddListener((product) => { PurchaseSucceeded(product.id); });
            mSystem.callbacks.onCanceled.AddListener((product) => { PurchaseFailed("Cancel"); });
            mSystem.callbacks.onFailure.AddListener((product, str) => { PurchaseFailed(str); });
        }

        void SdkboxProductRequestSuccess(Product[] products)
        {
            for (int i = 0; i < products.Length; ++i)
            {
                mManager.UpdatePrice(products[i].id, products[i].price);
            }
            if (EventOnIAPPackRefreshed != null) EventOnIAPPackRefreshed();
        }

        public override void PurchaseItem(string packageName)
        {
            mSystem.purchase(packageName);
        }
#endif
    }
}