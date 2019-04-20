using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace EnumParseVsToString
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TestRunner>();
        }
    }

    [ClrJob, MonoJob, CoreJob] // 可以針對不同的 CLR 進行測試
    [MinColumn, MaxColumn]
    [MemoryDiagnoser]
    public class TestRunner
    {
        private readonly TestClass _test = new TestClass();

        public TestRunner()
        {
        }

        [Benchmark]
        public void TestMethod1() => _test.ReplaceSwitchDispatch1(AlterType.GPU);

        [Benchmark]
        public void TestMethod2() => _test.ReplaceSwitchDispatch2(AlterType.GPU);
    }

    public class TestClass
    {
        private static IDictionary<AlterType, Action> mapping = null;
        public void ReplaceSwitchDispatch1(AlterType type)
        {
            if (mapping == null)
            {
                mapping = typeof(AlterType).GetEnumValues()
                                           .Cast<AlterType>()
                                           .Join(
                                                 typeof(AlterHandler).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                               , alterType => alterType
                                               , alterHandlerMethods => Enum.Parse(typeof(AlterType), alterHandlerMethods.Name.Replace("Alter", ""))
                                               , (t, e) => new
                                                           {
                                                               Type = t
                                                              ,
                                                               EventHandler = e
                                                           })
                                           .ToDictionary(a => a.Type
                                                       , a => new Action(() => a.EventHandler.Invoke(null, null)));
            }
            mapping[type].Invoke();
        }

        public void ReplaceSwitchDispatch2(AlterType type)
        {
            if (mapping == null)
            {
                mapping = typeof(AlterType).GetEnumValues()
                                           .Cast<AlterType>()
                                           .Join(
                                                 typeof(AlterHandler).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                               , alterType => alterType.ToString()
                                               , alterHandlerMethods => alterHandlerMethods.Name.Replace("Alter", "")
                                               , (t, e) => new
                                                           {
                                                               Type = t
                                                              ,
                                                               EventHandler = e
                                                           })
                                           .ToDictionary(a => a.Type
                                                       , a => new Action(() => a.EventHandler.Invoke(null, null)));
            }
            mapping[type].Invoke();
        }
    }

    public enum AlterType
    {
        Disk,
        Network,
        CPU,
        GPU
    }

    public static class AlterHandler
    {
        public static void AlterDisk()
        {
            // Console.WriteLine("Disk Alter");
        }

        public static void AlterNetwork()
        {
            // Console.WriteLine("Network Alter");
        }

        public static void AlterCPU()
        {
            // Console.WriteLine("CPU Alter");
        }

        public static void AlterGPU()
        {
            // Console.WriteLine("GPU Alter");
        }
    }
}
