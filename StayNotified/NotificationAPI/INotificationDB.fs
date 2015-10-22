namespace NotificationAPI
    
    open NotificationLibrary.Notifications
    open NotificationSearch
    
    type INotificationDB = 
        abstract member GetNotificationList : unit -> System.Collections.Generic.List<Notification>
        abstract member GetNotification : string -> Notification
        abstract member PutNotification : Notification -> bool
        abstract member MarkReviewed : string -> bool
        abstract member PostNotification : Notification -> unit
        abstract member BatchPostNotifications : List<Notification> -> bool
        abstract member SearchNotifications : NotificationSearch -> System.Collections.Generic.List<Notification>


