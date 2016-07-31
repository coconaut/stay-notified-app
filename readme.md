# README

## Description
This project makes use of agents and supervisors to track RSS feeds and Twitter streams. It's intended for internal use, not for production, but was a fun way to explore F# and may have something useful for you.
The project includes:
  * modules for accessing and authenticating Twitter streaming and rest APIs
  * RSS feed loading using type providers
  * library for creating and mapping simple notification records
  * agents for streaming Twitter and polling RSS feeds
  * an OWIN self-hosted Web API in F#
  * a simple db interface and Mongo implementation
  * agents for posting to Mongo
  * a simple AngularJS front-end example page
    + this was thrown together just as a sample
    + you'll need the Angular and Angular-Sanitze, as I never properly set up the packages

## This project uses:
1. FSharp.Data for type providers and Twitter source files
2. RestSharp for making REST calls not stupid
3. These posts on agents and async workflows in F# by Don Syme:
  *  http://blogs.msdn.com/b/dsyme/archive/2010/01/10/async-and-parallel-design-patterns-in-f-reporting-progress-with-events-plus-twitter-sample.aspx
  * http://blogs.msdn.com/b/dsyme/archive/2010/02/15/async-and-parallel-design-patterns-in-f-part-3-agents.aspx
