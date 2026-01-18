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
    open BlobOperations

    [<CLIMutable>]
    type FormModel = { Image: IFormFile; Test: string }
    // Check if file is an image based on MIME type
    let isImage (contentType: string) =
        match contentType with
        | "image/jpeg"
        | "image/png"
        | "image/gif" -> true
        | _ -> false

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


    let private uploadProfilePicture (minioClient:IMinioClient) (configuration:IConfiguration) (file: IFormFile)= 
        task{
            let minioSettings = configuration.GetSection "MinIO"

            let bucketName = minioSettings.GetValue<string> "PublicBucketName"

            return!
                { FileName = file.FileName
                  ContentStream = file.OpenReadStream()
                  BucketName = bucketName
                  ContentType = file.ContentType }
                |> buildPutArgs bucketName
                |> minioClient.PutObjectAsync
        }

    let private saveProfilePicture (userId: string) (next: HttpFunc) (ctx: HttpContext) (file: IFormFile) =
        task {

            let! putResponse= uploadProfilePicture (ctx.GetService<IMinioClient>()) (ctx.GetService<IConfiguration>()) file

            let fileModel =
                { UserId = userId
                  BlobName = putResponse.ObjectName
                  OriginalFilename = file.FileName }
            let! _ = upsertProfilePicture ctx fileModel

            return! text fileModel.BlobName next ctx
        }

    let profilePictureUploadHandler (next: HttpFunc) (ctx: HttpContext) =
        let userId = ctx.User.Identity.Name
        saveProfilePicture userId next ctx |> processHttpFileRequest next ctx


//Note:
// let presignesArgs =
//     (new PresignedGetObjectArgs())
//         .WithBucket("public-jehovahs-pictures")
//         .WithObject(guidName)
//         .WithExpiry(60)

// let! z = containerClient.PresignedGetObjectAsync(presignesArgs)
