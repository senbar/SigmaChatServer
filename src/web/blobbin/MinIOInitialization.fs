namespace SigmaChatServer

module MinIOInitialization =
    open Microsoft.AspNetCore.Http
    open Giraffe
    open Microsoft.Extensions.Configuration
    open Minio
    open Minio.DataModel.Args

    let setupMinIOBucket (ctx: HttpContext) =
        task {
            let settings = ctx.GetService<IConfiguration>()
            let client = ctx.GetService<IMinioClient>()
            let minioSection = settings.GetSection "MinIO"
            let bucketName = minioSection.["PublicBucketName"]

            let checkArgs = (new BucketExistsArgs()).WithBucket(bucketName)

            let! exists = client.BucketExistsAsync(checkArgs)

            if not exists then
                let createArgs =
                    (new MakeBucketArgs()).WithBucket(minioSection.["PublicBucketName"])

                let policy = minioSection.["Policy"]

                let policyArgs =
                    (new SetPolicyArgs())
                        .WithBucket(minioSection.["PublicBucketName"])
                        .WithPolicy(policy)

                do! client.MakeBucketAsync createArgs
                do! client.SetPolicyAsync policyArgs
        }
