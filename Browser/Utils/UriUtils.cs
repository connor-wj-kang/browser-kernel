#region

using System.Net;
using System.Net.Http;

#endregion

namespace Browser.Utils;

public static class UriUtils {
    private static readonly CookieContainer CookieJar = new();

    // private static readonly HttpClient HttpClient = new(new HttpClientHandler { CookieContainer = CookieJar });
    private static readonly WebClient WebClient = new();

    public static (Dictionary<string, string>, string) Request(Uri uri, Uri referrer = null,
        FormUrlEncodedContent
            postContent =
            null) {
        var response = WebClient.DownloadString(uri);
        // var cookieContainer = new CookieContainer();
        // var allowCookies = false;
        // if (CookieJar) {
        //     var (cookie, param) = CookieJar[uri.Host];
        //     if (referrer != null && param.GetValueOrDefault("samesite", "none") == "lax")
        //         if (postContent != null)
        //             allowCookies = uri.Host == referrer.Host;
        // }
        // if (postContent == null) {
        //     var source = await HttpClient.GetAsync(uri);
        //     var headers = source.Headers.ToDictionary(pair => pair.Key, pair => string.Join(";", pair.Value));
        //     var body = await source.Content.ReadAsStringAsync();
        //     return (headers, body);
        // }
        // var response = await HttpClient.PostAsync(uri, postContent);
        // var responseString = await response.Content.ReadAsStringAsync();
        // return responseString;
        return (new Dictionary<string, string>(), response);
    }

    public static string GetOrigin(Uri uri) {
        return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
    }
}