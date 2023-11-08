using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Reflection;

namespace AudioGenerationService
{
    internal class MinIOUtil
    {

        private static Logger log = Logger.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IMinioClient MinioClient { get; }

        public MinIOUtil(string endpoint, string accessKey, string secretKey)
        {
            MinioClient = new MinioClient().WithEndpoint(endpoint, 9000).WithCredentials(accessKey, secretKey).Build();
        }

        public async Task FileUpload(string bucketName, string objectName, string contentType, string filePath)
        {
            try
            {
                // Make a bucket on the server, if not already present.
                var beArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);
                bool found = await MinioClient.BucketExistsAsync(beArgs);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs()
                        .WithBucket(bucketName);
                    await MinioClient.MakeBucketAsync(mbArgs);
                }
                // Upload a file to bucket.
                var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFileName(filePath)
                    .WithContentType(contentType);
                await MinioClient.PutObjectAsync(putObjectArgs);
            }
            catch (MinioException e)
            {
                log.error("文件上传错误 "+ e.Message);
            }
        }

    }
}
