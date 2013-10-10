---
layout: post
title: "Minor Updates for FitBit Users"
date: 2012-09-26 22:00
published: true
comments: true
tags: TrendWeight FitBit
---

I just pushed out a minor update that applies mostly to users who have FitBit scales.  A few of you have noticed that when you delete a weight reading on the FitBit website, it doesn't seem to get removed from your TrendWeight charts.  That has changed as of tonight.

The problem is that when TrendWeight asks the FitBit website for your new weight data, the FitBit website doesn't give any indication that you have deleted or updated an old weight reading.  That's still the case, but as of tonight, when you visit the TrendWeight site, my code will now automatically look at all of your weight readings for the past 14 days and will notice if you deleted or updated any old readings from that 2 week time period.  

That means that the vast majority of the time, things will just work automatically.  For example, if you go in and delete an erroneous weight reading for yesterday, TrendWeight will automatically notice and delete the corresponding weight reading from the TrendWeight database.

But what happens if you delete something from more than 14 days ago?  Because of the limitations of the FitBit API, it's not practical for me to automatically look through your entire list of historical weight readings and so if you do happen to make a change to weight data, for example, from 2 months ago, you'll need to let TrendWeight know about it.

There is now a new button in your [account settings](https://trendweight.com/settings) to fully resynchronize all of your weight readings, no matter how old any changes are:

<img class="fancybox" src="/stuff/trendweight-resync.png" />

Just click the __Resync All Data__ button and any changes you have made to old data will get correctly picked up in TrendWeight.

Note, this button is also available for Withings scale users, but if you have a Withings scale, you'll generally not need to use this as the Withings API tells me if you have updated or deleted old data and TrendWeight handles those changes automatically.  That said, if you ever think the data looks fishy, the resync button may come in handy.

As always, if you have questions, email me at <support@trendweight.com>.