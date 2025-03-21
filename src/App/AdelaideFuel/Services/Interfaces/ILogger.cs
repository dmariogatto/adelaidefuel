﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface ILogger
    {
        void Error(Exception ex, IReadOnlyDictionary<string, string> data = null);
        void Event(string eventName, IReadOnlyDictionary<string, string> properties = null);

        bool ShouldLogException(Exception ex);

        string LogFilePath();
        long LogInBytes();
        Task<string> GetLogAsync();
        void DeleteLog();
    }
}