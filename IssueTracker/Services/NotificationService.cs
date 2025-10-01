using IssueTracker.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class NotificationOptions
{
    public EmailOptions Email { get; set; } = new();
    public WebhookOptions Webhooks { get; set; } = new();
}

public class EmailOptions
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public List<string> To { get; set; } = new();
}

public class WebhookOptions
{
    public bool Enabled { get; set; }
    public List<string> Urls { get; set; } = new();
    public string? Secret { get; set; }               // NEW
    public int TimeoutMs { get; set; } = 4000;        // NEW
    public int MaxRetries { get; set; } = 3;          // NEW
}

public class NotificationService(IOptions<NotificationOptions> options, ILogger<NotificationService> logger, IHttpClientFactory httpClientFactory)
{
    private readonly NotificationOptions _opt = options.Value;
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly IHttpClientFactory _http = httpClientFactory;

    // --- Public API ---

    public async Task NotifyIssueCreatedAsync(Issue issue)
        => await SendWebhookAsync("issue.created", new { issue = MapIssue(issue) });

    public async Task NotifyIssueUpdatedAsync(Issue before, Issue after)
        => await SendWebhookAsync("issue.updated", new { before = MapIssue(before), after = MapIssue(after) });

    public async Task NotifyStatusChangeAsync(Issue before, Issue after)
    {
        if (before.Status == after.Status) return;
        await SendWebhookAsync("issue.status.changed", new {
            before = new { id = before.Id, status = before.Status },
            after = MapIssue(after)
        });
        // (Email notification code you already have can stay here)
    }

    // --- Core sender with signing/retries ---

    private async Task SendWebhookAsync(string evt, object payload)
    {
        if (!_opt.Webhooks.Enabled || _opt.Webhooks.Urls.Count == 0) return;

        var envelope = new
        {
            @event = evt,
            data = payload,
            timestamp = DateTimeOffset.UtcNow
        };
        var json = JsonSerializer.Serialize(envelope);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var sig = Sign(json, ts, _opt.Webhooks.Secret);

        using var client = _http.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(_opt.Webhooks.TimeoutMs);

        foreach (var url in _opt.Webhooks.Urls)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Content = content;
                    req.Headers.Add("X-Issues-Event", evt);
                    req.Headers.Add("X-Issues-Timestamp", ts);
                    if (!string.IsNullOrEmpty(sig))
                        req.Headers.Add("X-Issues-Signature", $"sha256={sig}");

                    var resp = await client.SendAsync(req);
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                        break; // success

                    _logger.LogWarning("Webhook {Url} returned {Status}", url, resp.StatusCode);
                    if (!ShouldRetry(resp.StatusCode) || attempt >= _opt.Webhooks.MaxRetries)
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Webhook error for {Url} (attempt {Attempt})", url, attempt + 1);
                    if (attempt >= _opt.Webhooks.MaxRetries) break;
                }

                attempt++;
                await Task.Delay(BackoffDelay(attempt));
            }
        }
    }

    private static bool ShouldRetry(HttpStatusCode code)
        => code == HttpStatusCode.RequestTimeout
        || ((int)code >= 500 && (int)code <= 599);

    private static TimeSpan BackoffDelay(int attempt)
        => TimeSpan.FromMilliseconds(Math.Min(1000 * Math.Pow(2, attempt), 8000)); // 1s,2s,4s,8s cap

    private static string? Sign(string body, string timestamp, string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return null;
        var payload = $"{timestamp}.{body}";
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Map only what you need downstream (keeps payload lean)
    private static object MapIssue(Issue i) => new
    {
        id = i.Id,
        url = i.WebsiteUrl,
        typeId = i.IssueTypeId,
        type = i.IssueTypeRef?.Name ?? i.IssueType, // fallback if you kept legacy string
        priority = i.IssuePriority,
        status = i.Status,
        assignedTo = i.AssignedTo,
        screenshot = i.Screenshot,
        reported = i.DateReported,
        resolved = i.DateResolved
    };
}
