using DynamicUtil;
using Quartz;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimerSystem
{
    public class FullTask : IJob
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="context"></param>
        public void Execute(IJobExecutionContext context)
        {

            try
            {
                var job = context.JobDetail.JobDataMap.Get("task") as Task_t;
                ExeJob(job);
            }
            catch (Exception ex)
            {
                WriteLog("【日志信息: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "】[任务执行失败]:" + ex.ToString() + "\r\n");
            }
        }
        /// <summary>
        /// 执行具体任务
        /// </summary>
        /// <param name="job"></param>
        public void ExeJob(Task_t job)
        {
            string Info = "";
            string jobtype = "未知类型";
            switch (job.Type)
            {
                case "DLL":
                    {
                        jobtype = "dll调用";
                        break;
                    }
                case "CS":
                    {
                        jobtype = "编译CS文件";
                        break;
                    }
                case "EXE":
                    {
                        jobtype = "exe调用";
                        break;
                    }
                case "PY":
                    {
                        jobtype = "python调用";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            Info += "[任务名:" + job.Name + "][任务类型:" + jobtype + "]";

            //处理调用时的参数,默认没有参数
            Type[] types = new Type[] { };
            object[] paras = new object[] { };
            string[] parastring = new string[] { };
            if (job.Paras != null && job.Paras.Split(',').Length > 0)
            {
                parastring = job.Paras.Split(',');
                //有参数情况,参数默认是一个字符串数组
                types = new Type[] { typeof(string[]) };
                paras = new object[1];
                paras[0] = new string[parastring.Length];
                for (int i = 0; i < parastring.Length; i++)
                {
                    (paras[0] as string[])[i] = parastring[i];
                }
            }
            switch (job.Type)
            {
                case "DLL":
                    RunDll(ref Info, job, types, paras);
                    break;
                case "CS":
                    RunCSharp(ref Info, job, types, paras);
                    break;
                case "EXE":
                    RunExe(ref Info, job, types, paras);
                    break;
                //case "PY":
                //    RunPython(ref Info, job, types, paras);
                //    break;
                default:
                    break;
            }





        }




        private void log(Task_t job, int[] logwhen, Hashtable ht, ref string Info)
        {
            Info += "[出错]\r\n" + (ht["Data"] ?? "").ToString();
            if (logwhen.Contains(job.LogWhen))
            {
                //失败或一定记录时
                WriteLog(Info);
            }
        }
        public void WriteLog(string msg)
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory.Trim('\\').Trim('/') + "\\log";

            lock (typeof(FullTask))
            {
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                path += "\\TaskLog" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                System.IO.File.AppendAllText(path, "【日志信息: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "】" + msg + "\r\n");
            }
        }
        private void RunDll(ref string info, Task_t job, Type[] types, object[] paras)
        {
            info += "[程序集搜索路径:" + job.SearchPath + "][执行类:" + job.TypeName + "][执行方法:" + job.Method + "]";
            try
            {
                Hashtable ht = MainUtil.InvokeDll(job.SearchPath, job.TypeName, job.Method, types, paras);
                if (!(bool)ht["Success"])
                {
                    info += "[出错]\r\n" + (ht["Data"] ?? "").ToString();
                    if (job.LogWhen == 0 || job.LogWhen == 2)
                    {
                        //失败或一定记录时
                        WriteLog(info);
                    }
                    return;
                }
                else
                {
                    info += "[成功]\r\n";
                    if (job.LogWhen == 1 || job.LogWhen == 0)
                    {
                        //成功时记录
                        WriteLog(info);
                    }

                }
            }
            catch (Exception ex)
            {
                if (job.LogWhen == 0 || job.LogWhen == 2)
                {
                    //失败或一定记录时
                    WriteLog(info + "\r\n" + ex.ToString());
                }
                return;
            }
        }
        private void RunCSharp(ref string Info, Task_t job, Type[] types, object[] paras)
        {
            Info += "[程序集搜索路径:" + job.SearchPath + "][编译文件:" + job.TypeName + "][执行类:" + job.TypeName + "][执行方法:" + job.Method + "]";
            try
            {
                Hashtable ht = MainUtil.InvokeSrc(job.SearchPath, job.SrcCodeFilePath, job.TypeName, job.Method, types, paras);
                if (!(bool)ht["Success"])
                {
                    Info += "[出错]\r\n" + (ht["Data"] ?? "").ToString();
                    if (job.LogWhen == 0 || job.LogWhen == 2)
                    {
                        //失败或一定记录时
                        WriteLog(Info);
                    }
                    return;
                }
                else
                {
                    Info += "[成功]\r\n";
                    if (job.LogWhen == 1 || job.LogWhen == 0)
                    {
                        //成功时记录
                        WriteLog(Info);
                    }

                }
            }
            catch (Exception ex)
            {
                if (job.LogWhen == 0 || job.LogWhen == 2)
                {
                    //失败或一定记录时
                    WriteLog(Info + "\r\n" + ex.ToString());
                }
                return;
            }
        }
        private void RunExe(ref string Info, Task_t job, Type[] types, object[] paras)
        {

            Info += "[程序集搜索路径:" + job.SearchPath + "][加载exe:" + job.TypeName + "]";
            try
            {
                Hashtable ht = MainUtil.InvokeExe(job.SearchPath, job.TypeName, paras.Select(p => (string)p).ToArray<string>());
                if (!(bool)ht["Success"])
                {
                    Info += "[出错]\r\n" + (ht["Data"] ?? "").ToString();
                    if (job.LogWhen == 0 || job.LogWhen == 2)
                    {
                        //失败或一定记录时
                        WriteLog(Info);
                    }
                    return;
                }
                else
                {
                    Info += "[成功]\r\n";
                    if (job.LogWhen == 1 || job.LogWhen == 0)
                    {
                        //成功时记录
                        WriteLog(Info);
                    }

                }
            }
            catch (Exception ex)
            {
                if (job.LogWhen == 0 || job.LogWhen == 2)
                {
                    //失败或一定记录时
                    WriteLog(Info + "\r\n" + ex.ToString());
                }
                return;
            }
        }
        //private void RunPython(ref string Info, Task_t job, object[] paras)
        //{
        //    Info += "[程序集搜索路径:" + job.SearchPath + "][加载源码:" + job.SrcCodeFilePath + "]";
        //    try
        //    {
        //        Hashtable ht = MainUtil.InvokePython(job.SearchPath, job.SrcCodeFilePath, job.Method, paras.Select(p => (string)p).ToArray<string>());
        //        if (!(bool)ht["Success"])
        //        {
        //            Info += "[出错]\r\n" + (ht["Data"] ?? "").ToString();
        //            if (job.LogWhen == 0 || job.LogWhen == 2)
        //            {
        //                //失败或一定记录时
        //                WriteLog(Info);
        //            }
        //            return;
        //        }
        //        else
        //        {
        //            Info += "[成功]\r\n";
        //            if (job.LogWhen == 1 || job.LogWhen == 0)
        //            {
        //                //成功时记录
        //                WriteLog(Info);
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (job.LogWhen == 0 || job.LogWhen == 2)
        //        {
        //            //失败或一定记录时
        //            WriteLog(Info + "\r\n" + ex.ToString());
        //        }
        //        return;
        //    }
        //}
    }
}
