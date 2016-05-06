using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FolderMonitor
{
    public partial class FileWatcherBase
    {
        protected interface IRepeatableOperation
        {
            void Execute(Logger log);
            string Name { get; }
        }

        protected void TryFiveTimes(IRepeatableOperation repeatableOperation)
        {
            int counter = 0;
            while (counter < 5) {
                try {
                    repeatableOperation.Execute(Log);
                    break;
                } catch (Exception) {
                    Log.Debug("Operation {0} failed {1}. time", repeatableOperation.Name, counter + 1);
                    if (counter == 4) throw;
                    else System.Threading.Thread.Sleep(100 + counter * 200);
                }
                counter++;
            }
        }

        protected static string ComputeHash(string path)
        {
            byte[] retVal;
            using (FileStream file = File.OpenRead(path)) {
                MD5 md5 = new MD5CryptoServiceProvider();
                retVal = md5.ComputeHash(file);
                file.Close();
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++) {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Move source file to destination. 
        /// </summary>
        /// <remarks>
        /// If destination exists do:
        /// * If hashes match, just drop source
        /// * If hashes do not match, try to append (1) (or (2), ...) to the end of the file name
        /// * For each filename (n) check again for hash match before creating the next number and delete source if hashes match
        /// </remarks>
        protected class MoveFileOperation : IRepeatableOperation
        {
            private string _source;
            private string _destination;

            public MoveFileOperation(string source, string destination)
            {
                _source = source;
                _destination = destination;
            }

            public void Execute(Logger log)
            {
                if (File.Exists(_source)) {
                    if (!File.Exists(_destination)) {
                        File.Move(_source, _destination);
                    } else {
                        string sourceHash = ComputeHash(_source);
                        if (sourceHash == ComputeHash(_destination)) {
                            File.Delete(_source);
                        } else {
                            string path = Path.GetDirectoryName(_destination);
                            string file = Path.GetFileNameWithoutExtension(_destination);
                            string ext = Path.GetExtension(_destination);

                            for (int i = 1; i < 1000000; i++) {
                                string numberedDest = string.Format("{0} ({1})", file, i);
                                string newDest = Path.Combine(path, Path.ChangeExtension(numberedDest, ext));
                                if (!File.Exists(newDest)) {
                                    File.Move(_source, newDest);
                                    break;
                                } else if (sourceHash == ComputeHash(newDest)) {
                                    File.Delete(_source);
                                    break;
                                }
                            }
                        }
                    }
                } else {
                    log.Debug("{1}: File {0} does not exist.", _source, Name);
                }
            }

            public string Name
            {
                get { return "MoveFile"; }
            }
        }

        protected class ReadTextFileOperation : IRepeatableOperation
        {
            private string _filePath;
            private StringBuilder _contents;

            public ReadTextFileOperation(StringBuilder contents, string filePath)
            {
                _contents = contents;
                _filePath = filePath;
            }

            public void Execute(Logger log)
            {
                if (File.Exists(_filePath)) {
                    using (StreamReader reader = new StreamReader(File.OpenRead(_filePath))) {
                        _contents.Append(reader.ReadToEnd());
                        reader.Close();
                    }
                } else {
                    log.Debug("{1}: File {0} does not exist.", _filePath, Name);
                }
            }

            public string Name
            {
                get { return "ReadFile"; }
            }
        }

        protected class DeleteFileOperation : IRepeatableOperation
        {
            private string _filePath;

            public DeleteFileOperation(string filePath)
            {
                _filePath = filePath;
            }

            public void Execute(Logger log)
            {
                if (File.Exists(_filePath)) {
                    File.Delete(_filePath);
                } else {
                    log.Debug("{1}: File {0} does not exist.", _filePath, Name);
                }
            }

            public string Name
            {
                get { return "DeleteFile"; }
            }
        }
    }
}