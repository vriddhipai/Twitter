type Tweet(ID:string, tweetContent:string, retweetFlag:bool) =
    member this.ID = ID
    member this.tweetContent = tweetContent
    member this.retweetFlag = retweetFlag
    override this.ToString() =
      let mutable result = ""
      if retweetFlag then
        result <- sprintf "Retweeting:: %s %s" this.ID this.tweetContent
      else
        result <- sprintf " %s %s" this.ID this.tweetContent
      result

type UserDetails(name:string, pwd:string) =
    let mutable subscribeList = List.empty: UserDetails list
    let mutable tweets = List.empty: Tweet list
    member this.name = name
    member this.pwd = pwd
    member this.add x =
        subscribeList <- List.append subscribeList [x]
    member this.getsubscribeList() =
        subscribeList
    member this.addTweetToList x =
        tweets <- List.append tweets [x]
    member this.getTweets() =
        tweets
    override this.ToString() = 
       this.name

// Actor Types
type RegisterMsg = RegisterMessage of  string  * string  * string* string
type SendMsg = SendMessage of  string  * string  * string* string* bool
type SubscribeMsg = SubscribeMessage of  string  * string  * string* string 
type RetweetsMsg = RetweetMessage of  string  * string  * string * string
type SubscribedTweetsMsg = SubscribedTweetsMessage of  string  * string  * string 
type HashTagMsg = HashTagMessage of  string  * string   
type MentionMsg = MentionMessage of  string  * string  

type Twitter() =
    let mutable tweets = new Map<string,Tweet>([])
    let mutable users = new Map<string,UserDetails>([])
    let mutable hashtags = new Map<string, Tweet list>([])
    let mutable mentions = new Map<string, Tweet list>([])
    member this.AddTweetToList (tweet:Tweet) =
        tweets <- tweets.Add(tweet.ID,tweet)
    member this.AddUser (user:UserDetails) =
        users <- users.Add(user.name, user)
    member this.AddToHashTag hashtag tweet =
        let token = hashtag
        let mutable map = hashtags
        if map.ContainsKey(token)=false
        then
            let lt = List.empty: Tweet list
            map <- map.Add(token, lt)
        let value = map.[token]
        map <- map.Add(token, List.append value [tweet])
        hashtags <- map
    member this.AddToMention mention tweet = 
        let token = mention
        let mutable map = mentions
        if map.ContainsKey(token)=false
        then
            let lt = List.empty: Tweet list
            map <- map.Add(token, lt)
        let value = map.[token]
        map <- map.Add(token, List.append value [tweet])
        mentions <- map
    member this.registerUser name pwd =
        let mutable result = ""
        if users.ContainsKey(name) then
            result <- "error, name already exists"
        else
            let user = new UserDetails(name, pwd)
            this.AddUser user
            user.add user
            result <- "UserRegistration success name: " + name + "  pwd: " + pwd
        result
    member this.SendTweet name pwd tweetContent retweetFlag =
        let mutable result = ""
        if not (this.authenticateUser name pwd) then
            result <- "error, user authentication failed"
        else
            if users.ContainsKey(name) = false then
                result <-  "error, no user with this name"
            else
                let tweet = new Tweet(System.DateTime.Now.ToFileTimeUtc() |> string, tweetContent, retweetFlag)
                let user = users.[name]
                user.addTweetToList tweet
                this.AddTweetToList tweet
                let index1 = tweetContent.IndexOf("#")
                if not (index1 = -1) then
                    let index2 = tweetContent.IndexOf(" ",index1)
                    let hashtag = tweetContent.[index1..index2-1]
                    this.AddToHashTag hashtag tweet
                let index1 = tweetContent.IndexOf("@")
                if not (index1 = -1) then
                    let index2 = tweetContent.IndexOf(" ",index1)
                    let mention = tweetContent.[index1..index2-1]
                    this.AddToMention mention tweet
                result <-  "Tweet sent!!:: " + tweet.ToString()
        result
    member this.authenticateUser name pwd =
            let mutable result = false
            if not (users.ContainsKey(name)) then
                printfn "%A" "error, no user with this name"
            else
                let user = users.[name]
                if user.pwd = pwd then
                    result <- true
            result
    member this.getUser name = 
        let mutable result = new UserDetails("","")
        if not (users.ContainsKey(name)) then
            printfn "%A" "error, no user with this name"
        else
            result <- users.[name]
        result
    member this.subscribe name1 pwd name2 =
        let mutable result = ""
        if not (this.authenticateUser name1 pwd) then
            result <- "error, user authentication failed"
        else
            let user1 = this.getUser name1
            let user2 = this.getUser name2
            user1.add user2
            result <- "Subscribed sucessfully! :: " + name1 + " subscribe " + name2
        result
    member this.reTweet name pwd tweetContent =
        let result = "Retweet! ::" + (this.SendTweet name pwd tweetContent true)
        result
    member this.queryTweetsSubscribed name pwd =
        let mutable result = ""
        if not (this.authenticateUser name pwd) then
            result <- "error, user authentication failed"
        else
            let user = this.getUser name
            let result1 = user.getsubscribeList() |> List.map(fun x-> x.getTweets()) |> List.concat |> List.map(fun x->x.ToString()) |> String.concat "\n"
            result <- "Subscribed user sucessfully! ::" + "\n" + result1
        result
    member this.queryHashTag hashtag =
        let mutable result = ""
        if not (hashtags.ContainsKey(hashtag)) then
            result <- "error, no hashtag match"
        else
            let result1 = hashtags.[hashtag] |>  List.map(fun x->x.ToString()) |> String.concat "\n"
            result <- "Hashtag sucessful! :: " + "\n" + result1
        result
    member this.queryMention mention =
        let mutable result = ""
        if not (mentions.ContainsKey(mention)) then
            result <- "error, no mention match"
        else
            let result1 = mentions.[mention] |>  List.map(fun x->x.ToString()) |> String.concat "\n"
            result <-  "Mentions Sucessful! ::" + "\n" + result1
        result
    override this.ToString() =
        "Full Tweet"+ "\n" + tweets.ToString() + "\n" + users.ToString() + "\n" + hashtags.ToString() + "\n" + mentions.ToString()
        
    

