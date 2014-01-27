---
layout: post
title: Mystery Solved: FitBit Rounding Error
date: 2014-01-27 15:34
published: true
comments: true
tags: TrendWeight
---

A long-standing mystery has finally been solved...

**Short Version:**

If your TrendWeight account is connected to Fitbit, and you don't use the metric system (kilograms), you may have noticed that the actual weights shown on TrendWeight were often different from the weights show on fitbit.com by 0.1 - 0.2 pounds.  This doesn't happen anymore.

**Long Version:**

Sometimes Trendweight shows my actual scale weight as 0.1 lb higher or lower than what I remember the scale actually showing.  I've noticed it on and off for a while, and I have gotten quite a few emails about it.  I had looked into it in the past several times and confirmed that I am displaying *exactly* what Fitbit tells me in their API.  So, I chalked it up to an idiosyncrasy on the Fitbit side.  Who cares about 0.1 lb anyway?

Well, after yet another email today, I started looking into it again.  After some research (aka Google searches), I found a couple other cases where people were having similar problems and in those reports was the nudge I needed to finally understand the problem.  The problem is a rounding error when converting between Metric and English weight units.  Here's what happens:

1. You step on the scale and it displays your weight in pounds and sends it to the fitbit.com database.
2. TrendWeight makes an API request and fitbit.com returns your recent weight readings in kilograms and _rounded to 1 decimal place_.
3. TrendWeight converts your recent weight readings back into pounds.

The problem is the _highlighted_ part of step #2.  When your weight gets converted to kilograms and _rounded to 1 decimal place_ and then converted back into pounds, the 0.1 - 0.2 lbs error gets introduced.

If only there was a way to tell the Fitbit API that I want the results in pounds (for users who are using pounds) so that I don't have to convert them myself, then this problem would go away... Oh wait.  There is. Doh!

By asking Fitbit for weights directly in pounds, TrendWeight now gets actual scale weights that exactly match what was displayed on your scale and what you see on fitbit.com.  I should have realized this a long time ago, and so I feel a little foolish.  Sorry.

TrendWeight's logic has been updated, and the next time you visit your dashboard, your weight readings for the past 21 days will automatically be re-downloaded and "fixed".  That will result in an accurate current 'trend weight' (because that trend weight is based, essentially, on the past 20 days of weight readings).

If you really want to retroactively fix all your historical scale readings, you can do so by going into your [settings](https://trendweight.com/settings) and clicking the 'Resync All Data' button at the bottom of the page. However, **do not click this** if you have more than 3or 4 years of historical data in TrendWeight or you will risk running into [the other Fitbit limitation](/2013/06/08/dealing-with-lots-of-historical-data/) that prevents TrendWeight from loading lots of historical data.  If you have that much old data, you'll want to wait until that other limitation has been addressed.

**Side Note**

I also want to mention that I am actively working on a new version of the TrendWeight backend that addresses that [Fitbit API limitation](/2013/06/08/dealing-with-lots-of-historical-data/) and also opens up the doors for addressing some of the long requested enhancements you all have made.  I still have a bunch of work left to finish before it is ready, but at least the work is in progress...

As always, email me at <support@trendweight.com> if you have any questions or concerns.

