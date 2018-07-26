using System;
using System.Threading;
using CSRedis;
using ClassLibraryCommon;

namespace NonVisuals
{
    public static class RedisManager
    {
        public static string Host = "";
        public static int Port = -1;
        public static string Password = "";
        public static string RedisKey = "";
        private static RedisClient _client;
        private static RedisPollingClass _redisPollingClass;

        public static void StartPolling()
        {
            GetRedisClient();
            if (_redisPollingClass == null)
            {
                _redisPollingClass = new RedisPollingClass();
                _redisPollingClass.StartUp();
            }
            else
            {
                _redisPollingClass.StartUp();
            }
        }

        public static void SendRedisData(string key, string value)
        {
            try
            {
                _redisPollingClass.SetKey(key, value);
            }
            catch (Exception e)
            {
                Common.LogError(0, e, "SendRedisData Key ={" + key + "} Value={" + value + "}");
            }
        }

        public static RedisClient GetRedisClient()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new Exception("Cannot create Redis Client without Host information");
            }
            if (Port <= 0)
            {
                throw new Exception("Cannot create Redis Client without Port information");
            }

            if (_client == null)
            {
                _client = new RedisClient(Host, Port);
            }

            if (!string.IsNullOrWhiteSpace(Password))
            {
                _client.Auth(Password);
            }
            return _client;
        }

        public static void CloseRedisClient()
        {
            _client = null;
            //_client?.ClientKill();
            //_client?.Shutdown();
        }

        public static void AddRedisDataListener(IRedisDataListener iRedisDataListener)
        {
            if (_redisPollingClass != null)
            {
                _redisPollingClass.AddRedisDataListener(iRedisDataListener);
            }
            else
            {
                Common.LogError(123, "Cannot register RedisDataListener, polling class is null. Check that Redis server is running");
            }
        }

        public static void RemoveRedisDataListener(IRedisDataListener iRedisDataListener)
        {
            if (_redisPollingClass != null)
            {
                _redisPollingClass.RemoveRedisDataListener(iRedisDataListener);
            }
            else
            {
                Common.LogError(123, "Cannot deregister RedisDataListener, polling class is null. Check that Redis server is running");
            }
        }

    }

    class RedisPollingClass
    {
        public delegate void RedisDataListenerEventHandler(object sender, RedisDataListenerEventArgs e);
        public event RedisDataListenerEventHandler OnRedisDataAvailable;

        private bool _closed = false;
        private Thread _pollingThread = null;
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        //0 - 40000
        private int _altLCDArmAValue = 0;
        //-6000 - 6000
        private float _vsLCDArmAValue = 0;
        //private float _lastAltitude = 0;
        //private long _lastAltitudeTime = DateTime.Now.Ticks;
        //0-600
        private int _iasLCDArmAValue = 0;
        //0-360
        private int _hdgLCDArmAValue = 0;
        //0-360
        private int _crsLCDArmAValue = 0;

        private int _gameTime = 0;
        /*
            relDir (relative direction to Airport) // [21]
            navDir (direction of airport) // [22]
            relDirA (relative direction of aircraft to Airport) // [23]
            relDis (relative Distance to Airport) // [24]
            Airport Glide Slope [25]
        */
        //0-360
        private int _bearingToAirPortLCDArmAValue = 0; // 21
        private int _dirAirportStripToAirplaneLCDArmAValue = 0; // 22
        private int _bearingAirportToAircraftLCDArmAValue = 0;  // 23
        //0-?
        private int _distanceToAirportLCDArmAValue = 0;  //24

        private string _currentAirport = "";

        private readonly object _clientLockObject = new object();


        public void SetKey(string key, string value)
        {
            lock (_clientLockObject)
            {
                var client = RedisManager.GetRedisClient();
                client.Set(key, value);
            }
        }

        private void ThreadedRedisPollingMethod()
        {
            try
            {
                while (!_closed)
                {
                    lock (_clientLockObject)
                    {
                        var client = RedisManager.GetRedisClient();
                        var data = client.Get(RedisManager.RedisKey);
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            data = data.Substring(1, data.Length - 2); //cut start and aft get rid of []
                            var result = data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            _altLCDArmAValue = (int)(float.Parse(result[6]) / 0.3048f);
                            _vsLCDArmAValue = (int)(float.Parse(result[15]) / 0.00508f / 1000);

                            _iasLCDArmAValue = (int)(float.Parse(result[2]));
                            _hdgLCDArmAValue = (int)(float.Parse(result[7]));
                            _gameTime = int.Parse(result[9].Replace(":","").Replace("\"",""));
                            _bearingToAirPortLCDArmAValue = (int)(float.Parse(result[21]));
                            _dirAirportStripToAirplaneLCDArmAValue = (int)(float.Parse(result[22]));
                            _bearingAirportToAircraftLCDArmAValue = (int)(float.Parse(result[23]));
                            _distanceToAirportLCDArmAValue = (int)(float.Parse(result[24]));
                        }
                        else
                        {
                            _altLCDArmAValue = 0;
                            _vsLCDArmAValue = 0;
                            _iasLCDArmAValue = 0;
                            _hdgLCDArmAValue = 0;
                            _bearingToAirPortLCDArmAValue = 0;
                            _dirAirportStripToAirplaneLCDArmAValue = 0;
                            _bearingAirportToAircraftLCDArmAValue = 0;
                            _distanceToAirportLCDArmAValue = 0;
                        }

                        data = client.Get("DCS_CURRENTAIRPORT");
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            var result = data.Split(new char[] { ',' }, StringSplitOptions.None);
                            _currentAirport = result[0];
                        }
                        else
                        {
                            _currentAirport = "";
                        }

                        data = client.Get("DCS_Compass");
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            _crsLCDArmAValue = (int)(float.Parse(data));
                        }
                        else
                        {
                            _crsLCDArmAValue = 0;
                        }
                        SendData();
                    }
                    _autoResetEvent.WaitOne(200);
                }
            }
            catch (Exception e)
            {
                Common.LogError(333, e, "Error in RedisPollingClass.ThreadedRedisPollingMethod()");
            }
        }

        public bool IsRunning()
        {
            return _pollingThread != null && _pollingThread.IsAlive;
        }

        public bool StartUp()
        {
            try
            {
                _closed = true;
                _autoResetEvent.Set();
                Thread.Sleep(200);
                _closed = false;
                _pollingThread = new Thread(ThreadedRedisPollingMethod);
                _pollingThread.Start();
            }
            catch (Exception e)
            {
                Common.LogError(334, e, "Error in RedisPollingClass.StartUp()");
            }
            return true;
        }

        public void Shutdown()
        {
            _closed = true;
            _autoResetEvent.Set();
            Thread.Sleep(200);
        }

        public void AddRedisDataListener(IRedisDataListener iRedisDataListener)
        {
            OnRedisDataAvailable += iRedisDataListener.RedisDataAvailable;
        }

        public void RemoveRedisDataListener(IRedisDataListener iRedisDataListener)
        {
            OnRedisDataAvailable -= iRedisDataListener.RedisDataAvailable;
        }

        private void SendData()
        {
            OnRedisDataAvailable?.Invoke(this, new RedisDataListenerEventArgs()
            {
                Altitude = _altLCDArmAValue,
                VerticalSpeed = (int)_vsLCDArmAValue,
                IndicatedAirspeed = _iasLCDArmAValue,
                Heading = _hdgLCDArmAValue,
                Course = _crsLCDArmAValue,
                RelativeDirectionAircraftToAirport = _bearingAirportToAircraftLCDArmAValue,
                RelativeDirectionToAirport = _bearingToAirPortLCDArmAValue,
                DistanceToAirport = _distanceToAirportLCDArmAValue,
                TrueDirectionToAirport = _dirAirportStripToAirplaneLCDArmAValue,
                CurrentAirport = _currentAirport,
                GameTime = _gameTime
            });
        }
    }

    public class RedisDataListenerEventArgs : EventArgs
    {
        public int Altitude { get; set; }
        public int VerticalSpeed { get; set; }
        public int Heading { get; set; }
        public int Course { get; set; }
        public int IndicatedAirspeed { get; set; }
        public int RelativeDirectionToAirport { get; set; }
        public int TrueDirectionToAirport { get; set; }
        public int RelativeDirectionAircraftToAirport { get; set; }
        public int DistanceToAirport { get; set; }
        public string CurrentAirport { get; set; }
        public int GameTime { get; set; }
    }

    public interface IRedisDataListener
    {
        void RedisDataAvailable(object sender, RedisDataListenerEventArgs e);
    }
}
