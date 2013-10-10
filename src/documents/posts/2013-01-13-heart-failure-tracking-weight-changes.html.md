---
layout: post
title: "Heart Failure: Tracking Weight Changes"
date: 2013-01-13 22:00
published: true
comments: true
tags: TrendWeight
---

_Note: There __is__ a discussion of a new TrendWeight feature eventually in this post, but it is preceded by a bit more of my personal story than I normally share.  If you have or know someone that has heart failure or CHF, I encourage you to keep reading even though this is long.  If you don't, you probably won't care about this new feature and so can skip reading this post if you like._

Sometimes life throws you a curve ball.  Late last November, I was diagnosed with [heart failure](http://en.wikipedia.org/wiki/Heart_failure).  In a nutshell, that means my heart muscle is weak and doesn't pump as well as it should.  Fortunately, I am currently doing fine (thanks to modern medications).

One aspect of living with heart failure is that you have to weigh yourself every day and keep track of day to day changes.  That shouldn't be hard, right?  I already weigh myself every day and keep track of changes, and I have this nice web app that makes it easy.  Sort of.

Without getting into too many boring details, let me give you a little background.  When a person's heart gets weaker, a common symptom is that they start retaining water.  It was explained to me like this: You heart is not pumping as strongly as it used to, and your kidney notices and thinks the problem may be that there is a shortage of blood, and so your kidney starts keeping more water in your body to help make up for the perceived shortage.  The result is that a person will steadily gain weight from additional water over days or weeks.  Doctors want to watch for this happening, and so heart failure patients weigh themselves every day looking for sudden or steady increases in weight (which might indicate that their heart failure has gotten worse).

Since I have been weighing myself every day for the past year or so, I have some interesting pre-diagnosis data to look at.  Here is my summer / fall:

<img class="fancybox" src="/stuff/trendweight-chf-chart1.png" />

At the time, I didn't know what was going on, but I did know or at least strongly suspect that I was gaining water weight and not fat.  When you look at just October and November and compare my total weight gain with my change in fat mass, you can clearly see that I was not gaining fat (I was also certain that I was not gaining muscle because at the time I was nearly completely unable to do any kind of exercise):

<img class="fancybox" src="/stuff/trendweight-chf-chart3.png" />

When I finally got in front of a cardiologist at the end of November, it didn't take them long to figure out that I was suffering from heart failure.  The good news is that with just some medications, all this extra retained water drained off quickly, my symptoms disappeared, and I am back to feeling "normal" and have a normal level of physical activity including daily exercise.

<img class="fancybox" src="/stuff/trendweight-chf-chart2.png" />

<img class="float-right border" src="/stuff/trendweight-chf-table1.png" width="300" />

Ok, so let me get back to TrendWeight.

I weigh myself every day, and that data already shows up in TrendWeight automatically, so monitoring my weight for unexpected increases should be easy, right?.  But what matters, this time, is the actual scale reading and not the trend.  That's kind of the opposite of the normal TrendWeight philosophy which is all about helping you ignore the actual scale weight and instead focusing on the slower changing trend over time.

When you are trying to lose weight (fat), you want to ignore the day to day changes caused by varying amounts of water retention.  But when watching for weight changes in the context of heart failure, it's the water retention that we're looking for.  The smoothing of day to day weight changes is counter productive, in this case.

I _could_ just look at the normal TrendWeight dashboard, look at the "Actual" column of the recent weight readings table and do math in my head.  For example, you can see (to the right) that from Jan 7 to Jan 8, I gained 1.6 lbs.  But wait.  Manually doing math in my head every day?  Isn't avoiding that kind of thing the main reason I developed TrendWeight in the first place?  I can do better than that :)

So, I have been working on a sort of "CHF Dashboard" that uses the same data from WiFi scales, but analyzes it and presents it in a different way.  In particular, it doesn't show the "trend" weight at all, because that is not the important data point.  It also automatically calculates daily and weekly weight changes and highlights days where there are higher than normal changes.  Per my doctors, what I am looking for is a change of more than 2 pounds in a single day or 5 pounds in a week.  If that happens, I call my doctors/nurses and they will likely make adjustments to my medication doses to get things back under control.

## The Dashboard

This is still a work in progress, but I have a beta version of the CHF dashboard that I am now using daily (in addition to the normal dashboard, because I am also still trying to lose more fat).  If you have CHF and want to try this today, you can view _your_ version of this dashboard by visiting <https://trendweight.com/chf/>.

<img class="fancybox border" src="/stuff/trendweight-chf-dashboard.png" />

The table shows the last 28 days of weight readings and the relative weight changes for that day (1 day and 7 day).  As a side note, I expect this will be helpful for my doctors and nurses as well.  They usually want to see my weight changes every time I have an appointment with them, and I can show them this dashboard on my iPad instead of keeping track redundantly on a sheet of paper that I give them.

Single day changes of more than 1 pound are highlighted (whether it is an increase or a decrease--they want to know about significant changes either way).  Weekly changes of more than 4 pounds are highlighted as well.  You might notice that the 7 day column usually has 2 numbers in it.  The reason is that my weight today may represent both a gain in weight from the lowest weight I had in the past week while at the same time being a drop in weight from the highest weight I had in the past week.  So the table shows both unless today's weight _is_ the highest or lowest weight in the past week.

I use this dashboard every day to see if my weight today is different enough to need to call the doctor (I am fortunate that that has never happened).  In reality, I can usually remember what my weight was yesterday, so I usually know, even when just standing on the scale, if my weight has changed by more than 2 lbs in the past day.  But I definitely need this tool in order to identify if my weight has changed by more than 5 pounds in a week.

As I said, this is still a work in progress, and it lacks a certain amount of polish.  But if you have a reason to want to track your day to day weight changes, feel free to use it.  As with the rest of TrendWeight, I created this primarily for myself, but there are millions people with heart failure in the world, so if this is a useful tool for some part of that community, I'm happy to share it.

As always, if you have feedback or comments, feel free to email me at <support@trendweight.com>.