namespace NotificationLibrary

    module TwitterCommon =
        open System
        open FSharp.Data
        open Notifications
        open EnumsAndTypes

        /// Type provider for user_timeline request - from sample.
        type UserTimelines = JsonProvider<"sources/twitter_statuses_user_timeline.json">

        /// a bunch of possible streaming return types
        type Tweet = JsonProvider<"sources/Twitter_Types.json", SampleIsList=true>

        /// creating a new Twitter notification - just reducing params
        let createTweet source notification pub_date identifier = 
            createNotification source notification NotificationMethod.Twitter NotificationStatus.Pending pub_date identifier

        /// parses a DateTime from a string, or returns current date
        let parseDateOrNow str = 
            let tryDate = ref (new DateTime())
            if not (DateTime.TryParse(str,  tryDate)) then
                DateTime.Now
            else
                !tryDate
