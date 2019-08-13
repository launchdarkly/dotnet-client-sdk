using System;
namespace LaunchDarkly.Xamarin
{
    internal static class Constants
    {
        public const string KEY = "key";
        public const string VERSION = "version";
        public const string FLAGS_KEY_PREFIX = "flags:";
        public const string API_KEY = "api_key";
        public const string CONTENT_TYPE = "Content-Type";
        public const string APPLICATION_JSON = "application/json";
        public const string ACCEPT = "Accept";
        public const string GET = "GET";
        public const string REPORT = "REPORT";
        public const string FLAG_REQUEST_PATH_GET = "msdk/evalx/users/";
        public const string FLAG_REQUEST_PATH_REPORT = "msdk/evalx/user";
        public const string STREAM_REQUEST_PATH = "/meval/";
        public const string PUT = "put";
        public const string PATCH = "patch";
        public const string DELETE = "delete";
        public const string PING = "ping";
        public const string EVENTS_PATH = "/mobile/events/bulk";
        public const string UNIQUE_ID_KEY = "unique_id_key";
    }
}
