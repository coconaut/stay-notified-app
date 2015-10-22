namespace NotificationLibrary
    module TwitterRest =
        open FSharp.Data
        open RequestHelpers
        open Notifications
        open EnumsAndTypes
        open RestSharp
        open System
        open TwitterAuth
        open TwitterCommon
        

        // endpoints should pull from config 
        let private twitter = Helpers.getTwitterBase()
        let private timeline_endpoint = Helpers.getTwitterTimelineEndpoint()

        // error response    
        type ErrorResponse = JsonProvider<"""{"errors":[{"code":215,"message":"Bad Authentication data."}]}""">
    
        let private errorCheck (err : ErrorResponse.Error) = 
            err.Code = 215 || err.Message = "Bad Authentication data."
    
        // make requests
        let private makeAuthenticatedRequest (req : IRestRequest) token =
            let client = new RestClient(twitter)
            addRestHeader "Authorization" ("Bearer " + token) req
            |> client.Execute

        (* general request attempt -> idea is to return the twitter response, 
         * but also let us know if auth went bad by checking error codes ->
         * then we know in API to get a new token
         *)
        let private attemptRequest (req : IRestRequest) token =     
            let response = makeAuthenticatedRequest req token
            match response.Content with
            | null -> (None, false)
            | "" -> (None, false)
            | content ->
                match ErrorResponse.Parse(content).Errors with
                | [||] -> (Some content, false)
                | error_list -> (None, (error_list |> Seq.exists errorCheck))

        // try a request -> catch errors
        let private tryRequest (req: IRestRequest) =
            try 
                let (content, bad_auth) = 
                    Helpers.getTwitterToken()
                    |> (attemptRequest req)
                (content, bad_auth)
            with
            | ex -> printfn "%s" ex.Message
                    (None, false)
        
        // public request -> statuses/user_timeline request - may want to pass a since_id?
        let private getUserTimeline screen_name num_posts = 
            let (content, bad_auth) = 
                new RestRequest(timeline_endpoint, Method.GET)
                |> addRestQueryParameter "count" (num_posts.ToString())
                |> addRestQueryParameter "screen_name" screen_name
                |> tryRequest
            match content with
            | Some json_string -> (Some (UserTimelines.Parse(json_string)), bad_auth)
            | None -> (None, bad_auth)

        // map content -> may need to decide what content we want to grab here -> images? hashtags?
        let mapUserTimeline (utl : UserTimelines.Root) =
            createTweet utl.User.Name utl.Text (parseDateOrNow(utl.CreatedAt)) (utl.IdStr.ToString())
        
        // the public call
        let getUserTimelineNotifications screen_name num_posts = 
            let (posts, bad_auth) = getUserTimeline screen_name num_posts
            let notifications = 
                match posts with
                | Some post_list -> 
                    post_list 
                    |> Seq.map mapUserTimeline 
                    |> Seq.toList
                | None -> []
            (notifications, bad_auth)
       

   
