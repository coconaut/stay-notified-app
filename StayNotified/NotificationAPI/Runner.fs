namespace NotificationAPI

    module Runner =
        open System
        open System.ServiceProcess
        open Service
        open Program

        [<EntryPoint>]
        let main argv =
            if not Environment.UserInteractive then
                let service = new NotificationService()
                let servicesToRun = [| service :> ServiceBase|]
                ServiceBase.Run(servicesToRun)
                0
            else 
            Program.RunProgram(argv)
    

