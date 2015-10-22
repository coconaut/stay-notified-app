namespace NotificationLibrary

    module SourceReader =
        open EnumsAndTypes


        /// agent to read sources periodically
        type SourceReaderAgent(add_func, remove_func, wait_time, noti_meth) = 
            let agent = new MailboxProcessor<AgentMessage>(fun inbox ->
                let rec loop (source_state : Set<string>) = async {
                    let! msg = inbox.TryReceive(wait_time)
                    match msg with
                    | Some Die -> 
                        printfn "killing reader"
                        failwith DeathMessage
                    | None ->
                        // check sources for any updates
                        printfn "reader updating sources"
                        let sources = 
                            Helpers.getSources noti_meth
                            |> Set.ofSeq

                        // compare with state
                        let to_remove = Set.difference source_state sources
                        let to_add = Set.difference sources source_state
                    
                        // remove
                        to_remove
                        |> Seq.iter remove_func
                    
                        // add
                        to_add
                        |> Seq.iter add_func
                    
                        // loop
                        return! loop sources
                }
                loop Set.empty
            )

            member this.Start() = agent.Start()
            member this.Post(msg) = agent.Post(msg)
            member this.AddError(error_func) = agent.Error.Add(error_func)
