namespace SigmaChatServer

module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open SigmaChatServer.Models
    open SigmaChatServer.ChatDb
    open System.Data
    open System
    open SigmaChatServer.WebPush
    open Hub
    open Microsoft.AspNetCore.SignalR
    open UserDb
    open System.Threading.Tasks

    let handleGetChats (chatId: int) (next: HttpFunc) (ctx: HttpContext) =
        task {
            let! chat = getChat ctx chatId

            return!
                match chat with
                | Some chat -> json chat next ctx
                | None -> json (RequestErrors.NOT_FOUND(text "Basic")) next ctx
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
            let userId = ctx.User.Identity.Name

            let processTooShortMessage () =
                task { return! RequestErrors.BAD_REQUEST (text "Basic") next ctx }

            let processCorrectMessage model =
                task {
                    let! createdMessage = insertMessage ctx model userId
                    do! notifyNewMessageCreated hub createdMessage
                    do! webpushMessageForUser ctx userId model

                    return! json createdMessage next ctx
                }

            let! createMessageModel = ctx.BindJsonAsync<CreateMessageModel>()

            return!
                match createMessageModel with
                | model when model.Text.Length = 0 -> processTooShortMessage ()
                | model -> processCorrectMessage model
        }

    let handleCallback (next: HttpFunc) (ctx: HttpContext) =
        task {
            let userId = ctx.User.Identity.Name
            let! userInDb = getUser ctx userId

            let! resultingUser =
                match userInDb with
                | Some user -> Task.FromResult(user)
                | None -> createUser ctx userId

            return! json resultingUser next ctx
        }

    let handleUpdateMeProfile (next: HttpFunc) (ctx: HttpContext) =
        task {
            let userId = ctx.User.Identity.Name
            let! updateMeModel = ctx.BindJsonAsync<UpdateMeModel>()
            do! updateUser ctx userId updateMeModel

            return! json None next ctx
        }

    let handleGetUserMe (next: HttpFunc) (ctx: HttpContext) =
        task {
            let userId = ctx.User.Identity.Name
            let! user = getUser ctx userId

            let res =
                match user with
                | Some u -> json u next ctx
                | None -> RequestErrors.UNAUTHORIZED "Basic" "" "You must be logged in." next ctx

            return! res
        }
