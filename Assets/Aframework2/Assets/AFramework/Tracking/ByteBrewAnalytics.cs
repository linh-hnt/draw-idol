#if USE_BYTE_BREW
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ByteBrewSDK;

namespace AFramework
{
    namespace Analytics
    {
        public class ByteBrewAnalytics : IAnalytic
        {
            public bool InitSuccess { get; set; }

            public void Init(params string[] args)
            {
                ByteBrew.InitializeByteBrew();
            }

            public void TrackEvent(string eventName)
            {
                ByteBrew.NewCustomEvent(eventName);
            }

            public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            {
                ByteBrew.NewCustomEvent(eventName, ParseEventValues(parameters));
                //UnityEngine.Debug.LogError($"ByteBrewAnalytics TrackEvent {eventName}, {parameters.Count} \n{ParseEventValues(parameters)}");
            }

            public void ApplicationOnPause(bool Paused)
            {
            }

            public string ParseEventValues(Dictionary<string, object> values)
            {
                var parsedValueSTR = "";
                foreach (var keyPair in values)
                {
                    if (keyPair.Value is double && keyPair.Key.Equals("revenue"))
                    {
                        double value = (double)keyPair.Value;

                        if (value < 1)
                        {
                            uint valueInt = (uint)(value * 1000000);
                            string valueStr = valueInt.ToString();
                            parsedValueSTR += String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}={1};", keyPair.Key, valueStr);
                            continue;
                        }
                    }

                    parsedValueSTR += String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}={1};", keyPair.Key, keyPair.Value);
                }
                return parsedValueSTR;
            }
        }
    }
}
#endif