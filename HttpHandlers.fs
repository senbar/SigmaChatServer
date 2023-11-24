namespace SigmaChatServer

module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open SigmaChatServer.Models
    open SigmaChatServer.ChatDb

    let handleGetChat (next : HttpFunc) (ctx : HttpContext) =
            task {
                let! chat = getChat ctx
                return! json chat next ctx
            }