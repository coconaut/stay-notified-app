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



[<EntryPoint>]
let main argv = 

    // start the server -------------------------------------
    let baseAddress = ConfigurationManager.AppSettings.["APIBase"]
    try 
        let server = WebApp.Start<Startup>(baseAddress)
    
        serverLog ("Server listening at: " + baseAddress)

        // TWITTER -------------------------------------
    
        // start json agent(s)
        serverLog "Starting Twitter JSON to Mongo Agent..."
        let tweetDBAgent = new TweetDBAgent()
        tweetDBAgent.AddError(fun ex -> printfn "%s" ex.Message)
        tweetDBAgent.Start()
        serverLog "Agent started"
    
        // start Twitter listener -> inject ability to post to our agent
        serverLog "Starting Twitter Consumer..."
        let handler_function(str) = tweetDBAgent.Post(str)
        let twitterSupervisor = new TwitterSupervisor(handler_function, 60000, 300000)
        twitterSupervisor.AddError(fun ex -> printfn "%s" ex.Message)
        twitterSupervisor.Start()
        serverLog "Listening for tweets..."

        // RSS -------------------------------------

        // start Mongo poster - listener
        let rssDBAgent = new RSSDBAgent()
        let rss_post_func(notification) = rssDBAgent.Post(notification)
        rssDBAgent.Start()

        // start the supervisor - tell it to tell workers to post to our RSS-to-Mongo Agent
        let rssSupervisor = new RSSSupervisor(rss_post_func, 300000)
        rssSupervisor.AddError(fun ex -> printfn "%s" ex.Message)
        rssSupervisor.Start()
        rssSupervisor.Post(StartReader)
        serverLog "RSS Started"
    

        // Server exiting -------------------------------------

        serverLog "Press enter to exit"
        Console.ReadLine() |> ignore
        serverLog "Exiting..."

        // Twitter clean up
        twitterSupervisor.Post(KillTwitterSupervisor)

        // RSS clean up
        rssSupervisor.Post(KillSupervisor)

        server.Dispose()
        0 // return an integer exit code
    with 
    | ex -> 
        printfn "%s" ex.Message
        printfn "%s" ex.InnerException.Message
        1