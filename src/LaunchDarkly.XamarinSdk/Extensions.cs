using System;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal static class Extensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string AsJson(this User user)
        {
            return JsonUtil.EncodeJson(user);
        }

        // This differs from "new Uri(baseUri, path)" in that it always assumes a trailing "/" in
        // baseUri. For instance, if baseUri is http://hostname/basepath and path is "relativepath",
        // baseUri.AddPath(path) will return http://hostname/basepath/relativepath, whereas
        // new Uri(baseUri, path) would return http://hostname/relativepath (since, with no trailing
        // slash, it would treat "basepath" as the equivalent of a filename rather than the
        // equivalent of a directory name). We should assume that if an application has specified a
        // base URL with a non-empty path, the intention is to use that as a prefix for everything.
        public static Uri AddPath(this Uri baseUri, string path)
        {
            var ub = new UriBuilder(baseUri);
            ub.Path = ub.Path.TrimEnd('/') + "/" + path.TrimStart('/');
            return ub.Uri;
        }

        public static Uri AddQuery(this Uri baseUri, string query)
        {
            var ub = new UriBuilder(baseUri);
            ub.Query = string.IsNullOrEmpty(ub.Query) ?
                ("?" + query) :
                ub.Query + "&" + query;
            return ub.Uri;
        }
    }
}
