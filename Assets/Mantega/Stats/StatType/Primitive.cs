using System;
using UnityEngine;

namespace Mantega.Stats
{
    using Mantega.Core.Reflection;

    public static partial class StatType
    {
        /// <summary>
        /// Provides functionality for wrapping objects in a generic wrapper type
        /// </summary>
        /// <remarks>The <see cref="WrapperManager"/> class includes a generic wrapper structure and
        /// methods for creating instances of the wrapper dynamically. This can be useful for scenarios where objects
        /// need to be encapsulated in a uniform type for processing or serialization</remarks>
        private sealed class WrapperManager
        {
            /// <summary>
            /// Defines a contract for a wrapper that provides access to an underlying value
            /// </summary>
            /// <remarks>This interface is typically used to abstract access to a value, allowing the
            /// implementation  to encapsulate the value and provide additional behavior if needed</remarks>
            public interface IWrapper
            {
                /// <summary>
                /// Retrieves the current value associated with the object
                /// </summary>
                /// <returns>The current value as an <see cref="object"/>. The returned value may be <see langword="null"/> if no
                /// value is set</returns>
                object GetValue();
            }

            /// <summary>
            /// Represents a wrapper for a value of type <typeparamref name="T"/>
            /// </summary>
            /// <remarks>This struct is designed to encapsulate a value of type <typeparamref
            /// name="T"/> and provide additional functionality, such as retrieving the value as an object</remarks>
            /// <typeparam name="T">The type of the value being wrapped</typeparam>
            [Serializable]
            public struct Wrapper<T> : IWrapper
            {
                /// <summary>
                /// The content being wrapped
                /// </summary>
                [SerializeField] public T Content;

                /// <summary>
                /// Initializes a new instance of the <see cref="Wrapper{T}"/> class with the specified value
                /// </summary>
                /// <param name="value">The value to be wrapped by this instance</param>
                public Wrapper(T value)
                {
                    Content = value;
                }

                /// <summary>
                /// Retrieves the value of the content
                /// </summary>
                /// <returns>The value of the content as an <see cref="object"/>. Returns <see langword="null"/> if the content
                /// is not set</returns>
                public readonly object GetValue()
                {
                    return Content;
                }
            }

            /// <summary>
            /// Creates a generic wrapper instance for the specified object
            /// </summary>
            /// <remarks>The method dynamically creates an instance of a generic wrapper type
            /// <see cref="Wrapper{T}"/> using the runtime type of the provided object. This allows the object to be
            /// encapsulated in a strongly-typed wrapper at runtime</remarks>
            /// <param name="obj">The object to be wrapped. Can be of any type</param>
            /// <returns>A generic wrapper instance of type <see cref="Wrapper{T}"/>, where <c>T</c> is the runtime type of
            /// <paramref name="obj"/> Returns <see langword="null"/> if <paramref name="obj"/> is <see
            /// langword="null"/></returns>
            public static object WrapperFromObject(object obj)
            {
                if (obj == null) return null;
                Type genericWrapperType = typeof(Wrapper<>).MakeGenericType(obj.GetType());
                object wrapper = Activator.CreateInstance(genericWrapperType, new object[] { obj });
                return wrapper;
            }
        }

        /// <summary>
        /// Represents a primitive stat type that holds a value of any object type
        /// </summary>
        /// <remarks>The <see cref="Primitive"/> class provides functionality to store and manage a value
        /// of any object type, with support for applying changes through the <see cref="PrimitiveChange"/> type. The
        /// value can be wrapped using a wrapper object, if applicable, to provide additional behavior or
        /// processing</remarks>
        [Serializable]
        public class Primitive : StatTypeBase<object, PrimitiveChange>
        {
            [SerializeReference, SerializeField] private object _value = null;
            public override object Value
            {
                get
                {
                    if (_value is WrapperManager.IWrapper wrapper)
                        return wrapper.GetValue();

                    return _value;
                }
            }

            public Primitive() => _value = WrapperManager.WrapperFromObject(null);

            public Primitive(object content = null)
            {
                _value = WrapperManager.WrapperFromObject(content);
            }

            protected override void ApplyChangeLogic(PrimitiveChange change)
            {
                if (ReflectionUtils.CanConvert(change.Value, Value, out object converted))
                    _value = WrapperManager.WrapperFromObject(converted);
                else
                    Debug.LogWarning($"Failed to convert {change.Value?.GetType()} to {Value?.GetType()}, no change was made");
            }

            public override string ToString()
            {
                if (_value == null) return "Null";
                return _value.ToString();
            }
        }

        [Serializable]
        public class PrimitiveChange : StatTypeChange
        {
            [SerializeReference] public object _value;
            public object Value
            {
                get
                {
                    if (_value is WrapperManager.IWrapper wrapper)
                        return wrapper.GetValue();

                    return _value;
                }
            }
        }

    }
}