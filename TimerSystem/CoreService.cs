using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace TimerSystem
{
    public class CoreService
    {
        private static Hashtable ht = new Hashtable();
        HttpSelfHostConfiguration config;
        HttpSelfHostServer server;
        string srvName;
        string srvDesc;
        /// <summary>
        /// 构造函数用于启动webapi和 初始化serivce参数
        /// </summary>
        /// <param name="srvName"></param>
        /// <param name="srvDesc"></param>
        public CoreService(string srvName, string srvDesc)
        {
            config = new HttpSelfHostConfiguration("http://localhost:3333");
            config.Routes.MapHttpRoute("default", "api/{controller}/{action}/{id}", new { id = RouteParameter.Optional });
            server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
            this.srvName = srvName;
            this.srvDesc = srvDesc;

        }
        /// <summary>
        /// 启动windows服务时执行的代码
        /// </summary>
        public void Start()
        {
            try
            {
                //服务启动
                WriteLog("服务:【" + srvName + "】将要启动了...");
                LoadJob();
                WriteLog("服务:【" + srvName + "】启动成功!");
            }
            catch (Exception ex)
            {
                WriteLog("服务:【" + srvName + "】启动失败:" + ex.ToString());
                throw ex;
            }
        }
        /// <summary>
        /// 暂停服务时执行
        /// </summary>
        public void Stop()
        {
            //服务停止
            WriteLog("服务:【" + srvName + "】停止了!");
        }
        /// <summary>
        /// 关闭服务时执行
        /// </summary>
        public void Shutdown()
        {
            //服务关闭
            WriteLog("服务:【" + srvName + "】关闭了!");
        }
        /// <summary>
        /// 继续服务时
        /// </summary>
        public void Continue()
        {
            TaskHelper.Sche();
            //服务继续
            WriteLog("服务:【" + srvName + "】继续了!");
        }
        /// <summary>
        /// 暂停服务
        /// </summary>
        public void Pause()
        {
            //服务暂停
            WriteLog("服务:【" + srvName + "】暂停了!");
        }

        /// <summary>
        /// 加载任务
        /// </summary>
        private void LoadJob()
        {
            TaskHelper.Sche();
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(string msg)
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory.Trim('\\').Trim('/') + "\\log";

            lock (typeof(CoreService))
            {
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                path += "\\SrvManage" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                System.IO.File.AppendAllText(path, "【日志信息: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "】" + msg + "\r\n");
            }
        }
    }
}
