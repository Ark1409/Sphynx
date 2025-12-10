// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;

namespace Sphynx.Server.Extensions
{
    public static class SphynxErrorCodeExtensions
    {
        public static bool IsServerError(this SphynxErrorCode errorCode)
        {
            switch (errorCode)
            {
                case SphynxErrorCode.SERVER_ERROR:
                case SphynxErrorCode.DB_READ_ERROR:
                case SphynxErrorCode.DB_WRITE_ERROR:
                case SphynxErrorCode.INVALID_PASSWORD:
                case SphynxErrorCode.INVALID_FIELD:
                    return true;

                default:
                    return false;
            }
        }

        public static SphynxErrorCode MaskServerError(this SphynxErrorCode errorCode)
        {
            return errorCode.IsServerError() ? SphynxErrorCode.SERVER_ERROR : errorCode;
        }

        public static SphynxErrorInfo MaskServerError(this in SphynxErrorInfo errorInfo)
        {
            string? message = errorInfo.ErrorCode.IsServerError() && errorInfo.ErrorCode != SphynxErrorCode.SERVER_ERROR
                ? errorInfo.Message
                : null;

            return new SphynxErrorInfo(errorInfo.ErrorCode.MaskServerError(), message);
        }

        public static SphynxErrorInfo<T> MaskServerError<T>(this in SphynxErrorInfo<T> errorInfo, bool keepData = true)
        {
            string? message = errorInfo.ErrorCode.IsServerError() && errorInfo.ErrorCode != SphynxErrorCode.SERVER_ERROR
                ? errorInfo.Message
                : null;

            return new SphynxErrorInfo<T>(errorInfo.ErrorCode.MaskServerError(), message, keepData ? errorInfo.Data : default);
        }

        public static SphynxErrorInfo<T?> MaskServerError<T>(this in SphynxErrorInfo<T?> errorInfo, bool keepData = true) where T : struct
        {
            string? message = errorInfo.ErrorCode.IsServerError() && errorInfo.ErrorCode != SphynxErrorCode.SERVER_ERROR
                ? errorInfo.Message
                : null;

            return new SphynxErrorInfo<T?>(errorInfo.ErrorCode.MaskServerError(), message, keepData ? errorInfo.Data : null);
        }
    }
}
