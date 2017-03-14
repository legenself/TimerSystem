
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicUtil
{
    public class MainUtil {
        private class DllManageObj
        {
            public AppDomain Domain
            {
                get;
                set;
            }
            public RemoteUtil Remote
            {
                get;
                set;
            }
        }
        private static ConcurrentDictionary<string, MainUtil.DllManageObj> dllManagers;
        static MainUtil()
        {
            MainUtil.dllManagers = new ConcurrentDictionary<string, MainUtil.DllManageObj>();
            if (!AppDomain.CurrentDomain.FriendlyName.StartsWith("__prefix__flag__") && !AppDomain.CurrentDomain.ShadowCopyFiles)
            {
                Task.Factory.StartNew(delegate
                {
                    while (true)
                    {
                        try
                        {
                            Task.Factory.StartNew(delegate
                            {
                                Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "__dynamicutil_tmp_cache", true);
                            });
                        }
                        catch
                        {
                        }
                        Thread.Sleep(1800000);
                    }
                });
            }
        }
        private static AppDomain CreateShadowAppDomain(string appDomainName, string shadowSearchPath)
        {
            string text = "";
            string[] array = (shadowSearchPath ?? "").Split(new char[]
            {
                ';'
            }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++)
            {
                if (!array[i].Contains(":"))
                {
                    array[i] = AppDomain.CurrentDomain.BaseDirectory + array[i].Trim(new char[]
                    {
                        '/'
                    }).Trim(new char[]
                    {
                        '\\'
                    });
                }
                text = text + array[i] + ";";
            }
            if (!text.ToUpper().Replace("\\", "/").Contains(AppDomain.CurrentDomain.BaseDirectory.Trim(new char[]
            {
                '\\'
            }).Trim(new char[]
            {
                '/'
            }).ToUpper()))
            {
                text = text + AppDomain.CurrentDomain.BaseDirectory + ";";
            }
            text = text.Trim(new char[]
            {
                ';'
            }) + ";" + AppDomain.CurrentDomain.BaseDirectory + "bin;";
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            appDomainName = "__prefix__flag__" + appDomainName;
            appDomainSetup.ApplicationName = appDomainName;
            appDomainSetup.PrivateBinPath = text;
            Evidence evidence = AppDomain.CurrentDomain.Evidence;
            appDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            appDomainSetup.PrivateBinPathProbe = AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe;
            appDomainSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            AppDomain appDomain = AppDomain.CreateDomain(appDomainName, evidence, appDomainSetup);
          
            AppDomain.CurrentDomain.AppendPrivatePath(text);
            if (AppDomain.CurrentDomain.ShadowCopyFiles)
            {
             
                appDomain.SetCachePath(AppDomain.CurrentDomain.SetupInformation.CachePath);
            }
            else
            {
                appDomain.SetCachePath(AppDomain.CurrentDomain.BaseDirectory + "__dynamicutil_tmp_cache");
            }
            appDomain.SetShadowCopyPath(text);
            appDomain.SetShadowCopyFiles();
            appDomain.AssemblyLoad += new AssemblyLoadEventHandler(MainUtil.AddWatch);
            return appDomain;
        }
        private static RemoteUtil CreateRemoteUtil(AppDomain domain)
        {
            string codeBase = typeof(MainUtil).Assembly.CodeBase;
            return (RemoteUtil)domain.CreateInstanceFromAndUnwrap(codeBase, "DynamicUtil.RemoteUtil");
        }
        private static void AddWatch(object sender, AssemblyLoadEventArgs args)
        {
            Console.WriteLine("监视到了加载程序集:" + args.LoadedAssembly.CodeBase);
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
            string text = args.LoadedAssembly.CodeBase.Replace("file:///", "");
            string text2 = text.Substring(text.LastIndexOf('/') + 1);
            string path = text.Substring(0, text.Length - text2.Length);
            fileSystemWatcher.BeginInit();
            fileSystemWatcher.Path = path;
            fileSystemWatcher.NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.LastWrite);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Filter = text2;
            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.Changed += new FileSystemEventHandler(MainUtil.watch_Changed);
            fileSystemWatcher.Deleted += new FileSystemEventHandler(MainUtil.watch_Deleted);
            fileSystemWatcher.EndInit();
        }
        private static void watch_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                AppDomain.Unload(AppDomain.CurrentDomain);
            }
            catch
            {
            }
        }
        private static void watch_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                AppDomain.Unload(AppDomain.CurrentDomain);
            }
            catch
            {
            }
        }
        //public static Hashtable InvokePython(string searchPath,string srcCodePath, string methodName,string[] paras,int counter=0) {
        //    Hashtable hashtable = null;
        //    Hashtable result;
        //    try
        //    {
        //        var engine = IronPython.Hosting.Python.CreateEngine();
        //         engine.SetSearchPaths(searchPath.Split(';'));
           
        //        var paths = engine.GetSearchPaths();
        //        //执行某个文件
        //        ScriptScope _scope = engine.ExecuteFile(srcCodePath);
        //        //获取run函数
        //        var run = _scope.GetVariable(methodName);
        //        //传入参数
        //         run(paras);
        //        hashtable = new Hashtable();

        //        hashtable["Success"] = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex is AppDomainUnloadedException)
        //        {
        //            if (counter > 5)
        //            {
        //                throw ex;
        //            }
        //            counter++;
        //            result = MainUtil.InvokePython(searchPath, srcCodePath,methodName, paras, counter);
        //            return result;
        //        }
        //        else
        //        {
        //            hashtable = new Hashtable();
        //            hashtable["Success"] = false;
        //            hashtable["Data"] = ex.ToString();
        //        }
        //    }
        //    result = hashtable;
        //    return result;
        //}

        public static Hashtable InvokeExe(string searchPath, string exeFullName, string[] paras, int counter = 0)
        {
            Hashtable hashtable = null;
            Hashtable result;
            try
            {
                MainUtil.DllManageObj dllManageObj;
                if (!MainUtil.dllManagers.TryGetValue(exeFullName + "[" + searchPath + "]", out dllManageObj))
                {
                    lock (typeof(MainUtil))
                    {
                        if (!MainUtil.dllManagers.TryGetValue(exeFullName + "[" + searchPath + "]", out dllManageObj))
                        {
                            MainUtil.DllManageObj dllManageObj2 = new MainUtil.DllManageObj();
                            dllManageObj2.Domain = MainUtil.CreateShadowAppDomain(DateTime.Now.ToString("yyyyMMddHHmmssfff") + exeFullName, searchPath);
                            dllManageObj2.Remote = MainUtil.CreateRemoteUtil(dllManageObj2.Domain);
                            MainUtil.dllManagers[exeFullName + "[" + searchPath + "]"] = dllManageObj2;
                            dllManageObj = dllManageObj2;
                        }
                    }
                }
                hashtable = dllManageObj.Remote.InvokeExe(exeFullName, paras);
            }
            catch (Exception ex)
            {
                if (ex is AppDomainUnloadedException)
                {
                    if (counter > 5)
                    {
                        throw ex;
                    }
                    counter++;
                    MainUtil.DllManageObj dllManageObj3;
                    MainUtil.dllManagers.TryRemove(exeFullName + "[" + searchPath + "]", out dllManageObj3);
                    result = MainUtil.InvokeExe(searchPath, exeFullName, paras, counter);
                    return result;
                }
                else
                {
                    hashtable = new Hashtable();
                    hashtable["Success"] = false;
                    hashtable["Data"] = ex.ToString();
                }
            }
            result = hashtable;
            return result;
        }
        public static Hashtable InvokeExe_Once(string searchPath, string exeFullName, string[] paras)
        {
            AppDomain domain = null;
            string text = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString().Replace("-", "");
            Hashtable hashtable = null;
            try
            {
                domain = MainUtil.CreateShadowAppDomain(text, searchPath);
                RemoteUtil remoteUtil = MainUtil.CreateRemoteUtil(domain);
                hashtable = remoteUtil.InvokeExe(exeFullName, paras);
            }
            catch (Exception ex)
            {
                hashtable = new Hashtable();
                hashtable["Success"] = false;
                hashtable["Data"] = ex.ToString();
            }
            finally
            {
                AppDomain.Unload(domain);
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "DynamicCache/" + text, true);
                }
                catch (Exception)
                {
                }
            }
            return hashtable;
        }
        public static Hashtable InvokeDll(string searchPath, string typename, string methodName, Type[] paraTypes, object[] paras, int counter = 0)
        {
            Hashtable hashtable = null;
            Hashtable result;
            try
            {
                MainUtil.DllManageObj dllManageObj;
                if (!MainUtil.dllManagers.TryGetValue(typename + "[" + searchPath + "]", out dllManageObj))
                {
                    lock (typeof(MainUtil))
                    {
                        if (!MainUtil.dllManagers.TryGetValue(typename + "[" + searchPath + "]", out dllManageObj))
                        {
                            MainUtil.DllManageObj dllManageObj2 = new MainUtil.DllManageObj();
                            dllManageObj2.Domain = MainUtil.CreateShadowAppDomain(DateTime.Now.ToString("yyyyMMddHHmmssfff") + typename, searchPath);
                            dllManageObj2.Remote = MainUtil.CreateRemoteUtil(dllManageObj2.Domain);
                            MainUtil.dllManagers[typename + "[" + searchPath + "]"] = dllManageObj2;
                            dllManageObj = dllManageObj2;
                        }
                    }
                }
                hashtable = dllManageObj.Remote.InvokeDll(typename, methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                if (ex is AppDomainUnloadedException)
                {
                    if (counter > 5)
                    {
                        throw ex;
                    }
                    counter++;
                    MainUtil.DllManageObj dllManageObj3;
                    MainUtil.dllManagers.TryRemove(typename + "[" + searchPath + "]", out dllManageObj3);
                    result = MainUtil.InvokeDll(searchPath, typename, methodName, paraTypes, paras, counter);
                    return result;
                }
                else
                {
                    if (ex is FileNotFoundException)
                    {
                        MainUtil.DllManageObj dllManageObj3;
                        MainUtil.dllManagers.TryRemove(typename + "[" + searchPath + "]", out dllManageObj3);
                    }
                    hashtable = new Hashtable();
                    hashtable["Success"] = false;
                    hashtable["Data"] = ex.ToString();
                }
            }
            result = hashtable;
            return result;
        }
        public static Hashtable InvokeDll_Once(string searchPath, string dllFullName, string classFullName, string methodName, Type[] paraTypes, object[] paras)
        {
            AppDomain domain = null;
            Hashtable hashtable = null;
            string text = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString().Replace("-", "");
            try
            {
                domain = MainUtil.CreateShadowAppDomain(text, searchPath);
                RemoteUtil remoteUtil = MainUtil.CreateRemoteUtil(domain);
                hashtable = remoteUtil.InvokeDll(dllFullName, classFullName, methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                hashtable = new Hashtable();
                hashtable["Success"] = false;
                hashtable["Data"] = ex.ToString();
            }
            finally
            {
                AppDomain.Unload(domain);
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "DynamicCache/" + text, true);
                }
                catch (Exception)
                {
                }
            }
            return hashtable;
        }
        public static Hashtable InvokeSrc(string searchPath, string srcCodePath, string classFullName, string methodName, Type[] paraTypes, object[] paras, int counter = 0)
        {
            Hashtable hashtable = null;
            MainUtil.DllManageObj dllManageObj;
            if (!MainUtil.dllManagers.TryGetValue(srcCodePath + "[" + searchPath + "]", out dllManageObj))
            {
                lock (typeof(MainUtil))
                {
                    if (!MainUtil.dllManagers.TryGetValue(srcCodePath + "[" + searchPath + "]", out dllManageObj))
                    {
                        MainUtil.DllManageObj dllManageObj2 = new MainUtil.DllManageObj();
                        dllManageObj2.Domain = MainUtil.CreateShadowAppDomain(DateTime.Now.ToString("yyyyMMddHHmmssfff") + srcCodePath, searchPath);
                        dllManageObj2.Remote = MainUtil.CreateRemoteUtil(dllManageObj2.Domain);
                        MainUtil.dllManagers[srcCodePath + "[" + searchPath + "]"] = dllManageObj2;
                        dllManageObj = dllManageObj2;
                    }
                }
            }
            Hashtable result;
            try
            {
                hashtable = dllManageObj.Remote.InvokeSrc(srcCodePath, classFullName, methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                if (ex is RemotingException)
                {
                    if (counter > 5)
                    {
                        throw ex;
                    }
                    counter++;
                    MainUtil.DllManageObj dllManageObj3;
                    MainUtil.dllManagers.TryRemove(srcCodePath + "[" + searchPath + "]", out dllManageObj3);
                    result = MainUtil.InvokeSrc(searchPath, srcCodePath, classFullName, methodName, paraTypes, paras, counter);
                    return result;
                }
                else
                {
                    hashtable = new Hashtable();
                    hashtable["Success"] = false;
                    hashtable["Data"] = ex.ToString();
                }
            }
            result = hashtable;
            return result;
        }
        public static Hashtable InvokeSrc_Once(string searchPath, string srcCodePath, string classFullName, string methodName, Type[] paraTypes, object[] paras)
        {
            Hashtable hashtable = null;
            AppDomain domain = null;
            string text = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString().Replace("-", "");
            try
            {
                domain = MainUtil.CreateShadowAppDomain(text, searchPath);
                RemoteUtil remoteUtil = MainUtil.CreateRemoteUtil(domain);
                hashtable = remoteUtil.InvokeSrc(srcCodePath, classFullName, methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                hashtable = new Hashtable();
                hashtable["Success"] = false;
                hashtable["Data"] = ex.ToString();
            }
            finally
            {
                AppDomain.Unload(domain);
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "DynamicCache/" + text, true);
                }
                catch (Exception)
                {
                }
            }
            return hashtable;
        }
        public static Hashtable InvokeDll_Direct(string typeName, string methodName, Type[] paraTypes, object[] paras)
        {
            Hashtable hashtable = null;
            try
            {
                RemoteUtil remoteUtil = new RemoteUtil();
                hashtable = remoteUtil.InvokeDll(Type.GetType(typeName), methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                hashtable = new Hashtable();
                hashtable["Success"] = false;
                hashtable["Data"] = ex.ToString();
            }
            return hashtable;
        }
        public static Hashtable InvokeSrc_Direct(string srcCodePath, string typename, string methodName, Type[] paraTypes, object[] paras)
        {
            Hashtable hashtable = null;
            try
            {
                RemoteUtil remoteUtil = new RemoteUtil();
                hashtable = remoteUtil.InvokeSrc(srcCodePath, typename, methodName, paraTypes, paras);
            }
            catch (Exception ex)
            {
                hashtable = new Hashtable();
                hashtable["Success"] = false;
                hashtable["Data"] = ex.ToString();
            }
            return hashtable;
        }
    }
}
