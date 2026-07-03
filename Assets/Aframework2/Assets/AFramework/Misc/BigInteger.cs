using UnityEngine;
using System.Linq;
using System.Collections;

namespace AFramework
{
#pragma warning disable 0660, 0661
    [System.Serializable]
    public class BigInteger
    {
        //a = m*10^e note: 1.0f <=m < 10.0f
        public int e;
        public float m;

        public BigInteger()
        {
            e = 0;
            m = 0.0f;
        }

        public BigInteger(BigInteger other)
        {
            e = other.e;
            m = other.m;
        }

        public BigInteger(float _m, int _e)
        {
            e = _e;
            m = _m;
            Normalize();
        }

        public BigInteger(long l)
        {
            if (l == 0)
            {
                e = 0;
                m = 0.0f;
            }
            else
            {
                e = (int)System.Math.Floor(System.Math.Log10(Mathf.Abs(l)));
                m = (float)(l / System.Math.Pow(10.0, e));
            }
        }

        public BigInteger(double d)
        {
            if (d == 0)
            {
                e = 0;
                m = 0.0f;
            }
            else
            {
                e = (int)System.Math.Floor(System.Math.Log10(d));
                m = (float)(d / System.Math.Pow(10.0, e));
            }
        }

        public BigInteger(string str)
        {
            int floatLength = str.Length;//123.456 -> 7

            int intLength = str.IndexOf('.');//123.456 -> 3
            if (intLength < 0)
            {
                intLength = str.Length;//123456
            }

            int LogIndex = str.IndexOf('+'); //123.456e+10 -> 8
            if (LogIndex >= 0)
            {
                floatLength = LogIndex - 1;//123.456e+10 -> 7
                e = int.Parse(str.Substring(LogIndex + 1, str.Length - LogIndex - 1));
            }
            else
            {
                e = intLength - 1;
            }

            m = float.Parse(str.Substring(0, System.Math.Min(floatLength, 10))) / (float)System.Math.Pow(10.0f, System.Math.Min(intLength - 1, 10));
        }

