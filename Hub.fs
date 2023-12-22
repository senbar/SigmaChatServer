namespace SigmaChatServer

module Hub =
    open Microsoft.AspNetCore.SignalR
    open System
    open SigmaChatServer.Models

    type ChatHub() =
        inherit Hub()

        override this.OnConnectedAsync() =
            Console.WriteLine("connected: " + this.Context.ConnectionId)
            base.OnConnectedAsync()

    let notifyNewMessageCreated (hubContext: IHubContext<ChatHub>) (message: MessageModel) =
        task { return! hubContext.Clients.All.SendAsync("ReceiveMessage", message) }
