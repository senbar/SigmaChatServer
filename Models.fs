namespace SigmaChatServer.Models

open System

[<CLIMutable>]
type Message =
    { MessageId: int
      ChatId: int
      Sender: string
      Text: string
      DateCreated: DateTime }

[<CLIMutable>]
type Chat =
    { ChatId: int; Messages: List<Message> }
