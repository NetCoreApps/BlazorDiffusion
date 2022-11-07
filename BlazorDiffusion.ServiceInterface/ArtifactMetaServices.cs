using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BlazorDiffusion.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace BlazorDiffusion.ServiceInterface;

public class ArtifactServices : Service
{
    public async Task<object> Post(CreateArtifactLike request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userId = session.GetUserId();
        var row = new ArtifactLike
        {
            AppUserId = userId,
            ArtifactId = request.ArtifactId,
            CreatedDate = DateTime.UtcNow,
        };
        row.Id = await base.Db.InsertAsync(row, selectIdentity:true);

        PublishMessage(new BackgroundTasks { RecordArtifactLikeId = request.ArtifactId });
        return row;
    }

    public async Task Delete(DeleteArtifactLike request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userId = session.GetUserId();
        await Db.DeleteAsync<ArtifactLike>(x => x.ArtifactId == request.ArtifactId && x.AppUserId == userId);

        PublishMessage(new BackgroundTasks { RecordArtifactUnlikeId = request.ArtifactId });
    }

    public async Task<object> Post(CreateArtifactReport request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userId = session.GetUserId();
        var row = request.ConvertTo<ArtifactReport>();
        row.AppUserId = userId;
        row.CreatedDate = DateTime.UtcNow;
        row.Id = await base.Db.InsertAsync(row, selectIdentity: true);
        return row;
    }

    public async Task Delete(DeleteArtifactReport request)
    {
        var session = await SessionAsAsync<CustomUserSession>();
        var userId = session.GetUserId();
        await Db.DeleteAsync<ArtifactReport>(x => x.ArtifactId == request.ArtifactId && x.AppUserId == userId);
    }

    public async Task<object> Get(DownloadArtifact request)
    {
        var artifact = !string.IsNullOrEmpty(request.RefId)
            ? await Db.SingleAsync<Artifact>(x => x.RefId == request.RefId)
            : null;
        var file = artifact?.FilePath != null
            ? VirtualFiles.GetFile(artifact.FilePath)
            : null;

        if (file == null)
            return HttpError.NotFound("File not found");

        PublishMessage(new AnalyticsTasks {
            RecordArtifactStat = new ArtifactStat {
                Type = StatType.Download,
                ArtifactId = artifact!.Id,
                RefId = artifact.RefId,
                Source = nameof(DownloadArtifact),
                Version = ServiceStack.Text.Env.VersionString,
            }.WithRequest(Request, await GetSessionAsync())
        });

        return new HttpResult(file, asAttachment:true);
    }

    public AppConfig AppConfig { get; set; }

    public async Task<object> Any(DownloadDirect request)
    {
        var artifact = !string.IsNullOrEmpty(request.RefId)
            ? await Db.SingleAsync<Artifact>(x => x.RefId == request.RefId)
            : null;

        if (artifact == null)
            return HttpError.NotFound("File not found");

        var accessId = request.AccessId ?? AppConfig.R2AccessId;
        var accessKey = request.AccessKey ?? AppConfig.R2AccessKey;
        var s3Client = new AmazonS3Client(accessId, accessKey, new AmazonS3Config
        {
            ServiceURL = $"https://{AppConfig.R2Account}.r2.cloudflarestorage.com"
        });

        var s3Request = new GetObjectRequest
        {
            BucketName = AppConfig.ArtifactBucket,
            Key = artifact.FilePath.TrimStart('/'),
        };

        if (request.EncryptionMethod != null)
            s3Request.ServerSideEncryptionCustomerMethod = request.EncryptionMethod;


        var response = await s3Client.GetObjectAsync(s3Request);        
        return new HttpResult(response.ResponseStream, artifact.FileName)
        {
            Headers =
            {
                [HttpHeaders.ContentDisposition] = $"attachment; {HttpExt.GetDispositionFileName(artifact.FileName)}; size={response.ContentLength};"
            }
        };
    }
}
