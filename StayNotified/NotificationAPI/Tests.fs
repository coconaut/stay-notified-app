namespace NotificationAPI
    module Tests =
        open NotificationLibrary
        open NotificationLibrary.EnumsAndTypes
        open NotificationLibrary.Helpers
        open NotificationLibrary.RSS
        open ConsoleFun
        open System
        open TwitterRest

        (* These are more intergration tests than unit tests, 
         * but you can edit these and run them from main if
         * you want to make sure the calls and Mongo client
         * are working properly.
         *)

        // get controller
        let controller = new NotificationsController.NotificationsController()

        // test format
        let runTest test name = 
            trace ("Test: " + name)
            try
                let res_message = test()
                trace ("Test complete. Result: " + res_message)
            with
            | ex -> logEx ex.Message
            

        // Twitter REST API
        let twitterPost() = 
            let (tweets, bad_auth) = getUserTimelineNotifications "ElixirConf" 8
            let res = 
                match tweets with
                | [] -> false
                | tweet_list -> tweet_list |> controller.BatchPost
            res.ToString()

        // Twitter User Auth
        let twitterAuth() = 
            TwitterAuth.getRequestHeader [("screen_name", "docker"); ("count", "5")] "GET" "https://api.twitter.com/1.1/statuses/user_timeline.json"

            
        // RSS feed
        let rssPost() = 
            let testDate = DateTime.op_Addition(DateTime.Now, TimeSpan.FromDays(-10.))
            let res = 
                (Helpers.getSources (NotificationMethod.RSS)).[1]
                |> (RSS.tryProcessFeed) <| testDate
                |> controller.BatchPost
            res.ToString()


        // Retrieving notifications
        let notificationRetrieve() = 
            controller.Get()
            |> Seq.map (fun n -> sprintf "%A" n)
            |> Seq.reduce(fun acc next -> acc + "\n" + next)

        // tests
        let twitterTest() = 
            runTest twitterPost "Twitter"

        let rssTest() =
            runTest rssPost "RSS"

        let retrieveTest() =
            runTest notificationRetrieve "Retrieve"

        let twitterAuthTest() = 
            runTest twitterAuth "Twitter Auth"

        let runAllTests() = 
            twitterTest()
            rssTest()
            twitterAuthTest()
            retrieveTest()



            