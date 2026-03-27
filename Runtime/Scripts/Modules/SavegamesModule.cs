using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Net.OpenGameCloud {

    public class PlayerDataModule {
        private readonly OgcClient _client;

        internal PlayerDataModule(OgcClient client) => _client = client;

        public async Task Save(string key, object data) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/data-blocks/{key}";
            await _client.HttpRequest("PUT", url, new { data });
        }

        public async Task<T> Load<T>(string key) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/data-blocks/{key}";
            var json = await _client.HttpRequest("GET", url);
            if (json == null) return default;
            return JsonConvert.DeserializeObject<DataBlockResponse<T>>(json).data;
        }

        public async Task<DataBlockInfo[]> ListKeys() {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/data-blocks";
            var json = await _client.HttpRequest("GET", url);
            return json != null ? JsonConvert.DeserializeObject<DataBlockInfo[]>(json) : null;
        }

        public async Task Delete(string key) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/data-blocks/{key}";
            await _client.HttpRequest("DELETE", url);
        }

        private class DataBlockResponse<T> {
            public string key;
            public T data;
            public int revision;
        }
    }
}
