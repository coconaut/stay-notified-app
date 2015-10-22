namespace NotificationAPI

module ValuesController =

    open System.Collections.Generic
    open System.Web.Http

    type ValuesController() =
        inherit ApiController()

        member this.Get() = 
            "hello"

        