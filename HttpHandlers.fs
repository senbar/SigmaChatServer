namespace SigmaChatServer

module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open SigmaChatServer.Models
    open SigmaChatServer.ChatDb
    open System.Data
    open System
    open Hub
    open Microsoft.AspNetCore.SignalR

    let handleGetChats (chatId: int) (next: HttpFunc) (ctx: HttpContext) =
        task {
            let! chat = getChat ctx chatId
            return! json chat next ctx
        }

    let handlePostChat (next: HttpFunc) (ctx: HttpContext) =
        task {
            let! chatId = postChat ctx
            return! json chatId next ctx
        }


    let updateSchema (next: HttpFunc) (ctx: HttpContext) =
        task {
            let connection = ctx.GetService<IDbConnection>()
            let! res = setupDatabaseSchema connection
            return! json Ok next ctx
        }

    let handleGetMessages (chatId: int) (next: HttpFunc) (ctx: HttpContext) =
        task {
            let! chat = getMessages ctx chatId
            return! json chat next ctx
        }

    let handlePostMessage (next: HttpFunc) (ctx: HttpContext) =
        task {
            let hub = ctx.GetService<IHubContext<ChatHub>>()
            let! createdMessage = postMessage ctx
            do! NotifyNewMessageCreated hub createdMessage

            return! json createdMessage next ctx
        }
