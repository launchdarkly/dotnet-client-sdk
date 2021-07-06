using System;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client
{
    /// <summary>
    /// Convenience methods that extend the <see cref="ILdClient"/> interface.
    /// </summary>
    /// <remarks>
    /// These are implemented outside of <see cref="ILdClient"/> and <see cref="LdClient"/> because they do not
    /// rely on any implementation details of <see cref="LdClient"/>; they are decorators that would work equally
    /// well with a stub or test implementation of the interface.
    /// </remarks>
    public static class ILdClientExtensions
    {
        /// <summary>
        /// Equivalent to <see cref="ILdClient.StringVariation(string, string)"/>, but converts the
        /// flag's string value to an enum value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag has a value that is not one of the allowed enum value names, or is not a string,
        /// <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the enum type</typeparam>
        /// <param name="client">the client instance</param>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag (as an enum value)</param>
        /// <returns>the variation for the given user, or <c>defaultValue</c> if the flag cannot
        /// be evaluated or does not have a valid enum value</returns>
        public static T EnumVariation<T>(this ILdClient client, string key, T defaultValue) where T : struct, Enum
        {
            var stringVal = client.StringVariation(key, defaultValue.ToString());
            if (stringVal != null)
            {
                if (Enum.TryParse<T>(stringVal, out var enumValue))
                {
                    return enumValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Equivalent to <see cref="ILdClient.StringVariationDetail(string, string)"/>, but converts the
        /// flag's string value to an enum value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag has a value that is not one of the allowed enum value names, or is not a string,
        /// <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the enum type</typeparam>
        /// <param name="client">the client instance</param>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="defaultValue">the default value of the flag (as an enum value)</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        public static EvaluationDetail<T> EnumVariationDetail<T>(this ILdClient client,
            string key, T defaultValue) where T : struct, Enum
        {
            var stringDetail = client.StringVariationDetail(key, defaultValue.ToString());
            if (!stringDetail.IsDefaultValue && stringDetail.Value != null)
            {
                if (Enum.TryParse<T>(stringDetail.Value, out var enumValue))
                {
                    return new EvaluationDetail<T>(enumValue, stringDetail.VariationIndex, stringDetail.Reason);
                }
                return new EvaluationDetail<T>(defaultValue, stringDetail.VariationIndex, EvaluationReason.ErrorReason(EvaluationErrorKind.WrongType));
            }
            return new EvaluationDetail<T>(defaultValue, stringDetail.VariationIndex, stringDetail.Reason);
        }
    }
}
