namespace NotificationLibrary

module TwitterStreaming = 

    open FSharp.Data
    open System
    open System.Security.Cryptography
    open System.Xml.Linq
    open System.Net
    open System.Web
    open Notifications
    open EnumsAndTypes
    open RestSharp
    open System.Threading
    open System.IO
    open Microsoft.FSharp.Control.WebExtensions
    open Helpers
    open RequestHelpers
    open TwitterAuth
    open TwitterCommon
    open SourceReader

   
    let getSourceIDString() = 
        Helpers.getSources NotificationMethod.Twitter
        |> Seq.reduce(fun acc next -> next + "," + acc)
        
    let createStreamingRequest() =
        // get request info
        let source_ids = getSourceIDString()
        let req_method = "POST"
        let base_url = Helpers.gettwitterStreamingEndpoint()
        let request_header = 
            getRequestHeader [("follow", source_ids); ("delimited", "length")] req_method base_url
        
        // get bytes for request body
        let dataStream = 
            sprintf "follow=%s&delimited=length" source_ids
            |> System.Text.Encoding.UTF8.GetBytes
        // create request
        let req = 
            WebRequest.Create(base_url)
            |> addWebReqHeader "Authorization" request_header
            |> setWebReqMethod req_method
            |> setWebReqContentType "application/x-www-form-urlencoded"
            |> setWebReqContentLength dataStream.LongLength
     
        // write the body
        let stream = req.GetRequestStream()
        stream.Write(dataStream, 0, dataStream.Length)
        stream.Close()
        req

    /// is numeric active pattern
    let (|NUMERIC|_|) str = 
        let x = ref 0
        if Int32.TryParse(str, x) then
            Some(!x)
        else None

    /// the Twitter Consumer
    type TwitterConsumer() = 
        let mutable group = new CancellationTokenSource()

        // start listening for tweets -> inject a function for handling them
        let startListening(handler_function, error_function) = 
            try
                let listener() = 
                    async {
                        let req = createStreamingRequest()
                        use! resp = req.AsyncGetResponse()
                        use stream = resp.GetResponseStream()
                        use reader = new StreamReader(stream)
                        let rec loop() = 
                            async {
                                let atEnd = reader.EndOfStream
                                if not atEnd then
                                    let line = reader.ReadLine()
                                    // do stuff
                                    match line with
                                    | "" -> () //printfn "Stayin' alive"
                                    | NUMERIC num_bytes -> 
                                        //printfn "Num bytes: %i" num_bytes
                                        // read the number of bytes of message into buffer
                                        let buffer = Array.zeroCreate num_bytes
                                        let num_read = reader.ReadBlock(buffer, 0, num_bytes)
                                        let text = new System.String(buffer)

                                        // call injected function for processing
                                        handler_function(text)
                                    | _ -> ()
                                
                                return! loop()
                            }
                        return! loop()
                    }
                Async.Start(listener(), group.Token)
            with 
            | ex -> error_function(ex)        
        // stop agent
        let stopListening() =
            group.Cancel()
            group <- new CancellationTokenSource()

        // restart -> in case sources change, we need to redo the request and Twitter auth anyway...
        let restart(handler_function, error_function) = 
            stopListening()
            startListening(handler_function, error_function)

        member this.StartListening(handler_function, error_function) = startListening(handler_function, error_function)
        member this.StopListening() = stopListening()
        member this.Restart(handler_function, error_function) = restart(handler_function, error_function)



    // active pattern for making sure valid tweet
    let (|TWEET|NOTWEET|) (tweet: Tweet.Root) = 
        if tweet.Text.IsSome && tweet.IdStr.IsSome && tweet.User.IsSome then
            TWEET
        else
            NOTWEET

    // active pattern for filtering out replies -> make sure valid first...
    let (|REPLY|NOTREPLY|) (tweet: Tweet.Root) = 
        let rtsn =  
            match tweet.InReplyToScreenName with
            | Some(t) -> if t <> "" then true else false
            | _ -> false
        let rtsi = 
            match tweet.InReplyToStatusId with
            | Some(t) -> if t > (int64 0) then true else false
            | _ -> false
        let rtsu = 
            match tweet.InReplyToUserId with
            | Some(t) -> if t > 0 then true else false
            | _ -> false

        let rt =
            if tweet.Text.Value.Length > 2 then
                if (tweet.Text.Value.[0..1].ToLower()) = "rt" then true
                else false
            else false
        if rtsn || rtsi || rtsu || rt then REPLY
        else NOTREPLY
            

    /// returns a Notification option
    let mapStreamingTweetToNotification (tweet: Tweet.Root) = 
        match tweet with
        | TWEET ->
            match tweet with
            | NOTREPLY ->
                let use_date = 
                    match tweet.CreatedAt with
                    | Some c -> parseDateOrNow c
                    | None -> DateTime.Now
                Some(createTweet tweet.User.Value.Name tweet.Text.Value use_date (tweet.IdStr.Value.ToString()))
            | REPLY -> None
        | NOTWEET -> None
        
    type TwitterSupervisorMessage = 
    | StopConsumer
    | RestartConsumer
    | KillTwitterSupervisor
    | ConsumerError of string
    | ReaderError of string

    type TwitterSupervisor(handler_function, logger_function, source_wait_time, restart_wait_time) =
        let agent = new MailboxProcessor<TwitterSupervisorMessage>(fun inbox ->
            
            // symbols for error handling, think erlang atoms
            let twitter_agent = "twitter_agent"
            let reader_name = "reader"

            let consumer_error_func(ex : exn) = inbox.Post(ConsumerError(ex.Message))

            let startConsumer() = 
                logger_function("Starting Twitter Consumer")
                let new_consumer  = new TwitterConsumer()
                new_consumer.StartListening(handler_function, consumer_error_func)
                new_consumer

            let startReader() = 
                // need to form new request if any source changes -> don't really care what the change was, since 
                // auth forces us to reform whole request anyway
                logger_function("Starting Twitter Reader")
                let error_func(ex : exn) = inbox.Post(ReaderError(ex.Message))
                let restart_func(_) = inbox.Post(RestartConsumer)
                let reader = new SourceReaderAgent(restart_func, restart_func, logger_function, source_wait_time, NotificationMethod.Twitter)
                // MAKE SURE YOU ACTUALLY START IT
                reader.Start()
                reader

            let rec loop(consumer : TwitterConsumer, reader: SourceReaderAgent) = async {
                let! msg = inbox.Receive()
                match msg with 
                | StopConsumer -> consumer.StopListening()
                | RestartConsumer -> 
                    // wait a few, then restart the connection
                    logger_function("Restarting Twitter Consumer")
                    do! Async.Sleep(restart_wait_time)
                    consumer.Restart(handler_function, consumer_error_func)
                | KillTwitterSupervisor ->
                    consumer.StopListening()
                    failwith DeathMessage
                | ConsumerError(exn_msg) ->
                    match exn_msg with
                    | s when s = DeathMessage -> ()
                    | _ -> 
                        // wait a few minutes, then try to form new connection to Twitter
                        logger_function(sprintf "Twitter Consumer Error %s" (DateTime.Now.ToString()))
                        do! Async.Sleep(restart_wait_time)
                        return! loop(startConsumer(), reader)
                | ReaderError(exn_msg) ->
                    match exn_msg with
                    | s when s = DeathMessage -> ()
                    | _ -> 
                        logger_function(sprintf "Twitter Reader Error %s" (DateTime.Now.ToString()))
                        return! loop(consumer, startReader())
                return! loop(consumer, reader)
            }
            loop(startConsumer(), startReader())                   
        )

        member this.Start() = agent.Start()
        member this.Post(msg) = agent.Post(msg)
        member this.AddError(error_func) = agent.Error.Add(error_func)
