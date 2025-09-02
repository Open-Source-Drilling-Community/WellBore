using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NORCE.Drilling.WellBore.Model
{
    public struct CountPerDay
    {
        public DateTime Date { get; set; }
        public ulong Count { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public CountPerDay() { }
        /// <summary>
        /// initialization constructor
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        public CountPerDay(DateTime date, ulong count)
        {
            Date = date;
            Count = count;
        }
    }

    public class History
    {
        public List<CountPerDay> Data { get; set; } = new List<CountPerDay>();
        /// <summary>
        /// default constructor
        /// </summary>
        public History()
        {
            if (Data == null)
            {
                Data = new List<CountPerDay>();
            }
        }

        public void Increment()
        {
            if (Data.Count == 0)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else
            {
                if (Data[Data.Count - 1].Date < DateTime.UtcNow.Date)
                {
                    Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
                }
                else
                {
                    Data[Data.Count - 1] = new CountPerDay(Data[Data.Count - 1].Date, Data[Data.Count - 1].Count + 1);
                }
            }
        }
    }
    public class UsageStatisticsWellBore
    {
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;

        public DateTime LastSaved { get; set; } = DateTime.MinValue;
        public TimeSpan BackUpInterval { get; set; } = TimeSpan.FromMinutes(5);

        public History GetAllWellBoreIdPerDay { get; set; } = new History();
        public History GetAllWellBoreMetaInfoPerDay { get; set; } = new History();
        public History GetWellBoreByIdPerDay { get; set; } = new History();
        public History GetAllWellBorePerDay { get; set; } = new History();
        public History PostWellBorePerDay { get; set; } = new History();
        public History PutWellBoreByIdPerDay { get; set; } = new History();
        public History DeleteWellBoreByIdPerDay { get; set; } = new History();

        private static object lock_ = new object();

        private static UsageStatisticsWellBore? instance_ = null;

        public static UsageStatisticsWellBore Instance
        {
            get
            {
                if (instance_ == null)
                {
                    if (File.Exists(HOME_DIRECTORY + "history.json"))
                    {
                        try
                        {
                            string? jsonStr = null;
                            lock (lock_)
                            {
                                using (StreamReader reader = new StreamReader(HOME_DIRECTORY + "history.json"))
                                {
                                    jsonStr = reader.ReadToEnd();
                                }
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    instance_ = JsonSerializer.Deserialize<UsageStatisticsWellBore>(jsonStr);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsWellBore();
                    }
                }
                return instance_;
            }
        }

        public void IncrementGetAllWellBoreIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllWellBoreIdPerDay == null)
                {
                    GetAllWellBoreIdPerDay = new History();
                }
                GetAllWellBoreIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllWellBoreMetaInfoPerDay()
        {
            lock (lock_)
            {
                if (GetAllWellBoreMetaInfoPerDay == null)
                {
                    GetAllWellBoreMetaInfoPerDay = new History();
                }
                GetAllWellBoreMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetWellBoreByIdPerDay()
        {
            lock (lock_)
            {
                if (GetWellBoreByIdPerDay == null)
                {
                    GetWellBoreByIdPerDay = new History();
                }
                GetWellBoreByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPostWellBorePerDay()
        {
            lock (lock_)
            {
                if (PostWellBorePerDay == null)
                {
                    PostWellBorePerDay = new History();
                }
                PostWellBorePerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllWellBorePerDay()
        {
            lock (lock_)
            {
                if (GetAllWellBorePerDay == null)
                {
                    GetAllWellBorePerDay = new History();
                }
                GetAllWellBorePerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPutWellBoreByIdPerDay()
        {
            lock (lock_)
            {
                if (PutWellBoreByIdPerDay == null)
                {
                    PutWellBoreByIdPerDay = new History();
                }
                PutWellBoreByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementDeleteWellBoreByIdPerDay()
        {
            lock (lock_)
            {
                if (DeleteWellBoreByIdPerDay == null)
                {
                    DeleteWellBoreByIdPerDay = new History();
                }
                DeleteWellBoreByIdPerDay.Increment();
                ManageBackup();
            }
        }

        private void ManageBackup()
        {
            if (DateTime.UtcNow > LastSaved + BackUpInterval)
            {
                LastSaved = DateTime.UtcNow;
                try
                {
                    string jsonStr = JsonSerializer.Serialize(this);
                    if (!string.IsNullOrEmpty(jsonStr) && Directory.Exists(HOME_DIRECTORY))
                    {
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
