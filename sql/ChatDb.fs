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
                let! version = connection.QueryFirstOrDefaultAsync<{| Version: int |}>(GetVersion)
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
            let! chat = connection.QueryFirstOrDefaultAsync<Chat>(sql, data)
            return chat
        }

    let postChat (ctx: HttpContext) =
        task {
            use connection = ctx.GetService<IDbConnection>()
            let sql = """INSERT INTO "Chats" DEFAULT VALUES RETURNING "ChatId" """
            let! id = connection.ExecuteScalarAsync<int>(sql)
            return id
        }

    let postMessage (ctx: HttpContext) =
        task {
            use connection = ctx.GetService<IDbConnection>()
            let! createMessageModel = ctx.BindJsonAsync<Message>()

            let sql =
                """INSERT INTO "Messages" ("ChatId", "Sender", "Text", "DateCreated") VALUES (@chatId, @sender, @text, CURRENT_DATE) 
                RETURNING * """

            let sqlParams =
                {| chatId = createMessageModel.ChatId
                   sender = createMessageModel.Sender
                   text = createMessageModel.Text |}

            let! createdMessage = connection.QuerySingleOrDefaultAsync<Message>(sql, sqlParams)
            return createdMessage
        }

    let getMessages (ctx: HttpContext) (chatId: int) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql = """SELECT * FROM "Messages" WHERE "ChatId" = @chatId"""
            let data = {| chatId = chatId |}
            let! messages = connection.QueryAsync<Message>(sql, data)
            return messages
        }
