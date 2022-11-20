using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogsServidor.Data
{
    public class LogsDataAccess
    {
        private List<Log> logs;
        private object padlock;
        private static LogsDataAccess instance;

        private static object singletonPadlock = new object();
        public static LogsDataAccess GetInstance() {

            lock (singletonPadlock) { // bloqueante 
                if (instance == null) {
                    instance = new LogsDataAccess();
                }
            }
            return instance;
        }

        private LogsDataAccess() {
            logs = new List<Log>();
            padlock = new object();
        }

        public void AddLog(Log log) {
            lock (padlock) 
            {
                logs.Add(log);
            }
        }

        public Log[] GetLogs() {
            lock (padlock) { 
                return logs.ToArray();
            }
        }

    }
}
