using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeImuProtocol.Utility
{
    public class FunctionSequenceManager
    {
        private Task _task;
        private static FunctionSequenceManager _instance;
        private Dictionary<string, EventHandler> _functionQueue = new Dictionary<string, EventHandler>();
        private static int _packetsAllowedPerSecond = 1000;
        public static FunctionSequenceManager Instance { get => _instance; set => _instance = value; }
        public static int PacketsAllowedPerSecond { get => _packetsAllowedPerSecond; set => _packetsAllowedPerSecond = value; }

        public FunctionSequenceManager()
        {
            _instance = this;
            _task = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        lock (_functionQueue)
                        {
                            if (_functionQueue.Count > 0)
                            {
                                for (int i = 0; i < _functionQueue.Keys.Count; i++)
                                {
                                    string key = _functionQueue.Keys.ElementAt(i);
                                    if (key != null)
                                    {
                                        if (_functionQueue[key] != null)
                                        {
                                            _functionQueue[key].Invoke(this, EventArgs.Empty);
                                            _functionQueue[key] = null;
                                            if (PacketsAllowedPerSecond > 0)
                                            {
                                                Thread.Sleep(1000 / _packetsAllowedPerSecond);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Thread.Sleep(100);
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
