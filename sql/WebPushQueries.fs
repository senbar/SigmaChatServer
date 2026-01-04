namespace SigmaChatServer

module WebPushQueries =
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open Dapper
    open SigmaChatServer.Models
    open System
    open Microsoft.FSharp.Core

    let upsertSubscription (ctx: HttpContext) (json: string) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """
                INSERT INTO "PushSubscriptions" ( "UserId", "Json", "DateCreated") VALUES ( @userId, @json, NOW())
                ON CONFLICT ("UserId") DO UPDATE 
                    SET "Json" = EXCLUDED."Json", 
                        "DateCreated" = EXCLUDED."DateCreated";;
                """

            let sqlParams = {| userId = userId; json = json |}

            let! _ = connection.ExecuteScalarAsync<int>(sql, sqlParams)

            return ()
        }

    let getSubscriptions (ctx: HttpContext) (userId: string seq) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """WITH LatestSubscriptions AS (
                    SELECT "UserId", MAX("DateCreated") AS MaxDate
                    FROM "PushSubscriptions"
                    WHERE "UserId" = ANY(@userIds)
                    GROUP BY "UserId"
                )
                SELECT PS.*
                FROM "PushSubscriptions" PS
                INNER JOIN LatestSubscriptions LS ON PS."UserId" = LS."UserId" AND PS."DateCreated" = LS.MaxDate;"""

            let data = {| userIds = userId |}

            let! subscription = connection.QueryAsync<WebPushSubscriptionModel>(sql, data)
            return subscription
        }

    let getAllSubscriptions (ctx: HttpContext) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql = """SELECT * FROM "PushSubscriptions";"""

            let! subscriptions = connection.QueryAsync<WebPushSubscriptionModel>(sql)
            return subscriptions
        }
