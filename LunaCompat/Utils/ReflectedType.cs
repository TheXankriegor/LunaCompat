using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

namespace LunaCompat.Utils
{
    internal class ReflectedType
    {
        #region Fields

        private readonly Dictionary<string, MethodInfo> _methods;
        private readonly Dictionary<string, FieldInfo> _fields;
        private readonly Dictionary<string, PropertyInfo> _properties;

        #endregion

        #region Constructors

        public ReflectedType(Type type)
        {
            Type = type;

            _methods = new Dictionary<string, MethodInfo>();
            _fields = new Dictionary<string, FieldInfo>();
            _properties = new Dictionary<string, PropertyInfo>();
        }

        public ReflectedType(string typeName)
            : this(AccessTools.TypeByName(typeName))
        {
        }

        #endregion

        #region Properties

        public Type Type { get; }

        #endregion

        #region Public Methods

        public MethodInfo Method(string methodName)
        {
            if (!_methods.TryGetValue(methodName, out var method))
            {
                method = AccessTools.Method(Type, methodName);
                _methods.Add(methodName, method);
            }

            return method;
        }

        public object Invoke(string methodName, object instance, object[] args)
        {
            return Method(methodName).Invoke(instance, args);
        }

        public object GetField(string fieldName, object instance)
        {
            return Field(fieldName).GetValue(instance);
        }

        public void SetField(string fieldName, object instance, object value)
        {
            Field(fieldName).SetValue(instance, value);
        }

        public object GetProperty(string propertyName, object instance)
        {
            return Property(propertyName).GetValue(instance);
        }

        public void SetProperty(string propertyName, object instance, object value)
        {
            Property(propertyName).SetValue(instance, value);
        }

        #endregion

        #region Non-Public Methods

        private FieldInfo Field(string fieldName)
        {
            if (!_fields.TryGetValue(fieldName, out var field))
            {
                field = AccessTools.Field(Type, fieldName);
                _fields.Add(fieldName, field);
            }

            return field;
        }

        private PropertyInfo Property(string propertyName)
        {
            if (!_properties.TryGetValue(propertyName, out var property))
            {
                property = AccessTools.Property(Type, propertyName);
                _properties.Add(propertyName, property);
            }

            return property;
        }

        #endregion
    }
}
