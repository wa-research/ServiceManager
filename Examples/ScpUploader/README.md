# Scp Uploader

Monitors a folder and uploads any new files to a configured location via SCP.

## Configuration

### Watcher

`inputFolder` - full path to the folder to watch for new files

`filter` - wildcard extension to specify the type of files to watch for. For example, *.xml will only upload xml files and ignore anything else added to the folder.

`deleteMeansDelete` - if set to `true`, will permanently delete the file once it is processed. Recommended for high volume of data. The default is `false`, which will copy processed files into `deleted` folder.

### AppSettings

`User` - user name to use to authenticate with the SSH server.

`PrivateKey` - path to the private key to use to connect to the destination SSH server. If set, the uploader will use public key authentication for the specified user.

`Password` - use password authentication. Only necessary if `PrivateKey` path is not set.

`Host` - the name of the host to upload to.

`Port` - the port at which to connect. Defaults to `22`.

`Path` - the file path at which to upload. If the path specification ends with forward slash (`/`), then the file will be uploaded with its name. Otherwise the uploader will always upload to the same file name on the destination.
