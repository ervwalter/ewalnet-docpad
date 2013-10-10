---
layout: post
title: "Dealing with Lots of Historical Data"
date: 2013-06-08 22:00
published: true
comments: true
tags: TrendWeight
---

So, a TrendWeight user contacted me this week to let me know he was getting an error about the FitBit website not working.  It happened right after he loaded 10 years worth of historical data into FitBit and changed his TrendWeight start date to a date in 2013.  At the same time that the email from this user arrived in my inbox, I also received an automated email from FitBit telling me that a user had exceeded the API request limits and had been temporarily blocked for 1 hour.  Oops.

There are two "rules" that FitBit enforces on their API that are the heart of this problem:

* No single request for data can retrieve more than 1 month of data at a time.
* No single user can make more than 150 requests in a single hour (or they get blocked temporarily).

Add in that the API requires separate requests for weight and body fat data.  Now, let's do the math on how many requests are required to load 10 years worth of data:

10 years * 12 months/year * 2 requests/month = __240 requests__

So, 240 requests are needed to load 10 years worth of data, but FitBit blocks requests after 150.  What it boils down it is that, current, it is not possible to tell TrendWeight to use a start date 10 years in the past.

I have looked more carefully at the FitBit API and there are basically two options that I can think of to resolve this:

__Option 1:__ I could create a queuing system so that when you need to load 10 years worth of data, it doesn't do it all right away.  Instead it would do a few years, wait and hour, do another few years, wait an hour, etc, until all the data was loaded.

__Option 2:__ I could use a different API for "old" data.  FitBit has an alternate API that allows me to request up to 3 years of weight or body fat data in a single request.  The catch is that they only return a _single_ data point per day.  If you weight yourself multiple times in a single day, the FitBit API appears to return the _last_ weight of the day.  This is different than what TrendWeight does right now.  Currently, if you weigh yourself multiple times in a single day, TrendWeight uses the _first_ weight of the day because if you weigh yourself right after you wake up, the weight readings tend to be more consistent.  My approach would probably be to use the existing API for any data in the past 12 months and to use the less-precise bulk API for data older than 1 year.

Neither option is really very good, in my opinion.  Option 1 is a pain for users.  I wouldn't want to have to wait several hours just to see my data.  This delay would happen not only when you first setup your account, but it happens any time TrendWeight has to do a full resync: when you change your start date, when you change your time zone, when you change your weight units, when you click the 'Resync All Data' button.

Option 2 is a little better because it means you can get all your data pretty much instantaneously, but it isn't perfect either. The biggest issue would be that your weight for any given day might subtly change after 1 year.  For example, let's say I weigh myself today (June 8, 2013) in the morning and weigh 220 lbs.  Then I weigh myself in the evening after a large meal and weigh 224 lbs.  For the next few months, TrendWeight will use 220 lbs as my scale reading for the day.  However, if I need to do a resync 13 months from now, TrendWeight will suddenly start using 224 lbs as my scale reading for June 8, 2013.  

In the grand scheme of things, maybe this doesn't matter all that much.  It won't affect the trend weight calculations for July 2014 because the trend weight math is generally unaffected by weight readings older than 20 days or so.  Mostly, the graph will have a slightly different shape than it did before if I happen to look back in time at that older-than-one-year data.

My inclination is to go ahead and program Option 2 because having approximately correct old data is better than not having old data at all, but what do you guys think?  Would Option 1's queuing system be better even if it meant that you you have to wait an hour or two to see your data anytime TrendWeight decides it needs to do a full resync?
 