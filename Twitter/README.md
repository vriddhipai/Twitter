# Twitter
* In this project, you have to implement a Twitter Clone and a client tester/simulator.
* As of now, Tweeter does not seem to support a WebSocket API. As part I of this project, you need to build an engine that (in part II) will be paired up with WebSockets to provide full functionality. Specific things you have to do are: 
### Implement a Twitter like engine with the following functionality:
*	Register account
*	Send tweet. Tweets can have hashtags (e.g. #COP5615isgreat) and mentions (@bestuser)
* Subscribe to user's tweets
* Re-tweets (so that your subscribers get an interesting tweet you got by other means)
* Allow querying tweets subscribed to, tweets with specific hashtags, tweets in which the user is mentioned (my mentions)
* If the user is connected, deliver the above types of tweets live (without querying)
### Implement a tester/simulator to test the above
* Simulate as many users as you can
* Simulate periods of live connection and disconnection for users
* Simulate a Zipf distribution on the number of subscribers. For accounts with a lot of subscribers, increase the number of tweets. Make some of these messages re-tweets
### Other considerations:
* The client part (send/receive tweets) and the engine (distribute tweets) have to be in separate processes. Preferably, you use multiple independent client processes that simulate thousands of clients and a single engine process
* You need to measure various aspects of your simulator and report performance 
* More detail in lecture as the project progresses.
You need to submit your code, instructions how to run it and a report with performance numbers. 

