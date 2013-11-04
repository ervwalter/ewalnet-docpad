---
layout: post
title: iOS 7 and TrendWeight
date: 2013-11-04 10:24
published: true
comments: true
tags: TrendWeight
---

I have gotten several reports of problems with using TrendWeight on iOS7 devices.  Most commonly, I hear that TrendWeight seems to force you to login every time you open the site.

The underlying issue seems to be a bug or a series of bugs in Safari/iOS7 related to storing [cookies](http://en.wikipedia.org/wiki/HTTP_cookie).  This Apple bug appears to affect all web apps, and not just TrendWeight, and other people are also [reporting](http://www.infoworld.com/t/html5/bad-news-ios-7s-html5-full-of-bugs-227685) [problems](http://www.mobilexweb.com/blog/safari-ios7-html5-problems-apis-review) with iOS7 on this front.  There have always been oddities in iOS with web apps that are pinned to the home screen, but it appears that iOS 7 made things noticeably worse.

Unfortunately, there is not anything I can do to fix the underlying problem, but I did make a small change that should cause TrendWeight to launch in the full Safari app instead of by itself.  For some users, this seems to make your login session last longer, but it isn't a real fix and you'll eventually be asked to login again.

Until Apple addresses the underlying problems, there are two other workarounds I can suggest:

* Instead of bookmarking your normal TrendWeight dashboard, bookmark (or add to your home screen) your public "sharing URL" (which you can find on your [settings](https://trendweight.com/settings/) page).  Your public "sharing URL" doesn't require you to login, so you won't see a login page when you visit that page regardless of if cookies are working or not.
* You can also choose to use [Chrome](http://itunes.apple.com/us/app/chrome/id535886823) instead of Safari as a browser on iOS as cookies work fine in Chrome and so it will have no problem remembering your login.

If I hear more about the underlying Apple bug, I'll let you all know.  Until then, if you have questions or concerns, email me at <support@trendweight.com>.
