# README

## Description
This project makes use of agents and supervisors to track RSS feeds and Twitter streams. It's intended for internal use, not really for production, but was a great way to explore F# and may have something useful for you.
The project includes:
  * modules for accessing and authenticating Twitter streaming and rest APIs
  * RSS feed loading using type providers
  * library for creating and mapping simple notification records
  * agents for streaming Twitter and polling RSS feeds
  * an OWIN self-hosted Web API in F#
  * a simple db interface and Mongo implementation
  * agents for posting to Mongo
  * a simple AngularJS front-end example page

## This project may be useful if:
  * you're exploring agents and supervisors in F# (as I am!)
  * you want to check out self-hosting with OWIN
    * the API is decent
    * as a static file server, I'm leaving a lot to be desired...
    * maybe swap this part out for some Suave.io?
  * you want to use the C# MongoDB.NET driver in F#
  * you are in the same Twitter OAuth nightmare that I was...
    * Twitter streaming is the worst sometimes...
  * I'm no Angular expert, but there's enough there to get you started if you're interested

## This project GREATLY appreciates:
1. FSharp.Data for type providers, Twitter source files, and general awesomeness
2. RestSharp, for making REST calls not stupid
3. These posts on agents and async workflows in F# by Don Syme (YOU MUST CHECK THEM OUT!):
  *  http://blogs.msdn.com/b/dsyme/archive/2010/01/10/async-and-parallel-design-patterns-in-f-reporting-progress-with-events-plus-twitter-sample.aspx
  * http://blogs.msdn.com/b/dsyme/archive/2010/02/15/async-and-parallel-design-patterns-in-f-part-3-agents.aspx


## To get up and running...
1. clone
2. restore nuget-packages
  * if this is a pain, you may have to delete all folders within the packages folder first
  * if this is still a pain...well... godspeed! I will start using Paket or something in the future.
3. edit PowerShell root directory in api_post_build.ps1 and lib_post_build.ps1
  * I plan to parameterize this eventually
3. set PowerShell execution policy to RemoteSigned, or otherwise allow running of scripts
  * may need to be done separately for x86 as well
  * if you would prefer not to, you can remove the scripts altogether from the post-build command
    * they are really file-moving helpers as embedded resources were failing me
    * you will need to copy the source directories from the NotificationLibrary to the NotificationAPI
    * you will need to copy the source and site directories to the output folder for the NotificationAPI
    * you could always use FAKE :)
4. put in your Twitter credentials in the API app config or otherwise specify them
5. pop in some sources in the sources.json file in the NotificationLibrary
  * for Twitter, use UserIDs
  * for RSS, use the url
  * these can be updated while running (in the output directory) and changes will propogate in the next loop
5. set up a Mongo instance and create a simple Notification collection, or omit this part for now
6. if you want the front-end, you'll need these angular dependencies
  * angular.js (well, duh)
  * angular-sanitize.js
  * angular-mocks.js (I actually don't think I use this one...)
  * I just used the core NuGet package for Angular and grabbed the sanitize package as well
  * these should go in NotificationAPI > site > js if you want the scripts to automatically pick them up
6. adjust and start the API console app
  * start with small poll times to make sure it's working
    * I foolishly wait at the start of each loop, meaning the changes take time to cascade
    * this should be revised eventually

## Thoughts
I hope this can help you out in some way :)
Also I apologize if I commit something I shouldn't, I'm using a few too many Git clients and some seem to bypass my ignores these days (I'm looking at you VS...)
Enjoy!
