---
layout: post
title: "Major Update: Body Fat Tracking is Here"
date: 2012-10-26
published: true
comments: true
tags: TrendWeight
---

I just released a new version of TrendWeight that includes a bunch of changes:

* Body fat tracking
* Decent mobile support for more devices
* Configurable day start
* Access to raw scale readings
* Lots of behind the scenes improvements

Also, I'll take this opportunity to apologize in advance.  This update involved a nearly complete rewrite of the user dashboard (the page with the chart and the stats).  I probably broke a lot of things in the process.  I have tested the new version a fair amount and fixed a lot of small issues, but I probably didn't find them all.  I'll be watching the error logs closely for the next few days, but if you notice anything that seems wrong, please email me the details at <support@trendweight.com> and I'll look at it right away.

## Body Fat

If you have body fat data in your connected Withings or FitBit accounts, you'll now see some new buttons on your dashboard that let you look at body-fat-related trends.  There are three new options:

* __Fat %__.  This is the percent of your total body weight that the scale thinks is fat.
* __Fat Mass__.  This is how many pounds or kilograms of fat you have in your body.  This is calculated by multiplying your total body weight by your body fat %.  If you are trying to lose weight, watching for this trend line to go down over time may be interesting.
* __Lean Mass__.  This is how many pounds or kilograms of non-fat you have in your body.  This is calculated by subtracting your fat mass from your total body weight.  If you are actively trying to build muscle, watching this trend line go up over time may be interesting.  On the other hand, if you are trying to lose _fat_, making sure that this trend line is _not_ dropping over time may also be interesting.

<img class="border" src="/stuff/trendweight-bodyfat.png" />

_Fair warning_:  In my experience, neither the Withings nor FitBit wifi scales are particularly precise with body fat estimation.  What I mean is that your scale readings may change significantly from day to day even though your body hasn't really changed much at all.  The result is that you should expect the body-fat-related charts to be more erratic than the total weight chart.  Looking at your body fat trends may still be interesting, but my suggestion is not to worry as much about short term changes. If you care about body fat trends, watch the trends over a slightly longer term than you do for total weight (e.g. look at changes over 3 months and don't obsess about changes over 2 weeks).

You can also play with this using the [Sample Dashboard](https://trendweight.com/demo/).  Keep in mind that the sample user has entirely fabricated data and the body fat data may not be realistically random.  On a side note, if you have at least 9 months of weight and body fat data and are interested in volunteering your weight data to be used (anonymously) by the the sample user dashboard, let me know.

## TrendWeight on your SmartPhone

As part of this update, the mobile-optimized dashboard is now available on more devices (almost any device in theory as long as it has a decent browser).  I have only tested it on an iPhone, though, since that is all I have available.

<img class="border" src="/stuff/trendweight-iphone.png" />

## Day Start

You can now specify what time of day you want TrendWeight to use when deciding which day a weight reading occurs on.  See [this post](/2012/08/29/whats-next-for-trendweight/) for a more thorough discussion of the motivation for this feature.  Most people can probably just leave this set at midnight and not worry about it.

<img src="/stuff/trendweight-daystart.png" />

## Raw Scale Readings

Several people have noticed that the scale readings displayed in TrendWeight do not match the scale readings displayed on the FitBit website.  After some investigation, this seems to be because the two websites round numbers differently.  The FitBit and Withings APIs actually return weight readings with many decimal places (e.g. 141.4972 lbs).  On TrendWeight when that weight is used in calculations, it is used with the full precision (all the decimal places).  When weights are displayed, they are generally rounded to a single decimal place (e.g 141.5 lbs).  On the FitBit website, that weight would be displayed as 141.4 lbs though because it always rounds _down_ to the nearest tenth of a pound.

Anyway, in order to allow people to better determine if there are discrepancies between TrendWeight and FitBit/Withings, you can now go to a page which shows the raw scale readings that came from the FitBit/Withings API.  You can get to this page by clicking a new button on your settings page:

<img src="/stuff/trendweight-scalereadings1.png" />

Again, if you notice anything not working or fishy, please email me at <support@trendweight.com>.