        void Normalize()
        {
            if (m == 0.0f)
            {
                e = 0;
                return;
            }
            int _e = Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(m)));
            if (_e != 0)
            {
                e = e + _e;
                m = m / Mathf.Pow(10, _e);
            }
        }

        //operator
        public static BigInteger operator -(BigInteger a)
        {
            return new BigInteger(-a.m, a.e);
        }
        public static BigInteger operator -(BigInteger a, BigInteger b)
        {
            var powNum = a.e - b.e;
            if (powNum > 30)
            {
                return new BigInteger(a);
            }
            return new BigInteger(a.m * Mathf.Pow(10, powNum) - b.m, b.e);
        }

        public static BigInteger operator +(BigInteger a, BigInteger b)
        {
            var powNum = a.e - b.e;
            if (powNum > 30)
            {
                return new BigInteger(a);
            }
            return new BigInteger(a.m * Mathf.Pow(10, powNum) + b.m, b.e);
        }

        public static BigInteger operator *(BigInteger a, BigInteger b)
        {
            return new BigInteger(a.m * b.m, a.e + b.e);
        }

        public static BigInteger operator /(BigInteger a, BigInteger b)
        {
            return new BigInteger(a.m / b.m, a.e - b.e);
        }

        public static bool operator >(BigInteger a, BigInteger b)
        {
            BigInteger sub = a - b;
            return sub.m > 0.0f;
        }

        public static bool operator <(BigInteger a, BigInteger b)
        {
            BigInteger sub = a - b;
            return sub.m < 0.0f;
        }

        public static bool operator >=(BigInteger a, BigInteger b)
        {
            BigInteger sub = a - b;
            return sub.m >= 0.0f;
        }

        public static bool operator <=(BigInteger a, BigInteger b)
        {
            BigInteger sub = a - b;
            return sub.m <= 0.0f;
        }

        public static bool operator ==(BigInteger a, BigInteger b)
        {
            BigInteger sub = a - b;
            return sub.m == 0.0f || sub.e < 0;
        }

        public static bool operator !=(BigInteger a, BigInteger b)
        {
            return !(a == b);
        }


        public static implicit operator BigInteger(int i)
        {
            return new BigInteger((long)i);
        }

        public static implicit operator BigInteger(float f)
        {
            return new BigInteger((double)f);
        }

        public static implicit operator BigInteger(double d)
        {
            return new BigInteger(d);
        }

        public static implicit operator BigInteger(long i)
        {
            return new BigInteger(i);
        }

        public static implicit operator BigInteger(string str)
        {
            return new BigInteger(str);
        }

        public float ToFloat()
        {
            return m * Mathf.Pow(10, e);
        }

        public int ToInt()
        {
            return Mathf.RoundToInt(m * Mathf.Pow(10, e));
        }

        static string[] sDisplayPostfix = { "", "K", "M", "B", "T", "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll", "mm", "nn", "oo", "pp", "qq", "rr", "ss", "tt", "uu", "vv", "ww", "xx", "yy", "zz" };

        public string ToDisplayString(bool onlyInt = false)
        {
            if (e < 0) return "0";

            if (e < 3)
            {
                float floatValue = ToFloat();

                if(onlyInt || floatValue == (int)floatValue)
                {
                    return ((int)floatValue).ToString();
                }

                if(floatValue >= 100f)
                {
                    return (Mathf.RoundToInt(floatValue * 10) / 10.0f).ToString();
                }
                return (Mathf.RoundToInt(floatValue * 100) / 100.0f).ToString();
            }
            else if (e < sDisplayPostfix.Length * 3)
            {
                float tmp = m * Mathf.Pow(10, e % 3);
                if(tmp >= 100f)
                {
                    return (Mathf.Floor(tmp * 10) / 10) + sDisplayPostfix[e / 3];
                }
                
                return (Mathf.Floor(tmp * 100) / 100) + sDisplayPostfix[e / 3];
            }
            return m.ToString("F") + "E" + e.ToString();
        }

        public string ToShortDisplayString()
        {
            if (e < 0) return "0";

            if (e < 3)
            {
                return ((int)ToFloat()).ToString();
            }
            else if (e < sDisplayPostfix.Length * 3)
            {
                return (Mathf.Floor(m * Mathf.Pow(10, e % 3) * 10) / 10) + sDisplayPostfix[e / 3];
            }
            return m.ToString("F") + "E" + e.ToString();
        }


        ///Math
        public static BigInteger Abs(BigInteger a)
        {
            return new BigInteger(Mathf.Abs(a.m), a.e);
        }

        const int PowLimit = 100;
        public static BigInteger Pow(BigInteger a, int x)
        {
            if (x < 0)
            {
                return 1 / Pow(a, -x);
            }

            if (x == 0) return new BigInteger(1);

            if(x > PowLimit) //Result: a^x = a^(PowLimit*(x/PowLimit)+x%PowLimit)
            {
                BigInteger aPowLimit = Pow(a, PowLimit);
                int alpha = x % PowLimit;
                int beta = x / PowLimit;
                BigInteger factor = 1;

                if(alpha > 0)
                {
                    factor = Pow(a, alpha);
                }

                return Pow(aPowLimit, beta) * factor;
            }
            return new BigInteger(Mathf.Pow(a.m, x), a.e * x);
        }

        public static BigInteger Pow(BigInteger a, float x)
        {
            if (x < 0)
            {
                return 1 / Pow(a, -x);
            }

            if (x == 0.0f) return new BigInteger(1);

            BigInteger ret = Pow(a, (int)Mathf.Round(x));

            float alpha = x - Mathf.Round(x);

            ret = ret * new BigInteger(Mathf.Pow(a.m, alpha), (int)Mathf.Round(a.e * alpha));

            return ret;
        }

        public static BigInteger Pow(BigInteger a, BigInteger b)
        {
            return Pow(a, b.ToFloat());
        }
    }
}

