namespace NotificationLibrary

module TwitterAuth =
    open FSharp.Data
    open System
    open System.Security.Cryptography
    open System.Xml.Linq
    open System.Web
    open Notifications
    open EnumsAndTypes
    open RestSharp
    open RequestHelpers

    let private twitter = Helpers.getTwitterBase()
    let private oauth_endpoint = Helpers.getTwitterOAuthEndpoint()
    let private grant_type = "client_credentials"

    /// App Only Auth
    
    // encoder helper
    let private encode (some_string : string) = 
        let plainBytes = System.Text.Encoding.UTF8.GetBytes(some_string)
        System.Convert.ToBase64String(plainBytes)
    
    // make magic string from consumer key and secret
    let private makeSecret (key : string) (secret : string) =
        HttpUtility.UrlEncode(key) + ":" + HttpUtility.UrlEncode(secret)
        |> encode

    // login response
    [<CLIMutable>]
    type TokenResponse = { token_type : string; access_token : string }

    (* obtain bearer token - note: this pretty much doesn't expire and will keep returning the
    * same token unless it is revoked. We can keep this method to retrieve it if it goes null,
    * but maybe should try to preserve one instance somewhere secure to reduce requests.
    *)
    let getAppOnlyBearerToken key secret = 
        try
            let crazy_secret = makeSecret key secret
            let client = new RestClient(twitter)
            let request = 
                new RestRequest(oauth_endpoint, Method.POST)
                |> addRestHeader "Content-Type" "application/x-www-form-urlencoded;charset=UTF-8"
                |> addRestHeader "Authorization" ("Basic " + crazy_secret)
                |> addRestParameter "grant_type" "client_credentials"
        
            let response = client.Execute<TokenResponse>(request)
            response.Data.access_token
        with
            | ex -> printfn "%s" ex.Message
                    ""


     /// USER AUTH STUFF (needed for Streaming API)

    let urlEnc (str : string) = 
        // must comply with RFC 3986 -> DO NOT USE HTTPUTILITY.URLENCODE
        str |> Uri.EscapeDataString
        
    let encTup tup = 
        let (key, value) = tup
        ((urlEnc key), (urlEnc value))

    let getTwitterDateString() = 
        let ts = 
            new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc)
            |> DateTime.UtcNow.Subtract 
        let sec = 
            ts.TotalSeconds
            |> Convert.ToInt64
        sec.ToString()

    let getNonce() = 
       let enc = new System.Text.ASCIIEncoding()
       DateTime.Now.Ticks.ToString()
       |> enc.GetBytes
       |> Convert.ToBase64String
        
    let getDefaultParameters() = 
        let parameters = 
            [
                ("oauth_consumer_key", Helpers.getConsumerKey());
                ("oauth_nonce", getNonce());
                ("oauth_signature_method", "HMAC-SHA1");
                ("oauth_timestamp", getTwitterDateString());
                ("oauth_token", Helpers.getAuthToken())
                ("oauth_version","1.0");
            ]
        parameters

    let addParameters (tup_list : List<string*string>) parameters = 
        tup_list @ parameters

    let getParameterLine (parameters) = 
        parameters
        |> Seq.map encTup
        |> Seq.sortBy(fun (encoded_key, encoded_value) -> encoded_key)
        |> Seq.map(fun (encoded_key, encoded_value) -> encoded_key + "=" + encoded_value)
        |> Seq.reduce(fun acc next -> acc + "&" + next)

    let sigBase method_string base_url (parameterLine) = 
        let req_method = method_string
        let encoded_url = urlEnc base_url
        let encoded_params = urlEnc parameterLine
        sprintf "%s&%s&%s" req_method encoded_url encoded_params

    let getSigningKey() = 
        let encoded_consumer_secret = urlEnc (Helpers.getConsumerSecret())
        let encoded_token_secret = urlEnc (Helpers.getAuthSecret())
        sprintf "%s&%s" encoded_consumer_secret encoded_token_secret

    let makeSig (sigBase:string) (signingKey: string) =         
        let enc = System.Text.ASCIIEncoding.ASCII
        let hasher = new HMACSHA1(enc.GetBytes(signingKey))
        sigBase
        |> enc.GetBytes
        |> hasher.ComputeHash
        |> Convert.ToBase64String

    let makeHeaderLine parameters (signature: string) = 
        let param_string = 
            ("oauth_signature", signature)::parameters
            |> Seq.map (fun (key, value) -> ((urlEnc key) + "=\"" + (urlEnc value) + "\""))
            |> Seq.reduce(fun acc next -> acc + ", " + next)
        "OAuth " + param_string
        
    let getRequestHeader (request_parameters : List<string * string>) request_method base_url = 
        let default_parameters = getDefaultParameters()
        let all_params = addParameters request_parameters default_parameters
        let signature_base = 
            all_params
            |> getParameterLine
            |> (sigBase request_method base_url)
        let request_header = 
            makeSig signature_base (getSigningKey())
            |> (makeHeaderLine default_parameters)
        request_header