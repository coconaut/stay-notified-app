namespace NotificationLibrary
    module RequestHelpers =
        open RestSharp
        open System.Web
        open System.Net

        /// RestSharp combinators
        let addRestHeader key value (request : IRestRequest) = 
            request.AddHeader(key, value)

        let addRestParameter key value (request: IRestRequest) = 
            request.AddParameter(key, value)

        let addRestQueryParameter key value (request: IRestRequest) = 
            request.AddQueryParameter(key, value)

        /// WebRequest combinators
        let addWebReqHeader (key : string) (value : string) (request : WebRequest) = 
            request.Headers.Add(key, value)
            request

        let setWebReqMethod method_str (request : WebRequest) = 
            request.Method <- method_str
            request

        let setWebReqContentType content_type (request : WebRequest) = 
            request.ContentType <- content_type
            request

        let setWebReqContentLength content_length (request : WebRequest) = 
            request.ContentLength <- content_length
            request