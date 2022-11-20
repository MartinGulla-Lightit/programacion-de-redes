using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogsServidor.Data
{
    public class LogsDataAccess
    {
        private List<Logs> logs;
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
            logs = new List<Logs>();
            padlock = new object();
        }

        public void AddLog(Logs log) {
            lock (padlock) 
            {
                logs.Add(log);
            }
        }

        public Logs[] GetLogs() {
            lock (padlock) { 
            return logs.ToArray();
            }
        }

    }
}
