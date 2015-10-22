namespace NotificationLibrary
module Helpers =
    open System
    open System.Configuration
    open NotificationLibrary.EnumsAndTypes
    open FSharp.Data

    /// gets an app setting
    let getAppSetting (key: string) = 
        ConfigurationManager.AppSettings.[key]

    /// type provider for sources json
    type SourceJson = JsonProvider<"sources/sources.json", InferTypesFromValues = false>

    /// converts a notification method to a string representation
    let getMethodString notification_method = 
        match notification_method with
            | NotificationMethod.Twitter -> "Twitter"
            | NotificationMethod.RSS -> "RSS"
            | NotificationMethod.Email -> "Email"
            | _ -> "Unknown"

    /// converts a string to a notification method
    let getNotificationMethod str = 
        match str with
            | "Twitter" -> NotificationMethod.Twitter
            | "Email" -> NotificationMethod.Email
            | "RSS" -> NotificationMethod.RSS
            | _ -> NotificationMethod.Unknown

    /// converts a notification status to a string representation
    let getStatusString notification_status = 
        match notification_status with
        | NotificationStatus.Pending -> "Pending"
        | NotificationStatus.Reviewed -> "Reviewed"
        | NotificationStatus.All -> "All"

    /// converts a string to a notification status
    let getNotificationStatus str = 
         match str with
            | "Pending" -> NotificationStatus.Pending
            | "Reviewed" -> NotificationStatus.Reviewed
            // going to say unknowns are still pending...
            | _ -> NotificationStatus.Pending
    
    /// gets list of sources from JSON config
    let getSources (notification_method : NotificationMethod) = 
        let sourceJson = SourceJson.Load("sources/sources.json")
        sourceJson.Methods
        |> Seq.filter(fun m -> m.NotificationMethod.ToLower() = (getMethodString notification_method).ToLower())
        |> Seq.map (fun m -> m.Sources)
        |> Seq.exactlyOne

    /// get keys / tokens -> should be encrypted...
    let getTwitterToken() = 
        getAppSetting "TwitterToken"

    let getConsumerKey() = 
        getAppSetting "ConsumerKey"

    let getConsumerSecret() = 
        getAppSetting "ConsumerSecret"

    let getAuthToken() = 
        getAppSetting "AuthToken"

    let getAuthSecret() = 
        getAppSetting "AuthSecret"

    let getTwitterBase() = 
        getAppSetting "TwitterBase"

    let getTwitterOAuthEndpoint() = 
        getAppSetting "TwitterOAuthEndpoint"

    let getTwitterTimelineEndpoint() = 
        getAppSetting "TwitterTimelineEndpoint"

    let gettwitterStreamingEndpoint() =
        getAppSetting "TwitterStreamingEndpoint"
    
