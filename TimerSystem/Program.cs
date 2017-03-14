using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using static System.Net.Mime.MediaTypeNames;

namespace TimerSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Server is opened");
            HostFactory.Run(x =>                                 //1
            {
                x.Service<CoreService>(s =>                        //2
                {
                    s.ConstructUsing(name => new CoreService("timetorun", "timetorun"));     //3
                    s.WhenStarted(tc => tc.Start());              //4
                    s.WhenStopped(tc => tc.Stop());               //5
                });
                x.RunAsLocalSystem();                            //6

                x.SetDescription("Task System"+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());        //7
                x.SetDisplayName("EasyRun");                       //8
                x.SetServiceName("EasyRun");                       //9
            });
        }
    }
}
