using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Net.OpenGameCloud {

    public class AuthModule {
        private readonly OgcClient _client;

        internal AuthModule(OgcClient client) => _client = client;

        public async Task<AuthResponse> Guest() {
            var json = await AuthRequest("POST", "/auth/guest", new { });
            if (json == null) return null;
            var guest = JsonConvert.DeserializeObject<GuestAuthResponse>(json);
            if (guest?.token != null) _client.UserToken = guest.token;
            return new AuthResponse { token = guest.token, user = new UserData { id = guest.userId } };
        }

        public async Task<AuthResponse> Login(string email, string password) =>
            ParseAuth(await AuthRequest("POST", "/auth/login", new { email, password }));

        public async Task<AuthResponse> Register(string email, string password, string username = null) =>
            ParseAuth(await AuthRequest("POST", "/auth/register", new { email, password, username }));

        public async Task<AuthResponse> Steam(string ticket) =>
            ParseAuth(await AuthRequest("POST", "/auth/steam", new { ticket }));


        private AuthResponse ParseAuth(string json) {
            if (json == null) return null;
            var response = JsonConvert.DeserializeObject<AuthResponse>(json);
            if (response?.token != null) _client.UserToken = response.token;
            return response;
        }

        private async Task<string> AuthRequest(string method, string path, object body) {
            var url = $"{_client.Host}{path}";
            using var req = new UnityWebRequest(url, method);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-API-Key", _client.ApiKey);
            req.SetRequestHeader("X-Device-Id", SystemInfo.deviceUniqueIdentifier);
            req.SetRequestHeader("User-Agent", $"OpenGameCloud-Unity/1.0 ({SystemInfo.operatingSystem})");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"[OpenGameCloud] Auth {path} failed: {req.error}\n{req.downloadHandler?.text}");
                return null;
            }
            return req.downloadHandler.text;
        }
    }
}
