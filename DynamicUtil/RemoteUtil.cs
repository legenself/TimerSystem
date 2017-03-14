using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicUtil
{
    public class RemoteUtil : MarshalByRefObject
    {
        private static ConcurrentDictionary<string, object> ht_locks = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, object> ht_last_flags = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, object> ht_last_assem = new ConcurrentDictionary<string, object>();
        private static string GeneLastFlag(string filePath)
        {
            return "[" + File.GetLastWriteTime(filePath).ToString("yyyyMMddHHmmssfff") + "]" + filePath;
        }
        private static object GetLock(string filePath)
        {
            object obj;
            object result;
            if (!RemoteUtil.ht_locks.TryGetValue(filePath, out obj))
            {
                lock (typeof(MainUtil))
                {
                    if (!RemoteUtil.ht_locks.TryGetValue(filePath, out obj))
                    {
                        RemoteUtil.ht_locks.TryAdd(filePath, new object());
                    }
                    result = RemoteUtil.ht_locks[filePath];
                    return result;
                }
            }
            result = obj;
            return result;
        }
        public Hashtable InvokeDll(string dllFullName, string classFullName, string methodName, Type[] paraTypes, object[] args)
        {
            Assembly assem = Assembly.Load(dllFullName);
            return this.InvokeDll(assem, classFullName, methodName, paraTypes, args);
        }
        public Hashtable InvokeDll(string typename, string methodName, Type[] paraTypes, object[] args)
        {
            Type type = Type.GetType(typename);
            return this.InvokeDll(type, methodName, paraTypes, args);
        }
        public Hashtable InvokeDll(Assembly assem, string classFullName, string methodName, Type[] paraTypes, object[] args)
        {
            Hashtable hashtable = new Hashtable();
            Type type = assem.GetType(classFullName);
            Hashtable result;
            if (type == null)
            {
                hashtable.Add("Success", false);
                hashtable.Add("Data", "[类:" + classFullName + "]没找到");
                result = hashtable;
            }
            else
            {
                result = this.InvokeDll(type, methodName, paraTypes, args);
            }
            return result;
        }
        public Hashtable InvokeDll(Type t, string methodName, Type[] paraTypes, object[] args)
        {
            Hashtable hashtable = new Hashtable();
            Hashtable result;
            if (t == null)
            {
                hashtable.Add("Success", false);
                hashtable.Add("Data", "[类:null]没找到");
                result = hashtable;
            }
            else
            {
                MethodInfo method = t.GetMethod(methodName, paraTypes);
                if (method == null)
                {
                    hashtable.Add("Success", false);
                    hashtable.Add("Data", string.Concat(new string[]
                    {
                        "[类:",
                        t.FullName,
                        "][方法:",
                        methodName,
                        "]没找到"
                    }));
                    result = hashtable;
                }
                else
                {
                    object obj = Activator.CreateInstance(t);
                    if (obj == null)
                    {
                        hashtable.Add("Success", false);
                        hashtable.Add("Data", "[类:" + t.FullName + "]创建实例失败");
                        result = hashtable;
                    }
                    else
                    {
                        object value = method.Invoke(obj, args);
                        hashtable.Add("Success", true);
                        hashtable.Add("Data", value);
                        result = hashtable;
                    }
                }
            }
            return result;
        }
        public Hashtable InvokeExe(string exePath, string[] paras)
        {
            Assembly assembly = Assembly.Load(exePath);
            MethodInfo entryPoint = assembly.EntryPoint;
            if (entryPoint.GetParameters().Length == 1)
            {
                if (paras == null)
                {
                    entryPoint.Invoke(null, new object[]
                    {
                        new string[0]
                    });
                }
                else
                {
                    entryPoint.Invoke(null, new object[]
                    {
                        paras
                    });
                }
            }
            else
            {
                entryPoint.Invoke(null, new object[0]);
            }
            return new Hashtable
            {

                {
                    "Success",
                    true
                }
            };
        }
        public Hashtable InvokeSrc(string srcCodePath, string classFullName, string methodName, Type[] paraTypes, object[] args)
        {
            string text = RemoteUtil.GeneLastFlag(srcCodePath);
            object @lock = RemoteUtil.GetLock(srcCodePath);
            lock (@lock)
            {
                object obj2;
                if (RemoteUtil.ht_last_flags.TryGetValue(srcCodePath, out obj2))
                {
                    if (obj2.ToString() != text)
                    {
                        RemoteUtil.ht_last_assem[srcCodePath] = this.Compile(srcCodePath);
                        RemoteUtil.ht_last_flags[srcCodePath] = text;
                    }
                }
                else
                {
                    RemoteUtil.ht_last_assem.TryAdd(srcCodePath, this.Compile(srcCodePath));
                    RemoteUtil.ht_last_flags.TryAdd(srcCodePath, text);
                }
            }
            return this.InvokeDll(RemoteUtil.ht_last_assem[srcCodePath] as Assembly, classFullName, methodName, paraTypes, args);
        }
        private Assembly Compile(string srcCodePath)
        {
            CompilerParameters compilerParameters = new CompilerParameters();
            string text = "";
            this.ParseSrc(srcCodePath, ref text, compilerParameters);
            compilerParameters.GenerateExecutable = false;
            compilerParameters.GenerateInMemory = true;
            Assembly compiledAssembly;
            using (CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider())
            {
                CompilerResults compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, new string[]
                {
                    text
                });
                CompilerErrorCollection errors = compilerResults.Errors;
                string str = "";
                foreach (CompilerError compilerError in errors)
                {
                    if (compilerError.IsWarning)
                    {
                        str = string.Concat(new string[]
                        {
                            "Line: ",
                            compilerError.Line.ToString(),
                            " Warning Number: ",
                            compilerError.ErrorNumber,
                            " Warning Message: ",
                            compilerError.ErrorText,
                            "\r\n"
                        });
                    }
                    else
                    {
                        if (!compilerError.IsWarning)
                        {
                            str = string.Concat(new string[]
                            {
                                "Line: ",
                                compilerError.Line.ToString(),
                                " Error Number: ",
                                compilerError.ErrorNumber,
                                " Error Message: ",
                                compilerError.ErrorText,
                                "\r\n"
                            });
                        }
                    }
                }
                if (errors.Count > 0)
                {
                    throw new Exception("[编译出错]" + str);
                }
                compiledAssembly = compilerResults.CompiledAssembly;
            }
            return compiledAssembly;
        }
        private void ParseSrc(string srcCodePath, ref string srcCode, CompilerParameters parameters)
        {
            string[] array = File.ReadAllLines(srcCodePath);
            srcCode = "";
            bool flag = true;
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string text = array2[i];
                string text2 = text.Trim(new char[]
                {
                    ' '
                });
                if (text2.StartsWith("//#import"))
                {
                    if (flag)
                    {
                        text2 = text2.Substring(9).Trim(new char[]
                        {
                            ' '
                        });
                        text2 = this.ParseFilePath(text2);
                        if (!string.IsNullOrWhiteSpace(text2))
                        {
                            parameters.ReferencedAssemblies.Add(text2);
                        }
                    }
                }
                else
                {
                    flag = false;
                    srcCode = srcCode + "\r\n" + text2;
                }
            }
        }
        public string ParseFilePath(string path)
        {
            string result;
            if (string.IsNullOrWhiteSpace(path))
            {
                result = path;
            }
            else
            {
                if (path.Contains(":"))
                {
                    result = path;
                }
                else
                {
                    path = path.Replace("/", "\\");
                    if (!path.StartsWith("~") && !path.StartsWith("\\"))
                    {
                        result = path;
                    }
                    else
                    {
                        if (path.StartsWith("~"))
                        {
                            path = path.TrimStart(new char[]
                            {
                                '~'
                            });
                        }
                        if (path.StartsWith("\\"))
                        {
                            path = path.TrimStart(new char[]
                            {
                                '\\'
                            });
                        }
                        result = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    }
                }
            }
            return result;
        }
    }
}
