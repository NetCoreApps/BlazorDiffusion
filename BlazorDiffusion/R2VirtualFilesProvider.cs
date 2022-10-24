using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.Aws;
using ServiceStack.IO;

namespace BlazorDiffusion;

public class R2VirtualFilesProvider : S3VirtualFiles
{
    public R2VirtualFilesProvider(IAmazonS3 client, string bucketName) : base(client, bucketName)
    {
    }

    public override void WriteFile(string filePath, Stream stream)
    {
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            InputStream = stream,
            DisablePayloadSigning = true,
        });
    }

    public override void WriteFile(string filePath, string contents)
    {
        AmazonS3.PutObject(new PutObjectRequest
        {
            Key = SanitizePath(filePath),
            BucketName = BucketName,
            ContentBody = contents,
            DisablePayloadSigning = true,
        });
    }    
}