namespace NotificationLibrary
    
module EnumsAndTypes = 
    
    type NotificationMethod = 
    | RSS
    | Twitter
    | Email
    | Unknown

    type NotificationStatus = 
    | Reviewed
    | Pending
    | All // this is a punt. I'm fed up with Mongo not liking option types.

    type AgentMessage = 
    | Die

    let DeathMessage = "Agent Killed"
