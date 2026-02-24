namespace Mantega.Core
{
    using System;

    /// <summary>
    /// Represents a type without any meaningful value.
    /// </summary>
    /// <remarks>The Unit type is commonly used in scenarios where a method or operation conceptually returns
    /// no value, but a value type is required. It serves a similar purpose to void, but as a first-class value that can
    /// be used in generic type parameters, asynchronous operations, or functional programming constructs. All instances
    /// of Unit are considered equal.</remarks>
    public readonly struct Unit : IEquatable<Unit>
    {
        /// <summary>
        /// Represents the default value of the Unit type.
        /// </summary>
        /// <remarks>This static field provides a canonical instance of Unit, which can be used wherever a
        /// Unit value is required. The Unit type is typically used to indicate the absence of a meaningful value,
        /// similar to void, but as a first-class value type.</remarks>
        public static readonly Unit Default = new();

        /// <summary>
        /// Determines whether the current instance is equal to another instance of the same type.
        /// </summary>
        /// <remarks>This method always returns <see langword="true"/>, indicating that all instances are
        /// considered equal. This is typically used for types that represent a single, unique value.</remarks>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public readonly bool Equals(Unit other) => true;

        /// <summary>
        /// Determines whether the specified object is an instance of the Unit type.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified object is an instance of Unit; otherwise, <see langword="false"/>.</returns>
        public readonly override bool Equals(object obj) => obj is Unit;

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <remarks>This implementation always returns 0. As a result, using instances of this type as
        /// keys in hash-based collections may lead to poor performance due to hash collisions.</remarks>
        /// <returns>A hash code for the current object.</returns>
        public readonly override int GetHashCode() => 0;

        /// <summary>
        /// Determines whether two Unit instances are considered equal.
        /// </summary>
        /// <remarks>This operator always returns <see langword="true"/>, indicating that all Unit instances are considered
        /// equal.</remarks>
        /// <param name="left">The first Unit instance to compare.</param>
        /// <param name="right">The second Unit instance to compare.</param>
        /// <returns>Always returns <see langword="true"/>, indicating that all Unit instances are considered equal.</returns>
        public static bool operator ==(Unit left, Unit right) => true;

        /// <summary>
        /// Determines whether two Unit instances are not equal.
        /// </summary>
        /// <remarks>This operator always returns <see langword="false"/>, as all Unit instances are treated as equal by
        /// definition. Use this operator to compare Unit values for inequality.</remarks>
        /// <param name="left">The first Unit instance to compare.</param>
        /// <param name="right">The second Unit instance to compare.</param>
        /// <returns>Always returns <see langword="false"/>, indicating that all Unit instances are considered equal.</returns>
        public static bool operator !=(Unit left, Unit right) => false;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public readonly override string ToString() => "()";
    }
}