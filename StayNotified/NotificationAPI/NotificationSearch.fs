namespace NotificationAPI
    
    module NotificationSearch =
        open NotificationLibrary.EnumsAndTypes
        open NotificationLibrary.Helpers
        open System

        [<CLIMutable>]
        type RawNotificationSearch =
            {
                source_str: string;
                status_str: string;
                date_start_str: string;
                date_end_str: string;
            }

        let parseDateOrDefault(str, default_date) = 
            let tryDate = ref default_date
            if not (DateTime.TryParse(str, tryDate)) then
                default_date
            else
                !tryDate 

        let parseDateOrMin(str) = 
            parseDateOrDefault(str, DateTime.MinValue)

        let parseDateOrMax(str) = 
            parseDateOrDefault(str, DateTime.MaxValue)

        type NotificationSearch(source : string, status: string, date_start: string, date_end: string) =
            member this.source = source
            member this.status = 
                match status.ToLower() with
                | "pending" -> NotificationStatus.Pending
                | "reviewed" -> NotificationStatus.Reviewed
                | _ -> NotificationStatus.All
            member this.date_start = parseDateOrMin(date_start)
            member this.date_end = parseDateOrMax(date_end)
                

               


