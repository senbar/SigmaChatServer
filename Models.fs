namespace SigmaChatServer.Models

[<CLIMutable>]
type Message ={
    Sender: string
    Text: string
}

[<CLIMutable>]
type Chat =
    {
        ChatId: int
        Messages: List<Message>
    }

// [<CLIMutable>]
// type CreateChat ={} 