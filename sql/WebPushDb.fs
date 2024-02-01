namespace SigmaChatServer

module WebPushDb =
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open Dapper
    open SigmaChatServer.Models
    open System
    open Microsoft.FSharp.Core

    let insertSubscription (ctx: HttpContext) (json: string) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """
                INSERT INTO "PushSubscriptions" ( "UserId", "Json", "DateCreated") VALUES ( @userId, @json, NOW());
                """

            let sqlParams = {| userId = userId; json = json |}

            let! _ = connection.ExecuteScalarAsync<int>(sql, sqlParams)

            return ()
        }

    let getSubscription (ctx: HttpContext) (userId: string) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            //WHERE "UserId"= @userId
            let sql =
                """SELECT * FROM "PushSubscriptions" 
                    ORDER BY "DateCreated" DESC
                    LIMIT 1;"""

            let data = {| userId = userId |}

            try
                let! subscription = connection.QueryFirstAsync<WebPushSubscriptionModel>(sql, data)
                return Some subscription
            with :? InvalidOperationException ->
                return None
        }

    let getAllSubscriptions (ctx: HttpContext) =
        task {
            use connection = ctx.GetService<IDbConnection>()

            let sql =
                """SELECT * FROM "PushSubscriptions";"""

            let! subscriptions = connection.QueryAsync<WebPushSubscriptionModel>(sql)
            return subscriptions
        }
