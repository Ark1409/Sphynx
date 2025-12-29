using Sphynx.Core;

namespace Sphynx.Utils
{
    public static class SphynxErrorInfoExtensions
    {
        public static SphynxErrorInfo<T> ToErrorInfo<T>(this T val) where T : notnull
        {
            return new(val);
        }

        public static ref SphynxErrorInfo<T> ThrowIfError<T>(this ref SphynxErrorInfo<T> err, string? message = null)
        {
            if (err.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (message != null)
                    throw new SphynxErrorException(err.ErrorCode, message);
                else
                    throw new SphynxErrorException(err.ErrorCode);
            }

            return ref err;
        }

        public static T ValueOrThrow<T>(this in SphynxErrorInfo<T> err, string? message = null)
        {
            if (err.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                if (message != null)
                    throw new SphynxErrorException(err.ErrorCode, message);
                else
                    throw new SphynxErrorException(err.ErrorCode);
            }

            return err.Data!;
        }

        public static T ValueOrDefault<T>(this in SphynxErrorInfo<T> err) where T : struct
        {
            return err.Data;
        }

        public static T ValueOrDefault<T>(this in SphynxErrorInfo<T?> err) where T : struct
        {
            return err.Data ?? default;
        }

        public static T ValueOrDefault<T>(this in SphynxErrorInfo<T> err, T defaultValue)
        {
            return err.Data ?? defaultValue;
        }
    }
}
