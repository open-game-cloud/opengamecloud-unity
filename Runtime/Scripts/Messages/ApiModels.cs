using Newtonsoft.Json;

namespace Net.OpenGameCloud {

    public class AuthResponse {
        public UserData user;
        public string token;
    }

    public class GuestAuthResponse {
        public string token;
        public string userId;
    }

    public class UserData {
        public string id;
        public string username;
        [JsonProperty("first_name")]
        public string firstName;
        [JsonProperty("last_name")]
        public string lastName;
        public string description;
        [JsonProperty("avatar_url")]
        public string avatarUrl;
        [JsonProperty("email_verified")]
        public bool emailVerified;
        public object metadata;
        [JsonProperty("created_at")]
        public string createdAt;
    }

    public class SessionData {
        public string id;
        [JsonProperty("project_id")]
        public string projectId;
        public string label;
        public string status;
        [JsonProperty("session_type")]
        public string sessionType; // "public", "private", "matchmaking"
        [JsonProperty("active_user_count")]
        public int activeUserCount;
        [JsonProperty("player_limit")]
        public int playerLimit;
        [JsonProperty("is_password_protected")]
        public bool isPasswordProtected;
        public object metadata;
        [JsonProperty("created_at")]
        public string createdAt;
        [JsonProperty("expires_at")]
        public string expiresAt;
    }

    public class HighscoreEntry {
        public string username;
        [JsonProperty("avatar_url")]
        public string avatarUrl;
        [JsonProperty("best_score")]
        public float bestScore;
        [JsonProperty("updated_at")]
        public string updatedAt;
    }

    public class DataBlockInfo {
        public string key;
        public int revision;
    }
}
