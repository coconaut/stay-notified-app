namespace NotificationLibrary

module Notifications = 
    open EnumsAndTypes
    open System
    
    /// notification record
    [<CLIMutable>]
    [<Serializable>]
    type Notification = 
        {
            _id : string
            source : string
            notification: string
            notification_method : NotificationMethod
            status : NotificationStatus
            pub_date : DateTime
            identifier : string
        }
        
    
    /// creating a new notification - just a convenience function
    let createNotification source notification noti_meth status pub_date identifier = 
        {
            _id = null;
            source = source; 
            notification = notification; 
            notification_method = noti_meth; 
            status = status; 
            pub_date = pub_date;
            identifier = identifier
        }

    /// copy and update expression for status
    let changeStatus notification status = 
        {notification with status=status}

        