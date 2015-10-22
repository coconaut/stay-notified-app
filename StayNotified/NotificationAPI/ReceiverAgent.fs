namespace NotificationAPI
    module ReceiverAgent =
        open NotificationsController
        open NotificationLibrary
        open NotificationLibrary.Notifications
        open System

        /// agent for processing incoming tweets
        type TweetDBAgent() = 
            let agent = new MailboxProcessor<string>(fun inbox ->
                let controller = new NotificationsController()
                let rec loop() = async {
                    let! msg = inbox.Receive()
                    printfn "%s" msg
                    // parse tweet into notification option
                    let tweet = TwitterCommon.Tweet.Parse(msg)
                    match (TwitterStreaming.mapStreamingTweetToNotification tweet) with 
                    | Some notification -> 
                        controller.Post notification
                        return! loop() 
                    | None -> return! loop() 
                }
                loop()
            )

            member this.Start() = agent.Start()
            member this.Post(msg) = agent.Post(msg)
            member this.AddError(func) = agent.Error.Add(func)
    
        /// agent for processing incoming RSS (already notificationized)
        type RSSDBAgent() = 
            let agent = new MailboxProcessor<List<Notification>>(fun inbox ->
                let controller = new NotificationsController()
                let rec loop() = async {
                    let! msg = inbox.Receive()
                    controller.BatchPost(msg) |> ignore
                    return! loop()
                
                }
                loop()
            )

            member this.Start() = agent.Start()
            member this.Post(msg) = agent.Post(msg)
            member this.AddError(func) = agent.Error.Add(func)
