using BlazorDiffusion.ServiceModel;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorDiffusion.ServiceInterface;

public class AppUserQuotas
{
    public static HashSet<string> UnrestrictedRoles = new()
    {
        AppRoles.Admin,
        AppRoles.Moderator,
    };

    public Dictionary<string, int> DailyRoleQuotas { get; } = new()
    {
        [AppRoles.Creator] = 320,
    };

    public const int DefaultDailyQuota = 160; //80

    /// <summary>
    /// Future:
    /// Square    = 1 credit    (512 x 512)  0.4
    /// Portrait  = 3 credits   (512 x 896)  1.1
    /// Landscale = 3 credits   (896 x 512)  1.1
    /// Large     = 7 credits   (896 x 896)  2.3
    /// 2x Large  = 10 credits (1024 x 1024) 3.2
    /// 
    /// Steps: 
    ///   50   = * 1
    ///   100  = * 2
    ///   150  = * 3
    /// </summary>
    public int CalculateCredits(ImageGeneration request) => request.Images *
        (request.Width > 512
            ? 3
            : request.Height > 512
                ? 3
                : 1);

    // 12 credits = 4x Images x 3 credits (Portrait)
    public string ToRequestDetails(ImageGeneration request) => $"{CalculateCredits(request)} credits = {request.Images}x Images x " +
        (request.Width > 512
            ? "3 credits (Landscape)"
            : request.Height > 512
                ? "3 credits (Portrait)"
                : "1 credit (Square)");

    public async Task<QuotaError?> ValidateQuotaAsync(IDbConnection db, ImageGeneration request, int userId, ICollection<string> userRoles)
    {
        var requestCredits = CalculateCredits(request);
        var quotaError = await ValidateQuotaAsync(db, requestCredits, userId, userRoles);
        if (quotaError != null)
        {
            quotaError.RequestedDetails = ToRequestDetails(request);
        }
        return quotaError;
    }

    public int? GetDailyQuota(ICollection<string> userRoles)
    {
        if (userRoles.Any(x => UnrestrictedRoles.Contains(x)))
            return null;

        var dailyQuota = DefaultDailyQuota;
        foreach (var role in userRoles)
        {
            if (DailyRoleQuotas.TryGetValue(role, out var roleQuota) && roleQuota > dailyQuota)
                dailyQuota = roleQuota;
        }
        return dailyQuota;
    }

    public async Task<int> GetCreditsUsedAsync(IDbConnection db, int userId, DateTime since)
    {
        var creditsUsed = await db.ScalarAsync<int>(db.From<Artifact>()
            .Join<Creative>((x,y) => x.CreativeId == y.Id)
            .Where<Creative>(x => x.OwnerId == userId && x.CreatedDate >= since)
            .Select(x => new {
                Credits = Sql.Sum(x.Width > 512
                    ? 3
                    : x.Height > 512
                        ? 3
                        : 1)
            }));
        return creditsUsed;
    }

    public async Task<QuotaError?> ValidateQuotaAsync(IDbConnection db, int requestedCredits, int userId, ICollection<string> userRoles)
    {
        var dailyQuota = GetDailyQuota(userRoles);
        if (dailyQuota == null)
            return null;

        var startOfDay = DateTime.UtcNow.Date;
        var creditsUsed = await GetCreditsUsedAsync(db, userId, since:startOfDay);

        if (creditsUsed + requestedCredits > dailyQuota)
        {
            var timeRemaining = startOfDay.AddDays(1) - DateTime.UtcNow;
            var ret = new QuotaError
            {
                ErrorCode = AppErrors.QuotaExceeded,
                Message = "Daily Quota Exceeded",
                TimeRemaining = timeRemaining,
                CreditsUsed = creditsUsed,
                CreditsRequested = requestedCredits,
                DailyQuota = dailyQuota.Value,
                CreditsRemaining = dailyQuota.Value - creditsUsed,
            };
            return ret;
        }

        return null;
    }
}

public static class AppUserQuotasUtils
{
    public static HttpError ToHttpError(this QuotaError error, ResponseStatus status) => 
        new HttpError(status, System.Net.HttpStatusCode.TooManyRequests) {
            Headers = {
                ["Retry-After"] = error.TimeRemaining.TotalSeconds.ToString(),
            }
        };
}
