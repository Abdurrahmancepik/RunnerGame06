using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Reactional.Core;


namespace Reactional
{
    public class RequestError : Exception
    {
        public int StatusCode { get; }
        public string StatusText { get; }
        public string ResponseBody { get; }

        public RequestError(string message, int statusCode, string statusText, string responseBody) : base(message)
        {
            StatusCode = statusCode;
            StatusText = statusText;
            ResponseBody = responseBody;
        }
    }

    /// <summary>
    /// A simple Web API client for the Reactional Music Web API.
    /// </summary>
    public static class Backend
    {
        private const string API_URL = "https://api.reactionalmusic.com/v1";
        private static string _serverUrl { get; set; }
        private static bool _debug;
        private static string _appId;
        private static string _appSecret;
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly Auth storage = new Auth();
        internal static bool IsInitialized => storage.Client.Id != null && storage.Client.Secret != null;

        private class Auth
        {
            public ClientAuth Client { get; set; } = new ClientAuth();
            public UserAuth User { get; set; } = new UserAuth();
        }

        private class ClientAuth
        {
            public string Id;
            public string Secret;
            public string AccessToken;
        }

        public static string GetClientID()
        {
            return storage.Client.Id;
        }

        private class UserAuth
        {
            public string Id;
            public string AccessToken;
        }

        [Serializable]
        public class TrackPayload
        {
            public string id;
            public string title;
            public Artist[] artists;
            public Album album;
            public string isrc;
            public string image;
            public int duration;
            public string analysisStatus;
            public string sample;
            public string label;
            public string owner;
            public string[] tags;
            public PriceInfo priceInfo;
            public string[] priceTags;

            [Serializable]
            public struct Artist
            {
                public string id;
                public string name;
            }

            [Serializable]
            public struct Album
            {
                public string id;
                public string name;
            }

            [Serializable]
            public struct PriceInfo
            {
                public long price;
                public long b2b_price;
                public long b2c_price;
            }

            public override string ToString()
            {
                var sb = new StringBuilder(artists.Length * 16);
                foreach (var a in artists)
                {
                    if (sb.Length > 0) { sb.Append(", "); }
                    sb.Append(a.name);
                    sb.Append(" (");
                    sb.Append(a.id);
                    sb.Append(')');
                }
                var artistList = sb.ToString();
                var tagList = string.Join(", ", tags);
                return
                    $"id: {id}, title: {title}, artists: [{artistList}], album: {album.name}, "
                    + $"isrc: {isrc}, image: {image}, duration: {duration}, analysisStatus: {analysisStatus}, "
                    + $"sample: {sample}, label: {label}, owner: {owner}, tags: [{tagList}], "
                    + $"priceInfo: [price={priceInfo.price}, b2b={priceInfo.b2b_price}, b2c={priceInfo.b2c_price}], "
                    + $"priceTags: [{string.Join(", ", priceTags)}]";
            }
        }

        public class EntitlementsPayload
        {
            public string id;
            public string itemId;
            public string itemType;
            public string paymentId;
            public string clientId;
            public long grantedAt;
            public bool revoked;
        }

        /// <summary>
        /// Top level data response data
        /// </summary>
        [Serializable]
        public class BackendResponse<T>
        {
            public int totalCount;
            public string currentUrl;
            public string nextUrl;
            public string previousUrl;
            public int offset;
            public int limit;
            public T[] items;
        }

        [Serializable]
        public class PlaylistPayload
        {
            public string id;
            public string name;
            public string image;
            public string description;
            public bool b2cEnabled;
            public TracksPage tracksPage;
        }
        
        public class TracksPage
        {
            public TrackPayload[] Items;
            public Func<Task<TracksPage>> OnNext;
            public Func<Task<TracksPage>> OnPrevious;

            public TracksPage(
                TrackPayload[] items,
                Func<Task<TracksPage>> onNext,
                Func<Task<TracksPage>> onPrevious
            ) {
                Items = items ?? Array.Empty<Backend.TrackPayload>();
                OnNext = onNext;
                OnPrevious = onPrevious;
            }
        }
        
        public class PlaylistsPage
        {
            public PlaylistPayload[] Items;
            public Func<Task<PlaylistsPage>> OnNext;
            public Func<Task<PlaylistsPage>> OnPrevious;

