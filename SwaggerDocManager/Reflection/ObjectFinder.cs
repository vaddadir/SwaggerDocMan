using System.Reflection;
using System.Runtime.InteropServices;

namespace Reflection
{
    public interface IObjectFinder
    {
        Dictionary<string, List<string>> GetModelNames(string versionPrefix);

        List<string> GetClassNameQualifiedPropertyNames(string typeName);

        Dictionary<string, List<string>> GetClassNameQualifiedPropertyNames(IEnumerable<string> typeNames);

        List<string> GetControllerModelNames(string controllerName, bool includeControllerName = false, string versionPrefix = "v05", bool prefixAssemblyName = false);

        List<string> GetControllerActionResponseModelNames(string controllerName, string versionPrefix = "v05", bool prefixAssemblyName = false);

        Dictionary<string, List<PropertyInfo>> GetControllerActionResponseModelProperties(string controllerName, string versionPrefix = "v05", bool prefixAssemblyName = false);

        //List<string> GetControllerActionRequestModelNames(string controllerName, string versionPrefix = "v05", bool prefixAssemblyName = false);
        List<PropertyInfo> GetProperties(string typeName);

        List<string> GetChildrenTypeNames(string typeName);
    }

    public class ObjectFinder : IDisposable, IObjectFinder
    {
        private readonly MetadataLoadContext metadataLoadContext;
        private readonly string assemblyPath;
        private bool disposedValue;

        public ObjectFinder(string apiAssemblyPath)
        {
            ArgumentNullException.ThrowIfNull(apiAssemblyPath);
            assemblyPath = apiAssemblyPath;
            var resolver = GetAssemblyResolver(apiAssemblyPath);
            metadataLoadContext = new MetadataLoadContext(resolver, null);
        }

        public Dictionary<string, List<string>> GetModelNames(string versionPrefix = "v05")
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);

            string versionString = string.Empty;
            if (!string.IsNullOrWhiteSpace(versionPrefix))
            {
                versionString = versionPrefix.Replace(".", string.Empty);
            }

            var models = assemblyHelper.GetModels(versionString);
            return models;
        }

        public List<string> GetClassNameQualifiedPropertyNames(string typeName)
        {
            List<string> propertyNames = new();
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);

            var correspondingType = assemblyHelper.GetTypeByName(typeName);
            if (correspondingType != null)
            {
                return assemblyHelper.GetPropertyNamesQualifiedByClassname(correspondingType);
            }

            return propertyNames;
        }

        public Dictionary<string, List<string>> GetClassNameQualifiedPropertyNames(IEnumerable<string> typeNames)
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);

            Dictionary<string, List<string>> propertyNamesByType = new();
            foreach (var typeName in typeNames)
            {
                var correspondingType = assemblyHelper.GetTypeByName(typeName);
                if (correspondingType != null)
                {
                    propertyNamesByType.Add(typeName, GetClassNameQualifiedPropertyNames(typeName));
                }
            }

            return propertyNamesByType;
        }

        public List<string> GetControllerModelNames(string controllerName, bool includeControllerName = false, string versionPrefix = "v05", bool prefixAssemblyName = false)
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);

            string versionString = string.Empty;
            if (!string.IsNullOrWhiteSpace(versionPrefix))
            {
                versionString = versionPrefix.Replace(".", string.Empty);
            }

            var controllerType = assemblyHelper.GetControllerTypeByName(controllerName, versionString);
            List<string> modelNames = new();
            if (controllerType != null)
            {
                if (includeControllerName)
                {
                    modelNames.Add(assemblyHelper.GetFullNameExcludingAssemblyName(controllerType, prefixAssemblyName));
                }
                modelNames.AddRange(assemblyHelper.GetControllerModelTypes(controllerType)
                                       .Select(t => assemblyHelper.GetFullNameExcludingAssemblyName(t, prefixAssemblyName)));
            }
            return modelNames;
        }

        public List<string> GetControllerActionResponseModelNames(string controllerName, string versionPrefix = "v05", bool prefixAssemblyName = false)
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);

            string versionString = string.Empty;
            if (!string.IsNullOrWhiteSpace(versionPrefix))
            {
                versionString = versionPrefix.Replace(".", string.Empty);
            }

            var controllerType = assemblyHelper.GetControllerTypeByName(controllerName, versionString);
            List<string> modelNames = new();
            if (controllerType != null)
            {
                modelNames.AddRange(assemblyHelper.GetControllerModelTypes(controllerType)
                                       .Select(t => assemblyHelper.GetFullNameExcludingAssemblyName(t, prefixAssemblyName)));
            }
            return modelNames;
        }

        public Dictionary<string, List<PropertyInfo>> GetControllerActionResponseModelProperties(string controllerName, string versionPrefix = "v05", bool prefixAssemblyName = false)
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            AssemblyHelper assemblyHelper = new(assembly);
            Dictionary<string, List<PropertyInfo>> responseModelProperties = new();

            string versionString = string.Empty;
            if (!string.IsNullOrWhiteSpace(versionPrefix))
            {
                versionString = versionPrefix.Replace(".", string.Empty);
            }

            var controllerType = assemblyHelper.GetControllerTypeByName(controllerName, versionString);
            if (controllerType != null)
            {
                var methods = assemblyHelper.GetControllerMethods(controllerType);
                var responseModels = assemblyHelper.GetActionMethodResponseModels(methods).SelectMany(rm => assemblyHelper.GetLeafNodes(rm)).Distinct();
                foreach (Type responseModel in responseModels)
                {

                    var key = assemblyHelper.GetFullNameExcludingAssemblyName(responseModel, prefixAssemblyName);
                    var props = responseModel.GetProperties().ToList();                    
                    responseModelProperties.Add(key, props);
                } 
            }
            return responseModelProperties;
        }

        private PathAssemblyResolver GetAssemblyResolver(string targetAssemblyPath)
        {
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            List<string> paths = new(runtimeAssemblies);

            var currentAppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrWhiteSpace(currentAppPath))
            {
                var otherFiles = Directory.GetFiles(currentAppPath, "*.dll");
                paths.AddRange(otherFiles);
            }

            var assemblyDirectory = Path.GetDirectoryName(targetAssemblyPath);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                string[] targetAppAssemblies = Directory.GetFiles(assemblyDirectory, "*.dll");
                paths.AddRange(targetAppAssemblies);
            }

            PathAssemblyResolver resolver = new(paths);
            return resolver;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    metadataLoadContext.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public List<PropertyInfo> GetProperties(string typeName)
        {
            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            return assembly.GetType(typeName)?.GetProperties().ToList() ?? new List<PropertyInfo>();
        }

        public List<string> GetChildrenTypeNames(string typeName)
        {
            List<string> names = new();
            names.Add(typeName);

            Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            var matchingType = assembly.GetType(typeName);
            if (matchingType == null)
            {
                var derivedAssemblyPath = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(assemblyPath), typeName.Substring(0, typeName.LastIndexOf("."))), "dll");
                assembly = metadataLoadContext.LoadFromAssemblyPath(derivedAssemblyPath);
            }
            AssemblyHelper assemblyHelper = new(assembly);
            List<PropertyInfo> properties = GetProperties(typeName);
            foreach (PropertyInfo property in properties)
            {
                if (assemblyHelper.IsAvocadoNexusApiType(property.PropertyType))
                {
                    names.AddRange(GetChildrenTypeNames(property.PropertyType.FullName ?? property.PropertyType.Name));
                    return names;
                }
            }
            return names;
        }
    }
}