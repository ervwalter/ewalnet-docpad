---
layout: post
title: "Connecting CurrentCost, PlotWatt, and Cosm"
date: 2012-09-15
published: true
comments: true
tags: CurrentCost PlotWatt Cosm Gadgets
---

In my post, [Monitoring Energy Usage for Fun and Profit](/2012/09/03/monitoring-energy-usage-for-fun-and-profit/), I described my energy monitoring setup which consists of a CurrentCost energy monitor feeding data to PlotWatt.com and Cosm.com for analysis.  In this post, I will explain how all the connections work.

At a high level, the CurrentCost receiver is connected to a Windows computer via a special USB data cable.  That Windows box is running a piece of software, the CurrentCost Agent, that reads data from the CurrentCost receiver and feeds it to PlotWatt and Cosm using their APIs.

If you go to the CurrentCost website, you may notice that they have something called the Enerati Web Bridge (also sometimes called the CurrentCost Web Bridge).  This is a device that plugs into the receiver and into an Ethernet connection and sends energy data to their web service without the need for a computer in the middle.  I am not uses this box for several reasons:

1. The web bridge only uploads one data point every 5 minutes even though the CurrentCost receiver is capable of providing data points every 7 seconds.
2. Feedback from others who have these web bridges is that they are flaky.  In other words, sometimes they just stop uploading data and have to be power cycled to start working again.
3. The web bridge only uploads data to a single place.  In the latest iteration of the web bridge, that is the Enerati web site.  My personal opinion is that the Enerati web site is ugly and doesn't provide terribly useful functionality.  But most of all, I want to be able to control what happens with my data.  I want to send it to multiple web sites, and I want the option to store it in my own database, etc.

So, instead of using the web bridge that CurrentCost sells, I bought the USB Data Cable from them and wrote my own software to read the data and send it to the places I wanted to send it.