            public PlaylistsPage(
                PlaylistPayload[] items,
                Func<Task<PlaylistsPage>> onNext,
                Func<Task<PlaylistsPage>> onPrevious
            ) {
                Items = items ?? Array.Empty<PlaylistPayload>();
                OnNext = onNext;
                OnPrevious = onPrevious;
            }
        }

        /// <summary>
        /// Initializes the backend with app credentials and encryption settings.
        /// </summary>
        internal static async Task<(string id, string secret)> Initialize(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentException("Client ID and Client Secret are required");
            }
            storage.Client.Id = clientId;
            storage.Client.Secret = clientSecret;
            try
            {
                Setup.ReactionalLog($"Authenticating client in Initialize {clientId} ...");

                await Authenticate(
                    grantType: "client_credentials",
                    scope: "content:read project:read"
                );

                Setup.ReactionalLog($"Successfully authenticated client {clientId} during initialization");
            }
            catch (Exception e)
            {
                Setup.ReactionalLog($"Failed to authenticate client: {e.Message}", LogLevel.Error);
            }
            return (storage.Client.Id, storage.Client.Secret);
        }

        /// <summary>
        /// Retrieve the access token to use for authenticating requests to the API.
        /// <para>The order of precedence is:</para>
        /// <para>1. User access token</para>
        /// <para>2. Client access token</para>
        /// </summary>
        /// <returns>The access token.</returns>
        private static string GetAccessToken()
        {
            return storage.User.AccessToken ?? storage.Client.AccessToken;
        }

        /// <summary>
        /// Get the default headers to include in every request to the API.
        /// </summary>
        /// <returns>The default headers.</returns>
        private static Dictionary<string, string> GetDefaultHeaders()
        {
            var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
            var accessToken = GetAccessToken();
            if (!string.IsNullOrEmpty(accessToken))
            {
                headers["Authorization"] = $"Bearer {accessToken}";
            }
            return headers;
        }

        /// <summary>
        /// Extract the response body from an HTTP response, based on the content type.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <returns>The extracted response body</returns>
        private static async Task<Dictionary<string, object>> GetResponseBodyJson(HttpResponseMessage response)
        {
            var contentType = response.Content.Headers.ContentType?.ToString();
            if (contentType != null)
            {
                string json = await response.Content.ReadAsStringAsync();
                return MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
            }
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Extract the response body as a list from an HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <returns>The extracted response body as a list</returns>
        private static async Task<List<object>> GetResponseBodyList(HttpResponseMessage response)
        {
            var contentType = response.Content.Headers.ContentType?.ToString();
            if (contentType != null)
            {
                string json = await response.Content.ReadAsStringAsync();
                var result = MiniJSON.Json.Deserialize(json);
                if (result is List<object> list)
                    return list;
                if (result is Dictionary<string, object> dict)
                    return new List<object> { dict };
            }
            return new List<object>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static async Task<byte[]> GetResponseBodyByte(HttpResponseMessage response)
        {
            string contentType = response.Content.Headers.ContentType?.ToString();
            if (contentType == "application/octet-stream")
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }

        /// <summary>
        /// Extract the response body from an HTTP response, based on the content type.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <returns>The extracted response body</returns>
        private static async Task<string> GetResponseBodyString(HttpResponseMessage response)
        {
            var contentType = response.Content.Headers.ContentType?.ToString();
            if (contentType != null)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return response.Content.ReadAsByteArrayAsync().ToString();
        }

        /// <summary>
        /// Send a request to the API.
        /// </summary>
        /// <param name="method">One of the supported methods: "GET","POST","PUT","DELETE"</param>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Additional headers to include in the request.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="apiUrl">Override url to use instead of the static API_URL variable.</param>
        /// <param name="retry">Sets if authentication should be retried when unauthorized.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        /// <exception cref="RequestError">If the request fails.</exception>
        private static async Task<(T data, HttpResponseMessage response)> SendRequest<T>(
            string method,
            string endpoint,
            Dictionary<string, string> headers,
            object body   = null,
            string apiUrl = null,
            bool retry = true
        ) {
            var request = new HttpRequestMessage(new HttpMethod(method), $"{apiUrl ?? API_URL}{endpoint}");
            var allHeaders = new Dictionary<string, string>();
            foreach (var header in GetDefaultHeaders())
            {
                allHeaders[header.Key] = header.Value;
            }
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    allHeaders[header.Key] = header.Value;
                }
            }

            foreach (var header in allHeaders)
            {
                if (header.Key != "Content-Type")
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                if (header.Key == "Authorization")
                {
                    string[] split = header.Value.Split(" ");
                    if (split.Length == 2)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(split[0], split[1]);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid value for Authorization header: {header.Value}");
                    }
                }
            }

