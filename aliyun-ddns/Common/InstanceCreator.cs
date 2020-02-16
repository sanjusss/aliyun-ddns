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
        static InstanceCreator()
        {
            //LoadReferencedAssembly();
        }

        /// <summary>
        /// 载入所有相关的程序集。
        /// </summary>
        private static void LoadReferencedAssembly()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var names = new HashSet<AssemblyName>(assemblies.Select(a => a.GetName()));
            foreach (var a in assemblies)
            {
                LoadReferencedAssembly(a, ref names);
            }
        }

        private static void LoadReferencedAssembly(Assembly assembly, ref HashSet<AssemblyName> names)
        {
            foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            {
                if (names.Contains(name) == false)
                {
                    names.Add(name);
                    LoadReferencedAssembly(Assembly.Load(name), ref names);
                }
            }
        }

        public static IEnumerable<T> Create<T>(Func<Type, bool> check = null)
        {
            try
            {
                List<T> instances = new List<T>();
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in a.GetTypes())
                    {
                        try
                        {
                            if (t.IsSubclassOf(typeof(T)) &&
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
