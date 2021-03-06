﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ContextRunner.Logging;
using ContextRunner.State;
using System.Collections.Generic;

namespace ContextRunner.Base
{
    public delegate void ContextLoadedHandler(ActionContext context);
    public delegate void ContextUnloadedHandler(ActionContext context);

    public class ActionContext : IDisposable
    {
        private static readonly AsyncLocal<ActionContext> _current = new AsyncLocal<ActionContext>();

        public static event ContextLoadedHandler Loaded;
        public static event ContextUnloadedHandler Unloaded;

        private readonly Stopwatch _stopwatch;
        private readonly ActionContext _parent;

        public ActionContext([CallerMemberName]string name = null, IEnumerable<ISanitizer> logSanitizers = null)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _parent = _current.Value;
            _current.Value = this;

            ContextName = name;
            
            if(IsRoot)
            {
                Depth = 0;
                State = new ContextState(logSanitizers);
                Logger = new ContextLogger(this);
            }
            else
            {
                Depth = _parent.Depth + 1;
                State = _parent.State;

                Logger = _parent.Logger;
                Logger.TrySetContext(this);
            }

            Loaded?.Invoke(this);
        }

        public ContextLogger Logger { get; }
        public ContextState State { get; }

        public int Depth { get; }
        public string ContextName { get; }

        public bool IsRoot {
            get => _parent == null;
        }

        public TimeSpan TimeElapsed
        {
            get => _stopwatch.Elapsed;
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            Logger.Trace($"Context {ContextName} has ended.");

            _current.Value = _parent;

            Logger.CompleteIfRoot();
            Logger.TrySetContext(_parent);

            Unloaded?.Invoke(this);
        }
    }
}
