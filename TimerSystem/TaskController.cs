using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace TimerSystem
{
    /// <summary>
    /// webapi控制器
    /// </summary>
    public class TaskController : ApiController
    {
        [HttpGet]
        public Task_t[] All()
        {
            var jobs = TaskHelper.Tasks.ToArray();
            for (int i = 0; i < jobs.Length; i++)
            {

                //获取上一次和下一次执行时间以及任务状态
                JobKey key = new JobKey(jobs[i].Name, jobs[i].Name);
                IJobDetail detail = TaskHelper.sche.GetJobDetail(key);
                IList<ITrigger> triggers = TaskHelper.sche.GetTriggersOfJob(key);
                if (triggers.Count > 0)
                {
                    ICronTrigger croTri = triggers[0] as ICronTrigger;
                    DateTimeOffset? dtoff = croTri.GetPreviousFireTimeUtc();
                    if (dtoff != null && dtoff.ToString() != "")
                    {
                        jobs[i].LastDate = TimeZone.CurrentTimeZone.ToLocalTime(Convert.ToDateTime(dtoff.ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        jobs[i].LastDate = "";
                    }
                    dtoff = croTri.GetNextFireTimeUtc();
                    if (dtoff != null && dtoff.ToString() != "")
                    {
                        jobs[i].NextDate = TimeZone.CurrentTimeZone.ToLocalTime(Convert.ToDateTime(dtoff.ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        jobs[i].NextDate = "";
                    }
                    TriggerState state = TaskHelper.sche.GetTriggerState(croTri.Key);

                    if (state == TriggerState.Complete)
                    {
                        jobs[i].State = "完成";
                    }
                    else if (state == TriggerState.Error)
                    {
                        jobs[i].State = "出错";
                    }
                    else if (state == TriggerState.None)
                    {
                        jobs[i].State = "无";
                    }
                    else if (state == TriggerState.Normal)
                    {
                        jobs[i].State = "正常";
                    }
                    else if (state == TriggerState.Paused)
                    {
                        jobs[i].State = "暂停";
                    }
                    else if (state == TriggerState.Blocked)
                    {
                        jobs[i].State = "锁定";
                    }
                }
            }

            return jobs;
        }
        [HttpGet]
        public bool refresh() {
            try
            {
                TaskHelper.Sche();
                return true;
            }
            catch {
                return false;
            }
        }
    }
}
