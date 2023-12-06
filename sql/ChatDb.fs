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
            VALUES ({version})
            """

    let generateMigrationScript (migrationsTable: string array) (version: int) =
        Array.fold
            (fun (accu, vers) next -> (accu + "\n" + next + (generateVersionInsert (vers + 1)), vers + 1))
            ("", version)
            migrationsTable[version..]

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
