# C2DM-Sharp
C2DM-Sharp is a set of .NET Libraries for [Google Android's Cloud 2 Device Messaging](http://code.google.com/android/c2dm/index.html) Push Notification system.

## What's it Contain?
+ **C2dmSharp.Server** - .NET Library for sending C2DM Messages to Google's Servers.  Mono and .NET compatible, 100% managed code
+ **C2dmSharp.Server.Sample** - Command line sample that can send C2DM Messages with the library
+ **C2dmSharp.Client** - [MonoDroid](http://www.monodroid.net) Library to help with Registering for C2DM Messages, as well as listening for Incoming Messages
+ **C2dmSharp.Client.Sample** - Sample Android app (replace __PackageName__ in broadcast receiver with c2dmsharp.client.sample before compiling!!!)

## What's Missing?
The Client implementation, because of current MonoDroid limitations, does not generate the required AndroidManifest.xml changes in the referring application.
I've written some of the attributes that are needed to make this happen, but if you look at C2dmBroadcastReceiver.cs line 20 you can see that you would have to replace __PackageName__ with the full package name of your application for the manifest generation to work.  
There is a bug report in to address this issue.  The other issue is the generation of the required <permission> and <uses-permission> tags in the manifest.  Another bug was filed for this.

For now, it's recommended that you merge the uncommented section of the AndroidManifest.xml (replacing __PackageName__) with your Application's manifest, and then changing the line mentioned above to refer to your package name.

## How do I use it?
+ First, sign up for C2DM at: http://code.google.com/android/c2dm/signup.html
+ Download the source code and view the samples 
+ Better instructions coming soon... ;)

## Links
+ **[C2DM Whitelist Registration](http://code.google.com/android/c2dm/signup.html)** : Google's site to register your application for C2DM
+ **[APNS-Sharp](http://code.google.com/p/apns-sharp/)** : Sister Library, but for Apple iOS Push Notifications, for MonoTouch


## Changelog
**Jan 20, 2011 @ 1:12pm**
+ Server looks like it can actually send messages now :)
+ Added Samples for Client and Server apps
+ Server now will authenticate itself with the provided SenderID, Password, and ApplicationID