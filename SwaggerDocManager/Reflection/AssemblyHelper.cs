using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

namespace Reflection
{
    internal class AssemblyHelper
    {
        private readonly List<Type> allTypes;
        private Assembly assembly;
        private readonly string assemblyName;

        public AssemblyHelper(Assembly asm)
        {
            assembly = asm;
            ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
            ArgumentNullException.ThrowIfNull(assembly.FullName, nameof(assembly.FullName));
            assemblyName = assembly.FullName.Split(',')[0];
            allTypes = assembly.GetTypes().ToList();
        }

        public Dictionary<string, List<string>> GetModels(string version)
        {
            Dictionary<string, List<string>> modelTypesByController = new();
            var controllerTypes = GetControllerTypes(version);
            foreach (var controllerType in controllerTypes)
            {
                List<string> modelNames = new();
                modelNames.Add(GetFullNameExcludingAssemblyName(controllerType, false));
                var modelTypes = GetControllerModelTypes(controllerType).Distinct()
                                                                        .Select(t => GetFullNameExcludingAssemblyName(t, false))
                                                                        .ToList();
                modelNames.AddRange(modelTypes);
                modelTypesByController.Add(controllerType.Name, modelNames);
            }

            return modelTypesByController;
        }

        public MethodInfo[] GetControllerMethods(Type controllerType)
        {
            return controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public List<Type> GetControllerModelTypes(Type controllerType)
        {
            List<Type> modelTypes = new();

            var methods = GetControllerMethods(controllerType);

            var actionMethodReturnTypeModels = GetActionMethodResponseModels(methods);
            modelTypes.AddRange(actionMethodReturnTypeModels);

            var actionMethodParameterModels = GetActionMethodParameterModels(methods);
            modelTypes.AddRange(actionMethodParameterModels);

            modelTypes = modelTypes.Distinct().ToList();

            return modelTypes;
        }

        public List<Type> GetControllerActionResponseModelTypes(Type controllerType)
        {
            List<Type> modelTypes = new();

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var actionMethodReturnTypeModels = GetActionMethodResponseModels(methods);
            modelTypes.AddRange(actionMethodReturnTypeModels);

            modelTypes = modelTypes.Distinct().ToList();

            return modelTypes;
        }

        public List<Type> GetControllerActionRequestModelTypes(Type controllerType)
        {
            List<Type> modelTypes = new();

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var actionMethodReturnTypeModels = GetActionMethodResponseModels(methods);
            modelTypes.AddRange(actionMethodReturnTypeModels);

            modelTypes = modelTypes.Distinct().ToList();

            return modelTypes;
        }

        public Type? GetTypeByName(string fullyQualifiedName) => assembly.GetType(fullyQualifiedName);

        public List<string> GetPropertyNamesQualifiedByClassname(Type baseType)
        {
            var props = baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Select(p => $"{baseType.Name}{p.Name}")
                           .ToList();
            return props;
        }

        public Type? GetControllerTypeByName(string controllerName, string versionPrefix)
        {
            return allTypes.FirstOrDefault(t => t.BaseType != null
                                                   && t.FullName != null
                                                   && t.BaseType.Name == typeof(ControllerBase).Name
                                                   && t.FullName.Contains(versionPrefix) && t.FullName.Contains(controllerName));
        }

        public string GetFullNameExcludingAssemblyName(Type t, bool prefixAssemblyName)
        {
            if (string.IsNullOrEmpty(t.FullName))
            {
                return t.Name;
            }

            return prefixAssemblyName ? t.FullName : t.FullName.Replace(assemblyName, string.Empty).Trim('.');
        }

        public List<Type> GetActionMethodResponseModels(IEnumerable<MethodInfo> methods)
        {
            List<Type> modelTypes = new();
            var attributes = methods.SelectMany(m => m.GetCustomAttributesData())
                                    .Where(a => a.AttributeType.Name == typeof(SwaggerResponseAttribute).Name);

            foreach (var attribute in attributes)
            {
                if (attribute.ConstructorArguments.Any(a => a.ArgumentType.Name == typeof(int).Name && a.Value != null && a.Value is int i && i >= 200 && i < 300))
                {
                    var constructorArgType = attribute.ConstructorArguments.FirstOrDefault(t => t.ArgumentType.Name == typeof(Type).Name);
                    if (constructorArgType == default || constructorArgType.Value == null)
                    {
                        continue;
                    }
                    Type successResponseType = (Type)constructorArgType.Value;
                    if (IsAvocadoNexusApiType(successResponseType))
                    {
                        modelTypes.AddRange(GetLeafNodes(successResponseType));
                    }
                }
            }
            return modelTypes;
        }

        private List<Type> GetActionMethodParameterModels(IEnumerable<MethodInfo> methods)
        {
            List<Type> modelTypes = new();
            var parameterTypes = methods.SelectMany(m => m.GetParameters().Select(p => p.ParameterType));

            foreach (var parameterType in parameterTypes)
            {
                if (IsAvocadoNexusApiType(parameterType))
                {
                    modelTypes.AddRange(GetLeafNodes(parameterType));
                }
            }
            return modelTypes;
        }

        public List<Type> GetLeafNodes(Type rootNode)
        {
            List<Type> leafNodes = new();
            if (!IsAvocadoNexusApiType(rootNode))
            {
                return leafNodes;
            }
            if (rootNode.IsGenericType)
            {
                foreach (var item in rootNode.GetGenericArguments())
                {
                    if (IsAvocadoNexusApiType(item))
                    {
                        leafNodes.AddRange(GetLeafNodes(item));
                    }
                }
            }
            else if (rootNode.IsArray)
            {
                var elementType = rootNode.GetElementType();
                if (elementType != null && IsAvocadoNexusApiType(elementType))
                {
                    leafNodes.Add(elementType);
                }
            }
            else
            {
                if (rootNode.IsClass)
                {
                    leafNodes.Add(rootNode);
                    if (rootNode.BaseType != null && IsAvocadoNexusApiType(rootNode.BaseType))
                    {
                        leafNodes.AddRange(GetLeafNodes(rootNode.BaseType));
                    }
                    var propertyTypes = rootNode.GetProperties().Select(p => p.PropertyType).Where(IsAvocadoNexusApiType).ToList();
                    foreach (var propType in propertyTypes)
                    {
                        leafNodes.AddRange(GetLeafNodes(propType));
                    }
                }
            }
            return leafNodes;
        }

        private List<Type> GetControllerTypes(string versionPrefix)
        {
            var controllerTypes = allTypes.Where(t => t.BaseType != null
                                                   && t.FullName != null
                                                   && t.BaseType.Name == typeof(ControllerBase).Name
                                                   && t.FullName.Contains(versionPrefix))
                                          .ToList();
            return controllerTypes;
        }

        public bool IsAvocadoNexusApiType(Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericArguments().Any(t => IsAvocadoNexusApiType(t));
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                {
                    return false;
                }
                return IsAvocadoNexusApiType(elementType);
            }
            else
            {
                //return !string.IsNullOrWhiteSpace(type.Namespace) && type.Namespace.Contains(assemblyName);
                return ((type.Assembly.FullName == assembly.FullName) || type.Assembly.FullName.StartsWith("Emoney"));
            }
        }

        #region Not In Use

        private List<Type> GetDependencyModels(Type controllerType)
        {
            List<Type> modelTypes = new();
            var constructorParams = controllerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                                             .SelectMany(c => c.GetParameters());
            foreach (var constructorParam in constructorParams)
            {
                var paramType = constructorParam.ParameterType;
                if (paramType.IsInterface)
                {
                    if (paramType.IsGenericType)
                    {
                        Console.WriteLine("Here we are.. generic interface type");
                    }
                    else
                    {
                        modelTypes.AddRange(GetMethodModels(constructorParam.ParameterType));
                    }
                }
            }
            return modelTypes;
        }

        private List<Type> GetMethodModels(Type parentType)
        {
            List<Type> modelTypes = new();
            var parameterTypes = parentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                           .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType));

            foreach (var parameterType in parameterTypes)
            {
                if (IsAvocadoNexusApiType(parameterType))
                {
                    modelTypes.AddRange(GetLeafNodes(parameterType));
                }
            }
            return modelTypes;
        }

        #endregion Not In Use
    }
}