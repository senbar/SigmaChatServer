namespace SigmaChatServer

module BlobHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open SigmaChatServer.Models
    open Npgsql
    open Azure.Storage.Blobs
    open System
    open Giraffe.HttpStatusCodeHandlers.RequestErrors
    open UserQueries
    open System.IO
    open UserQueries
    open Minio
    open Minio.DataModel.Args
    open Microsoft.Extensions.Configuration

    [<CLIMutable>]
    type FormModel = { Image: IFormFile; Test: string }
    // Check if file is an image based on MIME type
    let isImage (contentType: string) =
        match contentType with
        | "image/jpeg"
        | "image/png"
        | "image/gif" -> true
        | _ -> false

    let getPublicBlobUrl (config: IConfiguration) blobName =
        let minioSettings = config.GetSection("Minio")
        minioSettings.GetValue<string>("PublicBucketUrl") + blobName

    let private processHttpFileRequest (next: HttpFunc) (ctx: HttpContext) handler =
        task {
            match ctx.Request.HasFormContentType with
            | true ->
                let! form = ctx.Request.ReadFormAsync() |> Async.AwaitTask
                let files = form.Files

                if files.Count > 0 then
                    let file = files.[0]
                    return! handler file
                else
                    return! badRequest (text "No files uploaded") next ctx
            | false -> return! badRequest (text "Unsupported media type") next ctx
        }

    let private uploadProfilePicture (userId: string) (next: HttpFunc) (ctx: HttpContext) (file: IFormFile) =
        task {
            let containerClient = ctx.GetService<IMinioClient>()
            let minioSettings = ctx.GetService<IConfiguration>().GetSection("Minio")

            let guid = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName)

            let fileModel =
                { UserId = userId
                  BlobName = guid
                  OriginalFilename = file.FileName }

            use stream = file.OpenReadStream()

            let putArgs =
                (new PutObjectArgs())
                    .WithBucket(minioSettings.GetValue<string>("PublicBucketName"))
                    .WithObject(fileModel.BlobName)
                    .WithObjectSize(stream.Length)
                    .WithContentType(file.ContentType)
                    .WithStreamData(stream)

            let! _ = containerClient.PutObjectAsync(putArgs)
            let! _ = upsertProfilePicture ctx fileModel

            let blobUrl = getPublicBlobUrl minioSettings fileModel.BlobName
            return! text blobUrl next ctx
        }

    let profilePictureUploadHandler (next: HttpFunc) (ctx: HttpContext) =
        let userId = ctx.User.Identity.Name
        uploadProfilePicture userId next ctx |> processHttpFileRequest next ctx


//Note:
// let presignesArgs =
//     (new PresignedGetObjectArgs())
//         .WithBucket("public-jehovahs-pictures")
//         .WithObject(guidName)
//         .WithExpiry(60)

// let! z = containerClient.PresignedGetObjectAsync(presignesArgs)
