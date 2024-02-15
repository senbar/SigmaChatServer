namespace SigmaChatServer.Models

open System

[<CLIMutable>]
type MessageModel =
    { MessageId: int
      ChatId: int
      UserNickname: string option
      Text: string
      DateCreated: DateTime }

type CreateMessageModel = { ChatId: int; Text: string }

[<CLIMutable>]
type WebPushSubscriptionModel = { ChatId: int; Json: string }

[<CLIMutable>]
type Chat =
    { ChatId: int
      Messages: List<MessageModel> }

[<CLIMutable>]
type User =
    { Id: string
      Email: string option
      Nickname: string }

[<CLIMutable>]
type UpdateMeModel = { Nickname: string }
