using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;

namespace QRRewardPlatform.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService(IConfiguration configuration)
        {
            var firebaseUrl = configuration["Firebase:DatabaseUrl"] 
                ?? "https://cvspincentive-default-rtdb.firebaseio.com/";
            var firebaseSecret = configuration["Firebase:DatabaseSecret"] ?? "";

            _client = new FirebaseClient(firebaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(firebaseSecret)
            });
        }

        public FirebaseClient Client => _client;

        public async Task<Dictionary<string, T>> GetAllAsync<T>(string node)
        {
            try
            {
                var items = await _client.Child(node).OnceAsync<T>();
                return items.ToDictionary(i => i.Key, i => i.Object);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from node '{node}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return new Dictionary<string, T>();
            }
        }

        public async Task<T?> GetByIdAsync<T>(string node, string id)
        {
            try
            {
                return await _client.Child(node).Child(id).OnceSingleAsync<T>();
            }
            catch
            {
                return default;
            }
        }

        public async Task<string> PushAsync<T>(string node, T data)
        {
            var result = await _client.Child(node).PostAsync(data);
            return result.Key;
        }

        public async Task SetAsync<T>(string node, string id, T data)
        {
            await _client.Child(node).Child(id).PutAsync(data);
        }

        public async Task UpdateAsync<T>(string node, string id, T data)
        {
            await _client.Child(node).Child(id).PutAsync(data);
        }

        public async Task DeleteAsync(string node, string id)
        {
            await _client.Child(node).Child(id).DeleteAsync();
        }

        public async Task DeleteNodeAsync(string node)
        {
            await _client.Child(node).DeleteAsync();
        }

        public async Task PatchAsync(string node, string id, Dictionary<string, object> updates)
        {
            await _client.Child(node).Child(id).PatchAsync(updates);
        }
    }
}
