namespace SigmaChatServer

module ChatDb =

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

    let getChat (ctx: HttpContext) (chatId: int) =
        task {
            use connection = ctx.GetService<IDbConnection>()
            let sql = """SELECT * FROM "Chats" WHERE "ChatId" = @chatId"""
            let data = {| chatId = chatId |}

            try
                let! chat = connection.QueryFirstAsync<Chat>(sql, data)
                return Some chat
            with :? InvalidOperationException ->
                return None
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

    let getMessages (ctx: HttpContext) (chatId: int) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """SELECT "Messages".*, "Users"."Nickname" as "UserNickname" FROM "Messages" 
                    LEFT JOIN "Users" on "Messages"."UserId"="Users"."Id"
                    WHERE "ChatId"= @chatId
                    ORDER BY "MessageId";"""

            let data = {| chatId = chatId |}
            let! messages = connection.QueryAsync<MessageModel>(sql, data)
            return messages
        }
