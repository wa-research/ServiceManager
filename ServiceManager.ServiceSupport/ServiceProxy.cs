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
        private object _service;
        private MethodInfo _start, _stop;

        public void Start()
        {
            if (_start == null) {
                _start = GetStartMethod(_service.GetType());
            }

            if (_start != null) {
                if (_start.GetParameters().Length == 1) {
                    _start.Invoke(_service, new object[] { new ServiceContext { ServiceName = this.Name } });
                } else {
                    _start.Invoke(_service, null);
                }
            }
        }

        /// <summary>
        /// Finds a <c>START_METHOD</c> with a <typeparamref name="ServiceContext"/> parameter, or a simple start without any parameters.
        /// </summary>
        /// <param name="t">Service type</param>
        /// <returns>A most specific start method found, or null if the assembly does not implement a <c>START_METHOD</c></returns>
        internal MethodInfo GetStartMethod(Type t)
        {
            var s = from m in t.GetMethods()
                     where m.Name == START_METHOD
                     let p = m.GetParameters()
                     where p.Length == 0 || (p.Length == 1 && p[0].ParameterType == typeof(ServiceContext))
                     orderby p.Length descending
                     select m;

            return s.FirstOrDefault();
        }

        internal MethodInfo GetStopMethod(Type t)
        {
            var stop = from m in t.GetMethods()
                       where m.Name == STOP_METHOD
                       let p = m.GetParameters()
                       where p.Length == 0
                       select m;

            return stop.FirstOrDefault();
        }

        public void Stop() 
        {
            if (_stop == null)
                _stop = GetStopMethod(_service.GetType());
            if (_stop != null)
                _stop.Invoke(_service, null);
        }

        public bool LoadService(string assemblyName)
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
                _service = assembly.CreateInstance(entryPoint.FullName, true);
            } catch (Exception ex) {
                throw new TypeInitializationException(entryPoint.FullName, ex);
            }

            return true;
        }

        private bool ContainsStartAndStopMethods(Type t)
        {
            return GetStartMethod(t) != null && GetStopMethod(t) != null;
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
