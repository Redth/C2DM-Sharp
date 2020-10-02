ATTENTION: THIS LIBRARY IS NOW OBSOLETE / DEPRECATED!
=====================================================
I've recently started a new project called [PushSharp](https://github.com/Redth/PushSharp/).  Its goal is to combine APNS-Sharp as well as C2DM-Sharp into a single project.  It takes some of the same great code from C2DM-Sharp, and still allows you to easily send push notifications, but it also includes an optional abstraction layer for sending notifications to multiple platforms.  Please go check it out.  Once PushSharp is a bit more mature, this project will be deprecated.  For now, I will not be adding any major new functionality to this library.  

So go check out [PushSharp](https://github.com/Redth/PushSharp/)!  It's open source (under Apache 2.0), and a solid step forward for push notifications!

(https://github.com/Redth/PushSharp/)



# C2DM-Sharp
C2DM-Sharp is a set of .NET Libraries for [Google Android's Cloud 2 Device Messaging](http://code.google.com/android/c2dm/index.html) Push Notification system.

## What's it Contain?
+ **C2dmSharp.Server** - .NET Library for sending C2DM Messages to Google's Servers.  Mono and .NET compatible, 100% managed code
+ **C2dmSharp.Server.Sample** - Command line sample that can send C2DM Messages with the library
+ **C2dmSharp.Client** - [MonoDroid](http://www.monodroid.net) Library to help with Registering for C2DM Messages, as well as listening for Incoming Messages
+ **C2dmSharp.Client.Sample** - Sample Android app (replace __PackageName__ in broadcast receiver with c2dmsharp.client.sample before compiling!!!)

## What's Missing?
The Client implementation, because of current MonoDroid limitations, does not completely generate the required AndroidManifest.xml changes in the referring application.
I've written some of the attributes that are needed to make this happen, but if you look at attributes in the subclassed BroadcastReceiver in the Client sample, you can see that you would have to use your own package name for the manifest generation to work.  
There is a bug report in to address this issue.  The other issue is the generation of the required <permission> and <uses-permission> tags in the manifest.  Another bug was filed for this.  Here again, you need to use your own package name where appropriate.

For now, it's recommended that you merge the uncommented section of the AndroidManifest.xml (replacing __PackageName__) with your Application's manifest, and then changing the line mentioned above to refer to your package name.

**Scalability** I have the service able to use a number of worker Tasks, but currently, all the HTTP requests aren't taking advantage of async calls.  I plan on changing this so that the service can scale well.

**Rate Limiting** Currently, the service will fire off messages from the queue as fast as possible.  It will back off if google tells it to, but there's no other rate limiting mechanism in place (eg: to say only allow x requests per second).  This is something I think that would be a good addition.  You could then queue up a ton of messages and 'slowly' (relative term) send them without getting Google upset.

## How do I use it?
1. First, sign up for C2DM at: http://code.google.com/android/c2dm/signup.html
2. Download the source code and view the samples 
3. Change your SenderID wherever relevant in the samples (eg: C2dmSharp.Client's DefaultActivity.cs in the top)
4. If you're not using the Client.Sample, make sure you add the right permissions from the sample's manifest, with your own package name in the right places.  Also, make sure you change the package name in your subclassed C2dmBroadcastReceiver's attributes!
5. Better instructions coming soon... ;)

## Links
+ **[Official C2DM Documentation](http://code.google.com/android/c2dm/index.html)** : Google's C2DM Documentation
+ **[Official C2DM Whitelist Registration](http://code.google.com/android/c2dm/signup.html)** : Google's site to register your application for C2DM
+ **[APNS-Sharp](http://code.google.com/p/apns-sharp/)** : Sister Library, but for Apple iOS Push Notifications, for MonoTouch


## Changelog
**Jan 22, 2011 @ 6:30pm**
-  Samples work!  YMMV, you need to register your own accounts and make the changes in the right places, but I got it working here using both libraries!
-  HUGE update, lots of changes, things actually work now, too many changes to list

**Jan 20, 2011 @ 1:12pm**
-  Server looks like it can actually send messages now :)
-  Added Samples for Client and Server apps
-  Server now will authenticate itself with the provided SenderID, Password, and ApplicationID
