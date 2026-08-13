using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services;

public class WhapiWhatsAppMessageService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<WhapiWhatsAppMessageService> logger) : IWhatsAppMessageService
{
    public async Task SendMessageAsync(string phoneNumber, string message)
    {
        var apiToken = configuration["Whapi:ApiToken"];
        if (string.IsNullOrEmpty(apiToken)) throw new InvalidOperationException("Whapi API token missing.");

        var formattedPhone = phoneNumber.Trim().TrimStart('+');
        if (formattedPhone.StartsWith("0")) formattedPhone = "2" + formattedPhone;
        else if (!formattedPhone.StartsWith("20")) formattedPhone = "20" + formattedPhone;

        var payload = new { to = formattedPhone, body = message };
        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        var response = await httpClient.PostAsync("https://gate.whapi.cloud/messages/text", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            logger.LogError("Whapi message failed: {Status} - {Error}", response.StatusCode, err);
            throw new HttpRequestException($"Whapi failed: {response.StatusCode}");
        }
    }
}
