using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace WNP78
{
    public static class ReflectionUtils
    {
        public static string ToArrayString(this Array a)
        {
            string s = "{ ";
            foreach (object v in a)
            {
                s += v.ToString() + ", ";
            }
            s += "}";
            return s;
        }
        public static BindingFlags allBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static Dictionary<string, Type> cachedTypes;
        public static Dictionary<Type, Dictionary<string, PropertyInfo>> cachedProperties;
        public static Dictionary<Type, Dictionary<string, MethodInfo>> cachedMethods;
        public static Dictionary<Type, Dictionary<string, FieldInfo>> cachedFields;
        public static List<Assembly> assemblies = new List<Assembly>();
        static private bool inited = false;
        public static Type GetTypeFast(string name)
        {
            Init();
            if (cachedTypes.ContainsKey(name))
            {
                return cachedTypes[name];
            }

            Type t = null;
            foreach (var asm in assemblies)
            {
                t = asm.GetType(name);
                if (t != null) { break; }
            }
            cachedTypes.Add(name, t);
            return t;
        }

        public static Type GetType(string name)
        {
            return GetTypeFast(name);
        }
        public static object Call(this object obj, string name, params object[] args)
        {
            return GetMethodFast(obj.GetType(), name).Invoke(obj, args);
        }
        public static object CallO(this object obj, string name, params object[] args)
        {
            return GetMethodFast(obj.GetType(), name, args).Invoke(obj, args);
        }
        public static object GetP(this object obj, string name, params object[] index)
        {
            return GetPropFast(obj.GetType(), name).GetValue(obj, index);
        }
        public static void SetP(this object obj, string name, object value, params object[] index)
        {
            GetPropFast(obj.GetType(), name).SetValue(obj, value, index);
        }
        public static object GetF(this object obj, string name)
        {
            return GetFieldFast(obj.GetType(), name).GetValue(obj);
        }
        public static void SetF(this object obj, string name, object value)
        {
            GetFieldFast(obj.GetType(), name).SetValue(obj, value);
        }
        public static object GetSP(this Type t, string name, params object[] index)
        {
            return GetPropFast(t, name).GetValue(null, index);
        }
        public static void SetSP(this Type t, string name, object value, params object[] index)
        {
            GetPropFast(t, name).SetValue(null, value, index);
        }
        public static object GetSF(this Type t, string name)
        {
            return GetFieldFast(t, name).GetValue(null);
        }
        public static void SetSF(this Type t, string name, object value)
        {
            GetFieldFast(t, name).SetValue(null, value);
        }
        public static object CallS(this Type t, string name, params object[] args)
        {
            return GetMethodFast(t, name).Invoke(null, args);
        }
        public static object CallSO(this Type t, string name, params object[] args)
        {
            return GetMethodFast(t, name, args).Invoke(null, args);
        }
        public static PropertyInfo GetPropFast(Type tp, string name)
        {
            Init();
            if (cachedProperties.ContainsKey(tp))
            {
                if (cachedProperties[tp].ContainsKey(name))
                {
                    return cachedProperties[tp][name];
                }
                PropertyInfo p = tp.GetProperty(name, allBindingFlags);
                cachedProperties[tp].Add(name, p);
                return p;
            }
            cachedProperties[tp] = new Dictionary<string, PropertyInfo>();
            PropertyInfo pt = tp.GetProperty(name, allBindingFlags);
            cachedProperties[tp].Add(name, pt);
            return pt;
        }
        public static MethodInfo GetMethodFast(Type tp, string name, object[] args = null)
        {
            Init();
            if (args != null)
            {
                return tp.GetMethod(name, allBindingFlags, null, args.Select(a => a.GetType()).ToArray(), null);
            }
            if (cachedMethods.ContainsKey(tp))
            {
                if (cachedMethods[tp].ContainsKey(name))
                {
                    return cachedMethods[tp][name];
                }
                MethodInfo m = tp.GetMethod(name, allBindingFlags);
                cachedMethods[tp].Add(name, m);
                return m;
            }
            cachedMethods.Add(tp, new Dictionary<string, MethodInfo>());
            MethodInfo mp = tp.GetMethod(name, allBindingFlags);
            cachedMethods[tp].Add(name, mp);
            return mp;
        }
        public static FieldInfo GetFieldFast(Type tp, string name)
        {
            Init();
            if (cachedFields.ContainsKey(tp))
            {
                if (cachedFields[tp].ContainsKey(name))
                {
                    return cachedFields[tp][name];
                }
                FieldInfo f = tp.GetField(name, allBindingFlags);
                cachedFields[tp].Add(name, f);
                return f;
            }
            cachedFields.Add(tp, new Dictionary<string, FieldInfo>());
            FieldInfo fp = tp.GetField(name, allBindingFlags);
            cachedFields[tp].Add(name, fp);
            return fp;
        }
        public static void Init()
        {
            if (!inited)
            {
                inited = true;
                cachedTypes = new Dictionary<string, Type>();
                cachedProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
                cachedMethods = new Dictionary<Type, Dictionary<string, MethodInfo>>();
                cachedFields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
                assemblies.Add(GetAssembly("SimpleRockets2"));
                assemblies.Add(GetAssembly("XmlLayout"));
            }
        }
        public static Assembly GetAssembly(string name)
        {
            Init();
            Assembly[] asmbs = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in asmbs)
            {
                if (a.GetName().Name == name)
                {
                    return a;
                }
            }
            return null;
        }
    }
}