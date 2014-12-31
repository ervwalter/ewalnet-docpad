// Imported from Lokad.Shared, 2011-02-08

using System;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Improved version of the Result[T], that could serve as a basis for it.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <remarks>It is to be moved up-stream if found useful in other projects.</remarks>
    public class Result<TValue, TError> : IEquatable<Result<TValue, TError>>
    {
        readonly bool _isSuccess;
        readonly TValue _value;
        readonly TError _error;

        Result(bool isSuccess, TValue value, TError error)
        {
            _isSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        /// <summary>
        /// Creates the success result.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>result encapsulating the success value</returns>
        /// <exception cref="ArgumentNullException">if value is a null reference type</exception>
        public static Result<TValue, TError> CreateSuccess(TValue value)
        {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (null == value) throw new ArgumentNullException("value");
            // ReSharper restore CompareNonConstrainedGenericWithNull

            return new Result<TValue, TError>(true, value, default(TError));
        }

        /// <summary>
        /// Creates the error result.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns>result encapsulating the error value</returns>
        /// <exception cref="ArgumentNullException">if error is a null reference type</exception>
        public static Result<TValue, TError> CreateError(TError error)
        {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (null == error) throw new ArgumentNullException("error");
            // ReSharper restore CompareNonConstrainedGenericWithNull

            return new Result<TValue, TError>(false, default(TValue), error);
        }

        /// <summary>
        /// item associated with this result
        /// </summary>
        public TValue Value
        {
            get
            {
                if (!_isSuccess)
                    throw new InvalidOperationException("Dont access result on error. " + _error);

                return _value;
            }
        }

        /// <summary>
        /// Error message associated with this failure
        /// </summary>
        public TError Error
        {
            get
            {
                if (_isSuccess)
                    throw new InvalidOperationException("Dont access error on valid result.");
                
                return _error;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this result is valid.
        /// </summary>
        /// <value><c>true</c> if this result is valid; otherwise, <c>false</c>.</value>
        public bool IsSuccess
        {
            get { return _isSuccess; }
        }


        /// <summary>
        /// Performs an implicit conversion from <typeparamref name="TValue"/> to <see cref="Result{TValue,TError}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        /// <exception cref="ArgumentNullException">If value is a null reference type</exception>
        public static implicit operator Result<TValue, TError>(TValue value)
        {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (null == value) throw new ArgumentNullException("value");
            // ReSharper restore CompareNonConstrainedGenericWithNull
            return CreateSuccess(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <typeparamref name="TError"/> to <see cref="Result{TValue,TError}"/>.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns>The result of the conversion.</returns>
        /// <exception cref="ArgumentNullException">If value is a null reference type</exception>
        public static implicit operator Result<TValue, TError>(TError error)
        {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (null == error) throw new ArgumentNullException("error");
            // ReSharper restore CompareNonConstrainedGenericWithNull
            return CreateError(error);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Result<TValue, TError> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._isSuccess.Equals(_isSuccess) && Equals(other._value, _value) && Equals(other._error, _error);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        /// </exception>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Result<TValue, TError>)) return false;
            return Equals((Result<TValue, TError>) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = _isSuccess.GetHashCode();
// ReSharper disable CompareNonConstrainedGenericWithNull
                result = (result*397) ^ (_value != null ? _value.GetHashCode() : 1);
                result = (result*397) ^ (_error != null ? _error.GetHashCode() : 0);
// ReSharper restore CompareNonConstrainedGenericWithNull
                return result;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (!_isSuccess)
                return "<Error: '" + _error + "'>";

            return "<Value: '" + _value + "'>";
        }
    }
}