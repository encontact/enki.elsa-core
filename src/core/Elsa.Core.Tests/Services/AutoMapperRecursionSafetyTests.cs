using System;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Core.Tests.Services
{
    /// <summary>
    /// Regression coverage for GHSA-rvv3-g6hj-g44x / CVE-2026-32933: AutoMapper &lt; 15.1.1 crashed the
    /// whole process with an uncatchable StackOverflowException when mapping a deeply self-referential
    /// object graph. The fixed engine (16.2.0 here) bounds recursion instead of crashing.
    /// </summary>
    public class AutoMapperRecursionSafetyTests
    {
        private class CircularSource
        {
            public CircularSource? Self { get; set; }
        }

        private class CircularDestination
        {
            public CircularDestination? Self { get; set; }
        }

        [Fact]
        public void Map_DeeplySelfReferentialGraph_DoesNotCrashProcess()
        {
            var configuration = new MapperConfiguration(cfg => cfg.CreateMap<CircularSource, CircularDestination>(), NullLoggerFactory.Instance);
            var mapper = configuration.CreateMapper();

            var root = new CircularSource();
            var current = root;
            for (var i = 0; i < 30_000; i++)
            {
                current.Self = new CircularSource();
                current = current.Self;
            }

            // The call completing at all (whether it returns a bounded result or throws a normal,
            // catchable exception) demonstrates the recursion is no longer unbounded.
            var exception = Record.Exception(() => mapper.Map<CircularDestination>(root));

            Assert.True(exception is null || exception is not StackOverflowException);
        }
    }
}
