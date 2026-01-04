namespace SigmaChatServer

module ChatQueries =

    open SigmaChatServer.Models
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open Dapper
    open Migrations
    open System

    let generateVersionInsert version =
        "\n"
        + $"""
            INSERT INTO "Migrations" ("Version")
            VALUES ({version});
            """

    let generateMigrationScript (migrationsTable: string array) (version: int) =
        let (sql: string, _: int) =
            Array.fold
                (fun (accu, vers) next -> (accu + "\r\n " + next + (generateVersionInsert (vers + 1)), vers + 1))
                ("", version)
                migrationsTable[version..]

        sql

    let lastMigrationVersion (connection: IDbConnection) =
        task {
            try
                let! version = connection.QueryFirstAsync<{| Version: int |}>(GetVersion)
                return version.Version
            with _ ->
                return 0
        }

    let setupDatabaseSchema (connection: IDbConnection) =
        task {
            let! version = lastMigrationVersion connection
            return! version |> generateMigrationScript Migrations |> connection.QueryAsync
        }

    let postChat (ctx: HttpContext) =
        task {
            use connection = ctx.GetService<IDbConnection>()
            let sql = """INSERT INTO "Chats" DEFAULT VALUES RETURNING "ChatId" """
            let! id = connection.ExecuteScalarAsync<int>(sql)
            return id
        }

    let insertMessage (ctx: HttpContext) (createMessageModel: CreateMessageModel) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """
                WITH new_message AS (INSERT INTO "Messages" ("ChatId", "UserId", "Text", "DateCreated") VALUES (@chatId, @userId, @text, NOW()) RETURNING *),
                message_model as (SELECT new_message.*, "Users"."Nickname" AS "UserNickname" FROM new_message LEFT JOIN "Users" ON new_message."UserId" = "Users"."Id" )
                SELECT message_model.* from message_model;"""

            let sqlParams =
                {| chatId = createMessageModel.ChatId
                   userId = userId
                   text = createMessageModel.Text |}

            let! createdMessage = connection.QuerySingleOrDefaultAsync<MessageModel>(sql, sqlParams)

            return createdMessage
        }

    let getMessages (ctx: HttpContext) (chatId: int) (paginationDate: DateTime) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """SELECT * FROM (SELECT "Messages".*, "Users"."Nickname" as "UserNickname", "UserProfilePictures"."BlobName" as "UserProfilePicture" FROM "Messages" 
                    LEFT JOIN "Users" ON "Messages"."UserId"="Users"."Id"
                    LEFT JOIN "UserProfilePictures" ON "Users"."Id" = "UserProfilePictures"."UserId"
                    WHERE "ChatId"= @chatId AND "Messages"."DateCreated" < @paginationDate
                    ORDER BY "MessageId" DESC 
                    LIMIT 30)
                    ORDER BY "MessageId" ASC;"""

            let data =
                {| chatId = chatId
                   paginationDate = paginationDate |}

            let! messages = connection.QueryAsync<MessageModel>(sql, data)
            return messages
        }

    let getChat (ctx: HttpContext) (chatId: int) =
        task {
            let! messages = getMessages ctx chatId DateTime.UtcNow

            try
                return
                    Some
                        { ChatId = chatId
                          Messages = messages |> Seq.toList }
            with :? InvalidOperationException ->
                return None
        }
