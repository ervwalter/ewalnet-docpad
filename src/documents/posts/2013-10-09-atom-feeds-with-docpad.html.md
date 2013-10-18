---
layout: post
title: "Atom Feeds With DocPad"
date: 2013-10-09 23:45
published: true
comments: true
tags: DocPad
---

If you are generating a blog or a blog-like site with DocPad, you'll probably want to create a atom or rss feed for the site.  Doing so is reasonably straightforward, but there is one little trick to be aware of.  The trick is that you need to convert any relative URLs in your blog content to absolute URLs.

First, start with a `feed.xml.eco` document that looks like this:

``` xml
<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom">
    <title><%= @site.title %></title>
    <link href="<%= @site.url %>/feed.xml" rel="self"/>
    <link href="<%= @site.url %>"/>
    <updated><%= @site.date.toISOString() %></updated>
    <id><%= @site.url %></id>
    <author>
        <name><%= @site.author %></name>
        <email><%= @site.email %></email>
    </author>

    <% for document in @getCollection('posts').toJSON()[0..9]: %>
    <entry>
        <title><%= document.title %></title>
        <link href="<%= @site.url %><%= document.url %>"/>
        <updated><%= document.date.toISOString() %></updated>
        <id><%= @getIdForDocument(document) %></id>
        <content type="html"><![CDATA[<%- @fixLinks(document.contentRenderedWithoutLayouts) %>]]></content>
    </entry>
    <% end %>
</feed>
```

You can see that the document is reasonably straightforward.  It generates a basic atom `feed` element, fills in the required elements using values from your docpad configuration.  Then it creates an `entry` for each of the 10 most recent blog posts.  It fills in each `entry` element with details about each post.

Notice the `@fixLinks()` call inside the `content` element.  This is the trick I mentioned.  This function parses the HTML content of each blog post looking for URLs in `<a>` and `<img>` tags that don't include a scheme (e.g. http://) and hostname in them and adds the site's base url (e.g. http://www.ewal.net).  For example this html in a blog post:

    <a href="/2013/10/08/blogging-with-docpad/">My last post</a>
    <img src="/stuff/forsalebyowner.png" />

becomes:

    <a href="http://www.ewal.net/2013/10/08/blogging-with-docpad/">My last post</a>
    <img src="http://www.ewal.net/stuff/forsalebyowner.png" />

This is very important if you include links in your blog posts that don't always include your domain's hostname.  Without this fix, people will see broken images and your internal site links won't work if they read you blog in a newsreader.

The `@getIdForDocument()` call just creates an appropriate unique Id for each post using [Mark Pilgrim's guidelines](http://web.archive.org/web/20110514113830/http://diveintomark.org/archives/2004/05/28/howto-atom-id).

The code for `fixLinks` and `getIdForDocument` lives in `docpad.coffee` as functions added to templateData.  They look like this:

``` coffeescript
docpadConfig = {
    templateData:

        getIdForDocument: (document) ->
            hostname = url.parse(@site.url).hostname
            date = document.date.toISOString().split('T')[0]
            path = document.url
            "tag:#{hostname},#{date},#{path}"

        fixLinks: (content) ->
            baseUrl = @site.url
            regex = /^(http|https|ftp|mailto):/

            $ = cheerio.load(content)
            $('img').each ->
                $img = $(@)
                src = $img.attr('src')
                $img.attr('src', baseUrl + src) unless regex.test(src)
            $('a').each ->
                $a = $(@)
                href = $a.attr('href')
                $a.attr('href', baseUrl + href) unless regex.test(href)
            $.html()
}
```

These two functions use a couple node modules&mdash;one named [cheerio](http://matthewmueller.github.io/cheerio/) to parse the HTML using a jQuery-like API and the standard `url` module from node.js.  Add references to them at the top of docpad.coffee (after you `npm install --save cheerio` of course):

    cheerio = require('cheerio')
    url = require('url')

You can see this code "in context" in [docpad.coffee](https://github.com/ervwalter/ewalnet-docpad/blob/master/docpad.coffee) and [feed.xml.eco](https://github.com/ervwalter/ewalnet-docpad/blob/master/src/documents/feed.xml.eco) from my blog's source code.

That's basically all there is too it. My site generates an atom feed, but you could just as easily use this technique to create an rss feed instead of or in addition to an atom feed if you prefer.