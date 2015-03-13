# Introduction #

jgrep is a handy little command line app for windows, using .NET, for when you don't have grep.


# Details #

jgrep /? gives usage:


Usage:
> jgrep /p:<search pattern> [/i][/e][/r:

&lt;root&gt;

][/x:<exclude pattern>,...][/n:<include pattern>,...][/d:<max depth>][/q][/v][/l][/c]
> jgrep /h

> Options:

> /p:<search pattern>   Text to search for (see /e).
> /i                    Make pattern case insensitive.
> /e                    Use regular expression to search.
> /r:

&lt;root&gt;

             Root path to begin search. Defaults to './'
> /x:<exclude pattern>  (can use multiple times)
> > String pattern to exclude files/directories
> > from the search.

> /n:<include pattern>  (can use multiple times)
> > String pattern to include files/directories
> > from the search. overrides exclude

> /d:<max depth>        Number of directory levels deep to go.
> > -1 for infinite (default),
> > 0 for current dir, etc.

> /q                    Quiet.  Minimum output.
> /c                    Chatty output.
> /l                    Like similar option in grep. Only display
> > filenames that match.

> /v                    Like grep option.  exclude match instead
> > of including.

> /h
> /?                    this help.




# Config File #

You can store a preferences file in your %USERPROFILE% directory called .jgreprc
The format of the file is a command line switch per line.

If this file is found, the switches will be applied automatically.