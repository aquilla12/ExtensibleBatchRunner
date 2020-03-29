# Extensible Batch Runner
An extension for Microsoft Visual Studio on Windows to provide an implemenation of Visual Studio path macros for project batch scripts.

## How it works
### Writing scripts
When writing scripts for this extension, most functions of batch will operate as normal. However, the engine provides a set of variables based on the paths to project files.

#### Variables
Each variable name is made up of a base variable and an optional modifier.
The following variables are available:
##### Base variables
* `solutionFile`: the VS solution file
* `projectFile`: the project file
* `file`: the script file
##### Modifiers
* _no modifier_: the name of the file specifed by the base variable without the extension
* `WithExtension`: the name of the file specifed by the base variable with the extension
* `Extension`: the extension of the file specifed by the base variable
* `Directory`: the direcrory where the file specifed by the base variable resides
* `FullPath`: the absolute full path to the file specifed by the base variable

##### Syntax
When using a variable in a script you must enclose it in curly braces. e.g. `{projectFileDirectory}`.

Expanded variables are automatically wrapped in double quotes. At this time there is no way to override this behaviour, but there may be in future versions (happy to accept contributions).

When writing scripts for this extewnsion you must use a `.batscript` extension. It is also recomended to put all your scripts in a `.scripts` folder in your project direcrory, but scripts anywhere in the project tree are supported, as long as they are visible to Visual Studio.

#### Looping
_See [Implementation](#Implementation) below for the reasons for these suggestions._
The engine will expand variables only in the file that is run and cannot analsye any scripts called from it. Therefore, if you require looping you should do one of the following:
* Write your child scripts as normal `.bat`s and use the `{fileDirectory}` variable to locate other scripts and run them from your main script, passing them any paths they need as parameters.
* For recursive scripts that must call themselves, use the `%0` batch variable, as the `{file}` variable will point to the original script file with unexpanded variables.

### Running scripts
To run a script, right click on it in the VS Solution Explorer, and find the `Run script` option.

### Implementation
This engine works by taking the file that is run and expanding any variables that it finds in it. It then saves the file in the local temp data directory as a batch script and calls it.
The following call paramaters are used:
* `UseShellExecute`: _true_
* `CreateNoWindow`: _true_
* `WorkingDirectory`: The direcrory where the original script file resides

The standard output and error feeds are also asynchronously redirected to the `Extensible batch engine` pane in the VS Output window in real time.

Temporary batch files are deleted after being run.

## Notes
It is recommended that before you use this extension for any production related purposes you run the [example script](../master/ExtensibleBatchRunner/.scripts/test.batscript) in order to familiarise yourself with the different variables.

## Thanks
My thanks to [@pauer24](https://github.com/pauer24/) for his [VS Namespace Fixer extension](https://github.com/pauer24/VsNamespaceFixer/) from which I have pulled a lot of the code needed to locate project and solution files. The license for this code can be found in the `licenses` folder.
