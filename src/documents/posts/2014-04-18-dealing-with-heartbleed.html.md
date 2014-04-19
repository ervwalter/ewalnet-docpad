---
layout: post
title: "Dealing with Heartbleed"
date: 2014-04-18 22:30
comments: true
tags: TrendWeight
---

If you have been on the internet for the last couple weeks, you have likely heard of the Heartbleed bug.  If not, read about it [here](http://heartbleed.com/).  Here's what you need to know about TrendWeight.

## FitBit and Withings Connections

FitBit has announced that they were affected (like many other sites) by this bug and they are recommending that FitBit users reauthorize third party applications like TrendWeight.  I have not heard anything from Withings, but it doesn't hurt to reauthorize your Withings account just in case as well.  Doing this is easy regardless of if you use FitBit or Withings.

1. Go to your [Settings](https://trendweight.com) page and scroll to the bottom.
2. Click the **Disconnect Scale** button.  This will deauthorize TrendWeight from getting at your FitBit or Withings data.
3. You'll be taken back to a page where you can reconnect to FitBit or Withings.  Once you do this, new authentication keys will be generated for you, and your data will be completely redownloaded (you won't lose anything).
4. You're done.  There is no step 4.

## TrendWeight Itself

Our SSL provider, CloudFlare, has already fixed their servers, and in fact [fixed them before the bug was made public](http://blog.cloudflare.com/staying-ahead-of-openssl-vulnerabilities).  I have no reason to believe that anyone exploited the bug to attack TrendWeight prior to CloudFlare fixing the bug. That said, better safe than sorry, as they say.  If you want to be extra cautious, you may want to consider changing your TrendWeight password [here](https://trendweight.com/changepassword/).

Isn't the Internet fun?  If you have any questions, feel free to email me at <support@trendweight.com>.

