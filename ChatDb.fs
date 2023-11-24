
namespace SigmaChatServer

module ChatDb =

    open SigmaChatServer.Models
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.Data
    open Dapper

    let getChat (ctx: HttpContext)=
        task {
            use connection = ctx.GetService<IDbConnection>()
            let! chat = connection.QueryAsync<Chat> 
                                "SELECT * FROM Chats"
            return chat 
        }