using System;
using System.Linq;
using System.Reflection;

namespace ServiceManager
{
    public class ServiceProxy : MarshalByRefObject
    {
        public const string START_METHOD = "StartService";
        public const string STOP_METHOD = "StopService";
       
        public string Name { get; set; }
        private object _worker;
        private MethodInfo _start, _stop;

        public void Start()
        {
            if (_start == null) {
                _start = _worker.GetType().GetMethods().Where(m => m.Name == START_METHOD).FirstOrDefault();
            }
            if (_start != null)
                _start.Invoke(_worker, null);
        }

        public void Stop() 
        {
            if (_stop == null)
                _stop = _worker.GetType().GetMethods().Where(m => m.Name == STOP_METHOD).FirstOrDefault();
            if (_stop != null)
                _stop.Invoke(_worker, null);
        }

        public bool LoadWorker(string assemblyName)
        {
            AppDomain cd = AppDomain.CurrentDomain;
            cd.AssemblyResolve += ResolveAlreadyLoadedAssemblies;

            Assembly assembly = null;
            try {
                assembly = cd.Load(assemblyName);
            } catch (Exception ex) {
                throw new ApplicationException(string.Format("Could not load asssembly {0} from path  {1}", assemblyName, cd.BaseDirectory), ex);
            }
            this.Name = assembly.GetName().Name;

            Type entryPoint = assembly.GetTypes().Where(t => ContainsStartAndStopMethods(t)).FirstOrDefault();
            if (entryPoint == null)
                return false;

            try {
                _worker = assembly.CreateInstance(entryPoint.FullName, true);
            } catch (Exception ex) {
                throw new TypeInitializationException(entryPoint.FullName, ex);
            }

            return true;
        }

        private bool ContainsStartAndStopMethods(Type t)
        {
            var methods = t.GetMethods();
            return methods.Any(m => m.Name == START_METHOD) && methods.Any(m => m.Name == STOP_METHOD);
        }

        /// <summary>
        /// Checks whether an assembly has already been loaded into the appdomain and returns it.
        /// </summary>
        /// <remarks>This allows us to inject assemblies remotely and avoid copying all service dependencies into a folder of every loaded service.</remarks>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly ResolveAlreadyLoadedAssemblies(object sender, ResolveEventArgs args)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName == args.Name)
                .FirstOrDefault();
        }

        public override object InitializeLifetimeService() { return null; }

    }
}
