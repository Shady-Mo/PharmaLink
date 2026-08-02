using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        var apiKey = "AQ.Ab8RN6ILwnW_kRysmFLo5Ud7WY58f7Q1SCTc8pCnv0LHzqNNww";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

        var requestBody = new
        {
            model = "gemini-1.5-flash",
            messages = new[] { new { role = "user", content = "Hello" } }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response =
            await client.PostAsync("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", content);
        Console.WriteLine(response.StatusCode);
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}