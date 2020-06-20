using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace aliyun_ddns.Common
{
    /// <summary>
    /// 实例反射创建类。
    /// </summary>
    public static class InstanceCreator
    {
        public static IEnumerable<T> Create<T>(Func<Type, bool> check = null)
        {
            var target = typeof(T);
            try
            {
                List<T> instances = new List<T>();
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in a.GetTypes())
                    {
                        try
                        {
                            if (target.IsAssignableFrom(t) &&
                                t.IsAbstract == false &&
                                t.IsInterface == false &&
                                (check == null || check(t)))
                            {
                                try
                                {
                                    T i = (T)Activator.CreateInstance(t);
                                    instances.Add(i);
                                }
                                catch (Exception e)
                                {
                                    Log.Print($"创建接口{ typeof(T) }的实例时，无法实例化{ t }：\n{ e }");
                                }
                            }
                        }
                        catch
                        {
                            //do noting;
                        }
                    }
                }

                return instances;
            }
            catch (Exception e)
            {
                Log.Print($"创建接口{ typeof(T) }的实例时出现异常：\n{ e }");
                return new List<T>();
            }
        }
    }
}
