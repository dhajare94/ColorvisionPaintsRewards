using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QRRewardPlatform.Services
{
    public class ImgBBService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ImgBBService()
        {
            _httpClient = new HttpClient();
            // Free key as requested
            _apiKey = "e7448fd16180c5787c1920b47cbfad1a"; 
        }

        public async Task<string?> UploadImageAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("key", _apiKey),
                    new KeyValuePair<string, string>("image", base64Image),
                    new KeyValuePair<string, string>("name", fileName)
                });

                var response = await _httpClient.PostAsync("https://api.imgbb.com/1/upload", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ImgBBResponse>();
                    return result?.Data?.DisplayUrl;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class ImgBBResponse
    {
        [JsonPropertyName("data")]
        public ImgBBData? Data { get; set; }
    }

    public class ImgBBData
    {
        [JsonPropertyName("display_url")]
        public string? DisplayUrl { get; set; }
    }
}
