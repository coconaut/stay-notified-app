namespace NotificationLibrary

    module SourceReader =
        open EnumsAndTypes


        /// agent to read sources periodically
        type SourceReaderAgent(add_func, remove_func, logger_func, wait_time, noti_meth) = 
            let agent = new MailboxProcessor<AgentMessage>(fun inbox ->
                logger_func("reader started")
                let rec loop (source_state : Set<string>) = async {
                    let! msg = inbox.TryReceive(wait_time)
                    match msg with
                    | Some Die -> 
                        logger_func("killing reader")
                        failwith DeathMessage
                    | None ->
                        // check sources for any updates
                        //logger_func("reader updating sources")
                        let sources = 
                            Helpers.getSources noti_meth
                            |> Set.ofSeq

                        // compare with state
                        let to_remove = Set.difference source_state sources
                        let to_add = Set.difference sources source_state
                    
                        if not (noti_meth = NotificationMethod.Twitter) then
                            // remove - TODO: this is bad for Twitter streaming, should only be one call...
                            to_remove
                            |> Seq.iter remove_func
                    
                            // add - TODO: this is bad for Twitter streaming
                            to_add
                            |> Seq.iter add_func
                        else
                            // only need to restart once if change -> really, should create it's own reader but...
                            if to_remove.Count > 0 || to_add.Count > 0 then
                                add_func ""
                    
                        // loop
                        return! loop sources
                }
                loop (Helpers.getSources noti_meth |> Set.ofSeq)
            )

            member this.Start() = agent.Start()
            member this.Post(msg) = agent.Post(msg)
            member this.AddError(error_func) = agent.Error.Add(error_func)