Note, all of the code I discuss below is available in this [GitHub repository](https://github.com/ervwalter/currentcost-agent).

The agent is internally broken into two pieces as shown below.  The serial port reader thread reads from the CurrentCost display as data comes in and queues it for processing.  An uploader thread wakes up periodically to upload any queued data to the cloud.

<img src="/stuff/currentcost-agent-diagram.png" />

## Getting Data from the CurrentCost Receiver

When you use the USB Data Cable, the CurrentCost receiver will appear as a virtual COM port.  Fair warning, getting the USB Data Cable to work correctly with 64bit Windows 7 is a pain in the ass.  They provide [some instructions](http://www.currentcost.net/Windows%207%2064bit%20Prolific%20Data%20Cable%20Drivers%20Code%2010%20Workaround.pdf) but it is still painful, and you have to be careful not to deviate from them at all.  First, you have to use the driver _they_ provide (not the one included with Windows 7).  The correct driver is included in the [Current Cost Smart Software](http://www.currentcost.net/softwareoptions.html) download, so install that even if you don't want the CurrentCost application.  Note, the C2 Terminal application on that same download page is very useful for debugging, so you might want to get that too.

Anyway, once you get the device showing up in Windows successfully, you will have a virtual COM port you can open and read from.  In my agent, I start a thread that will be dedicated to reading from the serial port.  The first step is opening the serial port for reading:

``` cs
using (SerialPort port = new SerialPort())
{
    port.PortName = _comPort;  // e.g. "COM5"
    port.BaudRate = _baudRate; // e.g. 57600
    port.DtrEnable = true;
    port.ReadTimeout = 5000;
    port.Open();

    // read data, etc...

}
```

Once the COM port is open, you can just read from it forever (until the app is terminated).  For each sensor paired with the receiver, you'll get one line of XML every 7 seconds.  That XML contains information on the current readings for that sensor.  If the sensor is a whole home transmitter, it may have multiple channels--one per connected CT clamp.  If it is an IAM, it will have only a single channel.  I parse the XML and add a SensorReading object to a `ConcurrentQueue<SensorReading>` queue for the other thread to eventually deal with:

``` cs
while (true)
{
	// exception handling has been removed for brevity. look on github for the real code.

    string line = port.ReadLine();
    XDocument doc = XDocument.Parse(line, LoadOptions.None);
    var msg = doc.Element("msg");
    if (msg != null)
    {
        SensorReading reading = new SensorReading();
        var ch1 = msg.Element("ch1");
        if (ch1 != null)
        {
            reading.Timestamp = (int)(DateTime.UtcNow - epochStart).TotalSeconds; // unix timestamp
            reading.Sensor = int.Parse(msg.Element("sensor").Value);
            reading.Watts = int.Parse(ch1.Element("watts").Value);
            var ch2 = msg.Element("ch2");
            if (ch2 != null)
            {
                reading.Watts += int.Parse(ch2.Element("watts").Value);
            }
            var ch3 = msg.Element("ch3");
            if (ch3 != null)
            {
                reading.Watts += int.Parse(ch3.Element("watts").Value);
            }
            readings.Enqueue(reading);
            _statusForm.Invoke(new NewReadingHandler(_statusForm.NewReading), reading); // inform the UI of the new reading
        }
    }
}
```

This is basically all the sensor reading thread does. Note, in my agent, I ignore the timestamp that comes from the receiver (because I don't want to bother keeping the receiver's clock accurate) and I just use the Windows computer's clock.  

## Processing Sensor Readings

There is a second thread in my agent that wakes up every 30 seconds and processes any pending new readings.  In my case, this means sending the data to PlotWatt and Cosm, but you could also store the data in a database or send it to some other cloud service for analysis.

``` cs
// exception handling has been removed for brevity. look on github for the real code.
DateTime nextUpload = DateTime.Now.AddSeconds(UploadFrequency);
while (true)
{
    Thread.Sleep(1000);
    if (DateTime.Now < nextUpload)
    {
        continue;  // it's not time for another upload, so go back to sleep
    }
    nextUpload = DateTime.Now.AddSeconds(UploadFrequency);
    List<SensorReading> newReadings = new List<SensorReading>();
    SensorReading newReading;
    while (readings.TryDequeue(out newReading))
    {
        newReadings.Add(newReading);
    }

    if (newReadings.Count == 0)
    {
        continue;
    }

    //upload to cloud services
    PostToPlotWatt(newReadings);
    PostToCosm(newReadings);

    //notify the UI that we did an upload
    _statusForm.Invoke(new NotifyUploadHandler(_statusForm.NotifyUpload));
}
```

## Uploading to Cosm

Uploading to Cosm requires two things.  First, you need to get an API key from your dashboard, and second, you need to create datastreams for each sensor that you have.  I have a main sensor and four IAMs, so that means I needed to create datastreams 0, 1, 2, 3, and 4.

The actual upload is pretty straightforward. The Cosm API requires one HTTP POST per datastream, so the first step is to organize the new readings by sensor id.  While doing that, each reading is converted to the required JSON format.  The second step is to make an HTTP POST for each datastream with all of the readings for that sensor.

``` cs
private void PostToCosm(List<SensorReading> newReadings)
{
    // organize new readings by sensor id
    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    Dictionary<int, JArray> feeds = new Dictionary<int, JArray>();
    foreach (var reading in newReadings)
    {
        if (!feeds.ContainsKey(reading.Sensor))
        {
            feeds.Add(reading.Sensor, new JArray());
        }
        JObject o = new JObject();
        o["at"] = epoch.AddSeconds(reading.Timestamp).ToString("o");  // convert unix timestamp to the required format
        o["value"] = (int)reading.Watts;
        feeds[reading.Sensor].Add(o);
    }

    // upload all the readings for each datastream
    WebClient cosmClient = new WebClient();
    foreach (var feedId in feeds.Keys)
    {
        string cosmUrl = string.Format("http://api.cosm.com/v2/feeds/71541/datastreams/{0}/datapoints?key={1}", feedId, _cosmKey);
        JObject cosm = new JObject();
        cosm["datapoints"] = feeds[feedId];
        string cosmData = cosm.ToString();
        cosmClient.UploadString(cosmUrl, cosmData);
    }
}
```

Cosm provides a nice API debugging console on their dashboard that you can use to watch your uploads happening in real time.

## Uploading to PlotWatt

Uploading to PlotWat also requires an API key.  To get one, go through the PlotWatt setup steps and when it asks you how you want to connect data, choose "Web-Connected Energy Meter" and then "PlotWatt API" from the list of energy meter types.  Last, to get your actual API key after you go through the setup, you have to go to your Settings page and click on "Connected Gateways".  This page will show your API key which will look something like `OGNmMmRhMWFjODBj`.  Perhaps in the future, PlotWatt will show the API key on a page during the initial setup so that you don't have to go looking for it afterwards.

With PlotWatt, the agent will automatically create "meters" based on the sensors connected to your receiver.  It does this in kind of a dumb way though.  First, it asks PlotWatt how many meters you already have and then it creates additional meters if it looks like you have data for more meters than that.  Depending on your situation, you may or may not have to contact PlotWatt support to get some help after the agent does this:

If you have only a single, whole home sensor connected to your CurrentCost system, the agent will create a single meter and everything will work fine for you.

If you have have multiple sensors (multiple transmitters or a single transmitter + IAMs, etc) then you'll probably have to have the PlotWatt guys tweak things for you.  The agent will create the correct number of meters, but there is no way in the API to tell PlotWatt what kind of sensor each is.  For example in my case, sensor 0 was the whole home sensor and sensors 1-4 were IAMs that represented a subset of the energy usage from sensor 0.  Until the PlotWatt guys tweaked my meters, the PlotWatt website didn't seem to understand how to handle this data.  Similarly, if you had multiple whole home meters and perhaps a meter for your solar power generation, the I don't know if PlotWatt would automatically know how to add the meters together.  In any case, my experience was that the PlotWatt staff are very friendly and willing to help get this initial setup in place, so I'd say don't hesitate to email them.

After the meters are setup, the last step is uploading the data.  The PlotWatt API supports a single bulk HTTP POST for all of the pending data for all sensors in a comma delimited format:

``` cs
private void PostToPlotWatt(List<SensorReading> newReadings)
{
    WebClient client = new WebClient();
    //tell the WebClient to use your API key
    string authInfo = _plotWattKey + ":";
    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
    client.Headers["Authorization"] = "Basic " + authInfo;

    //first check how many meters are on the account already and create new ones if necessary
    int maxSensorReadings = newReadings.Max(sr => sr.Sensor) + 1;
    string rawMeters = client.DownloadString("http://plotwatt.com/api/v2/list_meters").Trim().Trim('[', ']');
    string[] meters = rawMeters.Split(',');
    if (string.IsNullOrWhiteSpace(rawMeters) || meters.Length < maxSensorReadings)
    {
        int numberToCreate;
        if (string.IsNullOrWhiteSpace(rawMeters))
        {
            numberToCreate = maxSensorReadings;
        }
        else
        {
            numberToCreate = maxSensorReadings - meters.Length;
        }
        //create new meters if more are required
        client.UploadString("http://plotwatt.com/api/v2/new_meters", "number_of_new_meters=" + numberToCreate.ToString());
        rawMeters = client.DownloadString("http://plotwatt.com/api/v2/list_meters").Trim().Trim('[', ']');
        meters = rawMeters.Split(',');
    }

    //now build the upload and post the readings
    List<string> readingsToPost = new List<string>();
    foreach (var reading in newReadings)
    {
        readingsToPost.Add(string.Format("{0},{1},{2}", meters[reading.Sensor], reading.Watts / 1000.0, reading.Timestamp));
    }
    string postData = string.Join(",", readingsToPost);
    client.UploadString("http://plotwatt.com/api/v2/push_readings", postData);
}
```

## User Interface

<img class="float-right" src="/stuff/currentcost-agent.png" />

My agent has a minimal user interface that sits in the lower right corner of the screen.  There is also a system tray icon that can be used to reopen the UI if you close it.  You could just as easily create a Windows service instead of a system tray application if you didn't care about the UI.  Frankly the UI I created isn't terribly interesting and the only reason I created it was so that I had something I could look at the make sure the agent was still alive and functioning.  I'm sure you could do something much better if you really wanted to :)

In the code above there are two places where the background threads interact with the UI.  First, when the sensor reading thread gets a new reading, it tells the UI thread about it.

- The UI is updated with the new wattage reading
- The counter of pending uploads can be incremented

The second notification happens in the uploader thread.  After an upload is completed, the UI is notified so that the counter of pending uploads can be reset and so the "Last Upload: MM/DD/YYYY" message can be updated.

Of course, because the UI is running on the main application thread, the background threads use `_statusForm.Invoke(...)` to notify the UI thread of the relevant events.
<br class="clear" />
