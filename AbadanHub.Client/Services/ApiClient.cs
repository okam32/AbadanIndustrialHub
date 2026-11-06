using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using System.Text.Json.Serialization;
using AbadanHub.Shared;

namespace AbadanHub.Client.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public ApiClient(HttpClient http) => _http = http;

    public async Task<T?> Get<T>(string url)
    {
        using var response = await _http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiException("برای ادامه وارد حساب کاربری شوید.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ApiException("اجازه دسترسی به این بخش را ندارید.");
        if (!response.IsSuccessStatusCode) throw new ApiException($"خطا در دریافت اطلاعات ({(int)response.StatusCode})");
        return await response.Content.ReadFromJsonAsync<T>(_json);
    }

    public async Task<string> Upload(IBrowserFile file, Guid? issueId = null)
    {
        var csrf = await Get<CsrfInfo>("api/auth/csrf");
        using var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream(maxAllowedSize: 5_000_000);
        form.Add(new StreamContent(stream), "file", file.Name);
        if (issueId.HasValue) form.Add(new StringContent(issueId.ToString()!), "issueId");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/files/upload") { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf?.Token ?? "");
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException($"بارگذاری انجام نشد ({(int)response.StatusCode}).");
        return "فایل با موفقیت بارگذاری شد.";
    }

    public async Task<string> Post(string url, object? body = null)
    {
        var csrf = await Get<CsrfInfo>("api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body ?? new { }, options: _json)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf?.Token ?? "");
        using var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadFromJsonAsync<ApiMessage>(_json);
            return message?.Message ?? "عملیات با موفقیت انجام شد.";
        }
        var raw = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiException("ورود ناموفق است یا نشست شما پایان یافته است.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ApiException("شما مجوز انجام این عملیات را ندارید.");
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            try { var m = JsonSerializer.Deserialize<ApiMessage>(raw, _json); throw new ApiException(m?.Message ?? "تداخل اطلاعات."); }
            catch (JsonException) { throw new ApiException("این درخواست با وضعیت فعلی قابل انجام نیست."); }
        }
        throw new ApiException($"درخواست انجام نشد ({(int)response.StatusCode}): {raw[..Math.Min(180, raw.Length)]}");
    }
}
public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}
