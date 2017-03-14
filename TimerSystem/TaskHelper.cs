using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimerSystem
{
    public class TaskHelper
    {
        /// <summary>
        /// 获取所有的任务
        /// </summary>
        public static List<Task_t> Tasks
        {
            get
            {
                TaskEntities db = new TaskEntities();
                return db.Task_t.ToList();
            }
        }
        public static IScheduler sche = null;
        /// <summary>
        /// 任务构造函数
        /// </summary>
        static TaskHelper()
        {
            sche = new StdSchedulerFactory().GetScheduler();
            sche.Start();
        }

        /// <summary>
        /// 再次加载任务
        /// </summary>
        internal static void Sche()
        {
            sche.Clear();
            Tasks.ForEach(i =>
            {
                if (i.Enable == 1)
                {
                    JobDataMap map = new JobDataMap();
                    map.Add("task", i);
                    IJobDetail job = JobBuilder.Create<FullTask>()
                        .UsingJobData(map)
                        .WithIdentity(i.Name, i.Name)
                        .Build();

                    ICronTrigger tri = (ICronTrigger)TriggerBuilder.Create()
                        .StartNow()
                        .WithIdentity("tri_" + i.Name, "tri_" + i.Name)
                        .WithCronSchedule(i.Cron)
                        .Build();
                    sche.ScheduleJob(job, tri);
                }
            });
        }
    }
}
