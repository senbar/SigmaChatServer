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

    [<CLIMutable>]
    type FormModel = { Image: IFormFile; Test: string }
    // Check if file is an image based on MIME type
    let isImage (contentType: string) =
        match contentType with
        | "image/jpeg"
        | "image/png"
        | "image/gif" -> true
        | _ -> false

    let private processFileRequest (next: HttpFunc) (ctx: HttpContext) handler =
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
            let containerClient = ctx.GetService<BlobContainerClient>()
            let guidName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName)

            let fileModel =
                { UserId = userId
                  BlobName = guidName
                  OriginalFilename = file.FileName }

            let blobClient = containerClient.GetBlobClient(file.FileName)
            use stream = file.OpenReadStream()
            let! a = blobClient.UploadAsync(stream, true)
            let! _ = upsertProfilePicture ctx fileModel


            return! text blobClient.Uri.AbsoluteUri next ctx
        }

    let profilePictureUploadHandler (next: HttpFunc) (ctx: HttpContext) =
        let userId = ctx.User.Identity.Name
        uploadProfilePicture userId next ctx |> processFileRequest next ctx
