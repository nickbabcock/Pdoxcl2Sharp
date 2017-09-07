# Pdoxcl2Sharp

(.Netstandard 2.0): [![Build status](https://ci.appveyor.com/api/projects/status/d3uy1opirc4t19di)](https://ci.appveyor.com/project/nickbabcock/pdoxcl2sharp)
[![Build Status](https://travis-ci.org/nickbabcock/Pdoxcl2Sharp.svg?branch=master)](https://travis-ci.org/nickbabcock/Pdoxcl2Sharp)

Pdoxcl2Sharp is a general parser for files related to Paradox Interactive.
While the parser is aimed towards Paradox Interactive, it is not exclusive,
meaning that any file or configuration written in a similar
[style](#style-of-file) can be parsed without problems.

## Features

- Speed: Seriously. The parser was written to rip through 50MB files as fast as
  possible
- Reliability: There are a 100+ tests to ensure that the parser can handle any
  situation
- Encoding: The parser handles the encoding and decoding of files so that all
  your letters render fine. I'm looking at some of you guys (šžŸ)
- Ease of use: Don't worry about if something contains quotes, if a list spans
  multiple lines, or about brackets. The parser takes care of everything. The
  parser only shows you what you care about.
- Saving: You can as easily write info as parse it.
- Lossless Compression: If you don't care about a pretty output, you can
  compress what is written and it will still be read successfully from the
  parser and Paradox, hence the phrase "lossless". You can achieve compression
  ratios up to three (so your new file will be three times smaller than the
  old).
- No dependencies: Written in pure managed C#, relying on no other libraries,
  Pdoxcl2Sharp has seamless integration into any situation

## Motivation

Many of those who play Paradox Interactive's games are also programmers;
however, one of the biggest hurdles to developing a tool is the actual parsing
of the files.  Parsing is a nontrivial problem that is hard to get right.  This
project aims to be 100% compatible with the parser Paradox uses.  The end is to
eliminate of all the boiler plate code and provide a "plug and parse"
mechanism.

## Install

Pdoxcl2Sharp can be [installed from NuGet](https://nuget.org/packages/Pdoxcl2Sharp):

```
PM> Install-Package Pdoxcl2Sharp
```

If you will be doing all the parsing manually, then all you need to do is grab
the package from the latest release, and add the appropriate reference in your
favorite IDE.

If you want to use the parser generator, ParseTemplate.tt, which I highly
recommend, then you'll have to grab that file as well, and then modify the
text in between the commented "add" section. Add a reference to the downloaded
.dll in the generator.

## Example

Say you want to parse this file:

    # Hey, I'm a comment, put me anywhere and everything
    # until the end of line won't matter and will be chucked! 

    theoretical = {
        infantry_theory
        militia_theory
        mobile_theory
    }
    

Here's how:

```csharp
// Here we define a class that says "I can read and write myself"
public class TheoreticalFile : IParadoxRead, IParadoxWrite
{
    IList<string> theories = new List<string>();
    
    // This is the read interface. Whenever the parser finds something
    // interesting it will pass it to this function as the token. From
    // there, we decide if we want to process it further. This function
    // will never receive the whitespace, null, a bracket, etc. With the
    // example, token will be "theoretical", "infantry_theory",
    // "militia_theory", and "mobile_theory"
    public void TokenCallback(ParadoxParser parser, string token)
    {
        // Hey, we know what to do when we see "theoretical"!,
        // we know that it is simply a list of strings.
        if (token == "theoretical") 
        {
            theories = parser.ReadStringList();
        }
    }

    // This is the write interface, and unlike the read interface
    // this is not a callback. This simply means that given a writer
    // the class will dump its contents there.
    public void Write(ParadoxStreamWriter writer)
    {
        writer.WriteComment("Hey, I'm a new comment");
        
        // Write the header for the list. Notice that we include a bracket.
        // The writer will automatically indent subsequent lines by an
        // additional tab
        writer.WriteLine("theoretical = {");
        foreach (var theory in theoryFile.theories)
        {
            writer.WriteLine(theory, ValueWrite.LeadingTabs);
        }
        writer.WriteLine("}");
    }
}

public static int Main()
{
    TheoreticalFile theoryFile;

    using (FileStream fs = new FileStream("theories.txt", FileMode.Open))
    {
        theoryFile = ParadoxParser.Parse(fs, new TheoreticalFile());
    }

    // Save the information into a new file
    using (FileStream fs = new FileStream("theories.new.txt", FileMode.Create))
    using (ParadoxSaver saver = new ParadoxSaver(fs))
    {
        theoryFile.Write(saver);
    }
}
```

So the previous example was fine and all to write by hand, but as one can
imagine the more complex a document that needs to be parsed is, the more
complex the code to write the parser is. Luckily, we can take advantage of [T4
templates][] in the ParseTemplate.tt file to write the reading and writing
code. You'll gain a lot from using the parser template. For every one line of
code specified in the T4 document, expect at least six lines of code written
for you. The greatest thing is understanding how to modify the T4 template is
insanely simple. If you're trying to integrate the template into your project
for the first time, here are a couple of instructions that you need to execute
one.

- Locate the line `<#@ assembly
  name="$(SolutionDir)\Pdoxcl2Sharp\bin\Pdoxcl2Sharp.dll" #>`. This tells the
  template where to find some code needed to run the template. Right now, the
  value points to the build directory for this project, which will obviously
  differ for your project. You should take the Pdoxcl2Sharp.dll and move it a
  vendor directory in your project. Change the assembly line in the template to
  the new location, which should now be `<#@ assembly
  name="$(SolutionDir)\vendor\Pdoxcl2Sharp.dll" #>`
- Locate `namespace Pdoxcl2Sharp.Test` and change it to the desired namespace

Now we get to the good stuff. The T4 template works off an array whose elements
contain a Name, which will be the generated class's name, and an array of
properties. Each property has a required Name and Type field. The type of the
property is the literal string representation of the type eg. "string", "int",
etc and from this type, the template knows what to generate for reading and
writing. The template tries to guess what the property looks like in the text
by transforming the Name field. For instance, a property Name of
"GoodLookingMan" will cause the template to look for "good_looking_man" in the
text. The template is incredibly smart about this. Given an "IList<Army>" with
a property name of "Armies", the template will generate code for reading and
writing individual "army" elements. Naming can be overridden by explicitly
specifying the `Alias` field.

The last thing that you need to tell the template is if the string you are
writing should contain quotes surrounding it. Do this by specifying the `Quoted
= true` field.

[T4 templates]: http://msdn.microsoft.com/en-us/library/bb126445.aspx

Here is a more advanced example using what we just talked about. Let's say you
want to parse the following file (it is slightly unrealistic, but it serves as a
good example)

```
name = "My Prov"
tax = 1.000
add_core=MEE
add_core=YOU
add_core=THM
top_provinces={ "BNG" "ORI" "PEG" }
army={
  name = "My first army"
  leader = { id = 5 }
  unit = {
    name = "First infantry of Awesomeness"
    type = ninjas
    morale = 5.445
    strength = 0.998
  }
  unit = {
    name = "Second infantry of awesomeness"
    type = ninjas
    morale = 6.000
    strength = 1.000
  }
}
```

You could manually code this up in an hour, or you could modify
ParseTemplate.tt and in a few minutes and 35 lines of code, you could have the
same thing! Here are the relevant lines added to ParseTemplate.tt:

```csharp
var classes = new[] {
    new {
        Name = "Province",
        Props = new[] {
            new Property() { Type = "string", Name = "Name", Quoted = true },
            new Property() { Type = "double", Name = "Tax" },
            new Property() { Type = "IList<string>", Name = "Cores", Alias = "add_core" },
            new Property() { Type = "[ConsecutiveElements] IList<string>", 
                             Name = "TopProvinces", Quoted = true},
            new Property() { Type = "IList<Army>", Name = "Armies"}
        }
    },
    new {
        Name = "Army",
        Props = new[] {
            new Property() { Type = "string", Name = "Name", Quoted = true },
            new Property() { Type = "Leader", Name = "Leader" },
            new Property() { Type = "IList<Unit>", Name = "Units" }
        }
    },
    new {
        Name = "Unit",
        Props = new[] {
            new Property() { Type = "string", Name = "Name", Quoted = true },
            new Property() { Type = "string", Name = "Type" },
            new Property() { Type = "double", Name = "Morale" },
            new Property() { Type = "double", Name = "Strength" }
        }
    },
    new {
        Name = "Leader",
        Props = new[] {
            new Property() { Type = "int", Name = "Id" }
        }
    }
};
```

Here's the generated auto-generated content:

```csharp
  
  
using System;
using Pdoxcl2Sharp;
using System.Collections.Generic;

namespace Pdoxcl2Sharp.Test
{
    public partial class Province : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public double Tax { get; set; }
        public IList<string> Cores { get; set; }
        public IList<string> TopProvinces { get; set; }
        public IList<Army> Armies { get; set; }

        public Province()
        {
            Cores = new List<string>();
            Armies = new List<Army>();
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "tax": Tax = parser.ReadDouble(); break;
            case "add_core": Cores.Add(parser.ReadString()); break;
            case "top_provinces": TopProvinces = parser.ReadStringList(); break;
            case "army": Armies.Add(parser.Parse(new Army())); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            writer.WriteLine("tax", Tax);
            foreach(var val in Cores)
            {
                writer.WriteLine("add_core", val);
            }
            if (TopProvinces != null)
            {
                writer.Write("top_provinces={ ");
                foreach (var val in TopProvinces)
                {
                    writer.Write(val, ValueWrite.Quoted);
                    writer.Write(" ");
                }
                writer.WriteLine("}");
            }
            foreach(var val in Armies)
            {
                writer.Write("army", val);
            }
        }
    }

    public partial class Army : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public Leader Leader { get; set; }
        public IList<Unit> Units { get; set; }

        public Army()
        {
            Units = new List<Unit>();
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "leader": Leader = parser.Parse(new Leader()); break;
            case "unit": Units.Add(parser.Parse(new Unit())); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            if (Leader != null)
            {
                writer.Write("leader", Leader);
            }
            foreach(var val in Units)
            {
                writer.Write("unit", val);
            }
        }
    }

    public partial class Unit : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Morale { get; set; }
        public double Strength { get; set; }

        public Unit()
        {
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "type": Type = parser.ReadString(); break;
            case "morale": Morale = parser.ReadDouble(); break;
            case "strength": Strength = parser.ReadDouble(); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            if (Type != null)
            {
                writer.WriteLine("type", Type);
            }
            writer.WriteLine("morale", Morale);
            writer.WriteLine("strength", Strength);
        }
    }

    public partial class Leader : IParadoxRead, IParadoxWrite
    {
        public int Id { get; set; }

        public Leader()
        {
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "id": Id = parser.ReadInt32(); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            writer.WriteLine("id", Id);
        }
    }
}

```

## FAQ: What does ConsecutiveElements mean?

If you do all the parsing by hand then there is no need to worry about this;
however, if you are using the generator then there are a couple of ambiguous
situations. Let's say you have a list of strings representing factories:

    factories={Here There Everywhere}

This is also valid

    factory = Here
    factory = There
    factory = Everywhere

It would prohibitively expensive to support both variations with a single list,
hence the attribute must be appended lists that are in the format of the first
factory example.

Consecutive Elements for non-primitive types is even more interesting as there
three good representations for the list, and all three are used in practice.
Let's say there is a list of `Attachments` that we want parsed, the three ways
to encounter this list are as follows.

```
// First method - individual occurences (non-consecutive elemtents)
attachment={...}
attachment={...}

// Second method - nested structures
attachments={ {...} {...} }

// Third method - nested structures with headers
attachments={ attachment={...} attachment={...}}
```

The second and third methods have consecutive elements, and in a tough decision
it was decided that the parser would support the second method in automatic
parsing, whereas the third method involves the client fleshing out the parsing
structure.

To parse the second method, use the following line:

```csharp
list = parser.ReadList(() => parser.Parse(new Attachment()));
```

## On Automatic Deserialization

Popular libraries for XML, JSON, YAML, CSV, and basically any structured text
document has an automatic deserialization feature, where all one has to do is
call `Deserialize<MyType>(text)` and they will receive a populated object back.
There currently is a partially implemented version, which can be invoked with
`ParadoxParser.Deserialize<MyType>()`. It does everything fine except
consecutive list elements. Please feel free to implement this one missing
feature. I doubt I'll work on it as I don't see the benefits right now, as will
be explained.

It's hard to support automatic deserialization, as Paradox files are not
rigorously structured. For instance, in JSON there is only one-way to denote a
list `a: [...]`, but in a paradox file there are [multiple
ways](#faq-what-does-consecutiveelements-mean) to handle lists. Sure, there is
a way around this if we force the user to denote everything with custom
attributes. The only problem would be the number of attributes might get out of
control. An aliased list with consecutive elements of quoted strings would need
three attributes to deserialize and serialize correctly. To me this seems
pretty cluttered. Another technical reason that makes it hard is that objects
can be in a partially deserialized state such as a list. We know that once
`a:[...]` is read that `a` is fully constructed, but if ou construct the list
out of individual `a` elements, you have a situation with a partially
constructed list. This isn't a problem by itself; it just exasperates current
problems with parsing lists.

Another reason why I'm not a fan is that deserializeing something is inherently
slow if done dynamically and especially something as complex as Paradox files.
Some of the use cases involve parsing a 50MB save file with a deep and complex
hierarchical tree, and parsing 10,000 similarly structured small files. Any
time spent discovering how to deserialize an object will be too long. An
interesting notion is precompiling like [protobuf-net][], as there really isn't
a need to rediscover how to deserialize an object on each program invocation.
I've toyed with implementing this, but I've decided against this, as I'm unsure
of the value that would be added.

Some popular libraries like [ServiceStack.Text][] use a concurrent dictionary
to store type and the function that will parse out the type after this function
has been computed. This is a great time saver but it is still too much time.
Think about it this way. Say we have 1.5 million lines with 100,000 objects of
maybe three dozen different kinds. Even if the dictionary was preloaded, a
100,000 lookups on a concurrent dictionary is fast, but definitely
non-negligible. For those curious, in this instance a single threaded regular
dictionary would be faster, but there would be no caching bonus across parallel
deserialiations (think back to the 10,000 file example). 

[protobuf-net]: https://code.google.com/p/protobuf-net/
[ServiceStack.Text]: https://github.com/ServiceStack/ServiceStack.Text

## TokenCallback and ReadString

These methods are the heart of the parsing.  Whenever the parser comes across
something interesting, it invokes the callback with what it found.  The token
found will never be a comment, an equal sign, brackets, empty, null or a string
that has quotes (the quotes are stripped).  ReadString has the exact same
behavior

If there are child structures that are being parsed (delimited by squirrely
brackets `{}`) and the parent and the children share the same tokens such as
"name" and "id", these can be differentiated by querying `CurrentIndent`.  The
children will be at a higher indent than the parents.  Perhaps a better
solution to this approach is to define a separate class for the children or an
`Action<ParadoxParser, string>` and invoke `Parse` on the parser.

## Contributing

So you want to help? Great!  Here are a series of steps to get you on your
way! Let me first say that if you have any troubles, file an issue.

- Get a github account
- Fork the repo
- Add a failing test.  The purpose of this is to show that what you are adding
  couldn't have been done before, or was wrong.
- Add your changes
- Commit your changes in such a way there is only a single commit difference
  between the main branch here and yours.  If you need help, check out [git
  workflow][]
- Push changes to your repo
- Submit a pull request and I'll review it!

## Building

Install dotnet SDK 2.0 and then

```
dotnet build
```

If you have Visual Studio 2017, I believe you can just hit "Build"

## Style of File

This section describes in more detail the style of files that can be parsed.

The most important characters to the parser are `=`, `"`, `}`, `{`, `#`, `,`, and
whitespace.  Let's call this set lexemes.  The complement of lexemes is the
untyped.  Lexemes delimit tokens, which are composed of untyped characters.
The parsing process goes as such:

- Read a character and test to see if it is a lexeme.
    - If a command peeked at the next lexeme, return that
    - else advance through whitespace until a lexeme
        - If that lexeme is a comment, advance until newline and go back to step 1
        - else if `{` increase indent by one
        - else if `}` decrease indent by one
        - return encountered lexeme
- If lexeme is a quote, advance until closing quote
    - Everything enclosed in the quotes excluding the quotes is a token
    - Lexemes are considered untyped wrapped in quotes
- else if any other lexeme go back to step 1
- else if the character read was untyped
    - All characters until whitespace or a lexeme is considered a token
- Return token

This is probably, at best, hard to follow.  Hopefully, a couple of examples
will clear some of the confusion.

    player = "AAA"
      player= =  "AAA"

Both are parsed (ie, the tokens returned to the client) the same way `player`
and then `AAA`.  An explanation of this is that all whitespace is skipped until
something interesting is encountered.  Then `player` is grabbed.  From there,
the next token starts at the quotes.  Remember that tokens are composed
strictly of untyped characters, hence the second equals is completely ignored.

This leads to an interesting example showcasing the parser's flexibility.

    ids = { 1 2 3 4 }
    ids={1 2 3 4 }
    ids = {
        1, 2,
        3, 4,
    }

All three snippets will return `ids`, `1`, `2`, `3`, and `4` as tokens.  The
extra comma at the end of the '4' is even on purpose.

## License

Pdoxcl2Sharp is licensed under MIT, so feel free to do whatever you want, as
long as this license follows the code.

[git workflow]: https://sandofsky.com/blog/git-workflow.html
