using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sitecore.SharedSource.Sniper
{
    public class Common
    {
        public static void Log(string message, string level = "")
        {
            if (String.IsNullOrEmpty(level)) level = Configuration.Settings.GetAppSetting("SniperLoggingLevel", "Debug");
            switch (level.ToLower())
            {
                case "info":
                case "verbose":
                    Diagnostics.Log.Info(message, typeof(RichTextSniperProcessor));
                    break;
                default:
                    Diagnostics.Log.Debug(message, typeof(RichTextSniperProcessor));
                    break;
            }
        }

        /// <summary>
        /// Eval - Executes code and returns results
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="throwException">default: false, if true throws error, if false and error occurs results StartsWith "Error:"</param>
        /// <param name="providerType">CSharp (default), VisualBasic</param>
        /// <param name="referencedAssemblies">default null, if null and nameSpaces provided add's .dll to nameSpace</param>
        /// <param name="nameSpaces">default null, string array: system, system.windows.forms</param>
        /// <returns></returns>
        public static string Eval(string sourceCode, bool throwException = false, string providerType = "CSharp", string[] referencedAssemblies = null, string[] nameSpaces = null)
        {
            var results = new StringBuilder();
            Log(String.Format("Sniper found - Evaler: {0}", sourceCode), "verbose");
            try
            {
                var provider = CodeDomProvider.CreateProvider(providerType);

                var compilerParameters = new CompilerParameters { GenerateInMemory = true, TreatWarningsAsErrors = false };

                if (referencedAssemblies == null && nameSpaces == null)
                {
                    var references = new List<string>();
                    var names = new List<string>();

                    if (sourceCode.Contains("System.Web") || sourceCode.Contains("Sitecore.") || sourceCode.Contains("Tracker.CurrentVisit"))
                    {
                        AddReference(references, "System");
                        AddReference(references, "System.Web", names);
                        if (sourceCode.Contains("Sitecore."))
                        {
                            AddReference(references, "Sitecore.Kernel", names, "Sitecore");
                        }
                        if (sourceCode.Contains("Sitecore.Analytics") || sourceCode.Contains("Tracker.CurrentVisit"))
                        {
                            AddReference(references, "Sitecore.Analytics", names, "Sitecore.Analytics");
                            AddReference(references, "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                            AddReference(references, "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                        }
                    }
                    
                    //if (sourceCode.Contains("foreach"))
                    //{
                    //    AddReference(references, "System.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    //}

                    if (sourceCode.Contains("System.Web.Security"))
                    {
                        AddReference(references, "System.Web.ApplicationServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    }
                    referencedAssemblies = references.ToArray();
                    nameSpaces = names.ToArray();
                }

                if (referencedAssemblies == null)
                {
                    referencedAssemblies = nameSpaces.Where(item => item != null).Select(item => item + ".dll").ToArray();  
                }

                compilerParameters.ReferencedAssemblies.AddRange(referencedAssemblies);

                var source = new StringBuilder();

                if (nameSpaces != null)
                {
                    source.Append(string.Concat(nameSpaces.Select(n => string.Format("using {0};\n", n))));
                }

                source.Append("namespace SniperCodeEvaler{ \npublic class SniperCodeEvaler{ \npublic object EvalCode(){\nreturn " + sourceCode + "; \n} \n} \n}\n");

                var compilerResults = provider.CompileAssemblyFromSource(compilerParameters, source.ToString());

                if (compilerResults.Errors.Count > 0)
                {
                    results.Append("Error: During compilation\n\r");
                    foreach (var compilerError in compilerResults.Errors)
                        results.Append(String.Format("{0}\n\r", compilerError));
                    if (throwException) throw new Exception(results.ToString());
                    return results.ToString();
                }

                var a = compilerResults.CompiledAssembly;
                var o = a.CreateInstance("SniperCodeEvaler.SniperCodeEvaler");

                if (o == null)
                {
                    if (throwException) throw new Exception(String.Format("Error: CreateInstance returned null - {0}", results));
                    return String.Format("Error: CreateInstance returned null - {0}", results);
                }

                var t = o.GetType();
                var mi = t.GetMethod("EvalCode");

                var s = mi.Invoke(o, null);
                return s.ToString();
            }
            catch (Exception ex)
            {
                if (throwException) throw new Exception(String.Format("Error: CreateInstance returned null - {0} - {1}", results, ex));
                return String.Format("Error: CreateInstance returned null - {0} - {1}", results, ex);
            }
        }

        private static void AddReference(ICollection<string> references, string name, ICollection<string> names = null, string nameSpace = null)
        {
            var assemblyLocation= GetAssemblyLocation(name);
            if (assemblyLocation == null) return;
            references.Add(assemblyLocation);
            if(names!= null) names.Add(nameSpace ?? name);
        }

        private static string GetAssemblyLocation(string name)
        {
            var assembly = GetAssembly(name);
            return assembly != null ? Assembly.ReflectionOnlyLoad(assembly.FullName).Location : null;
        }
        
        private static Assembly GetAssembly(string name)
        {
            var assemblyName =  typeof(RichTextSniperProcessor).Assembly.GetReferencedAssemblies().FirstOrDefault(a => a.Name == name);
            return Assembly.ReflectionOnlyLoad(assemblyName != null ? assemblyName.FullName : name);
        }
    }
}
