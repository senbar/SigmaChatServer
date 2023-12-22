namespace SigmaChatServer

module UserDb =
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open Dapper
    open SigmaChatServer.Models
    open System
    open Microsoft.FSharp.Core

    let createUser (ctx: HttpContext) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """INSERT INTO "Users" ("Id") 
                VALUES (@userId) RETURNING *;"""

            let sqlParams = {| userId = userId |}

            let! user = connection.QueryFirstAsync<User>(sql, sqlParams)
            return (user)
        }

    let updateUser (ctx: HttpContext) (userId: string) (model: UpdateMeModel) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """UPDATE "Users" SET "Nickname" = @nickname
                WHERE "Id" = @userId; """

            let sqlParams =
                {| userId = userId
                   nickname = model.Nickname |}

            let! _ = connection.ExecuteAsync(sql, sqlParams)
            return ()
        }

    let getUser (ctx: HttpContext) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql = """SELECT * FROM "Users" WHERE "Id" = @userId;"""

            let sqlParams = {| userId = userId |}

            let! user = connection.QueryFirstOrDefaultAsync<User>(sql, sqlParams)

            let optioned =
                match box user with
                | null -> None
                | _ -> Some user

            return optioned
        }
