namespace NotificationLibrary

module RSS =

    open FSharp.Data
    open System
    open System.Xml.Linq
    open Notifications
    open EnumsAndTypes
    open SourceReader
    open Microsoft.FSharp.Collections

    /// load up default RSS specification
    type rss = FSharp.Data.XmlProvider<"sources/rss_schema.xml">

    /// creating a new RSS notification
    let createRSS source notification pub_date identifier = 
        createNotification source notification NotificationMethod.RSS NotificationStatus.Pending pub_date identifier

    /// maps a single RSS item to a notification record
    let private mapRSSItem source (item : rss.Item) = 
        createRSS source item.Description item.PubDate (item.Guid.ToString())
    
    /// tries to process a feed - if fails, returns an empty List<Notification> for convenience
    let tryProcessFeed (url: string) start = 
        try
            let feed = rss.Load(url)
            let source = feed.Channel.Title
            let mapper = mapRSSItem source
            feed.Channel.Items
            |> Seq.filter(fun (i : rss.Item) -> i.PubDate >= start)
            |> Seq.map mapper
            |> Seq.toList
        with
            | ex -> //printfn "%s" ex.Message
                    [] // todo: how do we want to handle errors?
   
    

    /// agent for pulling RSS from a source, injectable handler function
    type RSSAgent(handler_function, source, wait_time) = 
        let agent = new MailboxProcessor<AgentMessage>(fun inbox ->
            let rec loop time_from = async {
                // wait -> if told to die, bail out. Otherwise pull
                let! msg = inbox.TryReceive wait_time
                match msg with 
                | Some Die -> 
                    //printfn "Killing %s" source
                    failwith DeathMessage
                | None ->
                    //printfn "Looping %s" source
                    let new_time = DateTime.Now
                    let notifications = tryProcessFeed source time_from
                    // send for processing
                    match notifications with
                    | [] -> return! loop new_time
                    | notes -> 
                        handler_function(notes)
                        // loop and look for new feeds
                        return! loop new_time
            }
            loop DateTime.Now
        )

        member this.Start() = agent.Start()
        member this.Post(msg) = agent.Post(msg)
        member this.AddError(error_func) = agent.Error.Add(error_func)


    type SupervisorMessage = 
    | Create of string
    | Remove of string
    | Error of string * string
    | KillSupervisor

    /// make a supervisor to handle errors, add and remove agents as needed
    type RSSSupervisor(handler_function, wait_time) = 
        let agent = new MailboxProcessor<SupervisorMessage>(fun inbox ->
            let reader_name = "reader"

            // starts an RSS agent
            let start_agent (registry : Map<string, RSSAgent>) source_name = 
                //printfn "starting agent %s" source_name
                let sub_agent = new RSSAgent(handler_function, source_name, wait_time)
                // add to registry
                let new_reg = registry.Add(source_name, sub_agent)
                sub_agent.AddError(fun ex -> inbox.Post(Error(source_name, ex.Message)))
                sub_agent.Start()
                new_reg
                
            // restarts an RSS agent
            let restart_agent (registry : Map<string, RSSAgent>) source_name = 
                //printfn "restarting agent %s" source_name
                let new_reg = start_agent (registry.Remove(source_name)) source_name
                new_reg

            // starts (or re-starts) the reader agent -> should only ever be one instance
            // TODO: supervisor should maintain ref to reader as well...
            let start_reader() = 
                //printfn "starting reader"
                let add_func(sn) = inbox.Post(Create(sn))
                let remove_func(sn) = inbox.Post(Remove(sn))
                let error_func(ex : Exception) = inbox.Post(Error(reader_name, ex.Message))
                let reader = new SourceReaderAgent(add_func, remove_func, ignore, wait_time, NotificationMethod.RSS)
                reader.AddError(error_func)
                // MAKE SURE YOU ACTUALLY START IT
                reader.Start()
                reader

            // main msg loop
            let rec loop (registry : Map<string, RSSAgent>, reader: SourceReaderAgent) = async {
                let! msg = inbox.Receive()
                match msg with
                | Create source_name ->
                    //printfn "supervisor trying to create %s" source_name
                    // check registry
                    match registry.ContainsKey source_name with
                    | true -> ()
                    | false -> 
                        let new_reg = start_agent registry source_name
                        return! loop (new_reg, reader)

                | Remove source_name ->
                    //printfn "supervisor trying to remove %s" source_name
                    // check registry
                    match registry.ContainsKey source_name with
                    | true -> 
                        // tell agent to die
                        //printfn "found source to remove -> killing..."
                        (registry.[source_name]).Post(Die)
                        // remove from registry
                        return! loop (registry.Remove(source_name), reader)
                    | false -> 
                        //printfn "couldn't find source to remove!!!"
                        ()

                | Error (source_name, ex_msg) ->
                    //printfn "supervisor got error %s for %s" ex_msg source_name
                    // if we didn't tell it to die then restart it
                    match ex_msg with
                    | msg when msg = DeathMessage -> ()
                    | _ -> 
                        match source_name with 
                        | n when n = reader_name -> 
                            //printfn "RSS Reader Error %s" (DateTime.Now.ToString())
                            return! loop (registry, start_reader())
                        | sn -> 
                            //printfn "RSS Source Error: %s %s" sn (DateTime.Now.ToString())
                            let new_reg = restart_agent registry sn
                            return! loop(new_reg, reader)
                        
                | KillSupervisor -> 
                    //printfn "Supervisor about to die"
                    // kill children
                    registry
                    |> Seq.iter(fun map -> map.Value.Post(Die))
                    // die
                    failwith "Supervisor killed"
                return! loop(registry, reader)
            }
            loop(Map.empty, start_reader())
        )

        member this.Start() = agent.Start()
        member this.Post(msg) = agent.Post(msg)
        member this.AddError(error_func) = agent.Error.Add(error_func)


