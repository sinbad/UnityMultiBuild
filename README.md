# Unity MultiBuild

## What it does

Provides you with a simple in-editor interface to build for multiple platforms
at once.

![Screenshot](multibuild.png)

## How to use

1. From the menu, click `Tools` > `MultiBuild...`
1. Choose a base output folder (each platform will build into a subfolder)
1. Add the platforms you want to build for
1. Click the green build button

## More details

* The scenes that are built are the ones you've selected in Build Settings
* By default the project name is used as a base for all output, but you can
  disable that and provide a different name if you like
* Your settings are saved in your project at `Assets/MultiBuild/MultiBuildSettings.asset`
* After finishing the build, the active target is reset to whatever it was before
  you started the build
* Be aware that I've only really tested this with the standalone builds right now

## Why?

I often build for several platforms and found it tedious to keep switching in
the Build Settings window.

My first thought was to script it using the command line interface, but you can't
run a build of the current project if it's still open in the editor. It's
annoying to have to close Unity to do a build.

All the solutions I found were either scripted outside as above or commercial.
So I made a small open source version for myself &amp; others.

## Contributing

I'm not going to be spending loads of time on this so if you want a new feature
or you find a problem with one of the targets (or want to add more of them),
please fork and submit a pull request in the first instance. All contributions
welcome!

## License (MIT)

Copyright © 2017 Steve Streeting

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.




