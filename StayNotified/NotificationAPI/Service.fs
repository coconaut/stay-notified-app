namespace NotificationAPI

module Service = 

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
    open System.Diagnostics
    open Program


    type public NotificationService() as service = 
        inherit ServiceBase(ServiceName = "NotificationService")

        // event log
        let eventLog = new EventLog()
        let eventSource = "NotificationService"
        
        let initService =  
            if not (EventLog.SourceExists(eventSource)) then
                EventLog.CreateEventSource(eventSource, "Application")

            eventLog.Source <- eventSource
            eventLog.Log <- "Application"

//        let logger_function(str) = eventLog.WriteEntry(str)
        let logger_function(str) = trace(str:string)
        let program = new NotificationProgram(logger_function)
       
        // init service
        let initService = 
            
            // service name
            service.ServiceName <- "NotificationService"
            
            // event log
            let eventSource = "NotificationService" 
            if not (EventLog.SourceExists(eventSource)) then
                EventLog.CreateEventSource(eventSource, "Application")

            eventLog.Source <- eventSource
            eventLog.Log <- "Application"
        
        do initService   
        
        // start
        override service.OnStart(args) =
            base.OnStart(args)
            program.start()
            

        // stop
        override service.OnStop() =
            program.stop()
            base.OnStop()
        
    // Service Runner   
    let RunService(argv) =
        let service = new NotificationService()
        let servicesToRun = [| service :> ServiceBase|]
        ServiceBase.Run(servicesToRun)
        0


   