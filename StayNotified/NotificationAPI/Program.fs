namespace NotificationAPI

    module Program =

        open Microsoft.Owin.Hosting
        open System
        open System.Configuration
        open System.Net.Http
        open NotificationAPI.Startup
        open NotificationAPI.ConsoleFun
        open NotificationAPI.Tests
        open NotificationAPI.ReceiverAgent
        open NotificationLibrary.TwitterStreaming
        open NotificationLibrary.RSS
        open System.ServiceProcess
        open System.Diagnostics;


        // just the pure program 
        type public NotificationProgram(log) =
  
            let baseAddress = ConfigurationManager.AppSettings.["APIBase"]

            // define -- twitter
            let tweetDBAgent = new TweetDBAgent()
            let handler_function(str) = tweetDBAgent.Post(str)
            let twitterSupervisor = new TwitterSupervisor(handler_function, log, 300000, 300000)
       
            // define -- rss
            let rssDBAgent = new RSSDBAgent()
            let rss_post_func(notification) = rssDBAgent.Post(notification)
            let rssSupervisor = new RSSSupervisor(rss_post_func, 300000)
        
  
            // init -- server
            let server = WebApp.Start<Startup>(baseAddress)

            // start
            member this.start() =
                   
                log("Service started")
                log((sprintf "Listening at: %s" baseAddress))

                // twitter 
                tweetDBAgent.AddError(fun ex -> log ex.Message)
                twitterSupervisor.AddError(fun ex -> log ex.Message)
                tweetDBAgent.Start()
                log("Tweet DB agent started")
                twitterSupervisor.Start()
                log("Twitter supervisor started")

                // rss
                rssDBAgent.Start()
                log("RSS DB agent started")
                rssSupervisor.AddError(fun ex -> log ex.Message)
                rssSupervisor.Start()
                log("RSS supervisor Started")


            member this.stop() =

                // Twitter clean up
                twitterSupervisor.Post(KillTwitterSupervisor)
                log("Sent Twitter clean up post")

                // RSS clean up
                rssSupervisor.Post(KillSupervisor)
                log("Sent RSS clean up post")

                // server
                server.Dispose()
                log("Server disposed")

        //[<EntryPoint>]
        let RunProgram(argv) = 

            // run the program
            let log_function(str) = trace(str)
       
            let program = new NotificationProgram(log_function)
            program.start()

            let rec waitForExit() =
                let line = Console.ReadLine()
                if line.ToLower() = "exit" then
                    program.stop()
                    0
                else
                    trace("Enter 'exit' to quit")
                    waitForExit()   

            waitForExit()

