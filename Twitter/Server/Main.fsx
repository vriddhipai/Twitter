#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit" 
#load "Messages.fsx"

open Messages
open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration

let configuration = 
    ConfigurationFactory.ParseString
        (@"akka {
            // log-config-on-start : on
            // stdout-loglevel : DEBUG
            // loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
                log-dead-letters = 0
                log-dead-letters-during-shutdown = off
            }
            remote {
                helios.tcp {
                    maximum-payload-bytes = 30000000 bytes
                    port = 8777
                    hostname = localhost
                    message-frame-size =  30000000b
                    send-buffer-size =  30000000b
                    receive-buffer-size =  30000000b
                    maximum-frame-size = 30000000b
                }
            }
        }")

let system = ActorSystem.Create("TwitterClient", configuration)


let twitter = new Twitter()

// Actor for registering users
let registrationActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   RegisterMessage(POST,registerUser,name,pwd) ->
            let res = twitter.registerUser name pwd
            sender <? res |> ignore
        | _ ->  failwith "Message not known while registering"
        return! loop()     
    }
    loop ()

// Actor for sending tweets
let sendingActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        let sender_path = mailbox.Sender().Path.ToStringWithAddress()
        match message  with
        |   SendMessage(POST,name,pwd,tweet_content,false) -> 
            let res = twitter.SendTweet name pwd tweet_content false
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Sending Tweets"
        return! loop()     
    }
    loop ()

// Actor for subscribing to tweets
let subscribeActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   SubscribeMessage(POST,name,pwd,target_name) -> 
            let res = twitter.subscribe name pwd target_name
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Subscribing"
        return! loop()     
    }
    loop ()

// Actor for Retweeting 
let retweetActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   RetweetMessage(POST,name,pwd,tweet_content) -> 
            let res = twitter.reTweet  name pwd tweet_content
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Retweeting"
        return! loop()     
    }
    loop ()

//// Actor for getting the tweets subscribed to
let queryingActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   SubscribedTweetsMessage(POST,name,pwd ) -> 
            let res = twitter.queryTweetsSubscribed  name pwd
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Querying Users"
        return! loop()     
    }
    loop ()

// Actor for hashTag 
let hashTagActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   HashTagMessage(POST,queryhashtag) -> 
            let res = twitter.queryHashTag  queryhashtag
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Quering Hashtag"
        return! loop()     
    }
    loop ()

// Actor for mentions
let mentionsActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match message  with
        |   MentionMessage(POST,mention) -> 
            let res = twitter.queryMention mention
            sender <? res |> ignore
        | _ ->  failwith "Message not known while Quering Mentions"
        return! loop()     
    }
    loop ()

let mutable case= "register" 
let mutable POST="POST"
let mutable name="user2"
let mutable pwd="UFL"
let mutable registerUser="registerUser"
let mutable target_name="user1"
let mutable tweet_content="Go Gators!"
let mutable queryhashtag="#Gator"
let mutable mentions="@DanMullen"

// TweetMessage between client and server
//case,POST,name,pwd,target name,tweet content,query hashtag, query mention,register
type TweetMessage = TweetMsg of  string  * string * string* string* string * string* string* string * string

let RegisterActor = spawn system "registerUser" registrationActor
let TweetingActor = spawn system "sendTweet" sendingActor
let SubscribeActor = spawn system "subscribe" subscribeActor
let RetweetActor = spawn system "retweet" retweetActor
let QueryingActor = spawn system "query" queryingActor 
let HashTagActor = spawn system "hashtag" hashTagActor
let MentionsActor = spawn system "Mentions" mentionsActor

let receiverActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! message = mailbox.Receive ()
        let sender = mailbox.Sender()
        match box message with
        | :? string   ->
            if message="" then
                return! loop() 
            let result = message.Split ','
            let mutable case= result.[0]
            let mutable POST=result.[1]
            let mutable name=result.[2]
            let mutable pwd=result.[3]
            let mutable target_name=result.[4]
            let mutable tweet_content=result.[5]
            let mutable queryhashtag=result.[6]
            let mutable mention=result.[7]
            let mutable registerUser=result.[8]
            let mutable task = RegisterActor <? RegisterMessage("","","","")
            match case with
            | "register" -> task <- RegisterActor <? RegisterMessage(POST,registerUser,name,pwd)
            | "send" -> task <- TweetingActor <? SendMessage(POST,name,pwd,tweet_content,false)
            | "subscribe" -> task <- SubscribeActor <? SubscribeMessage(POST,name,pwd,target_name )
            | "retweet" -> task <- RetweetActor <? RetweetMessage(POST,name,pwd,tweet_content)
            | "querying" -> task <- QueryingActor <? SubscribedTweetsMessage(POST,name,pwd )
            | "#" -> task <- HashTagActor <? HashTagMessage(POST,queryhashtag )
            | "@" -> task <- MentionsActor <? MentionMessage(POST,mention )
            let result = Async.RunSynchronously (task, 1000)
            sender <? result |> ignore
        return! loop()     
    }
    loop ()

let actor_MSGreceived = spawn system "TwitterServer" receiverActor

actor_MSGreceived <? "" |> ignore
printfn "\n \n Server is up and running.. please start the client in a different terminal \n" 
Console.ReadLine() |> ignore
0
