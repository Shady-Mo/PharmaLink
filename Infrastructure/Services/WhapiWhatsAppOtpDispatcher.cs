using System.Net.Http.Headers;

namespace Infrastructure.Services;

/// <summary>
/// Implements IWebhookOtpDispatcher to send OTP codes via Whapi.Cloud WhatsApp API.
/// </summary>
public class WhapiWhatsAppOtpDispatcher(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<WhapiWhatsAppOtpDispatcher> logger) : IWebhookOtpDispatcher
{
    public async Task DispatchAsync(string phoneNumber, string otpCode)
    {
        var apiToken = configuration["Whapi:ApiToken"];
        var apiUrl = "https://gate.whapi.cloud/messages/text";

        if (string.IsNullOrEmpty(apiToken))
        {
            logger.LogError("Whapi API token is missing in configuration.");
            throw new InvalidOperationException("Whapi API token is not configured.");
        }

        var formattedPhone = phoneNumber.Trim().TrimStart('+');
        if (formattedPhone.StartsWith("0"))
        {
            formattedPhone = "2" + formattedPhone;
        }
        else if (!formattedPhone.StartsWith("20"))
        {
            formattedPhone = "20" + formattedPhone;
        }
        // -----------------------------------------------------------------

        var messageBody =
            $"رمز التحقق الخاص بك هو: *{otpCode}*\nصالح لمدة دقيقة واحدة فقط. لا تقم بمشاركة الكود مع أي شخص.";

        var payload = new
        {
            to = formattedPhone,
            body = messageBody
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        logger.LogInformation("Sending WhatsApp OTP via Whapi to phone number: {Phone}", formattedPhone);

        var response = await httpClient.PostAsync(apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to send WhatsApp OTP via Whapi. Status: {Status}, Response: {Response}",
                response.StatusCode, errorResponse);

            throw new HttpRequestException($"Whapi dispatch failed with status code {response.StatusCode}");
        }

        logger.LogInformation("WhatsApp OTP successfully dispatched to {Phone}", formattedPhone);
    }
}