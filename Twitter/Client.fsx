#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit" 

open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

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
                maximum-payload-bytes = 30000000 bytes
                helios.tcp {
                    port = 8123
                    hostname = localhost
                      message-frame-size =  30000000b
                          message-frame-size =  30000000b
                    send-buffer-size =  30000000b
                    receive-buffer-size =  30000000b
                    maximum-frame-size = 30000000b
                }
            }
        }")
        
let sw = System.Diagnostics.Stopwatch()

type TweetMessage = TweetMsg of  string  * string * string* string* string * string* string* string * string

// number of users
let usernum = int (string (fsi.CommandLineArgs.GetValue 1))
let num_tweets = int (string (fsi.CommandLineArgs.GetValue 2))
let K = usernum
let mutable iter1 = 0
let mutable iter2 = 0
let object = new Object()
let addIterByOne() =
    Monitor.Enter object
    iter2 <- iter2+1
    Monitor.Exit object
          
let system = ActorSystem.Create("TwitterClient", configuration)

let servercall = system.ActorSelection(
                            "akka.tcp://TwitterClient@localhost:8777/user/TwitterServer")

let randomNum = System.Random(1)
let newUserActor (mailbox: Actor<_>) = 
    let rec loop () = actor {
        let! msg = mailbox.Receive()
        let index = msg
        let mutable choice = "register"           
        let mutable POST = " "
        let mutable name = "user"+(string index)
        let mutable pwd = "password" + (string index)
        let mutable userQueried = " "
        let mutable hashtagQueried = " "
        let mutable mention = " "
        let mutable content = " "
        let mutable register = " "
        let query = choice+","+POST+","+name+","+pwd+","+userQueried+","+content+","+hashtagQueried+","+mention+","+register
        let perform = servercall <? query
        let reply = Async.RunSynchronously (perform, 1000)
        addIterByOne()
        return! loop()
    }
    loop ()
let autoClientActor (mailbox: Actor<_>) = 
    let rec loop () = actor {        
        let! msg = mailbox.Receive ()
        let sender = mailbox.Sender()
        let index = msg
        match box msg with
        | :? string   ->
            let mutable randomNumber = Random( ).Next() % 7
            let mutable choice = "register"           
            let mutable POST = "POST"
            let mutable name = "user"+(string index)
            let mutable pwd = "password" + (string index)
            let mutable userQueried = "user"+randomNum.Next(usernum) .ToString() 
            let mutable hashtagQueried = "#topic"+randomNum.Next(usernum) .ToString()
            let mutable mention = "@user"+randomNum.Next(usernum) .ToString()
            let mutable content = "tweet"+randomNum.Next(usernum) .ToString()+"... " + hashtagQueried + "..." + mention + " " 
            let mutable register = "register"
            //menu driven operations based on auto-generated random choice 
            match randomNumber + 1 with
            | 1 -> choice <- "register"
            | 2 -> choice <- "send"
            | 3 -> choice <- "subscribe"
            | 4 -> choice <- "retweet"
            | 5 -> choice <- "querying"
            | 6 -> choice <- "#"
            | 7 -> choice <- "@"
            let query = choice+","+POST+","+name+","+pwd+","+userQueried+","+content+","+hashtagQueried+","+mention+","+register
            let perform = servercall <? query
            let reply = Async.RunSynchronously (perform, 3000)
            addIterByOne()
        return! loop()     
    }
    loop ()

let newUserLogin = spawn system "newUserLogin" newUserActor    
let autoClient = spawn system "autoClient" autoClientActor

printfn "Registering Account Process Initiated!! \n " 

sw.Start()
iter1<-0
iter2<-0
while iter1<usernum do
    newUserLogin <! string iter1 |>ignore
    iter1<-iter1+1
while iter2<usernum-1 do
    Thread.Sleep(50)

let timeForRegister = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

printfn "Starting Sending tweets!!! \n " 
sw.Start()
for iter1 in 0..usernum-1 do
    for j in 0..num_tweets do
        let query = "send, ,user"+(string iter1)+",password"+(string iter1)+", ,tweet+user"+(string iter1)+"_"+(string j)+"th @user"+(string (randomNum.Next(usernum)))+" #topic"+(string (randomNum.Next(usernum)))+" , , , "
        let perform = servercall <? query
        let reply = Async.RunSynchronously (perform, 3000)
        printfn "Sending Tweets :: %s" (string(reply))

let timeForSend = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

let mutable step = 1
sw.Start()
printfn "Starting Zipf Subscribe!!! \n"  
for iter1 in 0..usernum-1 do
    for j in 0..step..usernum-1 do
        if not (j=iter1) then
            let query = "subscribe, ,user"+(string j)+",password"+(string j)+",user"+(string iter1)+", , , , "
            let perform = servercall <? query
            let reply = Async.RunSynchronously (perform, 3000)
            printfn "Subscribing :: %s" (string(reply))
        step <- step+1
let timeForSubscribe = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()   


sw.Start()
for iter1 in 0..usernum-1 do
    let query = "querying, ,user"+(string iter1)+",password"+(string iter1)+", , , , , "
    let perform = servercall <? query
    let reply = Async.RunSynchronously (perform, 5000)
    printfn "User Query :: %s" (string(reply))
let timeForQueringUsers = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

sw.Start()
for iter1 in 0..usernum-1 do
    let query = "#, , , , , ,#topic"+(string (randomNum.Next(usernum)))+", ,"
    let perform = servercall <? query
    let reply = Async.RunSynchronously (perform, 3000)
    printfn "HashTag Query :: %s" (string(reply))
let timeForHashTag = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

sw.Start()
for iter1 in 0..usernum-1 do
    let query = "@, , , , , , ,@user"+(string (randomNum.Next(usernum)))+","
    let perform = servercall <? query
    let reply = Async.RunSynchronously (perform, 3000)
    printfn "Mention Query :: %s" (string(reply))
let timeForMentions = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

printfn " %d number of random operations are perfomed!!! \n" K 

sw.Start()
iter1<-0
iter2<-0
while iter1<K do
    autoClient<! string (randomNum.Next(usernum)) |>ignore
    iter1 <- iter1+1
while iter2<K-1 do
    Thread.Sleep(50)
let timeForRandomQueries = sw.ElapsedMilliseconds
sw.Stop()
sw.Reset()

printfn "%d Users take %i ms time to register" usernum timeForRegister
printfn "%i ms time is takes for sending %d tweets for all users " timeForSend num_tweets
printfn "%d Users take %i ms time for Zipf Subscribe" usernum timeForSubscribe
printfn "%d Users get queried in %i ms time" usernum timeForQueringUsers
printfn "Hashtag query time is %i ms" timeForHashTag
printfn "Mention Query time is %i ms" timeForMentions
printfn "Perfoming %d number of random operations take %i ms time" K timeForRandomQueries

system.Terminate() |> ignore
0 