namespace SigmaChatServer

module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open SigmaChatServer.Models
    open SigmaChatServer.ChatQueries
    open System.Data
    open System
    open SigmaChatServer.WebPush
    open Hub
    open Microsoft.AspNetCore.SignalR
    open UserQueries
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

    let handleGetMessages (chatId: int, paginationDate: string) (next: HttpFunc) (ctx: HttpContext) =
        task {
            return!
                match DateTime.TryParse(paginationDate) with
                | true, date ->
                    task {
                        let! messages = getMessages ctx chatId (date.ToUniversalTime())
                        return! json messages next ctx
                    }
                | _ -> RequestErrors.BAD_REQUEST (text "Couldnt parse pagination date") next ctx
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
                    let! allUserids = getAllUserIds ctx
                    do! notifyNewMessageCreated hub createdMessage
                    let! _ = webpushMessageForUser ctx allUserids model

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
