namespace SigmaChatServer.Models

[<CLIMutable>]
type Message ={
    Sender: string
    Text: string
}

[<CLIMutable>]
type Chat =
    {
        Messages: List<Message>
    }