            string contentType = allHeaders["Content-Type"] ?? "application/json";
            if (body != null)
            {
                switch (contentType)
                {
                    case "application/json":
                        var jsonBody = MiniJSON.Json.Serialize(body); ;
                        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        break;
                    case "application/x-www-form-urlencoded":
                        request.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)body);
                        break;
                }
            }

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception e)
            {
                throw e.GetBaseException();
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized && retry)
            {
                Setup.ReactionalLog("Re-authenticating client...");

                var grantType = "client_credentials";
                var scope = "content:read project:read";
                await Retry(() => Authenticate(grantType, scope));
                return await SendRequest <T>(method, endpoint, headers, body, retry: false);
            }

            if (response.IsSuccessStatusCode)
            {
                return typeof(T) switch
                {
                    { } t when t == typeof(Dictionary<string, object>) => ((T)(object)await GetResponseBodyJson(response), response),
                    { } t when t == typeof(List<object>) => ((T)(object)await GetResponseBodyList(response), response),
                    { } t when t == typeof(byte[]) => ((T)(object)await GetResponseBodyByte(response), response),
                    _ => throw new Exception($"Unsupported response type: {typeof(T)}")
                };
            }
            var responseBody = await GetResponseBodyString(response);
            throw new RequestError(
                message: $"Request failed with status {(int)response.StatusCode} {response.ReasonPhrase}",
                statusCode: (int)response.StatusCode,
                statusText: response.ReasonPhrase,
                responseBody: responseBody
            );
        }

        /// <summary>
        /// Send a request to obtain an access token for authenticating requests to the API.
        /// <para>If the request is successful, the access token will be stored in the API client and used on subsequent requests.</para>
        /// </summary>
        /// <param name="grantType">One of the supported grant types: "client_credentials", "authorization_code"</param>
        /// <param name="scope">The scope(s) requested for the access token.</param>
        /// <param name="clientId">If not provided, the client ID stored in the API client will be used.</param>
        /// <param name="clientSecret">If not provided, the value provided during the initialization of the API client will be used.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        private static async Task<Dictionary<string, object>> Authenticate(string grantType, string scope = null)
        {
            if (string.IsNullOrEmpty(grantType))
            {
                throw new ArgumentException("Grant type is required");
            }
            if (grantType == "authorization_code")
            {
                throw new NotSupportedException("'authorization_code' grant type is not yet supported");
            }
            if (string.IsNullOrEmpty(storage.Client.Id) || string.IsNullOrEmpty(storage.Client.Secret))
            {
                throw new ArgumentException("Client ID and Client Secret are required for 'client_credentials' grant type");
            }
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{storage.Client.Id}:{storage.Client.Secret}"));
            Dictionary<string, string> body = new Dictionary<string, string> {
                { "grantType", grantType }
            };
            if (scope != null)
            {
                body.Add("scope", scope);
            }
            var (authResponse, _) = await SendRequest<Dictionary<string, object>>(
                method: "POST",
                endpoint: "/auth/token",
                headers: new Dictionary<string, string> {
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "Authorization", $"Basic {authHeader}" }
                },
                body:     body,
                retry:    false
            );
            storage.Client.AccessToken = authResponse["access_token"].ToString();
            return authResponse;
        }

        internal static async Task<T> Retry<T>(Func<Task<T>> operation, int maxRetries = 5, int initialDelay = 1000)
        {
            var retries = 0;
            while (retries < maxRetries)
            {
                var ret = await operation();
                if (ret != null)
                {
                    return ret;
                }
                retries++;
                Setup.ReactionalLog($"Operation failed on attempt {retries}", LogLevel.Warning);
                await Task.Delay((int)(Math.Pow(2, retries) * initialDelay));
            }
            throw new Exception("Operation failed. Max retries exceeded");
        }

        /// <summary>
        /// Send a GET request to the API.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Additional headers to include in the request.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        public static async Task<(T data, HttpResponseMessage response)> GET<T>(string endpoint, Dictionary<string, string> headers = null)
        {
            return await SendRequest<T>("GET", endpoint, headers);
        }

        /// <summary>
        /// Send a POST request to the API.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Additional headers to include in the request.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="apiUrl">Override url to use instead of the static API_URL variable.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        public static async Task<(T data, HttpResponseMessage response)> POST<T>(string endpoint, Dictionary<string, string> headers = null, object body = null, string apiUrl = null)
        {
            return await SendRequest<T>("POST", endpoint, headers, body, apiUrl);
        }

        /// <summary>
        /// Send a PUT request to the API.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Additional headers to include in the request.</param>
        /// <param name="body">The body of the request.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        public static async Task<(T data, HttpResponseMessage response)> PUT<T>(string endpoint, Dictionary<string, string> headers = null, object body = null)
        {
            return await SendRequest<T>("PUT", endpoint, headers, body);
        }

        /// <summary>
        /// Send a DELETE request to the API.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the request to.</param>
        /// <param name="headers">Additional headers to include in the request.</param>
        /// <returns>
        /// The body of the response from the API.
        /// </returns>
        public static async Task<(T data, HttpResponseMessage response)> DELETE<T>(string endpoint, Dictionary<string, string> headers = null)
        {
            return await SendRequest<T>("DELETE", endpoint, headers);
        }
        
        /// <summary>
        /// Extracts the path part from a full URL, starting from either "/dev" or "/content".
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string ExtractPath(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            int devIdx = url.LastIndexOf("/dev", StringComparison.OrdinalIgnoreCase);
            int contentIdx = url.LastIndexOf("/content", StringComparison.OrdinalIgnoreCase);
            int idx = Math.Max(devIdx, contentIdx);
            return (idx >= 0) ? url.Substring(idx) : url;
        }
        
        private static async Task<PlaylistsPage> GetPlaylistsFromUrl(string url, string countryCode)
        {
            var pathPart = ExtractPath(url);
            var page = await FetchPlaylistPage(pathPart, countryCode);
            return page;
        }
        
        private static async Task<TracksPage> GetPlaylistTracksFromUrl(string playlistId, string url, string countryCode)
        {
            var pathPart = ExtractPath(url);
            var tracksPage = await FetchPlaylistTracks(playlistId, countryCode, pathPart);
            return ToTracksPage(playlistId, tracksPage, countryCode);
        }
        
        private static PlaylistsPage ToPlaylistsPage(BackendResponse<PlaylistPayload> page, string countryCode)
        {
            Func<Task<PlaylistsPage>> next = null;
            if (!string.IsNullOrWhiteSpace(page?.nextUrl))
                next = () => GetPlaylistsFromUrl(page!.nextUrl!, countryCode);

            Func<Task<PlaylistsPage>> prev = null;
            if (!string.IsNullOrWhiteSpace(page?.previousUrl))
                prev = () => GetPlaylistsFromUrl(page!.previousUrl!, countryCode);

            return new PlaylistsPage(page?.items, next, prev);
        }
        
        private static TracksPage ToTracksPage(string playlistId, BackendResponse<TrackPayload> tracksPage, string countryCode)
        {
            Func<Task<TracksPage>> next = null;
            if (!string.IsNullOrWhiteSpace(tracksPage?.nextUrl))
                next = () => GetPlaylistTracksFromUrl(playlistId, tracksPage?.nextUrl, countryCode);

            Func<Task<TracksPage>> prev = null;
            if (!string.IsNullOrWhiteSpace(tracksPage?.previousUrl))
                prev = () => GetPlaylistTracksFromUrl(playlistId, tracksPage?.previousUrl, countryCode);

            return new TracksPage(tracksPage?.items, next, prev);
        }
        
        internal static async Task<PlaylistsPage> FetchPlaylistPage(string nextUrl, string countryCode)
        {
            var (dict, _) = await GET<Dictionary<string, object>>(
                endpoint: nextUrl,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            var page = new BackendResponse<PlaylistPayload>
            {
                totalCount = Convert.ToInt32(dict["totalCount"]),
                currentUrl = dict["currentUrl"]?.ToString(),
                nextUrl = dict["nextUrl"]?.ToString(),
                previousUrl = dict["previousUrl"]?.ToString(),
                offset = Convert.ToInt32(dict["offset"]),
                limit = Convert.ToInt32(dict["limit"]),
            };

            var rawItems = dict["items"] as List<object>;
            var list = new List<PlaylistPayload>();

            foreach (var outer in rawItems)
            {
                if (outer is List<object> innerList)
                {
                    foreach (var o in innerList)
                    {
                        list.Add(ParsePlaylist((Dictionary<string, object>)o, countryCode));
                    }
                }
                else if (outer is Dictionary<string, object> pd)
                {
                    list.Add(ParsePlaylist(pd, countryCode));
                }
            }
            page.items = list.ToArray();
            return ToPlaylistsPage(page, countryCode);
        }

        internal static async Task<PlaylistPayload> FetchPlaylist(string playlistId, string countryCode, string nextUrl = null)
        {
            string resourceEndpoint = string.IsNullOrWhiteSpace(nextUrl) ? $"/content/playlists/{playlistId}" : nextUrl;
            var (dict, _) = await GET<Dictionary<string, object>>(
                endpoint: resourceEndpoint,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            return ParsePlaylist(dict, countryCode);
        }

        internal static async Task<BackendResponse<TrackPayload>> FetchPlaylistTracks(string playlistId, string countryCode, string nextUrl = null, int limit = 10, int offset = 0)
        {
            string resourceEndpoint = string.IsNullOrWhiteSpace(nextUrl) ? $"/content/playlists/{playlistId}/tracks?offset={offset}&limit={limit}" : nextUrl;
            var (dict, _) = await GET<Dictionary<string, object>>(
                endpoint: resourceEndpoint,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            return ParseTracksPage(dict);
        }

        private static PlaylistPayload ParsePlaylist(Dictionary<string, object> pd, string countryCode)
        {
            var ps = new PlaylistPayload
            {
                id = pd["id"]?.ToString(),
                name = pd["name"]?.ToString(),
                image = pd["image"]?.ToString(),
                description = pd["description"]?.ToString(),
                b2cEnabled = Convert.ToBoolean(pd["b2cEnabled"]),
                tracksPage = ToTracksPage(pd["id"]?.ToString(), ParseTracksPage((Dictionary<string, object>)pd["tracks"]), countryCode: countryCode)
            };
            return ps;
        }

        internal static async Task<TrackPayload[]> FetchTracksPayload(string[] trackIds, string countryCode, bool isHash = true)
        {
            string resourceEndpoint = $"/content/tracks?hash={isHash}&ids={string.Join(",", trackIds)}";
            var (data, _) = await GET<List<object>>(
                endpoint: resourceEndpoint,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            var tracks = new List<TrackPayload>();
            foreach (var item in data)
            {
                if (item is Dictionary<string, object> dict)
                {
                    var t = ParseTrackPayload(dict);
                    if(t != null) { tracks.Add(t); }
                }
            }
            return tracks.ToArray();
        }

        internal static async Task<TrackPayload> FetchTrackPayload(string trackId, string countryCode, bool isHash = true)
        {
            string resourceEndpoint = $"/content/tracks/{trackId}?hash={isHash}";
            var (dict, _) = await GET<Dictionary<string, object>>(
                endpoint: resourceEndpoint,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            return ParseTrackPayload(dict);
        }

        private static TrackPayload ParseTrackPayload(Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("unavailable") && dict["unavailable"] is true)
            {
                return null;
            }

            var payloadArtists = dict["artists"] as List<object> ?? new List<object>();
            List<TrackPayload.Artist> artists = new List<TrackPayload.Artist>();


            foreach (var artistInfo in payloadArtists)
            {
                if (artistInfo is not Dictionary<string, object> artist)
                {
                    continue;
                }


                var a = new TrackPayload.Artist
                {
                    id = artist.GetValueOrDefault("id", string.Empty).ToString(),
                    name = artist.GetValueOrDefault("name", string.Empty).ToString(),
                };

                artists.Add(a);
            }

            TrackPayload.Album album = new TrackPayload.Album();
            if (dict.ContainsKey("album") && dict["album"] is Dictionary<string, object> albumDict)
            {
                album.id = albumDict.GetValueOrDefault("id", string.Empty).ToString();
                album.name = albumDict.GetValueOrDefault("name", string.Empty).ToString();
            }

            List<string> tags = new List<string>();
            if (dict.ContainsKey("tags") && dict["tags"] is List<object> payloadTags)
            {
                foreach (var tag in payloadTags)
                {
                    tags.Add(tag.ToString());
                }
            }

            List<string> priceTags = new List<string>();
            if (dict.ContainsKey("priceTags") && dict["priceTags"] is List<object> payloadPriceTags)
            {
                foreach (var tag in payloadPriceTags)
                {
                    priceTags.Add(tag.ToString());
                }
            }

            var priceInfo = new TrackPayload.PriceInfo();
            if (dict.ContainsKey("price") && dict["price"] is Dictionary<string, object> priceDict)
            {
                var price = priceDict.GetValueOrDefault("price");
                var b2b_price = priceDict.GetValueOrDefault("b2b_price");
                var b2c_price = priceDict.GetValueOrDefault("b2c_price");
                priceInfo.price = !string.Equals(price?.ToString() ?? "poa", "poa", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt64(price)
                    : 0;
                priceInfo.b2b_price =
                    !string.Equals(b2b_price?.ToString() ?? "poa", "poa", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt64(b2b_price)
                        : 0;
                priceInfo.b2c_price =
                    !string.Equals(b2c_price?.ToString() ?? "poa", "poa", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt64(b2c_price)
                        : 0;
                if (priceInfo.price == 0 || priceInfo.b2b_price == 0 || priceInfo.b2c_price == 0)
                {
                    Setup.ReactionalLog("B2C: Using user-uploaded tracks or tracks with price: POA on Platform requires you to reach out to Reactional Music for correct prices.", LogLevel.Error);
                }
            }

            return new TrackPayload
            {
                id = dict.GetValueOrDefault("id", string.Empty).ToString(),
                title = dict.GetValueOrDefault("title", string.Empty).ToString(),
                artists = artists.ToArray(),
                album = album,
                isrc = dict.GetValueOrDefault("isrc", string.Empty).ToString(),
                image = dict.GetValueOrDefault("image", string.Empty).ToString(),
                duration = Convert.ToInt32(dict.GetValueOrDefault("duration", "0")),
                analysisStatus = dict.GetValueOrDefault("analysisStatus", string.Empty).ToString(),
                sample = dict.GetValueOrDefault("sample", string.Empty).ToString(),
                label = dict.GetValueOrDefault("label", string.Empty).ToString(),
                owner = dict.GetValueOrDefault("owner", string.Empty).ToString(),
                tags = tags.ToArray(),
                priceTags = priceTags.ToArray(),
                priceInfo = priceInfo,
            };
        }

        internal static async Task<byte[]> FetchTrackMetadata(string trackId, string countryCode)
        {
            string resourceEndpoint = $"/content/tracks/{trackId}/reactional-metadata?hash=false&target=unlocked";
            var (metadata, metadataResult) = await GET<byte[]>(
                endpoint: resourceEndpoint,
                headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
            );
            if (!metadataResult.IsSuccessStatusCode)
            {
                Setup.ReactionalLog($"Failed to fetch track resource endpoint: {resourceEndpoint}", LogLevel.Error);
                return null;
            }
            Setup.ReactionalLog($"Successfully fetched track resource endpoint: {resourceEndpoint}");
            return metadata;
        }

        internal static async Task<byte[]> FetchTrackData(string trackId, string countryCode)
        {
            Setup.ReactionalLog($"Fetching track resource {trackId}");
            int REQUEST_ID_SIZE = 36;
            string audioResourceEndpoint;
            byte[] audioContentResponse = { };
            string requestId = "";
            int chunk = 0;
            HttpResponseMessage result;
            do
            {
                audioResourceEndpoint = string.IsNullOrEmpty(requestId)
                    ? $"/content/tracks/{trackId}/audio?hash=false&chunk={chunk}"
                    : $"/content/tracks/{trackId}/audio?hash=false&chunk={chunk}&req={requestId}";

                byte[] payload;
                (payload, result) = await GET<byte[]>(
                    endpoint: audioResourceEndpoint,
                    headers: countryCode != null ? new Dictionary<string, string> { { "Country", countryCode } } : null
                );
                if (result.StatusCode == HttpStatusCode.PartialContent)
                {
                    requestId = Encoding.UTF8.GetString(payload[..REQUEST_ID_SIZE]);
                    var oldSize = audioContentResponse.Length;
                    Array.Resize(ref audioContentResponse, oldSize + payload.Length - REQUEST_ID_SIZE);
                    Array.Copy(payload, REQUEST_ID_SIZE, audioContentResponse, oldSize, payload.Length - REQUEST_ID_SIZE);
                }
                else
                {
                    var oldSize = audioContentResponse.Length;
                    Array.Resize(ref audioContentResponse, oldSize + payload.Length);
                    Array.Copy(payload, 0, audioContentResponse, oldSize, payload.Length);
                }

                chunk++;
            } while (result.StatusCode == HttpStatusCode.PartialContent);

            if (!result.IsSuccessStatusCode)
            {
                Setup.ReactionalLog($"Failed to fetch track resource endpoint: {audioResourceEndpoint}", LogLevel.Error);
                return null;
            }
            Setup.ReactionalLog($"Successfully fetched track resource endpoint: {audioResourceEndpoint}");
            return audioContentResponse;
        }

        private static BackendResponse<TrackPayload> ParseTracksPage(Dictionary<string, object> td)
        {
            var page = new BackendResponse<TrackPayload>
            {
                totalCount = Convert.ToInt32(td["totalCount"]),
                currentUrl = td["currentUrl"]?.ToString(),
                nextUrl = td["nextUrl"]?.ToString(),
                previousUrl = td["previousUrl"]?.ToString(),
                offset = Convert.ToInt32(td["offset"]),
                limit = Convert.ToInt32(td["limit"]),
            };

            var rawTracks = td["items"] as List<object>;
            var tracks = new List<TrackPayload>();
            foreach (var o in rawTracks)
            {
                var dict = (Dictionary<string, object>)o;
                var ts = ParseTrackPayload(dict);
                if (ts != null) { tracks.Add(ts); }
            }
            page.items = tracks.ToArray();
            return page;
        }

        internal static async Task<EntitlementsPayload[]> FetchClientEntitlements()
        {
            string clientId = storage.Client.Id;
            if (string.IsNullOrEmpty(clientId))
            {
                Setup.ReactionalLog("Client ID is not set. Cannot fetch client entitlements.", LogLevel.Error);
                return null;
            }
            string resourceEndpoint = $"/customer/clients/{clientId}/entitlements";
            var (data, httpResponse) = await GET<List<object>>(
                endpoint: resourceEndpoint
            );
            
            if (!httpResponse.IsSuccessStatusCode)
            {
                Setup.ReactionalLog($"Failed to fetch client entitlements endpoint: {resourceEndpoint}, Status: {httpResponse.StatusCode}", LogLevel.Error);
                return null;
            }
            Setup.ReactionalLog($"Successfully fetched {data.Count} entitlements from endpoint: {resourceEndpoint}");

            // Parse the list directly
            var result = new List<EntitlementsPayload>();
            foreach (var item in data)
            {
                if (item is Dictionary<string, object> dict)
                {
                    result.Add(new EntitlementsPayload
                    {
                        id = dict.GetValueOrDefault("id", "").ToString(),
                        itemId = dict.GetValueOrDefault("itemId", "").ToString(),
                        itemType = dict.GetValueOrDefault("itemType", "").ToString(),
                        paymentId = dict.GetValueOrDefault("paymentId", "").ToString(),
                        clientId = dict.GetValueOrDefault("clientId", "").ToString(),
                        grantedAt = Convert.ToInt64(dict.GetValueOrDefault("grantedAt", 0)),
                        revoked = Convert.ToBoolean(dict.GetValueOrDefault("revoked", false))
                    });
                }
            }
            
            return result.ToArray();
        }
    }
}
