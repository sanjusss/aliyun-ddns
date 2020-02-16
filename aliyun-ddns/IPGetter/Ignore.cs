using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class Ignore : Attribute
    {
        public static bool CheckType(Type t)
        {
            return t.GetCustomAttributes(typeof(Ignore), false).Length == 0;
        }
    }
}
