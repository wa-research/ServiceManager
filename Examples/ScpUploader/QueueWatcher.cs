﻿using System;
using System.Configuration;
using System.IO;
using FolderMonitor;
using Renci.SshNet;

namespace ScpUploader
{
    public class QueueWatcher : FileWatcherBase
    {
        public QueueWatcher(WatcherInfo info) : base(info) { }

        protected override void HandleFile(string path, string name)
        {
            try {
                UploadFile(path, name);
                MarkAsDeleted(path);
            } catch (Exception ex) {
                Log.Error("Could not process file '{0}' ({1}:{2})", path, ex.Message, ex.StackTrace);
                MarkAsError(path);
            }
        }

        public void UploadFile(string fullPath, string fileName)
        {
            var host = ConfigurationManager.AppSettings["Host"];
            var user = ConfigurationManager.AppSettings["User"];
            var port = ConfigurationManager.AppSettings["Port"];
            var pass = ConfigurationManager.AppSettings["Password"];
            var key = ConfigurationManager.AppSettings["PrivateKey"];

            int p = 22;
            if (!string.IsNullOrEmpty(port))
                p = int.Parse(port);

            Log.Info("Uploading '{0}' to {1}@{2}:{3}", fileName, user, host, p);

            AuthenticationMethod auth;
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(user)) {
                Log.Info("Using public key authentication with key {0}", key);
                auth = new PrivateKeyAuthenticationMethod(user ,new PrivateKeyFile[]{ new PrivateKeyFile(key) });
            } else if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass)){
                Log.Info("Using password authentication");
                auth = new PasswordAuthenticationMethod(user,pass);
            } else {
                throw new Exception("Please ensure that username, and either PrivateKey or Password setting are defined in the configuration file.");
            }

            ConnectionInfo ConnNfo = new ConnectionInfo(host, p, user, new AuthenticationMethod[]{ auth } );

            using (var scp = new ScpClient(ConnNfo)) {
                scp.Connect();

                Log.Info("Connection opened, uploading file.");
                scp.Upload(new FileInfo(fullPath), fileName);
                Log.Info("File uploaded, closing connection.");
                scp.Disconnect();
            }
        }
    }
}
