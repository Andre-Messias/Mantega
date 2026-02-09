namespace Mantega.Core.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Mantega.Diagnostics;

    /// <summary>
    /// Provides utility methods for working with reflection, such as discovering types that implement specific
    /// interfaces.
    /// </summary>
    /// <remarks>This class is designed to simplify common reflection tasks, such as finding types
    /// that implement a specific generic interface. It operates on the current application domain and handles
    /// scenarios where some assemblies may not be fully loadable due to reflection issues.</remarks>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Finds all non-abstract classes in the current application domain that implement a specified generic
        /// interface type.
        /// </summary>
        /// <remarks>This method scans all loaded assemblies in the current application domain to
        /// find classes that implement the specified generic interface. Assemblies that cannot be fully loaded due
        /// to reflection issues are skipped.</remarks>
        /// <param name="genericInterfaceType">The generic interface type definition to search for. This must be an interface and a generic type
        /// definition.</param>
        /// <returns>A list of <see cref="Type"/> objects representing the classes that implement the specified generic
        /// interface type. The list will be empty if no matching classes are found.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="genericInterfaceType"/> is not an interface or is not a generic type
        /// definition.</exception>
        public static List<Type> FindAllClassesOfInterface(Type genericInterfaceType)
        {
            if (!genericInterfaceType.IsInterface || !genericInterfaceType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("O tipo fornecido deve ser uma definição de interface genérica.", nameof(genericInterfaceType));
            }

            // All loaded assemblies in the current application domain
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    // To avoid issues with assemblies that cannot be loaded, we catch the ReflectionTypeLoadException
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Enumerable.Empty<Type>();
                    }
                })
                .Where(type =>
                    !type.IsAbstract &&                                       // Non abstract classes only
                    !type.IsInterface &&                                      // Non interfaces only
                    type.GetInterfaces().Any(i =>                             // Get all implemented interfaces and check if any of them...
                        i.IsGenericType &&                                    // Generic interface?
                        i.GetGenericTypeDefinition() == genericInterfaceType  // Is the generic type definition the same as the one we're looking for?
                    )
                ).ToList();
        }

        /// <summary>
        /// Determines whether the specified <paramref name="sourceObject"/> can be converted to the type of 
        /// <paramref name="targetObject"/> and, if possible, provides the converted value.
        /// </summary>
        /// <remarks>This method attempts to convert the <paramref name="sourceObject"/> to the
        /// type of  <paramref name="targetObject"/> using the following rules: <list type="bullet"> <item>If
        /// <paramref name="sourceObject"/> is <see langword="null"/>, the method returns  <see langword="true"/> if
        /// the target type is nullable; otherwise, it returns <see langword="false"/>.</item> <item>If <paramref
        /// name="sourceObject"/> is already of the target type or a compatible type,  the method returns <see
        /// langword="true"/> and assigns <paramref name="sourceObject"/> to  <paramref name="converted"/>.</item>
        /// <item>If the conversion is possible using <see cref="Convert.ChangeType(object, Type)"/>, the method 
        /// returns <see langword="true"/> and assigns the converted value to <paramref name="converted"/>.</item>
        /// <item>If none of the above conditions are met, the method returns <see langword="false"/>.</item>
        /// </list></remarks>
        /// <param name="sourceObject">The object to be converted. Can be <see langword="null"/>.</param>
        /// <param name="targetObject">An object whose type is used as the target type for the conversion.  Cannot be <see langword="null"/>.</param>
        /// <param name="converted">When this method returns, contains the converted value if the conversion  was successful; otherwise,
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the <paramref name="sourceObject"/> can be converted to the type of  <paramref
        /// name="targetObject"/>; otherwise, <see langword="false"/>.</returns>
        public static bool CanConvert(object sourceObject, object targetObject, out object converted)
        {
            Validations.ValidateNotNull(sourceObject);
            Validations.ValidateNotNull(targetObject);

            converted = null;

            if (targetObject == null)
            {
                return false;
            }

            Type targetType = targetObject.GetType();

            // Null source handling
            if (sourceObject == null)
            {
                // Non-nullable value types cannot be assigned null
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    return false;
                }

                // converted is already null
                return true;
            }

            Type sourceType = sourceObject.GetType();

            // Is already the correct type
            if (targetType.IsAssignableFrom(sourceType))
            {
                converted = sourceObject;
                return true;
            }

            // Convertible via IConvertible
            try
            {
                converted = Convert.ChangeType(sourceObject, targetType);
                return true;
            }
            catch (Exception)
            {
                // Ignora as exceções (InvalidCastException, FormatException, OverflowException) 
                // e tenta o próximo método.
            }

            // Unable to convert
            return false;
        }
    }
}