using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
    public interface IRemoteConfigData
    {
        public bool GetBooleanValue();
        public IEnumerable<byte> GetByteArrayValue();
        public double GetDoubleValue();
        public long GetLongValue();
        public string GetStringValue();
    }
}