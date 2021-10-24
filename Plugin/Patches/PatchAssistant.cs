using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {

        public static class PatchAssistant
        {
            public static object GetProperty(object instance, string propertyName)
            {
                Type type = instance.GetType();
                foreach (PropertyInfo modifier in type.GetRuntimeProperties())
                {
                    if (modifier.Name.Contains(propertyName))
                    {
                        return modifier.GetValue(instance);
                    }
                }
                foreach (PropertyInfo modifier in type.GetProperties())
                {
                    if (modifier.Name.Contains(propertyName))
                    {
                        return modifier.GetValue(instance);
                    }
                }
                return null;
            }

            public static void SetProperty(object instance, string propertyName, object value)
            {
                Type type = instance.GetType();
                foreach (PropertyInfo modifier in type.GetRuntimeProperties())
                {
                    if (modifier.Name.Contains(propertyName))
                    {
                        modifier.SetValue(instance, value);
                        return;
                    }
                }
                foreach (PropertyInfo modifier in type.GetProperties())
                {
                    if (modifier.Name.Contains(propertyName))
                    {
                        modifier.SetValue(instance, value);
                        return;
                    }
                }
            }
            public static object GetField(object instance, string fieldName)
            {
                Type type = instance.GetType();
                foreach (FieldInfo modifier in type.GetRuntimeFields())
                {
                    if (modifier.Name.Contains(fieldName))
                    {
                        try
                        {
                            return modifier.GetValue(instance);
                        }
                        catch(Exception x)
                        {
                            Debug.LogWarning("Patch Assistant: Unable To GetValue Of '" + fieldName + "' From '" + instance + "'\r\n"+x);
                            return null;
                        }
                    }
                }
                foreach (FieldInfo modifier in type.GetFields())
                {
                    if (modifier.Name.Contains(fieldName))
                    {
                        try
                        {
                            return modifier.GetValue(instance);
                        }
                        catch (Exception x)
                        {
                            Debug.LogWarning("Patch Assistant: Unable To GetValue Of '" + fieldName + "' From '" + instance + "'\r\n"+x);
                            return null;
                        }
                    }
                }
                return null;
            }

            public static void SetField(object instance, string fieldName, object value)
            {
                Type type = instance.GetType();
                foreach (FieldInfo modifier in type.GetRuntimeFields())
                {
                    if (modifier.Name.Contains(fieldName))
                    {
                        modifier.SetValue(instance, value);
                        return;
                    }
                }
                foreach (FieldInfo modifier in type.GetFields())
                {
                    if (modifier.Name.Contains(fieldName))
                    {
                        modifier.SetValue(instance, value);
                        return;
                    }
                }
            }

            public static object UseMethod(object instance, string methodName, object[] parameters)
            {
                Type type = instance.GetType();
                foreach (MethodInfo modifier in type.GetRuntimeMethods())
                {
                    if (modifier.Name.Contains(methodName))
                    {
                        return modifier.Invoke(instance, parameters);
                    }
                }
                foreach (MethodInfo modifier in type.GetMethods())
                {
                    if (modifier.Name.Contains(methodName))
                    {
                        return modifier.Invoke(instance, parameters);
                    }
                }
                return null;
            }
        }
    }
}