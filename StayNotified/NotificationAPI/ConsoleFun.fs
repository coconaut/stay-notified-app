namespace NotificationAPI
    module ConsoleFun =
        open System

        // tracing function
        let trace (message : string) = 
            Console.ForegroundColor <- ConsoleColor.Green
            Console.WriteLine message
            Console.ForegroundColor <- ConsoleColor.White

        let traceError (message : string) = 
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine message
            Console.ForegroundColor <- ConsoleColor.White

        // blue fun
        let serverLog (message : string) = 
            Console.ForegroundColor <- ConsoleColor.Blue
            Console.WriteLine message
            Console.ForegroundColor <- ConsoleColor.White

        // debugging helpers
        let logEx msg = 
            traceError ("Caught exception: " + msg)