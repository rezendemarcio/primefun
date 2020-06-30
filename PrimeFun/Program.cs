using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PrimeFun
{
    /*
     * Implementação do código obtida do blog EximiaCo.Tech, em:
     * https://www.eximiaco.tech/en/2019/06/10/lets-have-fun-with-prime-numbers-threads-thread-pool-tpl-and-cuda/
     * 
     * Crédito: Elemar Júnior
     */

    class Program
    {
        static long start;
        static long end;

        static void Main(string[] args)
        {
            start = 200;
            end = 800_000;

            //Test_1();
            Test_2();
            Test_3();
            Test_4();
            Test_5();
            Test_6();
            Console.ReadKey();
        }

        static void Test_1()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_1(start, end);
            sw.Stop();
            Console.WriteLine($"General: {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        static void Test_2()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_2(start, end);
            sw.Stop();
            Console.WriteLine($"Threads (lock): {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        static void Test_3()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_3(start, end);
            sw.Stop();
            Console.WriteLine($"Thread (no lock): {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        static void Test_4()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_4(start, end);
            sw.Stop();
            Console.WriteLine($"Threads (interlocked): {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        static void Test_5()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_5(start, end);
            sw.Stop();
            Console.WriteLine($"TrheadPool: {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        static void Test_6()
        {
            var sw = Stopwatch.StartNew();
            var result = PrimesInRange_6(start, end);
            sw.Stop();
            Console.WriteLine($"Task Parallel Library: {result} prime numbers found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors).");
        }

        public static long PrimesInRange_1(long start, long end)
        {
            long result = 0;
            for (var number = start; number < end; number++)
            {
                if (IsPrime(number))
                {
                    result++;
                }
            }
            return result;
        }

        public static long PrimesInRange_2(long start, long end)
        {
            long result = 0;
            var lockObject = new object();

            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;

            var threads = new Thread[numberOfThreads];
            var chunkSize = range / numberOfThreads;

            for (long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = (i == (numberOfThreads - 1)) ? end : chunkStart + chunkSize;
                threads[i] = new Thread(() =>
                {
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                        {
                            lock (lockObject)
                            {
                                result++;
                            }
                        }
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return result;
        }

        public static long PrimesInRange_3(long start, long end)
        {
            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;

            var threads = new Thread[numberOfThreads];
            var results = new long[numberOfThreads];

            var chunkSize = range / numberOfThreads;

            for (long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = i == (numberOfThreads - 1) ? end : chunkStart + chunkSize;
                var current = i;

                threads[i] = new Thread(() =>
                {
                    results[current] = 0;
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                        {
                            results[current]++;
                        }
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return results.Sum();
        }

        public static long PrimesInRange_4(long start, long end)
        {
            long result = 0;
            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;

            var threads = new Thread[numberOfThreads];

            var chunkSize = range / numberOfThreads;

            for (long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = i == (numberOfThreads - 1) ? end : chunkStart + chunkSize;
                threads[i] = new Thread(() =>
                {
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                        {
                            Interlocked.Increment(ref result);
                        }
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return result;
        }

        public static long PrimesInRange_5(long start, long end)
        {
            long result = 0;
            const long chunkSize = 100;
            var completed = 0;
            var allDone = new ManualResetEvent(initialState: false);

            var chunks = (end - start) / chunkSize;

            for (long i = 0; i < chunks; i++)
            {
                var chunkStart = (start) + i * chunkSize;
                var chunkEnd = i == (chunks - 1) ? end : chunkStart + chunkSize;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    for (var number = chunkStart; number < chunkEnd; number++)
                    {
                        if (IsPrime(number))
                        {
                            Interlocked.Increment(ref result);
                        }
                    }

                    if (Interlocked.Increment(ref completed) == chunks)
                    {
                        allDone.Set();
                    }
                });

            }
            allDone.WaitOne();
            return result;
        }

        public static long PrimesInRange_6(long start, long end)
        {
            long result = 0;
            Parallel.For(start, end, number =>
            {
                if (IsPrime(number))
                {
                    Interlocked.Increment(ref result);
                }
            });
            return result;
        }

        static bool IsPrime(long number)
        {
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            for (long divisor = 3; divisor < (number / 2); divisor += 2)
            {
                if (number % divisor == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}