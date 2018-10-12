﻿using System;
using System.Reflection;

using WebAssembly;

namespace WebAssembly.Browser.DOM
{

    public class DOMObject : IDisposable
    {
        static readonly JSObject domBrowserInterface = (JSObject)Runtime.GetGlobalObject("__WASM_DOM_BROWSER_INTERFACE__");

        public JSObject ManagedJSObject { get; private set; }

        public int JSHandle
        {
            get { return ManagedJSObject.JSHandle;  }
        }

        public DOMObject(JSObject jsObject)
        {
            ManagedJSObject = jsObject;
        }

        public DOMObject(string globalName)
        {
            ManagedJSObject = (JSObject)Runtime.GetGlobalObject(globalName);
        }

        protected object InvokeMethod(Type type, string methodName, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                Type argType = null;

                // All DOMObjects will need to pass the JSObject that they are associated with
                for (int a = 0; a < args.Length; a++)
                {
                    argType = args[a].GetType();
                    if (argType.IsSubclassOf(typeof(DOMObject)) || argType == typeof(DOMObject))
                    {
                        args[a] = ((DOMObject)args[a]).ManagedJSObject;
                    }
                }
            }
            var res = ManagedJSObject.Invoke(methodName, args);
            return UnWrapObject(type, res);
        }

        protected T InvokeMethod<T>(string methodName, params object[] args)
        {
            return (T)InvokeMethod(typeof(T), methodName, args);
        }

        protected T GetProperty<T>(string expr)
        {

            var propertyValue = ManagedJSObject.GetObjectProperty(expr);

            #if DEBUG
                    Console.WriteLine($"CS::DOMObject::GetProperty return type {propertyValue.GetType()}");
            #endif

            return UnWrapObject<T>(propertyValue);

        }

        protected void SetProperty<T>(string expr, T value, bool createIfNotExists = true, bool hasOwnProperty = false)
        {
            ManagedJSObject.SetObjectProperty(expr, value, createIfNotExists, hasOwnProperty);
        }

        object UnWrapObject(Type type, object obj)
        {

            if (type.IsSubclassOf(typeof(JSObject)) || type == typeof(JSObject))
            {

#if DEBUG
                Console.WriteLine($"CS::DOMObject::UnWrapObject::JSObject");
#endif
                var jsobjectconstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null, new Type[] { typeof(Int32) }, null);

                var jsobjectnew = jsobjectconstructor.Invoke(new object[] { (obj == null) ? -1 : obj });
                return jsobjectnew;

            }
            else if (type.IsSubclassOf(typeof(DOMObject)) || type == typeof(DOMObject))
            {

#if DEBUG
                Console.WriteLine($"CS::DOMObject::UnWrapObject::DOMObject");
#endif
                var jsobjectconstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null, new Type[] { typeof(JSObject) }, null);

                var jsobjectnew = jsobjectconstructor.Invoke(new object[] { obj });
                return jsobjectnew;

            }
            else if (type.IsPrimitive || typeof(Decimal) == type)
            {

                // Make sure we handle null and undefined
                // have found this only on FireFox for now
                if (obj == null)
                {
                    return Activator.CreateInstance(type);
                }

                return Convert.ChangeType(obj, type);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {

                var conv = System.ComponentModel.TypeDescriptor.GetConverter(type);

                if (!conv.CanConvertFrom(obj.GetType()))
                {
                    throw new NotSupportedException();
                }

                if (conv.IsValid(obj))
                {
                    return conv.ConvertFrom(obj);
                }

                throw new InvalidCastException();
            }
            else if (type.IsEnum)
            {
                return Runtime.EnumFromExportContract(type, obj);
            }
            else if (type == typeof(string))
            {
                return obj;
            }
            else if (type is object)
            {
                // called via invoke
                if (obj == null)
                    return (object)null;
                else
                    throw new NotSupportedException($"Type {type} not supported yet.");

            }
            else
            {
                throw new NotSupportedException($"Type {type} not supported yet.");
            }


        }

        T UnWrapObject<T>(object obj)
        {

            return (T)UnWrapObject(typeof(T), obj);
        }

        protected void AddJSEventListener(string eventName, object eventDelegate, int uid)
        {

#if DEBUG
            Console.WriteLine($"CS::DOMObject::AddJSEventListener {eventName} value: {eventDelegate}");
#endif
            domBrowserInterface.Invoke("mono_wasm_add_js_event_listener", ManagedJSObject, eventName, eventDelegate, uid);

        }

        protected void SetJSStyleAttribute(string qualifiedName, string value)
        {
#if DEBUG
            Console.WriteLine($"CS::DOMObject::SetJSStyleAttribute {qualifiedName} value: {value}");
#endif
            domBrowserInterface.Invoke("mono_wasm_set_js_style_attribute", ManagedJSObject, qualifiedName, value);

        }

        protected object GetJSStyleAttribute(string qualifiedName)
        {
#if DEBUG
            Console.WriteLine($"CS::DOMObject::GetJSStyleAttribute {qualifiedName}");
#endif
            return domBrowserInterface.Invoke("mono_wasm_get_js_style_attribute", ManagedJSObject, qualifiedName);

        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {

            if (disposing)
            {

                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            ManagedJSObject?.Dispose();
            ManagedJSObject = null;
        }
    }

}