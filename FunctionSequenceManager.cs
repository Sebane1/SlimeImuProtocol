using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeImuProtocol
{
    public class FunctionSequenceManager
    {
        private Task _task;
        private static FunctionSequenceManager _instance;
        private Dictionary<string, EventHandler> _functionQueue = new Dictionary<string, EventHandler>();
        private int _packetsAllowedPerSecond = 1000;
        public static FunctionSequenceManager Instance { get => _instance; set => _instance = value; }

        public FunctionSequenceManager()
        {
            _instance = this;
            _task = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (_functionQueue.Count > 0)
                        {
                            for (int i = 0; i < _functionQueue.Keys.Count; i++)
                            {
                                lock (_functionQueue)
                                {
                                    string key = _functionQueue.Keys.ElementAt(i);
                                    if (_functionQueue[key] != null)
                                    {
                                        _functionQueue[key].Invoke(this, EventArgs.Empty);
                                        _functionQueue[key] = null;
                                        Thread.Sleep(1000 / _packetsAllowedPerSecond);
                                    }
                                }
                            }
                            Console.WriteLine("Cycled through " + _functionQueue.Keys.Count + " messages this cycle.");
                            _functionQueue.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
        }
        public void AddFunctionToQueue(string id, EventHandler function)
        {
            lock (_functionQueue)
            {
                _functionQueue[id] = function;
            }
        }
    }
}
