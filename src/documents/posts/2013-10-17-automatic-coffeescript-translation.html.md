---
layout: post
title: "Automatic CoffeeScript Translation in Blog Posts"
date: 2013-10-17 23:05
published: true
comments: true
tags: DocPad CoffeeScript
---

I like [CoffeeScript](http://coffeescript.org/), and I often include CoffeeScript code samples in blog posts.  But not everyone likes or has learned CoffeeScript.  To make my posts useful to a broader crowd, I could manually include the JavaScript equivalent for each CoffeeScript code block, but that is extra work for me, and it makes the posts more verbose.  Instead, I decided to see if I could automatically detect my CoffeeScript code blocks, and automatically make JavaScript translations available to readers on the fly.

It turns out it wasn't that hard.  In fact, you can see it in action in this blog post, itself.  Keep in mind that because this is client-side code, it won't be visible for people reading the blog in an RSS reader.  Those users will just see the normal CoffeeScript code blocks as before.

The first step was to remove [highlight.js](http://softwaremaniacs.org/soft/highlight/en/) processing from the server side of my DocPad blog engine and move it to the client side.  I did this because highlight.js adds additional markup to the code blocks, and I needed access to the unmolested CoffeeScript code.  Removing highlight.js from the server side was as easy as `npm remove --save docpad-plugin-highlightjs`.

The next step was to include highlight.js and coffee-script.js in my web pages.  I chose to most vendor scripts from the excellent [cdnjs](http://cdnjs.com/), but have to serve highlight.js myself because the CDN version doesn't have support for all the languages I care about (notably, CoffeeScript is not included).

``` html
<script src="/scripts/vendor/highlight.pack.js"></script>
<script src="//cdnjs.cloudflare.com/ajax/libs/coffee-script/1.6.3/coffee-script.min.js"></script>
```

Next, in my site's JavaScript code, I look for any code samples that are flagged as CoffeeScript by looking for the css class `lang-coffeescript`.  This css class is added by the markdown engine in DocPad when I add code blocks to my blog posts.  For each code block, I retrieve the CoffeeScript source code, compile it into JavaScript and then insert a new code block with the JavaScript source.  The extra markup uses Bootstrap tabs to make it easy to toggle between the two sets of code.

``` coffeescript
codeIndex = 0
$('pre code.lang-coffeescript').each ->
    codeIndex++
    $code = $(this)
    $pre = $code.parent()

    # add the markup to create the tabbed display
    $tabContent = $pre.wrap("<div class='tab-content'><div class='tab-pane active' id='code-#{codeIndex}-coffee'></div></div>").parent().parent()
    $("<ul class='nav nav-tabs auto-coffee'><li class='active'><a href='#code-#{codeIndex}-coffee' data-toggle='tab'>CoffeeScript</a></li><li><a href='#code-#{codeIndex}-js' data-toggle='tab'>JavaScript</a></li></ul>").insertBefore($tabContent)

    # compile into javascript
    coffeeSource = $code.text()
    jsSource = CoffeeScript.compile(coffeeSource, {bare: true})

    # add the javascript code block
    $tabContent.append("<div class='tab-pane' id='code-#{codeIndex}-js'><pre><code class='lang-javascript'>#{htmlEncode(jsSource)}</code></pre></div>")
```

The second-to-last step is to translate a couple language aliases I use.  `coffeescript-nojs` is an alias for `coffeescript` that just bypasses the code above and so won't be auto-translated.  `none` is just a shorter alias for highligh.js's normal `no-highlight`.  For example, I'll use `coffeescript-nojs` on the following code block to prevent the CoffeeScript/JavaScript UI from appearing:

``` coffeescript-nojs
$('.lang-coffeescript-nojs').removeClass('lang-coffeescript-nojs').addClass('lang-coffeescript')
$('.lang-none').removeClass('lang-none').addClass('lang-no-highlight')
```

The last step is to use highlight.js to format any code blocks on the page.  First, the code translates any `lang-whatever` classes into `language-whatever` because that is what the client-side highlight.js class is expecting.  Then highlight.js is called on any code blocks to do its thing.

``` coffeescript
$('pre code').each (index, element) ->
    $code = $(this)
    classes = $code.attr('class')?.split(' ')
    if classes? then for origClass in classes
        fixedClass = origClass.replace /^lang-/, 'language-'
        $code.removeClass(origClass).addClass(fixedClass) if fixedClass isnt origClass
    hljs.highlightBlock(element)
```

Throw in some CSS to make things look the way I wanted, and there you have it.

To see what the markdown source for this post looks like, look [here](https://raw.github.com/ervwalter/ewalnet-docpad/master/src/documents/posts/2013-10-17-automatic-coffeescript-translation.html.md).  And as always, you can find the complete source for my blog on GitHub, [here](https://github.com/ervwalter/ewalnet-docpad).
