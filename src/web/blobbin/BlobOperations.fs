namespace SigmaChatServer

module BlobOperations =
    open System.IO
    open Minio.DataModel.Args
    open System
    open Minio

    type FileUploadModel =
        { FileName: string
          ContentStream: Stream
          BucketName: string
          ContentType: string }

    let buildPutArgs (bucketName: string) (fileUploadModel: FileUploadModel)  =
        let guid = Guid.NewGuid().ToString() + Path.GetExtension fileUploadModel.FileName

        (new PutObjectArgs())
            .WithBucket(bucketName)
            .WithObject(guid)
            .WithObjectSize(fileUploadModel.ContentStream.Length)
            .WithContentType(fileUploadModel.ContentType)
            .WithStreamData
            fileUploadModel.ContentStream
