namespace SigmaChatServer

module Routing =

    open Giraffe
    open Microsoft.AspNetCore.Http
    open HttpHandlers

    let messages: HttpFunc -> HttpContext -> HttpFuncResult =
        choose [ GET >=> routef "/%i" handleGetMessages; POST >=> handlePostMessage ]

    let routing: HttpFunc -> HttpContext -> HttpFuncResult =
        choose
            [ subRoute
                  "/api"
                  (choose
                      [ subRoute
                            "/chat"
                            (choose [ GET >=> routef "/%i" (fun id -> handleGetChats id); POST >=> handlePostChat ])
                        subRoute "/db" (choose [ GET >=> updateSchema ])
                        subRoute "/messages" (messages) ])
              setStatusCode 404 >=> text "Not Found" ]
