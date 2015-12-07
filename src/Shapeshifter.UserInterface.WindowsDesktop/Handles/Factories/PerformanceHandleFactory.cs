﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories
{
    using System.Runtime.CompilerServices;

    using Handles.Interfaces;

    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    class PerformanceHandleFactory: IPerformanceHandleFactory
    {
        readonly ILogger logger;

        public PerformanceHandleFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public IPerformanceHandle StartMeasuringPerformance(
            [CallerMemberName] string methodName = "")
        {
            return new PerformanceHandle(logger, methodName);
        }
    }
}