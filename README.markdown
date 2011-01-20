# C2DM-Sharp
C2DM-Sharp is a set of .NET Libraries for [Google Android's Cloud 2 Device Messaging](http://code.google.com/android/c2dm/index.html) Push Notification system.

## What's it Contain?
+ **C2dmSharp.Server** - .NET Library for sending C2DM Messages to Google's Servers.  Mono and .NET compatible, 100% managed code
+ **C2dmSharp.Client** - [MonoDroid](http://www.monodroid.net) Library to help with Registering for C2DM Messages, as well as listening for Incoming Messages

## What's Missing?
I hope to include a utility to help with the oauth login process, and obtaining the Sender Auth Token easily.
Thinking about making the server login on its own, given the credentials...

## Other Links
+ **[APNS-Sharp](http://code.google.com/p/apns-sharp/)** : Sister Library, but for Apple iOS Push Notifications, for MonoTouch