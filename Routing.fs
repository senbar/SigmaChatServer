namespace SigmaChatServer

module Routing =

    open Giraffe
    open Microsoft.AspNetCore.Http
    open HttpHandlers
    open WebPush

    let notLoggedIn = RequestErrors.UNAUTHORIZED "Basic" "" "You must be logged in."

    let mustBeLoggedIn: HttpFunc -> HttpContext -> HttpFuncResult =
        requiresAuthentication notLoggedIn

    let messages: HttpFunc -> HttpContext -> HttpFuncResult =
        choose [ GET >=> routef "/%i" handleGetMessages; POST >=> handlePostMessage ]

    let routing: HttpFunc -> HttpContext -> HttpFuncResult =
        choose
            [ subRoute
                  "/api"
                  (choose
                      [ subRoute "/chat" mustBeLoggedIn
                        >=> (choose [ GET >=> routef "/%i" (fun id -> handleGetChats id); POST >=> handlePostChat ])
                        subRoute "/db" (choose [ GET >=> updateSchema ])
                        subRoute "/messages" mustBeLoggedIn >=> (messages)
                        subRoute "/user/me" mustBeLoggedIn
                        >=> (choose [ GET >=> handleGetUserMe; PATCH >=> handleUpdateMeProfile ])
                        subRoute "/callback" mustBeLoggedIn >=> (handleCallback)
                        subRoute "/web-push/subscribe" mustBeLoggedIn >=> (handleNewSubscription)
                        subRoute "/web-push/key" mustBeLoggedIn >=> (handleGetVapidKey) ])
              // subRoute "/web-push/custom-message" mustBeLoggedIn >=> (handlePushCustomMessage) ])
              setStatusCode 404 >=> text "Not Found" ]
