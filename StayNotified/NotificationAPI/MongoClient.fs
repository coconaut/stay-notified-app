namespace NotificationAPI

module Mongo =
    open FSharp.Data
    open MongoDB.Driver
    open MongoDB.Bson
    open MongoDB.Bson.Serialization
    open MongoDB.Bson.Serialization.Options
    open MongoDB.Bson.Serialization.Serializers
    open System.Collections.Generic
    open System.Configuration
    open System.Threading.Tasks
    open NotificationLibrary.Helpers
    open NotificationLibrary.Notifications
    open NotificationLibrary.EnumsAndTypes
    open System.Text.RegularExpressions
    
    /// op implicit helper operator
    let inline (~~) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)

    /// custom serializer for our NotificationMethod discriminated union
    type NotificationMethodSerializer() =
        inherit MongoDB.Bson.Serialization.Serializers.SerializerBase<NotificationMethod>()

        override this.Serialize(context, args, value) =
            let str = 
                match value with
                | NotificationMethod.Twitter -> "Twitter"
                | NotificationMethod.Email -> "Email"
                | NotificationMethod.RSS -> "RSS"
                | _ -> "Unknown"

            context.Writer.WriteString(str)

        override this.Deserialize(context, args) =
            match context.Reader.ReadString() with
            | "Twitter" -> NotificationMethod.Twitter
            | "Email" -> NotificationMethod.Email
            | "RSS" -> NotificationMethod.RSS
            | _ -> NotificationMethod.Unknown
                     
    /// custom serializer for our NotificationStatus discriminated union
    type NotificationStatusSerializer() =
        inherit MongoDB.Bson.Serialization.Serializers.SerializerBase<NotificationStatus>()

        override this.Serialize(context, args, value) =
            let str = 
                match value with
                | NotificationStatus.Pending -> "Pending"
                | NotificationStatus.Reviewed -> "Reviewed"
                | NotificationStatus.All -> "All"
            
            context.Writer.WriteString(str)

        override this.Deserialize(context, args) =
            match context.Reader.ReadString() with
            | "Pending" -> NotificationStatus.Pending
            | "Reviewed" -> NotificationStatus.Reviewed
            // going to say unknowns are still pending...
            | _ -> NotificationStatus.Pending


    /// mapping from string id to objectId, in case we want to switch from Mongo at some point
    let registerNotificationMap = 
        BsonClassMap.RegisterClassMap<Notification>(fun cm ->
            // map the other fields
            cm.AutoMap()
            
            // map the _id field
            cm.MapIdField<string>(fun n -> n._id).SetSerializer(new StringSerializer(BsonType.ObjectId)) |> ignore

            // map our discriminated union types
            cm.MapField<NotificationMethod>(fun n -> n.notification_method).SetSerializer(new NotificationMethodSerializer()) |> ignore
            cm.MapField<NotificationStatus>(fun n -> n.status).SetSerializer(new NotificationStatusSerializer()) |> ignore
        )

    /// gets our Mongo server
    let getMongoContext = 
        let server = NotificationLibrary.Helpers.getAppSetting "MongoServer"
        if (not (BsonClassMap.IsClassMapRegistered(typeof<Notification>))) then
            registerNotificationMap |> ignore
        let con_str = @"mongodb://" + server
        new MongoClient(con_str)

    /// gets our database
    let getDatabase =
        let ctx = getMongoContext
        ctx.GetDatabase("ThirdPartyNotifications")

    /// gets our collection
    let getCollection = 
        let db = getDatabase
        db.GetCollection<Notification>("notification_db")
        
    /// methods for the Notification collection
    type NotificationDB() =
        let collection = getCollection
        interface INotificationDB with
            // gets a list of notifications
            member this.GetNotificationList() = 
                collection.Find<Notification>(fun x -> true).ToListAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously

            // search
            member this.SearchNotifications(ns) =
                let source_filter = Builders<Notification>.Filter.Regex(~~"source", (new BsonRegularExpression(ns.source, "i")))
                let ds = Builders<Notification>.Filter.Gte((fun n -> n.pub_date), ns.date_start)
                let de = Builders<Notification>.Filter.Lte((fun n -> n.pub_date), ns.date_end)
                let stat = Builders<Notification>.Filter.Eq((fun n -> n.status), ns.status)
                let filters = 
                    [ds; de]
                let with_status = 
                    match ns.status with
                    | NotificationStatus.All -> filters
                    | _ -> stat::filters
                let final_filters = 
                    match ns.source with 
                    | "" -> with_status
                    | _ -> source_filter::with_status
                let def = Builders<Notification>.Filter.And(final_filters)

                collection.Find<Notification>(def)
                    .ToListAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                

            // gets a specific notification by id
            member this.GetNotification id =
                collection.Find<Notification>(fun n -> n._id = id).FirstOrDefaultAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously

            // replaces a notification with an updated one
            member this.PutNotification n = 
                collection.ReplaceOneAsync<Notification>((fun old -> old._id = n._id), n)
                |> Async.AwaitIAsyncResult
                |> Async.RunSynchronously

            member this.MarkReviewed id = 
                let filter = Builders<Notification>.Filter.Eq((fun old -> old._id), id)
                let update = Builders<Notification>.Update.Set((fun old -> old.status), NotificationStatus.Reviewed)
                collection.FindOneAndUpdateAsync(filter, update)
                |> Async.AwaitIAsyncResult
                |> Async.RunSynchronously

            // posts a new notification
            member this.PostNotification n = 
                // pop in a new Id first
                {n with _id = ObjectId.GenerateNewId().ToString()}
                |> collection.InsertOneAsync
                |> Async.AwaitIAsyncResult
                |> Async.RunSynchronously
                |> ignore

            // posts a list of notifications
            member this.BatchPostNotifications notifications =
                // make this unordered so that each write will occur (and error on dups) in //
                // and not affect the others
                let options = new InsertManyOptions()
                options.IsOrdered <- false

                // set up functional helper
                let insertMany notes = 
                    collection.InsertManyAsync(notes, options)

                // insert
                notifications
                |> Seq.map (fun n -> {n with _id = ObjectId.GenerateNewId().ToString()}) // pop in a new Id first
                |> insertMany
                |> Async.AwaitIAsyncResult
                |> Async.RunSynchronously


            
    
    
   

                
                
                
                
                

