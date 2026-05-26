using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LunaCompatCommon.Messages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class ModMessageCoverageTests
{
    #region Public Methods

    [Fact]
    public void Check_All_ModMessages_Tested()
    {
        var messageAssembly = typeof(IModMessage).Assembly;
        var testAssembly = Assembly.GetExecutingAssembly();

        var messageTypes = messageAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IModMessage).IsAssignableFrom(t)).ToList();
        var testedTypes = new HashSet<Type>();

        foreach (var t in testAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            var baseType = t.BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ModMessageTestBase<>))
                {
                    var arg = baseType.GetGenericArguments()[0];
                    testedTypes.Add(arg);
                    break;
                }

                baseType = baseType.BaseType;
            }
        }

        // Exclude test-only types
        var missing = messageTypes.Where(mt => !testedTypes.Contains(mt))
                                  .Where(mt => mt.Namespace == null || !mt.Namespace.StartsWith("LunaCompatCommon.Tests"))
                                  .ToList();

        if (missing.Any())
        {
            var list = string.Join("\n", missing.Select(t => t.FullName));
            Assert.Fail($"Missing mod message tests for the following message types:\n{list}");
        }

        Assert.True(true);
    }

    #endregion
}
