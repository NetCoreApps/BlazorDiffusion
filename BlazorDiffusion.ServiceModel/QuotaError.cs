using ServiceStack;
using System;

namespace BlazorDiffusion.ServiceModel;

public class QuotaError
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }

    public TimeSpan TimeRemaining { get; set; }
    public int CreditsUsed { get; set; }
    public int CreditsRequested { get; set; }
    public int CreditsRemaining { get; set; }
    public int DailyQuota { get; set; }
    public string? RequestedDetails { get; set; }

    public ResponseStatus ToResponseStatus() => new()
    {
        ErrorCode = ErrorCode,
        Message = Message,
        Meta = new()
        {
            [nameof(TimeRemaining)] = TimeRemaining.ToString("hh\\:mm\\:ss"),
            [nameof(DailyQuota)] = $"{DailyQuota}",
            [nameof(CreditsUsed)] = $"{CreditsUsed}",
            [nameof(CreditsRequested)] = $"{CreditsRequested}",
            [nameof(RequestedDetails)] = RequestedDetails ?? string.Empty,
        },
    };

    public static QuotaError FromResponseStatus(ResponseStatus status)
    {
        var to = new QuotaError
        {
            ErrorCode = status.ErrorCode,
            Message = status.Message,
            TimeRemaining = TimeSpan.Parse(status.Meta[nameof(TimeRemaining)]),
            DailyQuota = int.Parse(status.Meta[nameof(DailyQuota)]),
            CreditsUsed = int.Parse(status.Meta[nameof(CreditsUsed)]),
            CreditsRequested = int.Parse(status.Meta[nameof(CreditsRequested)]),
            RequestedDetails = status.Meta.TryGetValue(nameof(RequestedDetails), out var details) ? details : null,
        };
        to.CreditsRemaining = to.DailyQuota - to.CreditsUsed;
        return to;
    }
}
