---
layout: post
title: '"Fixing" Slow Wake for MacBook Pro w/ Retina Display'
date: 2012-09-09 22:00
published: true
comments: true
tags: Apple Hardware Gadgets
---

My main machine is a MacBook Pro w/ Retina Display.  I carry it at work every day and use it on and off throughout the day, mostly in meetings.  At home, I use it connected to a 27 inch Thunderbolt Display which powers the laptop and charges the battery.  I love almost everything about this laptop.  The one thing that has been getting on my nerves is that it is often painfully slow to wake up when I open the lid.

After opening the lid, it pretty instantly shows the password dialog box, but that UI is a lie.  In reality, what is immediately shown is a screenshot of what the screen looked like when it went to sleep.  The UI, although visible, is not functional for almost 10 seconds.  You can most easily tell that this is happening by watching the clock display in the upper right of the display.  Right after opening the lid, it will show the incorrect time (the time when the laptop went to sleep).  After 8-10 seconds, the time will become accurate and this is the signal that you can actually start typing your password to unlock the laptop.

What is actually happening is that these new MacBook Pro's (and recent MacBook Air's) have a new powersaving mode which Apple calls [standby](http://support.apple.com/kb/HT4392).  Standby mode kicks in after the laptop has been in normal sleep mode for about an hour.  When that happens, the contents of RAM are written to the hard drive and the RAM is powered down to further extend battery life.  In theory, the laptop will last up to 30 days in standby mode.  The trade off is that, when waking up, it takes a long time to reload 16 GB of RAM from the hard drive (even with SSD).

Note that if the laptop sleeps for less than an hour, then it will wake up nearly instantly.  I've seen this myself in cases where I close the lid briefly while walking between meetings.  This fact is the key to my workaround.  It turns out that this 1 hour delay is configurable.

I changed the standby delay for my machine from 1 hour to be 24 hours by running this command from a terminal window (86400 seconds = 24 hours):

    sudo pmset -a standbydelay 86400

Given that I plug my laptop into the Thunderbolt Display every night (which recharges the battery), I don't care as much about the standby feature keeping my battery alive for 30 days.  In theory, I may go on a trip or something and standby mode may be more useful, so I didn't disable it completely (which can be done by setting the delay to 0).  By setting it to 24 hours, standby will almost never happen with my normal day to day usage patterns and I'll almost always have instant wake times.  It may be that 24 hours is unnecessarily long and 12 or 18 hours would have worked just as well while providing a little better balance between battery life and day to day convenience.  Time will tell.

P.S. If you want to see what your current settings are, you can run this command from the terminal:

    pmset -g

