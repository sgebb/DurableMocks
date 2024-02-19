using FakeItEasy;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DurableMocks
{
    public static class DurableMocksExtensions
    {
        public static TaskOrchestrationContext CreateDurableMock(this IServiceProvider serviceProvider, Assembly assembly)
        {
            var orchestrationContext = A.Fake<TaskOrchestrationContext>();
            var taskActivityContext = A.Fake<TaskActivityContext>();
            A.CallTo(() => taskActivityContext.InstanceId).Returns(Guid.NewGuid().ToString());
            A.CallTo(() => taskActivityContext.Name).Returns("DurableMocks");

            foreach (Type type in assembly.GetTypes())
            {
                serviceProvider.SetupFakeCallForType(orchestrationContext, type, typeof(TaskOrchestrator<,>), "CallSubOrchestratorAsync", orchestrationContext);
                serviceProvider.SetupFakeCallForType(orchestrationContext, type, typeof(TaskActivity<,>), "CallActivityAsync", taskActivityContext);
            }
            return orchestrationContext;
        }

        private static void SetupFakeCallForType(this IServiceProvider serviceProvider, TaskOrchestrationContext mockedTaskOrchestrationContext, Type type, Type parentType, string methodName, object context)
        {
            if (type.IsClass && !type.IsAbstract && type.IsSubClassOfGeneric(parentType))
            {
                var genericArguments = type.BaseType!.GetGenericArguments();
                var inputType = genericArguments[0];
                var outputType = genericArguments[1];

                A.CallTo(mockedTaskOrchestrationContext)
                    .Where(call => call.Method.Name == methodName)
                    .Where(call => call.Arguments[0]!.ToString() == type.Name)
                    .WithNonVoidReturnType()
                    .ReturnsLazily((TaskName _, object input, TaskOptions _) =>
                    {
                        var instance = ActivatorUtilities.CreateInstance(serviceProvider, type);
                        var runAsyncMethodInfo = instance?.GetType().GetMethod("RunAsync");

                        return (Task)runAsyncMethodInfo?.Invoke(instance, new[] { context, input })!;
                    });
            }
        }

        private static bool IsSubClassOfGeneric(this Type child, Type parent)
        {
            if (child == parent)
            {
                return false;
            }

            if (child.IsSubclassOf(parent))
            {
                return true;
            }

            var parameters = parent.GetGenericArguments();
            var isParameterLessGeneric = !(parameters != null && parameters.Length > 0 &&
                ((parameters[0].Attributes & TypeAttributes.BeforeFieldInit) == TypeAttributes.BeforeFieldInit));

            while (child != null && child != typeof(object))
            {
                var cur = GetFullTypeDefinition(child);
                if (parent == cur || (isParameterLessGeneric && cur.GetInterfaces().Select(i => GetFullTypeDefinition(i)).Contains(GetFullTypeDefinition(parent))))
                {
                    return true;
                }
                else if (!isParameterLessGeneric)
                {
                    if (GetFullTypeDefinition(parent) == cur && !cur.IsInterface)
                    {
                        if (VerifyGenericArguments(GetFullTypeDefinition(parent), cur))
                        {
                            if (VerifyGenericArguments(parent, child))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in child.GetInterfaces().Where(i => GetFullTypeDefinition(parent) == GetFullTypeDefinition(i)))
                        {
                            if (VerifyGenericArguments(parent, item))
                            {
                                return true;
                            }
                        }
                    }
                }

                child = child.BaseType;
            }

            return false;
        }

        private static Type GetFullTypeDefinition(Type type)
        {
            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }

        private static bool VerifyGenericArguments(Type parent, Type child)
        {
            Type[] childArguments = child.GetGenericArguments();
            Type[] parentArguments = parent.GetGenericArguments();
            if (childArguments.Length == parentArguments.Length)
            {
                for (int i = 0; i < childArguments.Length; i++)
                {
                    if (childArguments[i].Assembly != parentArguments[i].Assembly || childArguments[i].Name != parentArguments[i].Name || childArguments[i].Namespace != parentArguments[i].Namespace)
                    {
                        if (!childArguments[i].IsSubclassOf(parentArguments[i]))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

}
