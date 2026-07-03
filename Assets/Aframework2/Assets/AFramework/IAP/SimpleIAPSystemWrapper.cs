using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.IAP
{
    public class SimpleIAPSystemWrapper : IAPBaseWrapper
    {
#if SIS_IAP
        SIS.IAPManager mSystem;

        public override void Init(object[] inputs)
        {
            SIS.IAPManager.purchaseSucceededEvent += PurchaseSucceeded;
            SIS.IAPManager.purchaseFailedEvent += PurchaseFailed;
            mManager = (AFramework.IAP.IAPManager)inputs[0];
            var sis_asset = new SIS.IAPScriptableObject();
            var productList = new List<SIS.IAPProduct>();
            var categoryList = new List<SIS.IAPCategory>();
            var currentPackage = mManager.PackageConfig;

            var prod_category = new SIS.IAPCategory();
            prod_category.storeIDs = new List<SIS.StoreMetaDefinition>();

            var store_meta = new SIS.StoreMetaDefinition();
            store_meta.ID = "IAP";
            prod_category.storeIDs.Add(store_meta);
            categoryList.Add(prod_category);

            for (int i = 0; i < currentPackage.CurrentData.Length; ++i)
            {
                var data = currentPackage.CurrentData[i];
                var item = new SIS.IAPProduct();
                item.ID = data.PackageIdentifier.getString();
                if (data.Type == eProductType.Consumable) item.type = UnityEngine.Purchasing.ProductType.Consumable;
                else if (data.Type == eProductType.NonConsumable) item.type = UnityEngine.Purchasing.ProductType.NonConsumable;
                else if (data.Type == eProductType.Subscription) item.type = UnityEngine.Purchasing.ProductType.Subscription;

                SIS.IAPExchangeObject price_info = new SIS.IAPExchangeObject();
                price_info.type = SIS.IAPExchangeObject.ExchangeType.RealMoney;
                price_info.realPrice = data.DisplayPrice;

                item.priceList = new List<SIS.IAPExchangeObject>();
                item.priceList.Add(price_info);

                item.category = prod_category;

                productList.Add(item);
            }
            sis_asset.productList = productList;
            sis_asset.categoryList = categoryList;

            //mSystem = this.GetComponentInChildren<SIS.IAPManager>();
            var prefab = Resources.Load<SIS.IAPManager>("IAPManager");
            prefab.GetComponent<SIS.DBManager>().memoryOnly = true;
            prefab.asset = sis_asset;
            prefab.autoInitialize = false;
            mSystem = Instantiate(prefab, this.transform);
#if VALIDATION_CLIENT
            mSystem.gameObject.AddComponent<SIS.ReceiptValidatorClient>();
#endif
            StartCoroutine(CRWaitForProductUpdate());
            mSystem.Initialize();
        }

        public override void PurchaseItem(string packageName)
        {
            SIS.IAPManager.Purchase(packageName);
        }

        IEnumerator CRWaitForProductUpdate()
        {
            WaitForSeconds waitTime = new WaitForSeconds(1);
            while (SIS.IAPManager.controller == null) yield return waitTime;
            var allProduct = SIS.IAPManager.controller.products.all;
            for (int i = 0; i < allProduct.Length; ++i)
            {
                if (!allProduct[i].metadata.localizedPrice.Equals(null))
                {
                    double newPrice;
                    try
                    {
                        newPrice = (double)allProduct[i].metadata.localizedPrice;
                        mManager.UpdatePrice(allProduct[i].definition.id, newPrice, allProduct[i].metadata.isoCurrencyCode);
                    }
                    catch (System.Exception e)
                    {

                    }
                }
#if !UNITY_ANDROID
                if (allProduct[i].definition != null)
                {
                    var prod = SIS.IAPManager.GetIAPProduct(allProduct[i].definition.id);
                    if (prod != null)
                    {
                        prod.type = allProduct[i].definition.type;
                    }
                }
#endif
            }

            var defaultPackages = mManager.PackageConfig.CurrentData;
            var currentPackages = mManager.ActivePackages;
            int priceRound = 0;
            int priceDelta = 0;
            for (int i = 0; i < defaultPackages.Length; ++i)
            {
                var defaultPack = defaultPackages[i];
                var currentPack = mManager.PackageIdentifierToPackageInfo(defaultPack.PackageIdentifier.getString(), false);
                if (currentPack.Currency == defaultPack.Currency)//USD
                {
                    if (currentPack.Price % 1 == 0)
                    {
                        ++priceRound;
                    }
                    priceDelta += Mathf.RoundToInt((float)currentPack.Price - (float)defaultPack.Price);
                }
            }

            if (defaultPackages.Length == priceRound || (priceDelta / defaultPackages.Length) >= 8)//if Price in USD is not XX.99 but is only has XX. or Delta change with default config is too big
            {
                IAP.IAPManager.FlagAsTampered();
            }

            if (EventOnIAPPackRefreshed != null) EventOnIAPPackRefreshed();
        }

        public override bool IsBoughtPackage(string productID)
        {
            if (SIS.IAPManager.controller == null ||
                SIS.IAPManager.controller.products == null ||
                SIS.IAPManager.controller.products.WithID(productID) == null) return false;
            return SIS.IAPManager.controller.products.WithID(productID).hasReceipt;
        }

        public override object GetProductInfo(string productID)
        {
            if (SIS.IAPManager.controller == null ||
                SIS.IAPManager.controller.products == null ||
                SIS.IAPManager.controller.products.WithID(productID) == null) return null;
            return SIS.IAPManager.controller.products.WithID(productID);
        }
#endif
    }
}