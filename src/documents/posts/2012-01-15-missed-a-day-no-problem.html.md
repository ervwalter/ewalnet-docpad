---
layout: post
title: "Missed a Day?  No Problem!  Linear Interpolation to the Rescue"
date: 2012-01-15 22:00
comments: true
tags: TrendWeight
---

If you go on a trip for a few days and aren't able to weigh yourself, TrendWeight now has an option to estimate your weight on those missing days so that your trend line and statistics are more accurate.

Previously, if you missed a few days, your trend line would look "flatter" than it should have been.  This is an artifact of how the exponentially weighted moving average works when there are missing data points.  Here is an example from my own recent chart.  In this example, you can see the 4 days where I wasn't able to weigh myself because I was out of town for the holiday:

<img src="/stuff/trendweight-interpolation1.png" />

Now, there is a option in TrendWeight to fill in those missing days using [linear interpolation](http://en.wikipedia.org/wiki/Linear_interpolation).  Obviously, having real data would be better than this estimation, but this is usually better than nothing.  This option helps when you miss a day or two every once in a while.  Think of it like this: when there are days missing, TrendWeight will look at the scale readings right before and right after the missing days and fill in the gap by drawing a straight line between the surrounding scale readings.

With this option enabled, my chart looks like this instead:

<img src="/stuff/trendweight-interpolation2.png" />

This option can be turned on and off on your TrendWeight [settings](https://trendweight.com/settings) page.  Because this almost always results in a better trend line, I enabled it for all users by default, but you can turn it off if you don't like it and prefer the old behavior.

Keep in mind that if your missing days become more frequent than the days on which you actually weigh yourself, this technique will be less accurate.  An occasional missed day or two won't matter much, but for the most accurate statistics, you should still try to weigh in more or less every day.

One other note is that the original Hacker's Diet Online tool didn't do this linear interpolation, so if you want TrendWeight's calculations to stay 100% consistent with that tool, make sure to turn this option off.
