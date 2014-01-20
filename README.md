# ServiceManager

The simplest .NET lightweight "service" host. Loads assemblies named *Service.dll into their own AppDomains and calls `StartService()` method on them. Supports shadow-copy for super-easy xcopy deployment.

## Quick Test

Build the solution and then start `ServiceManager.exe` from command line; it will scan Examples folder, load all samples, and start them.

Try re-building any of the examples in Visual Studio (right-click on project and pick "Rebuild") to simulate update--the service will be reloaded automatically.

Type `list` to see a list of loaded services. Type `exit` to end and unload services.

## Production Use

Type `ServiceManager /install` to install as a windows service. Set the path to the services folder in the `.config` file. Copy folders into services.
