# Project Description

Just some common utilities I've developed over the years that are shared among my various projects.
The original CodePlex side was here: https://archive.codeplex.com/?p=commonutils

# Details

There are numerous methods in a number of classes that have made my development tasks easier when working with .NET projects. I'm making the code available here in case anyone finds it useful. The documentation is included in the source for the most part. These libraries are continually updated as needed for my various projects, and are subject to change as required (not often, and usually for the betterment).

Because the source supports various .NET versions (officially .NET 3.5 and up, and .Net Core), and is fairly easy to include or build as is, binaries will NOT be provided at this time. However, you simply need to select your .NET version and compile the code. Preprocessor directives are included to make code compatible as needed (see top of Utilities.cs in the `Common.Shared` project for define explanations). When a .NET version is selected for a project, the Common Utilities projects will detect and create defines to reflect it (such as V1_1, V2, V3, V3_5, V4, and V4_5). The most noticeable differences are that optional parameters and dynamic objects are only supported in .NET 4.0 and higher.

The source is in C#.NET and should be easy to compile (only a few dependencies - depending on defines specified).

I don't plan on making a Nuget package for this, as it may be superseded by my CoreXT project.

# Basic Highlights

* Utility classes with miscellaneous methods to help with exceptions, objects, strings, arrays, collections, types, timing, SQL via SMO, and more.
  Examples: `Exceptions.GetFullErrorMessage(ex);` - Traverses the inner exceptions to build a simple text error message.
            `Types.ChangeType()` - A much more powerful type conversion utility, which also supports nullable types.
* Various extension method classes.
* Silverlight controls, such as BingMap, GoogleMap, RichTextEditor, etc. (Warning: Haven't been tested in years now due to lack of support for Silverlight)
* Some miscellaneous WPF specific utilities.
* And much more...
