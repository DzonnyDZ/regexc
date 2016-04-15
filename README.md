# regexc
.NET Regular Expression Compiler
The goal of this project is to provide command line compiler, and Vissual Studio project system that can take regular expressions defined in text files and compile them to .NET assembly which exposes the regular expressions as .NET classes and provides easy access not only to the regular expressions but also to named caprure groups.
The project is written in C#. .NET standard ``Regex.CompileToAssembly`` is usedto compile the regulare expressions. Generated assembly is then post-processed using Mono.cecil.

## Command line argumnets
```
regexc files and argumnets
```
* Files are parts to files that contain regex definitions
* Arguments are additional compilation argumnets
* Files and argumnets can be in any order and mixed
* Some argummnets has parameters

### Arguments
* ``/assembly name`` provides name of the assembly
* ``/ver`` assembly version
* ``/nop`` when specified assembly is generated but no post-processing is done. I.e. properties for named capture groups are not gonna be created.
* ``/obj path`` path to a directory to be used as temporary directory for storing files during compilation. The directory does not have to exists.
* ``/out path`` path to a files where to store the resulting assembly (DLL) to. If not specified output file is created in current directory.

## Syntax of regex files
* Each file can contain one or more regexes
* Each regex is represent as a block in the file
* Basic block structure is
```
Name: name
Option1: value
Option2: value
regex
------
````
* All options are case-insensitive
* There can be whitespaces at the beginning and end of the lines.
* Empty lines and lines that start with ``#`` (after any whitespaces at the beginning of the line) are ignored.
 * *Note*: Lines starting with ``#`` (after any whitespaces at the beginning of the line) are ignored also inside of the regex!
* ``Name:`` Is mandatory name of the regex (may contain namespace)
* Another options (optional, in any order) are:
 * ``Options:`` All ``RegexOptions`` enum members are supported + few additional options. Defalt ``None``. No need to specify ``Compiled`` option, regexes are always compiled. Additional options (except those from ``RegexOptions``) are:
  * ``Public``: Make the regex class public (default)
  * ``Private``: Make the regex class private (``internal`` in C#, ``assembly`` in CIL). Not very usefull ;-)
  * ``KeepWhite``: By default all leading and trainling whitespaces on lines of regular expression as well as new line characters between lines of the regex are excluded from the regular expression itself. By specifying this option the whitespaces are gonna be included. This does not affect whitespaces or whitespace groups inside of individual lines of the regular expression.
 * ``Timeout``: Specifies regex execution timeout in ``TimeSpan`` format
* After options the regular expression itself follows. It can be on multiple lines.
* In case thereare more regular expressions in the file each (except last) regular expression block must be terminated by a line that starts with at least 6 dashes (------).
