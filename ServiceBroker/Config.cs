using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ServiceBroker
{
	/// <summary>
	/// Used to retrieve config settings for a
	/// ServiceBroker service.
	/// </summary>
	public class Config
	{
		/// <summary>
		/// Caches application settings.
		/// </summary>
		/// <remarks>Key: App setting key; 
		/// Value: App setting value.
		/// </remarks>
		private HybridDictionary config;
		
		/// <summary>
		/// Caches path to config file.
		/// </summary>
		private string path;

		/// <summary>
		/// Used to watch for changes in config file.
		/// </summary>
		private FileSystemWatcher configWatcher = null;

		/// <summary>
		/// Gets the specified value from the config file.
		/// </summary>
		public string this[string key]
		{
			get
			{
				if (this.config == null)
					this.LoadSettings();
				if (this.config.Contains(key))
					return Convert.ToString(this.config[key]);
				else
					return string.Empty;
			}
		}
          /// <summary>
          /// 
          /// </summary>
		public  event EventHandler NotifyService;
		// Wire up the event

		/// <summary>
		/// 
		/// </summary>
		protected void OnNotifyService()
		{
			if(NotifyService !=null)NotifyService(true,EventArgs.Empty);
		}


		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <remarks>Attempts to get the path to the config
		/// file and start the watcher on that file.</remarks>
		public Config()
		{
			// gets the calling assembly, which is likely shadow copied,
			// so we get the AssemblyName instance, specifying we want the
			// original codebase, and then we replace the URL format with
			// UNC format
			this.path = Assembly.GetCallingAssembly().GetName(false)
				.CodeBase.Replace("file:///", "").Replace("/", "\\") + ".config";
			// create the watcher for the config file
			this.configWatcher = new FileSystemWatcher();
			this.configWatcher.Path = 
				path.Substring(0, path.LastIndexOf("\\"));
			this.configWatcher.Filter = 
				path.Substring(path.LastIndexOf("\\") + 1);
			// filter to watch only for writes
			this.configWatcher.NotifyFilter = NotifyFilters.LastWrite;
			// handle only changed and created events
			this.configWatcher.Changed += 
				new FileSystemEventHandler(this.ConfigChanged);
			this.configWatcher.Created += 
				new FileSystemEventHandler(this.ConfigChanged);
			this.configWatcher.EnableRaisingEvents = true;
		}

		/// <summary>
		/// Loads the expected config file and parses the 
		/// app settings into the config cache.
		/// </summary>
		private void LoadSettings()
		{
			// initialize the cache
			this.config = new HybridDictionary(10);
			// check for file
			if (File.Exists(this.path))
			{
				// load file into xml doc
				XmlDocument doc = new XmlDocument();
				try
				{
					doc.Load(this.path);
				}
				catch (Exception ex)
				{
					Logger.LogException(string.Format("Could not load '{0}' into an XML document.", path), ex);
					return;
				}
				// for each app setting
				foreach (XmlNode node in 
					doc.SelectNodes("/configuration/appSettings/add"))
				{
					// if a key attribute exists
					if (node.Attributes["key"] != null)
					{
						// check to see if key exists already in cache
						if (this.config.Contains(node.Attributes["key"].Value))
						{
							Logger.Error("Configuration for '{0}' already contains key '{1}'.", path, node.Attributes["key"].Value);
							continue;
						}
						// if value is not null, add this 
						// name-value pair to the cache
						if (node.Attributes["value"] != null)
							this.config.Add(node.Attributes["key"].Value,
								node.Attributes["value"].Value);
					}
				}
			}
			
		}

     
		private string FullPathString = String.Empty;
		private DateTime TimeFired;
		/// <summary>
		/// Handles reloading the config when the file changes.
		/// </summary>
		/// <param name="source">Source</param>
		/// <param name="e">Args</param>
		
		private void ConfigChanged(object source, FileSystemEventArgs e)
		{
			// nasty bug in FileSystemWatcher fires twice (in about 4 ms) on changed file. This is a workaround...
			if(e.FullPath.ToUpper()==FullPathString && TimeFired.Subtract(DateTime.Now).TotalMilliseconds < 50)                   return;
			 // set the values of the fullpath and time of the event fired to check / prevent dupe firings
			FullPathString =e.FullPath.ToUpper();
			TimeFired=DateTime.Now;
			// wait for any locks to be released
			System.Threading.Thread.Sleep(1500);
			// reload settings
			this.LoadSettings();
			// Trigger our notification event
			this.OnNotifyService();
		}
	}
}
