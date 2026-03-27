using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Net.OpenGameCloud {

    public class HighScoresModule {
        private readonly OgcClient _client;

        internal HighScoresModule(OgcClient client) => _client = client;

        public async Task Submit(float score) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/highscores";
            await _client.HttpRequest("POST", url, new { score });
        }

        public async Task<HighscoreEntry[]> GetLeaderboard(int limit = 10) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/highscores?limit={limit}";
            var json = await _client.HttpRequest("GET", url);
            return json != null ? JsonConvert.DeserializeObject<HighscoreEntry[]>(json) : null;
        }
    }
}
